using System.Globalization;
using System.Linq;

namespace datos_y_estadisticas
{
    // Reúne en un solo lugar las reglas de validación y normalización de los datos.
    // Así la grilla, el guardado y el marcado de errores usan EXACTAMENTE la misma lógica.
    public static class Validaciones
    {
        // ---------- EDAD ----------
        // Límites permitidos. Un ÚNICO lugar para la regla y para los mensajes:
        // si cambiás el máximo acá, el chequeo y el texto del error se actualizan solos.
        public const int EdadMinima = 2;
        public const int EdadMaxima = 120;

        // Devuelve un mensaje de error ESPECÍFICO según qué esté mal, o null si la edad es válida.
        // Deja la edad ya parseada en 'edad' (0 si no se pudo).
        public static string? ErrorDeEdad(string? texto, out int edad)
        {
            edad = 0;

            // 1) Vacío.
            if (string.IsNullOrWhiteSpace(texto))
                return "Falta la edad.";

            texto = texto.Trim();

            // 2) Tiene letras o símbolos (algo que no sea dígito).
            if (!texto.All(char.IsDigit))
                return "La edad debe contener solo números.";

            // 3) Son tantos dígitos que no entran en un int (número gigante).
            if (!int.TryParse(texto, out edad))
                return $"La edad máxima es {EdadMaxima}.";

            // 4) Demasiado chica (0 o menos).
            if (edad < EdadMinima)
                return $"La edad mínima es {EdadMinima}.";

            // 5) Demasiado grande (ej.: 151).
            if (edad > EdadMaxima)
                return $"La edad máxima es {EdadMaxima}.";

            return null; // válida
        }

        // Versión corta (true/false) que reutiliza EXACTAMENTE la misma regla.
        // La usa el guardado, que solo necesita saber si entra o no.
        public static bool EsEdadValida(string? texto, out int edad)
            => ErrorDeEdad(texto, out edad) == null;

        // ---------- CELULAR ----------
        // Normaliza cualquier número al formato celular "(11 XXXX-XXXX)".
        // Reglas:
        //   - Se ignoran espacios, guiones, paréntesis: solo importan los dígitos.
        //   - Si quedan 10 dígitos (incluye el código de área 11), se usan los últimos 8.
        //   - Si quedan 8 dígitos, se asume el área 11.
        //   - Cualquier otra cantidad => inválido (faltan o sobran números).
        // Devuelve true y deja el número ya formateado en 'formateado'.
        public static bool TryFormatearCelular(string? texto, out string formateado)
        {
            formateado = "";
            if (string.IsNullOrWhiteSpace(texto)) return false;

            // Me quedo únicamente con los dígitos.
            string digitos = new string(texto.Where(char.IsDigit).ToArray());

            // Si vienen 10 dígitos (área + número), descarto el área y me quedo con los 8 finales.
            if (digitos.Length == 10) digitos = digitos.Substring(2);

            // A esta altura tienen que ser exactamente 8 dígitos.
            if (digitos.Length != 8) return false;

            // Armo el formato "(11 XXXX-XXXX)".
            formateado = $"(11 {digitos.Substring(0, 4)}-{digitos.Substring(4, 4)})";
            return true;
        }

        // ---------- CAPITALIZAR (nombre y localidad) ----------
        // Pone la primera letra de cada palabra en mayúscula y el resto en minúscula.
        // Ejemplos: "vicente lOPEZ" => "Vicente Lopez"   |   "juan PÉREZ" => "Juan Pérez".
        // El resultado NO depende de cómo lo haya tipeado el usuario (todo en mayúsculas,
        // todo en minúsculas, mezclado: siempre queda parejo).
        //
        // Antes este método se llamaba "CapitalizarLocalidad". Como el nombre del alumno
        // necesita EXACTAMENTE el mismo tratamiento, lo dejamos genérico ("Capitalizar")
        // y lo usamos para los dos campos. Así la regla vive en un solo lugar (principio DRY:
        // "Don't Repeat Yourself" = no repetir la misma lógica copiada en varios lados).
        public static string Capitalizar(string? texto)
        {
            // Si viene vacío o solo espacios, devuelvo cadena vacía (no hay nada que capitalizar).
            if (string.IsNullOrWhiteSpace(texto)) return "";

            // ToTitleCase("HOLA") NO corrige las mayúsculas: deja "HOLA" igual.
            // Por eso primero paso TODO a minúscula con ToLowerInvariant() y recién ahí
            // ToTitleCase pone bien la inicial de cada palabra.
            return CultureInfo.GetCultureInfo("es-AR")
                              .TextInfo
                              .ToTitleCase(texto.Trim().ToLowerInvariant());
        }
    }
}