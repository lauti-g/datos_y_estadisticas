namespace datos_y_estadisticas
{
    // ============================================================
    //  CONTROLADOR DE LOS DOS SELECTORES DE FECHA (DateTimePicker)
    // ============================================================
    //
    // Junta en un solo lugar TODO lo que el formulario hacía con los dtp:
    //
    //   1) La paleta de colores del dtp de Estadísticas (el rojo/negro que
    //      antes se aplicaba dentro de AplicarTema). El de Datos no necesita
    //      nada: DateTimePickerOscuro ya trae la paleta azul por defecto.
    //
    //   2) El truco de cambiar la fecha POR CÓDIGO sin disparar el evento
    //      ValueChanged. Antes era el patrón repetido "cargando = true;
    //      dtp.Value = X; cargando = false;" desparramado por Form1; ahora
    //      son los métodos FijarFechaDatos / FijarFechaEstadisticas, que
    //      usan la compuerta de carga de la máquina de estados.
    //
    //   3) La distinción USUARIO vs CÓDIGO: los eventos FechaDatosCambiada y
    //      FechaEstadisticasCambiada se disparan SOLO cuando el cambio vino
    //      del usuario (la compuerta de carga está abierta). El formulario se
    //      suscribe a estos eventos y ya no tiene que preguntar "¿estoy
    //      cargando?" en cada handler: si le llegó el aviso, fue el usuario.
    //
    // OJO: qué HACER cuando el usuario cambia la fecha (avisar por cambios
    // sin guardar, recargar la grilla, redibujar gráficos) sigue siendo
    // decisión del formulario, que es quien conoce la grilla y la base.
    // Este módulo solo administra los dtp.
    public class ControladorFechas
    {
        private readonly DateTimePickerOscuro dtpDatos;        // el de la pestaña Datos
        private readonly DateTimePickerOscuro dtpEstadisticas; // el de la pestaña Estadísticas
        private readonly MaquinaEstadoDatos estado;            // por la compuerta de carga

        // El usuario eligió otra fecha en la pestaña Datos (trae la fecha nueva).
        public event Action<DateTime>? FechaDatosCambiada;

        // El usuario eligió otra fecha en la pestaña Estadísticas.
        public event Action? FechaEstadisticasCambiada;

        public ControladorFechas(DateTimePickerOscuro dtpDatos,
                                 DateTimePickerOscuro dtpEstadisticas,
                                 MaquinaEstadoDatos estado)
        {
            this.dtpDatos = dtpDatos;
            this.dtpEstadisticas = dtpEstadisticas;
            this.estado = estado;

            // Me suscribo acá, por código, a los ValueChanged de los dos dtp
            // (por eso ya no están cableados en el diseñador). El filtro
            // "!estado.Cargando" es el que separa usuario de código: cuando la
            // fecha se fija con FijarFecha... la compuerta está cerrada y el
            // aviso NO sale.
            dtpDatos.ValueChanged += (s, e) =>
            {
                if (!estado.Cargando)
                    FechaDatosCambiada?.Invoke(dtpDatos.Value.Date);
            };

            dtpEstadisticas.ValueChanged += (s, e) =>
            {
                if (!estado.Cargando)
                    FechaEstadisticasCambiada?.Invoke();
            };

            AplicarPaletaEstadisticas();
        }

        // ---------- Lectura cómoda de las fechas elegidas ----------
        // Siempre .Date (sin hora), que es como se usan en todo el programa.
        public DateTime FechaDatos => dtpDatos.Value.Date;
        public DateTime FechaEstadisticas => dtpEstadisticas.Value.Date;

        // ---------- Cambiar la fecha por código, SIN disparar el evento ----------
        // Cierro la compuerta de carga, muevo el valor y la vuelvo a abrir.
        // Así el ValueChanged de arriba ve "Cargando == true" y no avisa a nadie.
        public void FijarFechaDatos(DateTime fecha)
        {
            estado.EmpezarCarga();
            dtpDatos.Value = fecha;
            estado.TerminarCarga();
        }

        public void FijarFechaEstadisticas(DateTime fecha)
        {
            estado.EmpezarCarga();
            dtpEstadisticas.Value = fecha;
            estado.TerminarCarga();
        }

        // ---------- Paleta del dtp de Estadísticas ----------
        // El dtp de esa pestaña acompaña el tema rojo/negro (el de Datos ya
        // trae de fábrica la paleta azul/dorada de DateTimePickerOscuro).
        private void AplicarPaletaEstadisticas()
        {
            dtpEstadisticas.ColorFondo = Tema.PanelEst;
            dtpEstadisticas.ColorTexto = Tema.GrisTexto;
            dtpEstadisticas.ColorBorde = Color.FromArgb(120, 30, 20);
            dtpEstadisticas.ColorFlecha = Tema.RojoClaro;
            dtpEstadisticas.ColorDiasOtroMes = Tema.RojoClaro;
            dtpEstadisticas.ColorTituloFondo = Tema.RojoClaro;
            dtpEstadisticas.ColorTituloTexto = Tema.PanelEst;
        }
    }
}
