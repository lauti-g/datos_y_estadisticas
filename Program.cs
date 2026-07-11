namespace datos_y_estadisticas
{
    internal static class Program
    {
        /// <summary>
        ///  Punto de entrada de la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Inicializa la configuración de WinForms (DPI, fuente por defecto, etc.).
            ApplicationConfiguration.Initialize();

            // Arranca la ventana principal.
            Application.Run(new Form1());
        }
    }
}
