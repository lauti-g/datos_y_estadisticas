using ClosedXML.Excel;          // librería para leer/escribir Excel (.xlsx). Se instala con NuGet.
using datos_y_estadisticas.Logica;  // las reglas de Validaciones (capitalizar, formatear celular)
using datos_y_estadisticas.Modelos; // la clase Alumno
using System;
using System.Collections.Generic;

namespace datos_y_estadisticas.Datos
{
    // Clase dedicada a UNA sola cosa: leer un archivo Excel y devolver la lista de
    // alumnos que contiene. No toca la base ni la interfaz; solo "traduce" el Excel
    // a objetos Alumno. Mantenerla separada (igual que RepositorioAlumnos, Validaciones
    // o Tema) hace que sea fácil de entender, probar y cambiar sin tocar el resto.
    //
    // Es 'static' porque no guarda estado: es solo una función de ayuda. No hace falta
    // crear un objeto ImportadorExcel; se llama directo: ImportadorExcel.LeerDesdeArchivo(...).
    public static class ImportadorExcel
    {
        // ---- Posición de cada dato en el Excel (1 = columna A, 2 = B, 3 = C...) ----
        // Las dejo como constantes con nombre, igual que las COL_* de la grilla en FormPrincipal,
        // para no usar números sueltos y para que cambiar el orden sea trivial.
        // OJO: este orden DEBE coincidir con cómo está armado tu Excel. Si tu archivo
        // tiene las columnas en otro orden (o columnas de más en el medio), ajustá SOLO
        // estos números y el resto del código sigue funcionando igual.
        private const int COL_NOMBRE = 1; // A
        private const int COL_EDAD = 2; // B
        private const int COL_CELULAR = 3; // C
        private const int COL_LOCALIDAD = 4; // D
        private const int COL_MOTIVO = 5; // E

        private const bool TieneEncabezado = true;

        // ============================================================
        //  FLUJO COMPLETO DE IMPORTACIÓN (lo que dispara el botón)
        // ============================================================
        // Maneja TODO el ida y vuelta con el usuario para importar un Excel:
        // abre el cuadro de "elegir archivo", lee los alumnos y muestra los
        // carteles de error o de "no había nada". Antes todo esto vivía en el
        // clic del botón en FormPrincipal; ahora el formulario solo hace:
        //
        //     var importados = ImportadorExcel.ImportarConDialogo(this);
        //     if (importados == null) return;   // canceló, falló o venía vacío
        //     ...meter las filas en la grilla...
        //
        // Devuelve la lista de alumnos leídos, o NULL si no hay nada que
        // insertar (el motivo ya se le avisó al usuario con su cartel).
        // 'dueño' es la ventana sobre la que se centran el diálogo y los
        // carteles (el formulario principal).
        public static List<Alumno>? ImportarConDialogo(IWin32Window dueño)
        {
            // OpenFileDialog = el cuadro nativo de Windows para elegir un archivo.
            // El 'using' lo libera solo al terminar.
            using var dialogo = new OpenFileDialog
            {
                Title = "Elegí el archivo de alumnos",
                // El Filter limita lo que se muestra. Formato: "Texto visible|patrón".
                // Sumo .xlsm por si el archivo tiene macros (ClosedXML también lo lee).
                Filter = "Archivos Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm"
            };

            // ShowDialog() abre la ventana y ESPERA. Devuelve OK si el usuario eligió
            // un archivo y aceptó. Si canceló o cerró, corto acá sin hacer nada.
            if (dialogo.ShowDialog(dueño) != DialogResult.OK)
                return null;

            // Envuelvo la lectura en try/catch: el Excel puede estar corrupto, con
            // un formato raro, o abierto en otra ventana de Excel (y entonces queda
            // bloqueado). Mejor avisar con un cartel claro que dejar que la app se
            // cierre sola.
            try
            {
                // Leo el Excel y obtengo la lista de alumnos (con la fecha de hoy).
                List<Alumno> importados = LeerDesdeArchivo(dialogo.FileName, DateTime.Today);

                // Si el archivo no tenía ninguna fila aprovechable, aviso y corto.
                if (importados.Count == 0)
                {
                    MessageBox.Show(dueño,
                        "El archivo no tenía filas para importar.\n" +
                        "Revisá que los datos estén en la primera hoja y que el orden de columnas coincida.",
                        "Nada para importar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }

                return importados;
            }
            catch (Exception ex)
            {
                // ex.Message trae el detalle (archivo bloqueado, formato inválido, etc.).
                MessageBox.Show(dueño,
                    $"No se pudo importar el archivo:\n\n{ex.Message}",
                    "Error al importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Lee TODO el archivo y devuelve los alumnos listos para insertar en la base.
        //
        // 'fechaParaTodos' es la fecha que se le asigna a CADA alumno importado (le
        // pasamos la de hoy desde el botón). Recordá que a este proyecto le da igual
        // que queden todos cargados el mismo día.
        public static List<Alumno> LeerDesdeArchivo(string rutaArchivo, DateTime fechaParaTodos)
        {
            // La lista que vamos llenando fila por fila y devolvemos al final.
            var alumnos = new List<Alumno>();

            // XLWorkbook representa el archivo Excel completo (el "libro"). El 'using'
            // lo cierra y libera solo al terminar el bloque; si no, el archivo queda
            // "tomado" por la app y el usuario no lo puede abrir/mover hasta cerrar el programa.
            using (var libro = new XLWorkbook(rutaArchivo))
            {
                // Tomo la PRIMERA hoja (la primera pestaña de abajo en Excel).
                // Si tus datos están en una hoja con nombre, usá libro.Worksheet("NombreHoja").
                var hoja = libro.Worksheet(1);

                // RowsUsed() devuelve SOLO las filas que tienen contenido; ignora el
                // millón de filas vacías que un Excel arrastra por defecto. Así no
                // recorremos de gusto miles de renglones en blanco.
                foreach (var fila in hoja.RowsUsed())
                {
                    if (TieneEncabezado && fila.RowNumber() == 1)
                        continue; 

                    // Leo cada celda como texto, ya sin espacios sobrantes (.Trim()),
                    // que en datos cargados a mano aparecen muchísimo.
                    // GetString() nunca devuelve null: si la celda está vacía, devuelve "".
                    string nombre = fila.Cell(COL_NOMBRE).GetString().Trim();
                    string edadTxt = fila.Cell(COL_EDAD).GetString().Trim();
                    string celular = fila.Cell(COL_CELULAR).GetString().Trim();
                    string localidad = fila.Cell(COL_LOCALIDAD).GetString().Trim();
                    string motivo = fila.Cell(COL_MOTIVO).GetString().Trim();

                    // Si la fila no tiene ni nombre, la considero basura / renglón en
                    // blanco del medio y la salteo (no quiero alumnos vacíos en la base).
                    if (string.IsNullOrWhiteSpace(nombre))
                        continue;

                    // --- Normalizo IGUAL que la carga manual, para que los datos
                    //     importados queden con el mismo formato que los tipeados a mano. ---

                    // Nombre y localidad capitalizados ("juan perez" -> "Juan Perez").
                    // Reuso Validaciones.Capitalizar, la MISMA función que usa la grilla,
                    // así no hay dos reglas distintas de capitalización dando vueltas.
                    nombre = Validaciones.Capitalizar(nombre);
                    if (!string.IsNullOrWhiteSpace(localidad))
                        localidad = Validaciones.Capitalizar(localidad);

                    // Celular: si se puede formatear a "(11 XXXX-XXXX)", lo formateo;
                    // si no se puede (vino incompleto o raro), lo dejo TAL CUAL vino del
                    // Excel. La validación estricta (marcar en rojo) ocurre recién si
                    // después editás y guardás ese día desde la grilla.
                    // 'out string celFormateado' recibe el resultado solo si devolvió true.
                    if (Validaciones.TryFormatearCelular(celular, out string celFormateado))
                    {
                        celular = celFormateado;
                    }


                    // Edad: en el Excel viene como texto. int.TryParse intenta convertir
                    // SIN explotar si está mal escrita o vacía. Si falla, 'edad' queda en 0
                    // y seguimos: no corto toda la importación por una sola fila floja.
                    int.TryParse(edadTxt, out int edad);
                    if (edad <= 0)
                    {
                        edad = 0;
                    }

                    if (motivo != "Redes" && motivo != "Recomendacion" && motivo != "Volantes")
                    {
                        motivo = "Otro";
                    }
                    // Armo el objeto Alumno con los datos ya normalizados y lo sumo a la lista.
                    alumnos.Add(new Alumno
                    {
                        Nombre = nombre,
                        Edad = edad,
                        Celular = celular,
                        Localidad = localidad,
                        MotivoIngreso = motivo,
                        Fecha = fechaParaTodos // a todos la misma fecha (la de hoy)
                    });
                }
            } // acá termina el 'using' -> el archivo Excel se cierra y se libera solo.

            return alumnos;
        }
    }
}