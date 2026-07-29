using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace datos_y_estadisticas.UI.Controles
{
    // DateTimePicker con estética propia (oscura), incluido el CALENDARIO desplegable.
    //
    // ¿Por qué hace falta?  El DateTimePicker normal lo dibuja Windows e IGNORA
    // BackColor / ForeColor, y su calendario desplegable usa el tema del sistema
    // (por eso aparece blanco). Acá:
    //   1) El campo lo pintamos nosotros (UserPaint) en OnPaint.
    //   2) Al desplegar, le quitamos el "tema visual" al calendario y le mandamos
    //      sus colores con mensajes nativos (MCM_SETCOLOR), así queda oscuro también.
    //   3) Al sacarle el tema, el calendario necesita otro tamaño; Windows NO reajusta
    //      la ventana solo, así que la agrandamos a su tamaño mínimo real (si no, queda
    //      cortado: se ve medio mes y no aparece el "Hoy" de abajo).
    //
    // Nota: las propiedades de color llevan [Browsable(false)] y [DesignerSerialization-
    // Visibility(Hidden)] para que el DISEÑADOR de Visual Studio no intente serializarlas
    // (las manejamos por código). Sin eso, el diseñador tira un error de serialización.
    public class DateTimePickerOscuro : DateTimePicker
    {
        // ---------- Win32 ----------
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private const int DTM_FIRST = 0x1000;
        private const int DTM_GETMONTHCAL = DTM_FIRST + 8; // handle del calendario

        private const int MCM_FIRST = 0x1000;
        private const int MCM_SETCOLOR = MCM_FIRST + 10;      // setear un color
        private const int MCM_GETMINREQRECT = MCM_FIRST + 9;  // tamaño mínimo necesario

        // Índices de color del MonthCalendar.
        private const int MCSC_BACKGROUND = 0; // fondo general (alrededor del mes)
        private const int MCSC_TEXT = 1; // números de los días
        private const int MCSC_TITLEBK = 2; // fondo del título (mes/año)
        private const int MCSC_TITLETEXT = 3; // texto del título
        private const int MCSC_MONTHBK = 4; // fondo de la grilla de días
        private const int MCSC_TRAILINGTEXT = 5; // días de los meses vecinos

        // Flags de SetWindowPos: no mover, no cambiar orden Z, no activar.
        private const uint SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;

        // COLORREF que espera Win32 es 0x00BBGGRR (no ARGB).
        private static IntPtr Ref(Color c) => (IntPtr)(c.R | (c.G << 8) | (c.B << 16));

        // ---------- Colores configurables (por defecto, paleta azul de "Datos") ----------
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorFondo { get; set; } = Tema.PanelDatos;       // campo + fondo del calendario

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorTexto { get; set; } = Tema.Blanco;           // texto del campo + días

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorBorde { get; set; } = Tema.AzulBorde;        // borde del campo

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorFlecha { get; set; } = Tema.Dorado;          // flechita del campo

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorTituloFondo { get; set; } = Tema.AzulAccent; // barra del mes/año

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorTituloTexto { get; set; } = Tema.Dorado;     // texto del mes/año

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorDiasOtroMes { get; set; } = Tema.AzulBorde;  // días apagados (mes vecino)

        // Margen extra de alto para el calendario (por si el "Hoy" del pie queda justo).
        // Si en alguna PC ves que se corta abajo, subí este número.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MargenAltoCalendario { get; set; } = 6;

        public DateTimePickerOscuro()
        {
            // El dibujo del CAMPO lo hacemos nosotros (OnPaint).
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // Al abrir el calendario: le saco el tema, le aplico mis colores y lo reajusto de tamaño.
        // OnDropDown se dispara cada vez que se despliega (el calendario se recrea).
        protected override void OnDropDown(EventArgs eventargs)
        {
            base.OnDropDown(eventargs);

            IntPtr hCal = SendMessage(Handle, DTM_GETMONTHCAL, IntPtr.Zero, IntPtr.Zero);
            if (hCal == IntPtr.Zero) return;

            // 1) Sin tema visual, el calendario respeta los colores que le mandemos.
            SetWindowTheme(hCal, "", "");

            // 2) Colores.
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_BACKGROUND, Ref(ColorFondo));
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_MONTHBK, Ref(ColorFondo));
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_TEXT, Ref(ColorTexto));
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_TITLEBK, Ref(ColorTituloFondo));
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_TITLETEXT, Ref(ColorTituloTexto));
            SendMessage(hCal, MCM_SETCOLOR, (IntPtr)MCSC_TRAILINGTEXT, Ref(ColorDiasOtroMes));

            // 3) Reajuste de tamaño: pregunto el rectángulo mínimo que necesita el
            //    calendario (ya sin tema) y agrando tanto el calendario como su ventana
            //    contenedora para que se vea el mes completo y el "Hoy".
            RECT rc = new RECT();
            if (SendMessage(hCal, MCM_GETMINREQRECT, IntPtr.Zero, ref rc) != IntPtr.Zero)
            {
                int ancho = rc.Right - rc.Left;
                int alto = rc.Bottom - rc.Top + MargenAltoCalendario;

                SetWindowPos(hCal, IntPtr.Zero, 0, 0, ancho, alto,
                    SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);

                IntPtr contenedor = GetParent(hCal); // la ventana emergente que lo aloja
                if (contenedor != IntPtr.Zero)
                    SetWindowPos(contenedor, IntPtr.Zero, 0, 0, ancho, alto,
                        SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        // Cuando cambia la fecha, redibujo el campo para que se vea el texto nuevo.
        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);
            Invalidate();
        }

        // ---------- Dibujo del campo (lo que se ve cerrado) ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle area = ClientRectangle;

            // 1) Fondo.
            using (var fondo = new SolidBrush(ColorFondo))
                g.FillRectangle(fondo, area);

            // 2) Borde de 1 px.
            using (var lapiz = new Pen(ColorBorde))
                g.DrawRectangle(lapiz, 0, 0, area.Width - 1, area.Height - 1);

            // 3) Zona de la flecha (a la derecha) + triangulito apuntando hacia abajo.
            const int anchoFlecha = 20;
            int xFlecha = area.Width - anchoFlecha;
            int cx = xFlecha + anchoFlecha / 2;
            int cy = area.Height / 2;
            Point[] triangulo =
            {
                new Point(cx - 4, cy - 2),
                new Point(cx + 4, cy - 2),
                new Point(cx,     cy + 3)
            };
            using (var pincelFlecha = new SolidBrush(ColorFlecha))
                g.FillPolygon(pincelFlecha, triangulo);

            // 4) Texto de la fecha. "Text" ya viene formateado según Format / CustomFormat.
            var rectTexto = new Rectangle(area.Left + 8, area.Top,
                                          area.Width - anchoFlecha - 8, area.Height);
            TextRenderer.DrawText(g, Text, Font, rectTexto, ColorTexto,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}