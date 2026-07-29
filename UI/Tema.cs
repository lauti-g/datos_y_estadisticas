using System.Drawing;

namespace datos_y_estadisticas.UI
{
    // Paleta de colores centralizada para mantener una estética coherente y elegante.
    // Todo es oscuro. La pestaña "Datos" usa azul + dorado + blanco;
    // la pestaña "Estadísticas" usa rojo + escalas de negro.
    // Tener los colores en un solo lugar permite retocar la estética sin tocar la lógica.
    public static class Tema
    {
        // -------- DATOS (azul / dorado / blanco) --------
        public static readonly Color FondoDatos   = Color.FromArgb(13, 27, 42);    // azul noche profundo
        public static readonly Color PanelDatos    = Color.FromArgb(27, 38, 59);   // azul un poco más claro (paneles)
        public static readonly Color AzulAccent    = Color.FromArgb(46, 90, 136);  // azul de botones y encabezados
        public static readonly Color AzulBorde     = Color.FromArgb(65, 90, 119);  // líneas de la grilla
        public static readonly Color Dorado        = Color.FromArgb(201, 162, 39); // dorado de acentos y texto destacado
        public static readonly Color Blanco        = Color.FromArgb(224, 225, 221);// texto principal
        public static readonly Color FilaAlterna   = Color.FromArgb(22, 33, 52);   // fondo de filas alternas

        // -------- ESTADÍSTICAS (rojo / negro) --------
        public static readonly Color FondoEst      = Color.FromArgb(20, 20, 20);   // casi negro
        public static readonly Color PanelEst      = Color.FromArgb(32, 32, 32);   // gris muy oscuro (paneles)
        public static readonly Color Rojo          = Color.FromArgb(192, 57, 43);  // rojo principal
        public static readonly Color RojoClaro     = Color.FromArgb(231, 76, 60);  // rojo de resalte
        public static readonly Color GrisTexto     = Color.FromArgb(236, 236, 236);// texto claro sobre negro
        public static readonly Color BordeSelec    = Color.FromArgb(15, 120, 69);  // verde contraste con rojo
        public static readonly Color FondoBtnSel   = Color.FromArgb(12, 63, 24);// verde mas oscuro para fondo




        // -------- COMÚN --------
        public static readonly Color CeldaCorrupta = Color.FromArgb(192, 57, 43);  // rojo para marcar datos mal cargados
        public static readonly Color TextoCorrupto = Color.White;


    }
}
