using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace datos_y_estadisticas
{
    // ============================================================================
    //  AutocompletadoOscuro — desplegable de sugerencias con estética propia
    // ============================================================================
    //
    // ¿POR QUÉ EXISTE ESTO?
    // ---------------------
    // El autocompletado que trae WinForms de fábrica (TextBox.AutoCompleteMode +
    // AutoCompleteCustomSource) lo dibuja WINDOWS, no nosotros. Por eso queda chiquito,
    // claro y no se le pueden cambiar tamaño ni colores. Es el mismo problema que el
    // calendario del DateTimePicker. La única forma de tener un desplegable grande,
    // oscuro y combinado con la solapa es hacerlo a mano: eso es esta clase.
    //
    // CÓMO ESTÁ HECHO (la parte fina)
    // -------------------------------
    // Es un TextBox (el de la celda) + una VENTANA flotante sin bordes (un Form) que
    // aparece debajo con una lista que pintamos nosotros.
    //
    //   * Foco: la ventana flotante NO debe robar el foco; si lo robara, la grilla
    //     pensaría que dejaste de editar la celda y cerraría todo. Para eso:
    //       - la ventana usa WS_EX_NOACTIVATE + responde MA_NOACTIVATE al clic, y
    //       - la lista de adentro es "no seleccionable" (no toma el foco al clickearla).
    //
    //   * Teclado: dentro de una grilla, las flechas / Enter / Escape las atrapa la
    //     GRILLA para moverse entre celdas, así que NO llegan al TextBox. Para ganarles
    //     de mano, esta clase es un IMessageFilter: mira los mensajes de teclado ANTES
    //     de que los procese la grilla y, si el desplegable está abierto, se queda con
    //     esas teclas para navegar/elegir.  (Esto reemplaza el intento anterior con
    //     PreviewKeyDown/KeyDown, que en una grilla no alcanza.)
    //
    //   * La lista la dibujamos nosotros (OwnerDrawFixed): controlamos color de fondo,
    //     color del texto, color del renglón elegido, la sangría, el ALTO de cada
    //     renglón y la fuente.
    //
    // CÓMO SE USA (ver Form1.cs)
    // -------------------------
    //   autoLocalidad = new AutocompletadoOscuro(() => localidadesSugeridas);
    //   ... y en EditingControlShowing:  autoLocalidad.Conectar(tb) / .Desconectar()
    //
    // Es REUTILIZABLE: también sirve para un TextBox suelto (por ej., la barra de
    // búsqueda) sin tocar esta clase.
    // ============================================================================
    public class AutocompletadoOscuro : IDisposable, IMessageFilter
    {
        // -------- Configurable desde afuera (con valores por defecto) --------
        // La fuente de la lista. ES EL parámetro que más importa (la lee una persona
        // mayor). Si querés más grande, subí el número.
        public Font FuenteLista { get; set; } = new Font("Segoe UI", 12.5F);

        // Alto (px) de cada renglón. Más alto = más fácil de leer y de tocar.
        public int AltoItem { get; set; } = 32;

        // Cuántas opciones se ven antes de que aparezca el scroll.
        public int MaxVisibles { get; set; } = 8;

        // Ancho mínimo del desplegable. 0 = usar el ancho de la CELDA (lo normal).
        public int AnchoMinimo { get; set; } = 0;

        // -------- Colores (por defecto, paleta azul/dorado/blanco de la solapa Datos) --------
        public Color ColorFondo { get; set; } = Tema.PanelDatos;        // fondo de la lista
        public Color ColorTexto { get; set; } = Tema.Blanco;            // texto de cada opción
        public Color ColorBorde { get; set; } = Tema.AzulBorde;         // marquito de 1px
        public Color ColorItemSelFondo { get; set; } = Tema.AzulAccent; // fondo del renglón elegido
        public Color ColorItemSelTexto { get; set; } = Tema.Dorado;     // texto del renglón elegido

        // -------- Estado interno --------
        private TextBox? _caja;                 // cuadro de texto al que estamos enganchados AHORA
        private readonly PopupSinFoco _popup;   // la ventana flotante (sin bordes, no roba foco)
        private readonly ListaSinFoco _lista;   // la lista de opciones (no toma el foco)
        private readonly Func<IEnumerable<string>> _obtenerCandidatos; // de dónde salen las opciones
        private bool _aceptando;                // evita reabrir cuando NOSOTROS escribimos en la caja

        public AutocompletadoOscuro(Func<IEnumerable<string>> obtenerCandidatos)
        {
            _obtenerCandidatos = obtenerCandidatos
                ?? throw new ArgumentNullException(nameof(obtenerCandidatos));

            // ---- La lista de opciones (la pintamos nosotros, y NO toma el foco) ----
            _lista = new ListaSinFoco
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,     // el borde lo hace el popup (su Padding)
                DrawMode = DrawMode.OwnerDrawFixed, // "yo dibujo cada renglón", alto fijo = AltoItem
                IntegralHeight = false,             // alto EXACTO (no redondeado a renglones enteros)
                ItemHeight = AltoItem,
                Font = FuenteLista,
                BackColor = ColorFondo,
                ForeColor = ColorTexto,
                TabStop = false
            };
            _lista.DrawItem += DibujarItem;
            _lista.MouseDown += AlClickEnLista;     // clic en una opción = elegirla

            // ---- La ventana flotante que aloja la lista ----
            _popup = new PopupSinFoco
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Padding = new Padding(1),  // 1px que deja ver el BackColor del popup como marco
                BackColor = ColorBorde
            };
            _popup.Controls.Add(_lista);

            // Me registro como "filtro de mensajes" de la aplicación. Así puedo mirar las
            // teclas ANTES que la grilla (ver PreFilterMessage). Es livianito: solo actúo
            // cuando el desplegable está visible.
            Application.AddMessageFilter(this);
        }

        // ------------------------------------------------------------------------
        //  Conectar / Desconectar
        // ------------------------------------------------------------------------
        public void Conectar(TextBox caja)
        {
            if (ReferenceEquals(_caja, caja)) return; // ya conectado a este mismo cuadro
            Desconectar();

            _caja = caja;
            _caja.TextChanged += AlEscribir;  // al tipear -> filtrar y mostrar opciones
            _caja.LostFocus += AlPerderFoco;  // si el cuadro pierde el foco -> cerrar el desplegable
            // OJO: el teclado (flechas/Enter/Escape) NO se maneja acá, sino en
            // PreFilterMessage, porque en una grilla esas teclas no llegan al TextBox.
        }

        public void Desconectar()
        {
            Ocultar();
            if (_caja != null)
            {
                _caja.TextChanged -= AlEscribir;
                _caja.LostFocus -= AlPerderFoco;
                _caja = null;
            }
        }

        public void Ocultar()
        {
            if (_popup.Visible) _popup.Hide();
        }

        // ------------------------------------------------------------------------
        //  Al escribir: filtrar candidatos y mostrar/ocultar el cuadro
        // ------------------------------------------------------------------------
        private void AlEscribir(object? sender, EventArgs e)
        {
            if (_aceptando || _caja == null) return; // si el cambio lo hicimos nosotros, no reabrir

            string texto = _caja.Text.Trim();
            if (texto.Length == 0) { Ocultar(); return; }

            string q = Normalizar(texto); // sin tildes y en minúscula, para comparar parejo

            // Opciones que CONTIENEN lo escrito ("isi" -> "San Isidro"). Primero las que
            // ARRANCAN con lo escrito; dentro de cada grupo, alfabético. Take(50) = tope.
            List<string> coincidencias = _obtenerCandidatos()
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Where(c => Normalizar(c).Contains(q))
                .OrderByDescending(c => Normalizar(c).StartsWith(q))
                .ThenBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .Distinct()
                .Take(50)
                .ToList();

            if (coincidencias.Count == 0) { Ocultar(); return; }

            // Si la única coincidencia es EXACTAMENTE lo ya escrito, no molesto con el cuadro.
            if (coincidencias.Count == 1 && Normalizar(coincidencias[0]) == q) { Ocultar(); return; }

            _lista.BeginUpdate();
            _lista.Items.Clear();
            foreach (string c in coincidencias) _lista.Items.Add(c);
            _lista.EndUpdate();
            _lista.SelectedIndex = 0; // dejo la primera resaltada (Enter elige la más probable)

            Mostrar();
        }

        // ------------------------------------------------------------------------
        //  Mostrar el desplegable: ubicarlo y dimensionarlo (anclado a la CELDA)
        // ------------------------------------------------------------------------
        private void Mostrar()
        {
            if (_caja == null) return;

            // Sincronizo apariencia por si cambiaste fuente/alto/colores luego de crearlo.
            _lista.Font = FuenteLista;
            _lista.ItemHeight = AltoItem;
            _lista.BackColor = ColorFondo;
            _popup.BackColor = ColorBorde;

            // ---- Dónde aparecer (en coordenadas de PANTALLA) ----
            // Si estamos editando una celda de una grilla, me anclo a la CELDA, NO al
            // cuadrito de edición interno. ¿Por qué? Porque ese cuadrito es un poco más
            // angosto que la celda y está metido un par de píxeles hacia adentro: por eso
            // antes el desplegable salía "más arriba" y "más angosto" que la celda.
            // Usando el rectángulo de la celda, el ancho coincide y queda pegado al borde
            // inferior de la celda.
            Rectangle ancla;
            if (_caja is IDataGridViewEditingControl ec
                && ec.EditingControlDataGridView is DataGridView grilla
                && grilla.CurrentCellAddress.X >= 0
                && grilla.CurrentCellAddress.Y >= 0)
            {
                // GetCellDisplayRectangle da el rectángulo de la celda en coordenadas de la
                // grilla; PointToScreen lo pasa a coordenadas de pantalla.
                Rectangle celda = grilla.GetCellDisplayRectangle(
                    grilla.CurrentCellAddress.X, grilla.CurrentCellAddress.Y, false);
                ancla = new Rectangle(grilla.PointToScreen(celda.Location), celda.Size);
            }
            else
            {
                // Caso TextBox suelto (fuera de una grilla): me anclo al propio cuadro.
                ancla = new Rectangle(_caja.PointToScreen(Point.Empty), _caja.Size);
            }

            int ancho = AnchoMinimo > 0 ? Math.Max(AnchoMinimo, ancla.Width) : ancla.Width;
            int filas = Math.Min(_lista.Items.Count, MaxVisibles);
            int alto = filas * AltoItem + 2; // +2 = el marco de 1px (arriba y abajo)

            // Por defecto, JUSTO DEBAJO de la celda.
            Point esquina = new Point(ancla.Left, ancla.Bottom);

            // Si no entra para abajo (lo taparía el borde de la pantalla), lo abro para arriba.
            Rectangle pantalla = Screen.FromControl(_caja).WorkingArea;
            if (esquina.Y + alto > pantalla.Bottom)
                esquina = new Point(ancla.Left, ancla.Top - alto);

            _popup.Bounds = new Rectangle(esquina, new Size(ancho, alto));

            // Lo muestro SIN activarlo (para no sacarle el foco al cuadro de edición).
            if (!_popup.Visible)
            {
                Form? duenio = _caja.FindForm();
                if (duenio != null) _popup.Show(duenio);
                else _popup.Show();
            }
        }

        // ------------------------------------------------------------------------
        //  Elegir una opción (con el mouse o con Enter)
        // ------------------------------------------------------------------------
        private void Aceptar()
        {
            if (_caja == null || _lista.SelectedItem == null) { Ocultar(); return; }

            _aceptando = true;                       // para que el TextChanged de abajo no reabra
            _caja.Text = _lista.SelectedItem.ToString();
            _caja.SelectionStart = _caja.Text.Length; // cursor al final
            _aceptando = false;

            Ocultar();
        }

        // ------------------------------------------------------------------------
        //  Teclado: lo manejamos como FILTRO DE MENSAJES (antes que la grilla)
        // ------------------------------------------------------------------------
        // PreFilterMessage lo llama el bucle de mensajes de WinForms para CADA mensaje,
        // ANTES de despacharlo. Devolver true = "me lo quedo yo, no sigas". Así le ganamos
        // a la grilla, que normalmente usa flechas/Enter/Escape para moverse entre celdas.
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_KEYDOWN = 0x0100;

            // Solo me meto si el desplegable está abierto y es una tecla "para abajo".
            if (_popup.Visible && m.Msg == WM_KEYDOWN)
            {
                // El código de la tecla viaja en la palabra baja de WParam.
                Keys tecla = (Keys)(int)(m.WParam.ToInt64() & 0xFFFF);
                if (ProcesarTecla(tecla))
                    return true; // consumida: la grilla NO la ve
            }
            return false; // cualquier otra cosa, que siga su curso normal
        }

        // Devuelve true si "consumió" la tecla (es decir, si hizo algo con ella).
        private bool ProcesarTecla(Keys tecla)
        {
            if (!_popup.Visible) return false;

            switch (tecla)
            {
                case Keys.Down: MoverSeleccion(+1); return true; // bajar en la lista
                case Keys.Up: MoverSeleccion(-1); return true; // subir en la lista
                case Keys.Enter: Aceptar(); return true; // elegir el resaltado
                case Keys.Escape: Ocultar(); return true; // cerrar sin elegir
                default: return false;
            }
        }

        private void MoverSeleccion(int delta)
        {
            if (_lista.Items.Count == 0) return;
            int i = _lista.SelectedIndex + delta;
            if (i < 0) i = 0;
            if (i > _lista.Items.Count - 1) i = _lista.Items.Count - 1;
            _lista.SelectedIndex = i;
        }

        // Si el cuadro pierde el foco (te fuiste a otra celda u otro control), cierro.
        // Hacerle clic a la lista NO dispara esto, porque la lista no toma el foco.
        private void AlPerderFoco(object? sender, EventArgs e) => Ocultar();

        // Clic con el mouse sobre una opción: averiguo el renglón y lo elijo.
        private void AlClickEnLista(object? sender, MouseEventArgs e)
        {
            int i = _lista.IndexFromPoint(e.Location);
            if (i >= 0 && i < _lista.Items.Count)
            {
                _lista.SelectedIndex = i;
                Aceptar();
            }
        }

        // ------------------------------------------------------------------------
        //  Dibujo de cada renglón
        // ------------------------------------------------------------------------
        private void DibujarItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool elegido = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color fondo = elegido ? ColorItemSelFondo : ColorFondo;
            Color texto = elegido ? ColorItemSelTexto : ColorTexto;

            using (var pincel = new SolidBrush(fondo))
                e.Graphics.FillRectangle(pincel, e.Bounds);

            Rectangle rect = Rectangle.Inflate(e.Bounds, -8, 0); // pequeña sangría izquierda
            string opcion = _lista.Items[e.Index]?.ToString() ?? string.Empty;
            TextRenderer.DrawText(e.Graphics, opcion, FuenteLista, rect, texto,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // ------------------------------------------------------------------------
        //  Liberar recursos
        // ------------------------------------------------------------------------
        public void Dispose()
        {
            Application.RemoveMessageFilter(this); // dejo de mirar mensajes de teclado
            Desconectar();
            _lista.DrawItem -= DibujarItem;
            _lista.MouseDown -= AlClickEnLista;
            _popup.Dispose();
            FuenteLista?.Dispose();
            GC.SuppressFinalize(this);
        }

        // ========================================================================
        //  Ventana flotante que NO roba el foco
        // ========================================================================
        private sealed class PopupSinFoco : Form
        {
            // Al mostrarse, no se activa (no saca el foco).
            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    const int WS_EX_NOACTIVATE = 0x08000000; // "no me actives aunque me cliqueen"
                    const int WS_EX_TOOLWINDOW = 0x00000080; // ventanita auxiliar (no en barra de tareas)
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                    return cp;
                }
            }

            // Refuerzo: cuando me hacen clic, Windows pregunta si me quiere activar.
            // Respondo MA_NOACTIVATE = "no me actives, pero SÍ procesá el clic" (para que el
            // clic llegue a la lista). Así el cuadro de edición conserva el foco.
            protected override void WndProc(ref Message m)
            {
                const int WM_MOUSEACTIVATE = 0x0021;
                const int MA_NOACTIVATE = 0x0003;
                if (m.Msg == WM_MOUSEACTIVATE)
                {
                    m.Result = (IntPtr)MA_NOACTIVATE;
                    return;
                }
                base.WndProc(ref m);
            }
        }

        // ========================================================================
        //  Lista que NO toma el foco al hacerle clic
        // ========================================================================
        private sealed class ListaSinFoco : ListBox
        {
            public ListaSinFoco()
            {
                // Si la lista tomara el foco al clickearla, el cuadro de edición de la grilla
                // lo perdería y la grilla cerraría la edición (y este desplegable) ANTES de
                // registrar la opción. Con esto, el clic elige la opción sin cerrar nada.
                SetStyle(ControlStyles.Selectable, false);
            }
        }

        // ========================================================================
        //  Normalizar texto para comparar sin tildes ni mayúsculas
        // ========================================================================
        // "San Martín" -> "san martin". FormD separa la letra de su tilde; descartamos los
        // acentos (NonSpacingMark) y pasamos a minúscula. Así "martin" encuentra "Martín".
        private static string Normalizar(string texto)
        {
            string descompuesto = texto.Normalize(NormalizationForm.FormD);
            char[] sinAcentos = descompuesto
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(sinAcentos).ToLowerInvariant();
        }
    }
}