using System.Globalization;

namespace datos_y_estadisticas.UI
{
    // ============================================================
    //  SELECTOR DE MODO DE LAS ESTADÍSTICAS (Semana / Mes / 3 meses / Todo)
    // ============================================================
    //
    // Es la "máquina de estados" de los botones de la pestaña Estadísticas:
    // el estado es el MODO activo (uno solo por vez) y las transiciones las
    // disparan los clics en los botones, que funcionan como interruptores
    // (toggle) EXCLUYENTES: prender uno apaga a los demás, y volver a tocar
    // el que está prendido lo apaga (se vuelve al modo "día").
    //
    // Esta clase concentra TODO lo que antes estaba repartido en FormPrincipal:
    //   1) El modo activo (el enum de abajo).
    //   2) Los clics de los cuatro botones (se suscribe sola en el constructor,
    //      por eso ya no hace falta cablearlos en el diseñador).
    //   3) El estilo visual de los botones (el activo se ve "presionado").
    //   4) El CÁLCULO del rango de fechas de cada modo (ObtenerRango), que es
    //      la parte con más matemática de calendario.
    //
    // El formulario solo se suscribe al evento ModoCambiado para redibujar
    // los gráficos, y llama a ObtenerRango cuando necesita saber qué período
    // pedirle a la base.
    //
    // ¿Por qué acá NO hay variables que "recuerden" el rango mostrado?
    // Porque serían información DUPLICADA: con el modo activo + la fecha del
    // selector, ObtenerRango ya sabe reconstruir exactamente la vista actual.
    // Tener UNA sola fuente de la verdad evita que las copias se desincronicen.

    // Modo de visualización: un día, la semana, el mes, los últimos 3 meses,
    // o TODA la base (sin filtrar por fecha).
    public enum ModoEstadistica { Dia, Semana, Mes, TresMeses, Todo }

    public class SelectorModoEstadistica
    {
        private readonly Button btnSemana;
        private readonly Button btnMes;
        private readonly Button btnTresMeses;
        private readonly Button btnTodo;

        // El modo activo. 'private set': solo se cambia con Alternar (los clics).
        public ModoEstadistica Modo { get; private set; } = ModoEstadistica.Dia;

        // Se dispara cada vez que cambia el modo (el formulario redibuja ahí).
        public event Action? ModoCambiado;

        public SelectorModoEstadistica(Button btnSemana, Button btnMes, Button btnTresMeses, Button btnTodo)
        {
            this.btnSemana = btnSemana;
            this.btnMes = btnMes;
            this.btnTresMeses = btnTresMeses;
            this.btnTodo = btnTodo;

            // Cada botón alterna su propio modo. Como los cuatro clics hacen lo
            // mismo cambiando solo el modo, con una lambda por botón alcanza
            // (antes eran cuatro métodos casi idénticos en FormPrincipal).
            btnSemana.Click += (s, e) => Alternar(ModoEstadistica.Semana);
            btnMes.Click += (s, e) => Alternar(ModoEstadistica.Mes);
            btnTresMeses.Click += (s, e) => Alternar(ModoEstadistica.TresMeses);
            btnTodo.Click += (s, e) => Alternar(ModoEstadistica.Todo);

            // Estilo inicial (todos apagados, porque se arranca en modo "día").
            ActualizarBotones();
        }

        // El interruptor: si me piden el modo que YA está activo, lo apago
        // (vuelvo a "día"); si no, lo prendo (y eso apaga al anterior).
        public void Alternar(ModoEstadistica modo)
        {
            Modo = (Modo == modo) ? ModoEstadistica.Dia : modo;
            ActualizarBotones();
            ModoCambiado?.Invoke();
        }

        // ============================================================
        //  CÁLCULO DEL RANGO DE FECHAS DE CADA MODO
        // ============================================================

        // Arma el período que corresponde al modo activo, tomando 'fechaBase'
        // (la fecha elegida en el selector) como referencia.
        //
        // Devuelve true y deja el rango en 'desde' / 'hasta' para los modos que
        // filtran por fecha. Devuelve FALSE para el modo "Todo", que no tiene
        // rango: ahí hay que pedirle a la base todos los alumnos sin filtrar.
        // En ambos casos 'descripcion' queda con el texto para los títulos.
        public bool ObtenerRango(DateTime fechaBase, out DateTime desde, out DateTime hasta, out string descripcion)
        {
            DateTime d = fechaBase.Date;

            if (Modo == ModoEstadistica.Semana)
            {
                // Queremos lunes = 0 ... domingo = 6 para saber cuántos días retroceder al lunes.
                // ((int)DayOfWeek + 6) % 7 transforma la numeración de .NET (domingo = 0) a la nuestra.
                // Ej.: miércoles (.NET = 3) -> (3 + 6) % 7 = 2 -> retrocedo 2 días y caigo en lunes.
                int desdeLunes = ((int)d.DayOfWeek + 6) % 7;
                desde = d.AddDays(-desdeLunes);       // lunes
                hasta = desde.AddDays(6);             // domingo
                descripcion = $"Semana {desde:dd/MM} al {hasta:dd/MM}";
                return true;
            }

            if (Modo == ModoEstadistica.Mes)
            {
                desde = new DateTime(d.Year, d.Month, 1);          // día 1 del mes
                hasta = desde.AddMonths(1).AddDays(-1);            // último día del mes
                string nombreMes = desde.ToString("MMMM yyyy", new CultureInfo("es-AR"));
                descripcion = $"Mes de {nombreMes}";
                return true;
            }

            if (Modo == ModoEstadistica.TresMeses)
            {
                // "Últimos 3 meses" = el mes de la fecha elegida + los DOS meses anteriores, completos.
                // (Igual que "Mes", pero abarcando 3 meses calendario en vez de 1.)
                // Tomo el día 1 del mes elegido como ancla:
                DateTime primeroMesElegido = new DateTime(d.Year, d.Month, 1);
                // El inicio del rango es el día 1, pero dos meses atrás. AddMonths(-2) maneja
                // solo el cambio de año (ej.: enero - 2 meses => noviembre del año anterior).
                desde = primeroMesElegido.AddMonths(-2);
                // El final es el último día del mes elegido: mes siguiente, día 1, menos 1 día.
                hasta = primeroMesElegido.AddMonths(1).AddDays(-1);
                descripcion = $"Últimos 3 meses ({desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy})";
                return true;
            }

            if (Modo == ModoEstadistica.Todo)
            {
                // Modo "todo": no hay rango; se ignora la fecha del selector.
                // Los out hay que asignarlos igual (lo exige el compilador),
                // pero quien llama NO debe usarlos: el false le avisa que acá
                // corresponde pedir TODOS los alumnos.
                desde = DateTime.MinValue;
                hasta = DateTime.MaxValue;
                descripcion = "Todos los alumnos";
                return false;
            }

            // ModoEstadistica.Dia: el rango es un solo día (desde = hasta).
            desde = d;
            hasta = d;
            descripcion = (d == DateTime.Today) ? "Hoy" : d.ToString("dd/MM/yyyy");
            return true;
        }

        // ============================================================
        //  ESTILO VISUAL DE LOS BOTONES
        // ============================================================

        // Resalta el botón del modo activo y "apaga" los demás (feedback visual del toggle).
        private void ActualizarBotones()
        {
            EstiloBotonModo(btnSemana, Modo == ModoEstadistica.Semana);
            EstiloBotonModo(btnMes, Modo == ModoEstadistica.Mes);
            EstiloBotonModo(btnTresMeses, Modo == ModoEstadistica.TresMeses);
            EstiloBotonModo(btnTodo, Modo == ModoEstadistica.Todo);
        }

        // Estilo de un botón de modo según esté activo (encendido) o no.
        private static void EstiloBotonModo(Button btn, bool activo)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.UseVisualStyleBackColor = false;
            btn.Font = new Font("Segoe UI Semibold", 9.5F);
            btn.ForeColor = Color.White;

            if (activo)
            {
                // Activo: rojo más vivo + borde dorado grueso => se ve "presionado".
                btn.BackColor = Tema.FondoBtnSel;
                btn.FlatAppearance.BorderColor = Tema.BordeSelec;
                btn.FlatAppearance.BorderSize = 2;
            }
            else
            {
                // Inactivo: rojo apagado, borde fino oscuro.
                btn.BackColor = Tema.Rojo;
                btn.FlatAppearance.BorderColor = Color.FromArgb(120, 30, 20);
                btn.FlatAppearance.BorderSize = 1;
            }
        }
    }
}
