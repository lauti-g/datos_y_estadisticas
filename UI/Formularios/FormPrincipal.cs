using System.Runtime.InteropServices;
using datos_y_estadisticas.Datos;        // RepositorioAlumnos e ImportadorExcel
using datos_y_estadisticas.Logica;       // MaquinaEstadoDatos y Validaciones
using datos_y_estadisticas.Modelos;      // la clase Alumno
using datos_y_estadisticas.UI.Controles; // AutocompletadoOscuro y demás controles a medida

namespace datos_y_estadisticas.UI.Formularios
{
    // El formulario quedó como COORDINADOR: crea los módulos en el arranque,
    // los conecta entre sí y maneja lo que es puramente de la ventana (barra
    // de título propia, redimensionado, tema). La lógica pesada vive afuera:
    //
    //   - MaquinaEstadoDatos      -> en qué estado está la grilla (normal /
    //                                con cambios / búsqueda) y la compuerta
    //                                de carga. Su evento EstadoCambiado
    //                                sincroniza los botones y la columna.
    //   - ValidacionGrilla        -> validar/normalizar celdas y pintarlas.
    //   - GraficosEstadisticas    -> todo el dibujo con LiveCharts.
    //   - SelectorModoEstadistica -> los botones Semana/Mes/3 meses/Todo y
    //                                el cálculo del rango de fechas.
    //   - ControladorFechas       -> los dos DateTimePicker (paleta, cambiar
    //                                fecha sin disparar eventos, avisos SOLO
    //                                cuando el cambio vino del usuario).
    //   - ImportadorExcel         -> elegir y leer el archivo de Excel.
    //   - RepositorioAlumnos      -> la base de datos (ya existía).
    public partial class FormPrincipal : Form
    {
        // ---------- Módulos entre los que se repartió la lógica ----------
        private RepositorioAlumnos repositorio = null!;        // capa de base de datos
        private MaquinaEstadoDatos estado = null!;             // máquina de estados de la grilla
        private GraficosEstadisticas graficos = null!;         // gráficos LiveCharts
        private SelectorModoEstadistica selectorModo = null!;  // botones de modo de estadísticas
        private ControladorFechas fechas = null!;              // los dos DateTimePicker

        // Fecha que se está mostrando/editando en la grilla.
        private DateTime fechaActual = DateTime.Today;

        // Textos de la columna del botón (borrarColumna) según el modo.
        // OJO: el encabezado (HeaderText) y el texto del botón (Text) son cosas
        // distintas; por eso hay dos constantes para cada modo. Los aplica
        // SincronizarUiSegunEstado cada vez que la máquina cambia de estado.
        private const string COL_BOTON_HEADER_NORMAL = "Limpiar";    // encabezado en modo normal
        private const string COL_BOTON_TEXTO_NORMAL = "Borrar";      // texto del botón en modo normal
        private const string COL_BOTON_HEADER_BUSQUEDA = "Reinsertar"; // encabezado en modo búsqueda
        private const string COL_BOTON_TEXTO_BUSQUEDA = "Reinsertar";  // texto del botón en modo búsqueda

        // Autocompletado de localidades (versión PROPIA, estilo oscuro).
        // 'localidadesSugeridas' es la lista de localidades ya cargadas en la base
        // y es la FUENTE de sugerencias; la consume el componente
        // 'AutocompletadoOscuro'. Se llena al arrancar y se refresca tras guardar.
        private readonly List<string> localidadesSugeridas = new();

        // Componente que muestra el desplegable oscuro de sugerencias de localidad.
        // Se crea UNA sola vez (en ConfigurarTabsYEventos) y se "conecta" al cuadro
        // de edición cada vez que entramos a editar la columna Localidad.
        private AutocompletadoOscuro autoLocalidad = null!;

        public FormPrincipal()
        {
            InitializeComponent();
        }

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int IParam);

        // ============================================================
        //  ARRANQUE
        // ============================================================
        private void FormPrincipal_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;

            // Base de datos lista.
            repositorio = new RepositorioAlumnos();
            repositorio.CrearTablaSiNoExiste();

            // Migración puntual: si en la base quedaron motivos viejos "Amigos",
            // los paso al nuevo nombre "Recomendacion". (Es inofensivo dejarlo
            // siempre: si no hay "Amigos", no toca ninguna fila.)
            repositorio.RenombrarMotivo("Amigos", "Recomendacion");

            // Cargo las localidades ya existentes para el autocompletado.
            RefrescarLocalidadesSugeridas();

            // ----- Armo y conecto los módulos -----

            // La máquina de estados. Cada vez que cambia de estado, sincronizo
            // la interfaz completa en UN solo lugar (imposible olvidarse un botón).
            estado = new MaquinaEstadoDatos();
            estado.EstadoCambiado += SincronizarUiSegunEstado;

            // Los dos selectores de fecha. Me avisan SOLO cuando el cambio vino
            // del usuario (los cambios por código usan FijarFecha... y no avisan).
            fechas = new ControladorFechas(dtpFecha, dtpEstadisticas, estado);
            fechas.FechaDatosCambiada += CuandoElUsuarioCambiaLaFechaDeDatos;
            fechas.FechaEstadisticasCambiada += RefrescarEstadisticas;

            // Los botones de modo (Semana/Mes/3 meses/Todo). Se cablean solos;
            // acá solo pido que al cambiar el modo se redibujen los gráficos.
            selectorModo = new SelectorModoEstadistica(btnSemana, btnMes, btnTresMeses, btnTodo);
            selectorModo.ModoCambiado += RefrescarEstadisticas;

            // Estética oscura y eventos de la grilla.
            AplicarTema();
            ConfigurarTabsYEventos();

            // Los gráficos de la pestaña Estadísticas (crea los controles
            // LiveCharts y los mete en sus paneles).
            graficos = new GraficosEstadisticas(panelLocalidades, panelEdades, panelMotivos,
                                                lblLocalidades, lblEdades, lblMotivos);

            // Marco invisible en los bordes para poder ESTIRAR la ventana con el
            // mouse desde bordes y esquinas, como cualquier ventana de Windows.
            // (Ver ConfigurarRedimensionado y WndProc, al final del archivo.)
            ConfigurarRedimensionado();

            // Arranco mostrando el día de HOY en las dos pestañas
            // (sin disparar los eventos de los dtp: es un cambio por código).
            fechas.FijarFechaDatos(DateTime.Today);
            fechas.FijarFechaEstadisticas(DateTime.Today);

            CargarTablaDeFecha(DateTime.Today);
            RefrescarEstadisticas();
        }

        // ============================================================
        //  SINCRONIZACIÓN DE LA INTERFAZ CON LA MÁQUINA DE ESTADOS
        // ============================================================
        // ÚNICO lugar que acomoda los controles según el estado. Se ejecuta
        // automáticamente en cada cambio de estado (está suscripto al evento
        // de la máquina), así ninguna salida de la búsqueda puede olvidarse
        // de restaurar un botón.
        private void SincronizarUiSegunEstado()
        {
            bool busqueda = estado.EnBusqueda;

            // La columna del botón cambia de "Limpiar/Borrar" a "Reinsertar".
            borrarColumna.HeaderText = busqueda ? COL_BOTON_HEADER_BUSQUEDA : COL_BOTON_HEADER_NORMAL;
            borrarColumna.Text = busqueda ? COL_BOTON_TEXTO_BUSQUEDA : COL_BOTON_TEXTO_NORMAL;

            // Entre resultados de búsqueda no tiene sentido la fila vacía de "agregar".
            tabla.AllowUserToAddRows = !busqueda;

            // El botón "Buscar" se reemplaza por el de "Salir de la búsqueda".
            btnBuscar.Visible = !busqueda;
            BotonSalirModoBusqueda.Visible = busqueda;
        }

        // ============================================================
        //  PESTAÑA DATOS: carga de la grilla por fecha
        // ============================================================

        // Trae de la base los alumnos de esa fecha y los muestra en la grilla.
        private void CargarTablaDeFecha(DateTime fecha)
        {
            estado.EmpezarCarga(); // los eventos de la grilla no deben marcar "cambios sin guardar".

            tabla.Rows.Clear();
            foreach (Alumno a in repositorio.ObtenerPorFecha(fecha))
            {
                tabla.Rows.Add(a.Nombre, a.Edad, a.Celular, a.Localidad, a.MotivoIngreso);
            }

            fechaActual = fecha;

            // Etiqueta: "Hoy" si es el día de hoy, si no la fecha elegida.
            lblFecha.Text = (fecha.Date == DateTime.Today) ? "Hoy" : fecha.ToString("dd/MM/yyyy");

            estado.TerminarCarga();
            estado.TablaCargada(); // estado Normal: lo recién cargado no son cambios pendientes.
        }

        // El usuario cambió la fecha de la grilla (aviso del ControladorFechas;
        // los cambios por código no llegan hasta acá).
        private void CuandoElUsuarioCambiaLaFechaDeDatos(DateTime nueva)
        {
            // Si venía mostrando resultados de búsqueda, cambiar la fecha significa
            // "quiero volver a la edición normal de ese día": salgo del modo búsqueda
            // y cargo la fecha elegida.
            if (estado.EnBusqueda)
            {
                SalirModoBusqueda();
                CargarTablaDeFecha(nueva);
                return;
            }

            if (nueva == fechaActual) return;

            // Si hay cambios sin guardar, aviso antes de cambiar de día (se perderían).
            if (estado.HayCambiosSinGuardar)
            {
                var r = MessageBox.Show(
                    "Tenés cambios sin guardar en esta fecha.\n¿Descartarlos y cambiar de día?",
                    "Cambios sin guardar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (r == DialogResult.No)
                {
                    // Vuelvo el selector a la fecha anterior sin volver a disparar el evento.
                    fechas.FijarFechaDatos(fechaActual);
                    return;
                }
            }

            CargarTablaDeFecha(nueva);
        }

        // ============================================================
        //  PESTAÑA DATOS: guardar
        // ============================================================
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // 1) Normalizo: localidad capitalizada y celular al formato "(11 XXXX-XXXX)".
            ValidacionGrilla.NormalizarGrilla(tabla);

            // 2) Valido todas las filas y marco en rojo lo que esté mal.
            List<string> errores = ValidacionGrilla.ValidarTodo(tabla);

            // 3) Si hay datos corruptos, aviso cuáles son y cómo arreglarlos, y NO guardo.
            if (errores.Count > 0)
            {
                MessageBox.Show(
                    "No se puede guardar: hay datos mal cargados (resaltados en rojo).\n\n" +
                    string.Join("\n", errores),
                    "Datos corruptos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 4) Todo válido: guardo. El CÓMO depende del modo.

            if (estado.EnBusqueda)
            {
                // MODO BÚSQUEDA: los resultados son alumnos que YA existen en la base,
                // cada uno con su Id y su fecha original (vienen escondidos en el Tag
                // de cada fila). Se actualizan con un UPDATE por Id, así cada dato
                // queda guardado EN LA FECHA DE ESE ALUMNO (el UPDATE no toca la
                // columna Fecha): no hace falta reinsertar para editar.
                repositorio.ActualizarAlumnos(LeerAlumnosDeBusqueda());

                // Pudo haberse cargado una localidad nueva: actualizo las sugerencias.
                RefrescarLocalidadesSugeridas();

                // Guardado listo: salgo del modo búsqueda y vuelvo a la vista normal
                // de hoy. La máquina de estados restaura sola la columna del botón
                // ("Reinsertar" -> "Borrar"), el botón "Buscar" y la fila de agregar.
                buscador.Clear();
                SalirModoBusqueda();

                MessageBox.Show(
                    "Datos guardados correctamente.\nCada alumno quedó guardado en su fecha original.",
                    "Guardado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                RefrescarEstadisticas();
                return;
            }

            // MODO NORMAL: guardo SOLO la fecha seleccionada (no piso los otros días).
            repositorio.GuardarDelDia(fechaActual, LeerAlumnosDeGrilla());
            estado.Guardado(); // ya no hay cambios pendientes.

            // Pudo haberse cargado una localidad nueva: actualizo las sugerencias.
            RefrescarLocalidadesSugeridas();

            MessageBox.Show("Datos guardados correctamente.", "Guardado",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 5) Refresco las estadísticas que se estén mostrando (los datos pudieron
            //    cambiar), respetando el modo activo (día / semana / mes / 3 meses / todo).
            RefrescarEstadisticas();
        }

        // ============================================================
        //  PESTAÑA DATOS: importar desde Excel
        // ============================================================
        // Todo el trabajo con el ARCHIVO (elegirlo, leerlo, avisar errores) vive
        // en ImportadorExcel. Acá queda solo lo que es de esta pantalla: dejar la
        // vista en el día de hoy y meter las filas leídas en la grilla.
        private void btnImportar_Click(object sender, EventArgs e)
        {
            // Diálogo + lectura + carteles de error. Si devuelve null no hay nada
            // que insertar (canceló, falló o el archivo venía vacío).
            List<Alumno>? importados = ImportadorExcel.ImportarConDialogo(this);
            if (importados == null)
                return;

            // Dejo la vista prolija en el día de hoy, donde quedan los importados.
            // Si venías de una búsqueda, salgo de ese modo primero.
            if (estado.EnBusqueda) SalirModoBusqueda();

            buscador.Clear(); // limpio la barra de búsqueda por las dudas

            // Llevo el selector a hoy SIN disparar su evento, para que no se pisen
            // las cargas, y agrego las filas con la compuerta de carga cerrada.
            fechas.FijarFechaDatos(DateTime.Today);

            estado.EmpezarCarga();
            foreach (Alumno i in importados)
            {
                tabla.Rows.Add(i.Nombre, i.Edad, i.Celular, i.Localidad, i.MotivoIngreso);
            }
            estado.TerminarCarga();

            // Aviso cuántos entraron.
            MessageBox.Show(
                $"Se importaron {importados.Count} alumnos y quedaron cargados en el día de hoy.",
                "Importación lista",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Recorre la grilla en MODO BÚSQUEDA y arma la lista para actualizar en la base.
        //
        // La diferencia con LeerAlumnosDeGrilla (abajo): las filas de una búsqueda
        // llevan escondido en su Tag el Alumno ORIGINAL, con su Id y su Fecha (ver
        // EntrarModoBusqueda). Acá recupero ese objeto y le piso SOLO los campos
        // editables con lo que quedó en las celdas. Así el UPDATE del repositorio
        // sabe exactamente a qué fila de la base apuntar (por el Id) y el alumno
        // sigue guardado en SU fecha original, que no se toca.
        //
        // Si armara alumnos nuevos como hace LeerAlumnosDeGrilla, saldrían con
        // Id = 0 y el UPDATE no encontraría ninguna fila: se "guardaría" sin
        // guardar nada (justo el bug que tenía el guardado en modo búsqueda).
        private List<Alumno> LeerAlumnosDeBusqueda()
        {
            var alumnos = new List<Alumno>();
            foreach (DataGridViewRow fila in tabla.Rows)
            {
                // Solo me sirven las filas que traen su Alumno original en el Tag.
                // (En búsqueda no hay fila vacía de "agregar", pero el chequeo también
                // la saltearía si existiera: seguridad doble.)
                if (fila.Tag is not Alumno alumno) continue;

                Validaciones.EsEdadValida(ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_EDAD), out int edad);

                alumno.Nombre = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_NOMBRE);
                alumno.Edad = edad;
                alumno.Celular = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_CELULAR);
                alumno.Localidad = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_LOCALIDAD);
                alumno.MotivoIngreso = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_MOTIVO);
                // Id y Fecha quedan como estaban: son los que dicen "qué fila de la
                // base" y "en qué día vive este alumno".

                alumnos.Add(alumno);
            }
            return alumnos;
        }

        // Recorre la grilla y arma la lista de Alumno (asume que ya está todo validado).
        // Es el lector del MODO NORMAL: crea alumnos nuevos con la fecha que se está
        // editando; GuardarDelDia después borra ese día y los inserta de cero.
        private List<Alumno> LeerAlumnosDeGrilla()
        {
            var alumnos = new List<Alumno>();
            foreach (DataGridViewRow fila in tabla.Rows)
            {
                if (fila.IsNewRow) continue; // salteo la fila vacía del final.

                Validaciones.EsEdadValida(ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_EDAD), out int edad);

                alumnos.Add(new Alumno
                {
                    Nombre = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_NOMBRE),
                    Edad = edad,
                    Celular = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_CELULAR),     // ya formateado
                    Localidad = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_LOCALIDAD), // ya capitalizada
                    MotivoIngreso = ValidacionGrilla.Texto(fila, ValidacionGrilla.COL_MOTIVO),
                    Fecha = fechaActual
                });
            }
            return alumnos;
        }

        // ============================================================
        //  EVENTOS DE LA GRILLA
        // ============================================================
        private void ConfigurarTabsYEventos()
        {
            // ----- Solapas verticales (TabControlOscuro) -----
            // El dibujo de las solapas lo hace el propio control TabControlOscuro.
            // Lo ÚNICO que le decimos desde acá es QUÉ colores usa cada solapa. El
            // orden de la lista es el orden de las solapas: 0 = "Datos", 1 = "Estadisticas".
            tab.Paletas.Add(new TabControlOscuro.PaletaSolapa // solapa 0: Datos (azul/dorado)
            {
                FondoNormal = Tema.PanelDatos,        // apagada: azul de panel
                FondoSeleccionada = Tema.AzulAccent,  // prendida: azul de acento
                TextoNormal = Tema.Blanco,
                TextoSeleccionada = Tema.Dorado
            });
            tab.Paletas.Add(new TabControlOscuro.PaletaSolapa // solapa 1: Estadísticas (rojo/negro)
            {
                FondoNormal = Tema.PanelEst,          // apagada: gris muy oscuro
                FondoSeleccionada = Tema.Rojo,        // prendida: rojo principal
                TextoNormal = Tema.GrisTexto,
                TextoSeleccionada = Color.White
            });

            // La "tira" (columna izquierda de las solapas) acompaña a la pestaña
            // activa: azul noche en Datos, casi negro en Estadísticas.
            tab.SelectedIndexChanged += (s, e) =>
                tab.ColorFondoControl = (tab.SelectedIndex == 0) ? Tema.FondoDatos : Tema.FondoEst;
            tab.ColorFondoControl = Tema.FondoDatos; // arrancamos en la solapa Datos

            // Eventos de la grilla (los agrupo acá, comentados, en vez de en el diseñador).
            tabla.CellContentClick += tabla_CellContentClick;       // botón "Borrar"/"Reinsertar"
            tabla.EditingControlShowing += tabla_EditingControlShowing; // filtro numérico en Edad
            tabla.CellEndEdit += tabla_CellEndEdit;                 // normalizar al salir de la celda
            tabla.CellValueChanged += tabla_CellValueChanged;       // marcar cambios (incluye el combo)
            tabla.UserDeletedRow += (s, e) => estado.MarcarCambio(); // borró una fila con Supr
            tabla.RowsAdded += (s, e) => estado.MarcarCambio();      // agregó una fila
            tabla.DataError += (s, e) => e.ThrowException = false;  // ignoro errores internos de celda

            // La barra de búsqueda también busca con ENTER, no solo con el botón.
            // Reuso el MISMO handler del botón (btnBuscar_Click) para que las dos
            // formas de buscar hagan exactamente lo mismo: una sola lógica, dos
            // disparadores. Funciona incluso en modo búsqueda (donde el botón
            // "Buscar" está oculto): Enter con otro texto re-busca, y Enter con
            // la barra vacía sale de la búsqueda, igual que el botón.
            buscador.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    // SuppressKeyPress evita el "ding" de Windows: sin esto, el Enter
                    // sigue viajando al TextBox, que no sabe qué hacer con él y suena.
                    e.SuppressKeyPress = true;
                    btnBuscar_Click(buscador, EventArgs.Empty);
                }
            };

            // Creo el autocompletado oscuro de localidades UNA sola vez.
            // Le paso una función que, al momento de tipear, devuelve la lista actual
            // de localidades. Fuente grande porque la usa una persona mayor.
            autoLocalidad = new AutocompletadoOscuro(() => localidadesSugeridas)
            {
                FuenteLista = new Font("Segoe UI", 12.5F), // grande y legible (subilo si hace falta)
                AltoItem = 32,                             // renglones altos = fáciles de leer/tocar
                MaxVisibles = 8                            // hasta 8 opciones antes de hacer scroll
            };
        }

        // Clic en el botón de la columna. Hace una de dos cosas según el estado:
        //   - Modo normal   -> el botón dice "Borrar": saca la fila de la grilla.
        //   - Modo búsqueda -> el botón dice "Reinsertar": mueve ese alumno al día de hoy.
        private void tabla_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Me interesa SOLO la columna del botón, en una fila real (no la fila vacía del final).
            if (e.ColumnIndex != ValidacionGrilla.COL_BORRAR || e.RowIndex < 0 || tabla.Rows[e.RowIndex].IsNewRow)
                return;

            if (estado.EnBusqueda)
            {
                ReinsertarFila(e.RowIndex); // "Reinsertar"
            }
            else
            {
                tabla.Rows.RemoveAt(e.RowIndex); // "Borrar"
                estado.MarcarCambio();
            }
        }

        // Cuando empieza a editarse una celda, configuro el cuadro de texto según la columna.
        private void tabla_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                // -- Filtro numérico SOLO en la columna Edad --
                tb.KeyPress -= ValidacionGrilla.SoloNumeros; // evito suscribir dos veces
                if (tabla.CurrentCell?.ColumnIndex == ValidacionGrilla.COL_EDAD)
                {
                    tb.KeyPress += ValidacionGrilla.SoloNumeros;
                }

                // -- Autocompletado de localidades SOLO en la columna Localidad --
                // OJO: WinForms reutiliza el MISMO TextBox para editar todas las celdas
                // de texto. Por eso CONECTO el autocompletado al entrar a Localidad y lo
                // DESCONECTO en las demás columnas; si no, el desplegable de localidades
                // aparecería también al escribir el nombre.
                //
                // Además apago el autocompletado NATIVO en TODAS las columnas por si
                // hubiera quedado prendido de antes: no queremos los dos a la vez.
                tb.AutoCompleteMode = AutoCompleteMode.None;
                tb.AutoCompleteSource = AutoCompleteSource.None;

                if (tabla.CurrentCell?.ColumnIndex == ValidacionGrilla.COL_LOCALIDAD)
                    autoLocalidad.Conectar(tb);   // engancho el desplegable a este cuadro de edición
                else
                    autoLocalidad.Desconectar();  // en otras columnas, sin sugerencias
            }
        }

        // Al terminar de editar una celda de texto: normalizo.
        private void tabla_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            // Salí de la celda: por las dudas, cierro el desplegable de sugerencias.
            autoLocalidad?.Ocultar();

            if (estado.Cargando || e.RowIndex < 0) return;
            var fila = tabla.Rows[e.RowIndex];
            if (fila.IsNewRow) return;

            ValidacionGrilla.NormalizarFila(fila);
            estado.MarcarCambio();
        }

        // Cambió el valor de una celda (incluye elegir en el combo de motivo): marco cambio.
        private void tabla_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (estado.Cargando || e.RowIndex < 0) return;
            var fila = tabla.Rows[e.RowIndex];
            if (fila.IsNewRow) return;

            estado.MarcarCambio();
        }

        // Recarga la lista de localidades sugeridas desde la base de datos.
        private void RefrescarLocalidadesSugeridas()
        {
            localidadesSugeridas.Clear();
            localidadesSugeridas.AddRange(repositorio.ObtenerLocalidades().ToArray());
        }

        // ============================================================
        //  PESTAÑA ESTADÍSTICAS
        // ============================================================

        // Decide QUÉ mostrar según el modo activo (+ la fecha elegida cuando
        // corresponde) y lo dibuja. Lo llaman: el cambio de fecha del selector,
        // el cambio de modo (Semana / Mes / 3 meses / Todo) y los refrescos tras
        // guardar o reinsertar.
        //
        // El CÁLCULO del rango vive en SelectorModoEstadistica y el DIBUJO en
        // GraficosEstadisticas: acá solo se juntan las dos puntas con la base.
        private void RefrescarEstadisticas()
        {
            if (selectorModo.ObtenerRango(fechas.FechaEstadisticas,
                                          out DateTime desde, out DateTime hasta, out string descripcion))
            {
                // Modo con rango (día / semana / mes / 3 meses): pido a la base
                // SOLO los alumnos de ese período.
                graficos.Mostrar(repositorio.ObtenerEntreFechas(desde, hasta), descripcion);
            }
            else
            {
                // Modo "Todo": sin filtro de fechas, van TODOS los alumnos de la base.
                graficos.Mostrar(repositorio.ObtenerTodos(), descripcion);
            }
        }

        // ============================================================
        //  CIERRE DE LA APP
        // ============================================================
        private void FormPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Si no hay cambios pendientes, dejo cerrar sin molestar.
            if (!estado.HayCambiosSinGuardar) return;

            // Si hay cambios, muestro el diálogo que obliga a escribir "si" o cancelar.
            using var dlg = new DialogoSalida();
            if (dlg.ShowDialog(this) != DialogResult.OK)
                e.Cancel = true; // canceló (o cerró el diálogo): no salgo.
        }

        // ============================================================
        //  ESTÉTICA (tema oscuro)
        // ============================================================
        private void AplicarTema()
        {
            BackColor = Tema.FondoDatos;
            Font = new Font("Segoe UI", 9.5F);

            // ----- Pestaña Datos (azul / dorado / blanco) -----
            tabDatos.BackColor = Tema.FondoDatos;
            panelTopDatos.BackColor = Tema.PanelDatos;
            lblFecha.ForeColor = Tema.Dorado;
            EstiloBoton(btnGuardar, Tema.AzulAccent, Tema.Dorado, Tema.Dorado);
            EstiloBoton(btnImportar, Tema.AzulAccent, Tema.Dorado, Tema.Dorado);
            EstiloGrilla();

            // ----- Pestaña Estadísticas (rojo / negro) -----
            // (La paleta del dtp de esta pestaña la aplica ControladorFechas, y el
            // estilo de los botones de modo lo aplica SelectorModoEstadistica.)
            tabEstadisticas.BackColor = Tema.FondoEst;
            panelTopEst.BackColor = Tema.PanelEst;
            panelScroll.BackColor = Tema.FondoEst;   // panel contenedor con scroll (mismo fondo oscuro)
            tlpGraficos.BackColor = Tema.FondoEst;
            lblLocalidades.BackColor = Tema.PanelEst;
            lblLocalidades.ForeColor = Tema.RojoClaro;
            lblMotivos.BackColor = Tema.PanelEst;
            lblMotivos.ForeColor = Tema.RojoClaro;
            lblEdades.BackColor = Tema.PanelEst;
            lblEdades.ForeColor = Tema.RojoClaro;
            panelLocalidades.BackColor = Tema.PanelEst;
            panelMotivos.BackColor = Tema.PanelEst;
            panelEdades.BackColor = Tema.PanelEst;
        }

        // Aplica el estilo plano y oscuro a un botón.
        private void EstiloBoton(Button btn, Color fondo, Color texto, Color borde)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = fondo;
            btn.ForeColor = texto;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderColor = borde;
            btn.FlatAppearance.BorderSize = 1;
            btn.Font = new Font("Segoe UI Semibold", 9.5F);
        }

        // Estilo oscuro de la grilla estilo Excel.
        private void EstiloGrilla()
        {
            tabla.BackgroundColor = Tema.FondoDatos;
            tabla.BorderStyle = BorderStyle.None;
            tabla.EnableHeadersVisualStyles = false;   // necesario para que tomen mis colores.
            tabla.GridColor = Tema.AzulBorde;
            tabla.RowHeadersVisible = false;
            tabla.ColumnHeadersHeight = 40;
            tabla.RowTemplate.Height = 40;

            // Encabezados (azul con texto dorado).
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Tema.AzulAccent;
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Tema.Dorado;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Tema.AzulAccent;
            tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 12F);

            // Celdas normales.
            // Como las filas alternas NO definen su propia fuente, heredan ESTA.
            tabla.DefaultCellStyle.Font = new Font("Segoe UI", 12F); // ← número clave del tamaño de letra
            tabla.DefaultCellStyle.BackColor = Tema.PanelDatos;
            tabla.DefaultCellStyle.ForeColor = Tema.Blanco;
            tabla.DefaultCellStyle.SelectionBackColor = Tema.Dorado;
            tabla.DefaultCellStyle.SelectionForeColor = Tema.FondoDatos;

            // Filas alternas, para que se lea como una planilla.
            tabla.AlternatingRowsDefaultCellStyle.BackColor = Tema.FilaAlterna;
            tabla.AlternatingRowsDefaultCellStyle.ForeColor = Tema.Blanco;
            tabla.AlternatingRowsDefaultCellStyle.SelectionBackColor = Tema.Dorado;
            tabla.AlternatingRowsDefaultCellStyle.SelectionForeColor = Tema.FondoDatos;
        }

        // ============================================================
        //  PESTAÑA DATOS: BÚSQUEDA Y REINSERTAR
        // ============================================================

        // Botón "Buscar": toma lo escrito en la barra y muestra en la grilla TODOS los
        // alumnos (sin importar la fecha) cuyo nombre, localidad o motivo coincidan.
        private void btnBuscar_Click(object sender, EventArgs e)
        {
            // Trim() saca los espacios de los costados; si quedó vacío, no hay nada que buscar.
            string texto = buscador.Text.Trim();

            if (texto.Length == 0)
            {
                // Búsqueda vacía = "salir de la búsqueda y volver a la vista normal".
                // Si estaba mostrando resultados, vuelvo al día que tenga elegido el selector.
                // Si no estaba en búsqueda, no hago nada (no molesto con un cartel).
                if (estado.EnBusqueda)
                {
                    SalirModoBusqueda();
                    CargarTablaDeFecha(fechas.FechaDatos);
                }
                return;
            }

            // Si en la grilla normal hay ediciones SIN guardar, aviso antes de
            // reemplazarla por los resultados (si no, se perderían esos cambios).
            if (!estado.EnBusqueda && estado.HayCambiosSinGuardar)
            {
                var r = MessageBox.Show(
                    "Tenés cambios sin guardar en esta fecha.\n¿Descartarlos y mostrar los resultados de la búsqueda?",
                    "Cambios sin guardar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (r == DialogResult.No) return; // el usuario prefiere no perder lo que tenía.
            }

            // Le pido al repositorio los alumnos que coinciden (busca en nombre, localidad y motivo).
            List<Alumno> resultados = repositorio.BuscarAlumnos(texto);

            if (resultados.Count == 0)
            {
                // Sin coincidencias: aviso y me quedo como estaba (no entro en modo búsqueda).
                MessageBox.Show(
                    $"No se encontró ningún alumno que coincida con \"{texto}\".",
                    "Sin resultados",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hay resultados: entro en modo búsqueda y lleno la grilla con ellos.
            EntrarModoBusqueda(texto, resultados);
        }

        private void BotonSalirModoBusqueda_Click(object sender, EventArgs e)
        {
            // (La visibilidad de Buscar/Salir la restaura SincronizarUiSegunEstado
            // cuando la máquina cambia de estado; acá no hace falta tocarla.)
            buscador.Clear();
            SalirModoBusqueda();
        }

        // Prepara la grilla para MOSTRAR RESULTADOS de búsqueda (en vez de la carga de un día).
        private void EntrarModoBusqueda(string texto, List<Alumno> resultados)
        {
            // La transición dispara SincronizarUiSegunEstado: columna "Reinsertar",
            // sin fila de agregar, botón "Salir de la búsqueda" visible.
            estado.EntrarBusqueda();

            // Cargo las filas con los resultados, con la compuerta cerrada para que
            // los eventos (RowsAdded, etc.) no marquen "cambios sin guardar".
            estado.EmpezarCarga();
            tabla.Rows.Clear();
            foreach (Alumno a in resultados)
            {
                // Add devuelve el índice de la fila recién creada.
                int idx = tabla.Rows.Add(a.Nombre, a.Edad, a.Celular, a.Localidad, a.MotivoIngreso);

                // GUARDO el Alumno completo en el Tag de la fila. El Tag es un "bolsillo"
                // libre que tiene cada fila. Lo necesito para que, al tocar "Reinsertar",
                // pueda saber el Id del alumno (para ubicarlo en la base) y su fecha
                // original (para mostrarla en el mensaje). La grilla NO muestra ni el Id
                // ni la fecha, pero acá los llevamos "escondidos" pegados a la fila.
                tabla.Rows[idx].Tag = a;
            }
            estado.TerminarCarga();

            // Reuso la etiqueta de la fecha para avisar que estamos viendo resultados.
            lblFecha.Text = $"Resultados de \"{texto}\" ({resultados.Count})";
        }

        // Vuelve a dejar la grilla en modo NORMAL (edición de un día) mostrando HOY.
        private void SalirModoBusqueda()
        {
            estado.SalirBusqueda(); // la UI se restaura sola (SincronizarUiSegunEstado).

            // Llevo el selector de fecha a hoy SIN disparar su evento,
            // para que no se pisen las cargas.
            fechas.FijarFechaDatos(DateTime.Today);

            CargarTablaDeFecha(DateTime.Today); // deja fechaActual = hoy y lblFecha = "Hoy"
        }

        // "Reinsertar" un alumno de los resultados: lo saca de su fecha original y lo deja
        // cargado en el día de HOY.
        private void ReinsertarFila(int indiceFila)
        {
            // Recupero el Alumno que había guardado en el Tag al armar los resultados.
            // El patrón "is not Alumno alumno" chequea que el Tag sea un Alumno y,
            // si lo es, lo deja en la variable "alumno" lista para usar.
            if (tabla.Rows[indiceFila].Tag is not Alumno alumno)
                return; // si por alguna razón no hay Alumno en el Tag, no hago nada (seguridad).

            // Confirmo antes de tocar la base: esto cambia la fecha del alumno.
            var r = MessageBox.Show(
                $"¿Reinsertar a {alumno.Nombre} en el día de hoy?\n\n" +
                $"Se va a quitar de su fecha original ({alumno.Fecha:dd/MM/yyyy}) " +
                $"y va a quedar cargado en la fecha de hoy ({DateTime.Today:dd/MM/yyyy}).",
                "Reinsertar alumno",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (r != DialogResult.Yes) return; // si no confirma, no hago nada.

            // Muevo el registro a hoy (un UPDATE de la columna Fecha por Id, ver el repositorio).
            repositorio.MoverAlumnoAFecha(alumno.Id, DateTime.Today);

            // Cambiaron datos de fecha: refresco las estadísticas que se estén mostrando,
            // respetando el modo activo (día / semana / mes / 3 meses / todo).
            RefrescarEstadisticas();

            MessageBox.Show(
                $"{alumno.Nombre} quedó reinsertado en el día de hoy.",
                "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ============================================================
        //  BARRA DE TÍTULO PROPIA (cerrar / minimizar / maximizar / arrastrar)
        // ============================================================

        private void BotonCerrar_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BotonMaximizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            BotonMaximizar.Visible = false;
            BotonMinimizarTamaño.Visible = true;

            // Maximizada la ventana NO se puede estirar (WndProc ya lo ignora),
            // así que el marco invisible no cumple ninguna función: lo saco para
            // que el contenido llegue justo hasta el borde de la pantalla y no
            // quede un anillo de píxeles "muertos" alrededor.
            Padding = new Padding(0);
        }

        private void BotonMinimizarTamaño_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            BotonMaximizar.Visible = true;
            BotonMinimizarTamaño.Visible = false;

            // Al volver al tamaño normal, devuelvo el marco invisible para que
            // la ventana se pueda estirar otra vez (ver ConfigurarRedimensionado).
            Padding = new Padding(grosorMarco);
        }

        private void BotonMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void PanelTop_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        // ============================================================
        //  REDIMENSIONADO DE LA VENTANA (bordes y esquinas)
        // ============================================================
        //
        // ¿POR QUÉ NO FUNCIONABA SOLO?
        // ----------------------------
        // Una ventana común de Windows se puede estirar porque tiene un MARCO
        // (el "área no-cliente"): esa franja finita alrededor que dibuja y maneja
        // el propio Windows. Nuestra ventana usa FormBorderStyle = None justamente
        // para poder dibujar la barra de título a gusto... pero al sacar el marco,
        // también se fue el redimensionado, que venía "de regalo" con él.
        //
        // ¿CÓMO LO RECUPERAMOS?
        // ---------------------
        // Cada vez que el mouse se mueve o hace clic, Windows le pregunta a la
        // ventana "¿QUÉ hay en este punto?" mediante el mensaje WM_NCHITTEST.
        // Según lo que la ventana conteste, Windows decide qué hacer:
        //   - HTCLIENT      -> "es contenido normal" (no pasa nada especial)
        //   - HTLEFT        -> "es el borde izquierdo": muestra el cursor <-> y,
        //                      si hacés clic y arrastrás, ÉL MISMO estira la ventana
        //   - HTBOTTOMRIGHT -> "es la esquina inferior derecha": cursor diagonal, etc.
        //
        // O sea: NO tenemos que programar el estiramiento; solo tenemos que
        // CONTESTAR BIEN la pregunta. Si en los ~8 px del borde respondemos los
        // códigos correctos, Windows pone los cursores de siempre, hace el arrastre
        // nativo y hasta respeta el MinimumSize del formulario. Fijate que es el
        // camino inverso al arrastre de PanelTop_MouseDown: allá nosotros le
        // MANDAMOS un mensaje a Windows ("movete"); acá Windows nos PREGUNTA
        // y nosotros le respondemos.
        //
        // EL DETALLE FINO (por qué hace falta el Padding)
        // -----------------------------------------------
        // WM_NCHITTEST se le pregunta a la ventana QUE ESTÁ BAJO EL MOUSE. Y hoy
        // el formulario está tapado al 100% por sus hijos (PanelTop arriba y
        // PanelContenedor rellenando el resto): el mouse siempre está "sobre un
        // hijo", así que la pregunta le llega al panel y NUNCA al formulario.
        // La solución es dejar una franja del FORMULARIO al descubierto en los
        // bordes: eso hace el "Padding = ..." de ConfigurarRedimensionado(). Los
        // controles con Dock respetan el Padding del padre, así que todos los
        // hijos se corren unos píxeles hacia adentro y ese anillo queda "siendo
        // formulario". Como el BackColor del formulario se pinta igual que el
        // fondo de la pestaña activa (ver tab_SelectedIndexChanged), el anillo
        // es invisible: solo se nota al acercar el mouse, cuando aparece el
        // cursor de estirar. (Si algún día querés que el borde SE VEA, alcanza
        // con pintarlo de otro color, por ejemplo Tema.AzulBorde.)

        // --- El mensaje con el que Windows pregunta "¿qué hay en este punto?" ---
        private const int WM_NCHITTEST = 0x84;

        // --- Las respuestas posibles (códigos "hit test" de Windows) ---
        // Los valores son fijos: están definidos así dentro del propio Windows,
        // igual que el 0x112 (WM_SYSCOMMAND) que ya usamos para mover la ventana.
        private const int HTTOP = 12; // borde superior
        private const int HTBOTTOM = 15; // borde inferior
        private const int HTLEFT = 10; // borde izquierdo
        private const int HTRIGHT = 11; // borde derecho
        private const int HTTOPLEFT = 13; // esquina superior izquierda
        private const int HTTOPRIGHT = 14; // esquina superior derecha
        private const int HTBOTTOMLEFT = 16; // esquina inferior izquierda
        private const int HTBOTTOMRIGHT = 17; // esquina inferior derecha

        // Grosor del marco invisible donde se puede "agarrar" la ventana, en
        // píxeles REALES de pantalla. Se calcula una sola vez, en
        // ConfigurarRedimensionado(); lo usan el Padding y el WndProc, y por eso
        // es un campo y no una constante: tiene que ser EL MISMO número en los dos.
        private int grosorMarco;

        // Deja la ventana lista para redimensionar. Se llama UNA vez, desde FormPrincipal_Load.
        private void ConfigurarRedimensionado()
        {
            // 8 px "lógicos" convertidos a píxeles reales según la escala de Windows
            // (al 100% da 8, al 150% da 12, etc.). Ya nos pasó que una PC con otra
            // escala rompía el layout; acá lo contemplamos de entrada para que el
            // borde se sienta igual de "agarrable" en cualquier monitor.
            grosorMarco = LogicalToDeviceUnits(8);

            // La franja del formulario que queda al descubierto en los 4 bordes.
            // Sin esto, WM_NCHITTEST nunca llegaría al formulario porque los
            // paneles lo tapan todo (ver el comentario grande de arriba).
            Padding = new Padding(grosorMarco);

            // Tamaño mínimo = el tamaño con el que abre la app. Windows lo respeta
            // solo DURANTE el estiramiento nativo: no te deja achicar más que esto.
            // El layout fue diseñado para este tamaño (hay botones en posiciones
            // fijas que se taparían), así que por ahora solo permitimos AGRANDAR.
            MinimumSize = new Size(1020, 300);

            // Color del anillo del marco: igual al fondo de la pestaña inicial
            // (Datos) para que sea invisible. Después, al cambiar de pestaña,
            // tab_SelectedIndexChanged lo mantiene sincronizado.
            BackColor = Tema.FondoDatos;
        }

        // WndProc es el "buzón" de la ventana: TODOS los mensajes de Windows pasan
        // por acá antes de convertirse en los eventos de siempre (Click, Paint...).
        // Al sobreescribirlo podemos interceptar mensajes que WinForms no expone
        // como evento, como WM_NCHITTEST. La regla de oro: todo lo que no
        // atendamos nosotros se lo pasamos a base.WndProc, para que el resto del
        // formulario siga funcionando exactamente igual que siempre.
        protected override void WndProc(ref Message m)
        {
            // Solo me interesa la pregunta "¿qué hay en este punto?", y solo con la
            // ventana en tamaño normal: maximizada no tiene sentido estirar (las
            // ventanas comunes de Windows tampoco lo permiten) y además en ese
            // estado el marco está oculto (ver BotonMaximizar_Click).
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                // El punto viene "empaquetado" en LParam: los 16 bits bajos son la X
                // y los 16 altos la Y, en COORDENADAS DE PANTALLA y CON SIGNO (con
                // dos monitores, el de la izquierda tiene X negativas). El cast a
                // short es el que conserva ese signo; un simple "& 0xFFFF" solo
                // daría siempre positivos y fallaría en multi-monitor.
                long lp = m.LParam.ToInt64();
                int xPantalla = (short)(lp & 0xFFFF);
                int yPantalla = (short)((lp >> 16) & 0xFFFF);

                // Paso de coordenadas de pantalla a coordenadas del formulario
                // (0,0 = esquina superior izquierda de NUESTRA ventana), que es
                // donde puedo comparar contra ClientSize.
                Point p = PointToClient(new Point(xPantalla, yPantalla));

                // Zona de esquina más generosa (el doble del borde): apuntarle a un
                // cuadradito de 8x8 sería incómodo; con 16 a lo largo del borde la
                // diagonal se agarra fácil, igual que en las ventanas comunes.
                int esquina = grosorMarco * 2;

                bool izquierda = p.X < esquina;
                bool derecha = p.X >= ClientSize.Width - esquina;
                bool arriba = p.Y < esquina;
                bool abajo = p.Y >= ClientSize.Height - esquina;

                // OJO con el orden: primero las esquinas (que son la combinación de
                // DOS bordes) y recién después los bordes sueltos. Si preguntara
                // primero "¿es el borde izquierdo?", una esquina contestaría
                // "izquierdo" y nunca llegaría a ser diagonal.
                //
                // m.Result es NUESTRA RESPUESTA a Windows. El "return" sin llamar a
                // base.WndProc es a propósito: la pregunta ya quedó contestada.
                if (arriba && izquierda) { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (arriba && derecha) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (abajo && izquierda) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (abajo && derecha) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }

                // Bordes rectos: acá sí uso el grosor real del marco (los 8 px).
                if (p.Y < grosorMarco) { m.Result = (IntPtr)HTTOP; return; }
                if (p.Y >= ClientSize.Height - grosorMarco) { m.Result = (IntPtr)HTBOTTOM; return; }
                if (p.X < grosorMarco) { m.Result = (IntPtr)HTLEFT; return; }
                if (p.X >= ClientSize.Width - grosorMarco) { m.Result = (IntPtr)HTRIGHT; return; }

                // Si el punto no cayó en el marco (no debería pasar: el formulario
                // solo queda expuesto ahí), sigue por el camino normal de abajo.
            }

            // Cualquier otro mensaje (o un hit test que no era del marco): que lo
            // atienda WinForms como siempre.
            base.WndProc(ref m);
        }

        private void tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            PanelTop.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;

            // El fondo del FORMULARIO también acompaña: es el color del marco
            // invisible de los bordes (ver ConfigurarRedimensionado). Si no lo
            // cambiara acá, al pasar a Estadísticas se notaría un anillo azul
            // alrededor del fondo negro (y al revés).
            this.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;

            BotonCerrar.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;
            BotonMinimizar.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;
            BotonMaximizar.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;
            BotonMinimizarTamaño.BackColor = (tab.SelectedTab == tabDatos) ? Tema.FondoDatos : Tema.FondoEst;

            BotonCerrar.FlatAppearance.MouseOverBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMaximizar.FlatAppearance.MouseOverBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMinimizar.FlatAppearance.MouseOverBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMinimizarTamaño.FlatAppearance.MouseOverBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;

            BotonCerrar.FlatAppearance.MouseDownBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMaximizar.FlatAppearance.MouseDownBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMinimizar.FlatAppearance.MouseDownBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
            BotonMinimizarTamaño.FlatAppearance.MouseDownBackColor = (tab.SelectedTab == tabDatos) ? Tema.AzulAccent : Tema.Rojo;
        }
    }
}
