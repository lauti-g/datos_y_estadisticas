using System;

namespace datos_y_estadisticas.Modelos
{
    // Representa una fila de la tabla: un alumno cargado en una fecha determinada.
    // Es el "molde" que viaja entre la grilla, la base de datos y las estadísticas.
    public class Alumno
    {
        // Id de la fila en la base. 0 = todavía no guardado.
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        // La edad ya validada como número entero (la validación se hace antes de crear el objeto).
        public int Edad { get; set; }

        // El celular ya normalizado al formato "(11 XXXX-XXXX)".
        public string Celular { get; set; } = "";

        // La localidad ya capitalizada ("San Isidro", "Vicente Lopez", etc.).
        public string Localidad { get; set; } = "";

        // Cómo se enteró de las clases: "Redes", "Amigos" o "Volantes".
        public string MotivoIngreso { get; set; } = "";

        // Día al que pertenece el dato. Permite filtrar la grilla y armar estadísticas por fecha.
        public DateTime Fecha { get; set; }
    }
}
