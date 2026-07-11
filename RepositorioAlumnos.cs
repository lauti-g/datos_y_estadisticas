using Microsoft.Data.Sqlite; // herramientas para hablar con SQLite
using System;
using System.Collections.Generic;
using System.IO;

namespace datos_y_estadisticas
{
    // Única clase que sabe cómo guardar y leer de la base.
    // El resto del programa le pide datos sin saber qué pasa por dentro.
    public class RepositorioAlumnos
    {
        // Formato de fecha que uso en la base: "2026-06-17".
        // Guardar las fechas como texto ISO (año-mes-día) permite compararlas y ordenarlas
        // directamente como string, sin ambigüedades de día/mes.
        private const string FormatoFecha = "yyyy-MM-dd";

        // La "cadena de conexión" le dice a SQLite QUÉ archivo usar.
        private readonly string _cadenaConexion;

        public RepositorioAlumnos()
        {
            // Carpeta donde corre el .exe + nombre del archivo de base.
            string ruta = Path.Combine(Application.StartupPath, "alumnos.db");
            // Si el archivo no existe, SQLite lo crea solo la primera vez.
            _cadenaConexion = "Data Source=" + ruta;
        }

        // Crea la tabla si todavía no existe. Se llama una vez al arrancar.
        public void CrearTablaSiNoExiste()
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            // Columnas: las del alumno + Fecha (texto ISO) para poder filtrar y armar estadísticas.
            comando.CommandText = @"
                CREATE TABLE IF NOT EXISTS Alumnos (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre        TEXT,
                    Edad          INTEGER,
                    Celular       TEXT,
                    Localidad     TEXT,
                    MotivoIngreso TEXT,
                    Fecha         TEXT
                )";
            comando.ExecuteNonQuery();
        }

        // Trae los alumnos cargados en UN día concreto (para mostrar en la grilla).
        public List<Alumno> ObtenerPorFecha(DateTime fecha)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT Id, Nombre, Edad, Celular, Localidad, MotivoIngreso, Fecha
                FROM Alumnos
                WHERE Fecha = $fecha
                ORDER BY Id";
            comando.Parameters.AddWithValue("$fecha", fecha.ToString(FormatoFecha));

            return LeerAlumnos(comando);
        }

        // Trae los alumnos cargados ENTRE dos fechas (inclusive), para semana/mes en estadísticas.
        public List<Alumno> ObtenerEntreFechas(DateTime desde, DateTime hasta)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            // BETWEEN funciona perfecto porque las fechas son texto ISO ordenable.
            comando.CommandText = @"
                SELECT Id, Nombre, Edad, Celular, Localidad, MotivoIngreso, Fecha
                FROM Alumnos
                WHERE Fecha BETWEEN $desde AND $hasta
                ORDER BY Fecha, Id";
            comando.Parameters.AddWithValue("$desde", desde.ToString(FormatoFecha));
            comando.Parameters.AddWithValue("$hasta", hasta.ToString(FormatoFecha));

            return LeerAlumnos(comando);
        }


        // ============================================================
        //  OBTENER TODOS — todos los alumnos de la base, sin filtro de fecha
        // ============================================================
        // Devuelve TODAS las filas de la tabla Alumnos, de todas las fechas.
        // La pestaña Estadísticas la usa con el botón "Mostrar todo" para armar las
        // estadísticas globales (sin importar el día / semana / mes elegido).
        //
        // Ojo: este SELECT NO tiene WHERE, así que trae la tabla entera. Por eso es el
        // único de los "Obtener..." que no recibe fechas como parámetro. Igual ordeno por
        // Fecha y después por Id para que el resultado salga prolijo y siempre en el mismo orden.
        public List<Alumno> ObtenerTodos()
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT Id, Nombre, Edad, Celular, Localidad, MotivoIngreso, Fecha
                FROM Alumnos
                ORDER BY Fecha, Id";
            return LeerAlumnos(comando);
        }

        // Guarda lo de UN día: borra solo las filas de esa fecha y reinserta las nuevas.
        // IMPORTANTE: borra únicamente la fecha que se está editando, así NO se pierden
        // los datos de los otros días (incluidas fechas pasadas).
        public void GuardarDelDia(DateTime fecha, List<Alumno> alumnos)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            // Una transacción: o se aplica todo, o no se aplica nada (evita dejar la base a medias).
            using var transaccion = conexion.BeginTransaction();

            // 1) Borro lo que había guardado para ESE día.
            var comandoBorrar = conexion.CreateCommand();
            comandoBorrar.CommandText = "DELETE FROM Alumnos WHERE Fecha = $fecha";
            comandoBorrar.Parameters.AddWithValue("$fecha", fecha.ToString(FormatoFecha));
            comandoBorrar.ExecuteNonQuery();

            // 2) Inserto cada alumno de la grilla con esa fecha.
            foreach (Alumno alumno in alumnos)
            {
                var comandoInsertar = conexion.CreateCommand();
                comandoInsertar.CommandText = @"
                    INSERT INTO Alumnos (Nombre, Edad, Celular, Localidad, MotivoIngreso, Fecha)
                    VALUES ($nombre, $edad, $celular, $localidad, $motivo, $fecha)";
                comandoInsertar.Parameters.AddWithValue("$nombre", alumno.Nombre);
                comandoInsertar.Parameters.AddWithValue("$edad", alumno.Edad);
                comandoInsertar.Parameters.AddWithValue("$celular", alumno.Celular);
                comandoInsertar.Parameters.AddWithValue("$localidad", alumno.Localidad);
                comandoInsertar.Parameters.AddWithValue("$motivo", alumno.MotivoIngreso);
                comandoInsertar.Parameters.AddWithValue("$fecha", fecha.ToString(FormatoFecha));
                comandoInsertar.ExecuteNonQuery();
            }

            // 3) Confirmo los cambios.
            transaccion.Commit();
        }

        // ============================================================
        //  ACTUALIZAR VARIOS — guardar cambios de alumnos EXISTENTES, cada uno en su día
        // ============================================================
        // Es el "guardar" del MODO BÚSQUEDA de la pestaña Datos. Recibe alumnos que YA
        // están en la base (todos con su Id) y sobreescribe sus datos con un UPDATE por
        // Id: la versión anterior de cada alumno queda REEMPLAZADA por la nueva en su
        // misma fila, o sea que cada uno sigue guardado en su fecha original.
        //
        // ¿Por qué NO reutilizar GuardarDelDia (el método de arriba) agrupando por fecha?
        // Porque GuardarDelDia es DESTRUCTIVO: borra el día COMPLETO y reinserta lo que
        // le pasen. En la grilla normal eso está bien, porque ahí la grilla ES la foto
        // completa de ese día. Pero una búsqueda trae solo ALGUNOS alumnos de cada día
        // (los que coincidieron con el texto): borrar el día entero y reinsertar solo a
        // esos haría DESAPARECER de la base a los compañeros que no coincidieron.
        // El UPDATE, en cambio, toca únicamente las filas cuyos Id le pasamos.
        //
        // Detalles finos:
        //   - El SET no incluye la columna Fecha: lo que no se nombra en un UPDATE queda
        //     como está. Así este método NO PUEDE mover a un alumno de día por accidente
        //     (para mover de día ya existe MoverAlumnoAFecha, que hace justo lo inverso:
        //     toca SOLO la fecha y nada más).
        //   - Todo corre dentro de UNA transacción: o se actualizan todos, o ninguno.
        //     Si algo fallara a mitad de camino, la base no queda a medias.
        //   - Los valores viajan como parámetros ($nombre, $id, ...) y nunca pegados en
        //     el texto del SQL: misma regla de todo el repositorio contra la inyección.
        public void ActualizarAlumnos(List<Alumno> alumnos)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            // La transacción agrupa TODOS los UPDATE en una sola operación atómica.
            using var transaccion = conexion.BeginTransaction();

            foreach (Alumno alumno in alumnos)
            {
                var comando = conexion.CreateCommand();
                // WHERE Id = $id -> se actualiza EXACTAMENTE una fila: la de ese alumno.
                comando.CommandText = @"
                    UPDATE Alumnos
                    SET Nombre        = $nombre,
                        Edad          = $edad,
                        Celular       = $celular,
                        Localidad     = $localidad,
                        MotivoIngreso = $motivo
                    WHERE Id = $id";
                comando.Parameters.AddWithValue("$nombre", alumno.Nombre);
                comando.Parameters.AddWithValue("$edad", alumno.Edad);
                comando.Parameters.AddWithValue("$celular", alumno.Celular);
                comando.Parameters.AddWithValue("$localidad", alumno.Localidad);
                comando.Parameters.AddWithValue("$motivo", alumno.MotivoIngreso);
                comando.Parameters.AddWithValue("$id", alumno.Id);
                comando.ExecuteNonQuery();
            }

            // Recién acá los cambios quedan firmes en el archivo .db.
            transaccion.Commit();
        }





        // ============================================================
        // Autocompletado de localidades
        // ============================================================
        // Devuelve la lista de localidades DISTINTAS que ya están cargadas en la base,
        // ordenadas alfabéticamente. La pestaña Datos la usa para sugerir mientras se escribe.
        //
        // SELECT DISTINCT  => no repite localidades aunque aparezcan en muchos alumnos.
        // El WHERE descarta nulos y vacíos (TRIM saca espacios) para no sugerir basura.
        public List<string> ObtenerLocalidades()
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT DISTINCT Localidad
                FROM Alumnos
                WHERE Localidad IS NOT NULL AND TRIM(Localidad) <> ''
                ORDER BY Localidad";

            var localidades = new List<string>();
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                localidades.Add(lector.GetString(0)); // columna 0 = Localidad
            }
            return localidades;
        }

        // ============================================================
        // Renombrar un motivo viejo en toda la base
        // ============================================================
        // Cambia TODAS las filas que tengan el motivo 'viejo' por el motivo 'nuevo'.
        // Se usa una sola vez para migrar los registros antiguos que decían "Amigos"
        // al nuevo nombre "Recomendacion".
        //
        // Es seguro llamarlo siempre al arrancar: si ya no queda ningún "Amigos",
        // el UPDATE afecta 0 filas y no hace nada (no rompe ni duplica datos).
        public void RenombrarMotivo(string viejo, string nuevo)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = "UPDATE Alumnos SET MotivoIngreso = $nuevo WHERE MotivoIngreso = $viejo";
            comando.Parameters.AddWithValue("$nuevo", nuevo);
            comando.Parameters.AddWithValue("$viejo", viejo);
            comando.ExecuteNonQuery();
        }

        // ============================================================
        //  BÚSQUEDA — buscar alumnos por nombre, localidad o motivo
        // ============================================================
        // Devuelve TODOS los alumnos (sin importar la fecha) cuyo Nombre, Localidad
        // o MotivoIngreso contengan el texto buscado. La pestaña Datos la usa cuando
        // se aprieta el botón "Buscar".
        //
        // ¿Cómo funciona el filtro?
        //   - LIKE $patron  => compara la columna contra un patrón con comodines.
        //   - "%" + texto + "%"  => el "%" significa "cualquier cosa antes y después".
        //     Así, buscar "isi" encuentra "San Isidro" (coincidencia PARCIAL, no exacta).
        //   - Uso OR entre las tres columnas: alcanza con que coincida UNA de ellas.
        //
        // Detalle fino (para que lo tengas en el radar): el LIKE de SQLite ignora
        // mayúsculas/minúsculas SOLO en letras sin acento (a-z). O sea, "san" encuentra
        // "San", pero "MARTIN" no necesariamente encuentra "Martín" por la tilde.
        // Para una app de este tamaño alcanza; si algún día querés que ignore acentos,
        // se puede normalizar el texto antes de comparar.
        public List<Alumno> BuscarAlumnos(string texto)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
                SELECT Id, Nombre, Edad, Celular, Localidad, MotivoIngreso, Fecha
                FROM Alumnos
                WHERE Nombre        LIKE $patron
                   OR Localidad     LIKE $patron
                   OR MotivoIngreso LIKE $patron
                ORDER BY Fecha, Nombre";
            // Un solo parámetro reutilizado en las tres columnas: el patrón con comodines.
            comando.Parameters.AddWithValue("$patron", "%" + texto + "%");

            return LeerAlumnos(comando);
        }

        // ============================================================
        //  REINSERTAR — mover un alumno a otra fecha (la de hoy)
        // ============================================================
        // Cambia la Fecha de UN alumno (identificado por su Id) a la fecha indicada.
        // La pestaña Datos la usa con el botón "Reinsertar": agarra un alumno de los
        // resultados de búsqueda y lo "trae" al día de hoy.
        //
        // ¿Por qué un UPDATE y no un DELETE + INSERT?
        //   El registro YA existe en la base; lo único que cambia es a qué día pertenece.
        //   Con un UPDATE de la columna Fecha logramos las dos cosas que pedimos de una sola
        //   vez y sin riesgo: deja de pertenecer a su fecha vieja y pasa a la nueva. Además,
        //   al ser UNA sola instrucción, es atómica (no puede quedar a medias) y conserva el
        //   mismo Id y el resto de los datos del alumno.
        //
        // El Id viaja como parámetro ($id): nunca se pega directo en el texto del SQL,
        // así evitamos inyección de SQL (misma regla que usamos en todo el repositorio).
        public void MoverAlumnoAFecha(int id, DateTime nuevaFecha)
        {
            using var conexion = new SqliteConnection(_cadenaConexion);
            conexion.Open();

            var comando = conexion.CreateCommand();
            comando.CommandText = "UPDATE Alumnos SET Fecha = $fecha WHERE Id = $id";
            comando.Parameters.AddWithValue("$fecha", nuevaFecha.ToString(FormatoFecha));
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }

        // Método interno: recorre el resultado de un SELECT y arma la lista de Alumno.
        // Lo comparten ObtenerPorFecha, ObtenerEntreFechas y BuscarAlumnos para no repetir código.
        private static List<Alumno> LeerAlumnos(SqliteCommand comando)
        {
            var alumnos = new List<Alumno>();
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                var alumno = new Alumno
                {
                    Id = lector.GetInt32(0),
                    Nombre = lector.GetString(1),
                    Edad = lector.GetInt32(2),
                    Celular = lector.GetString(3),
                    Localidad = lector.GetString(4),
                    MotivoIngreso = lector.GetString(5),
                    // La fecha viene como texto ISO; la vuelvo a DateTime.
                    Fecha = DateTime.ParseExact(lector.GetString(6), FormatoFecha, null)
                };
                alumnos.Add(alumno);
            }
            return alumnos;
        }
    }
}