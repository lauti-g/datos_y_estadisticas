namespace datos_y_estadisticas
{
    // ============================================================
    //  VALIDACIÓN Y NORMALIZACIÓN DE LA GRILLA
    // ============================================================
    //
    // Este módulo es la "capa de arriba" de Validaciones.cs:
    //
    //   - Validaciones.cs      = las REGLAS puras de texto (¿es una edad
    //                            válida? ¿cómo se formatea un celular?).
    //                            No sabe nada de WinForms.
    //   - ValidacionGrilla.cs  = APLICA esas reglas sobre el DataGridView:
    //                            recorre filas, pinta de rojo las celdas
    //                            mal cargadas, arma los mensajes de error
    //                            y normaliza (capitaliza, formatea) lo tipeado.
    //
    // Antes todo esto vivía en Form1. Al separarlo, el formulario solo
    // pregunta "¿está todo bien?" y este módulo se encarga del cómo.
    //
    // Es 'static' porque no guarda estado (igual que Validaciones): son
    // funciones de ayuda que reciben la grilla o la fila y trabajan sobre ella.
    public static class ValidacionGrilla
    {
        // ---------- Índices de las columnas de la grilla ----------
        // (Para no usar números "mágicos".) Viven acá porque este módulo es
        // el que más los usa, pero son públicos: el formulario también los
        // necesita (para leer los alumnos, detectar el botón Borrar, etc.).
        // Si algún día cambia el orden de las columnas en el diseñador,
        // se ajusta SOLO acá.
        public const int COL_NOMBRE = 0;
        public const int COL_EDAD = 1;
        public const int COL_CELULAR = 2;
        public const int COL_LOCALIDAD = 3;
        public const int COL_MOTIVO = 4;
        public const int COL_BORRAR = 5;

        // ============================================================
        //  VALIDAR
        // ============================================================

        // Valida TODAS las filas de la grilla y devuelve la lista de errores
        // (vacía si está todo bien). De paso, cada celda queda pintada:
        // rojo con tooltip si está mal, color normal si está bien.
        public static List<string> ValidarTodo(DataGridView tabla)
        {
            var errores = new List<string>();
            foreach (DataGridViewRow fila in tabla.Rows)
            {
                errores.AddRange(ValidarFila(fila));
            }
            return errores;
        }

        // Valida UNA fila, pinta sus celdas (rojo si están mal) y devuelve los errores en texto.
        public static List<string> ValidarFila(DataGridViewRow fila)
        {
            var errores = new List<string>();
            if (fila.IsNewRow) return errores;

            int n = fila.Index + 1; // número de fila para los mensajes (empezando en 1).

            // -- Nombre: no puede estar vacío --
            string nombre = Texto(fila, COL_NOMBRE);
            bool nombreOk = !string.IsNullOrWhiteSpace(nombre);
            MarcarCelda(fila, COL_NOMBRE, nombreOk, "Falta el nombre.");
            if (!nombreOk) errores.Add($"Fila {n}: el Nombre está vacío. Escribí el nombre del alumno.");

            // -- Edad: número entero dentro del rango permitido --
            string edadTxt = Texto(fila, COL_EDAD);
            string? errorEdad = Validaciones.ErrorDeEdad(edadTxt, out _);
            bool edadOk = errorEdad == null;
            MarcarCelda(fila, COL_EDAD, edadOk, errorEdad ?? "");
            if (!edadOk) errores.Add($"Fila {n}: {errorEdad}");

            // -- Celular: formateable a (11 XXXX-XXXX) --
            string celTxt = Texto(fila, COL_CELULAR);
            bool celOk = Validaciones.TryFormatearCelular(celTxt, out _);
            MarcarCelda(fila, COL_CELULAR, celOk, "Celular incompleto o mal escrito.");
            if (!celOk) errores.Add($"Fila {n}: el Celular \"{celTxt}\" está mal. Necesita 8 dígitos (o 10 con el área 11). Ej: 1122223333.");

            // -- Localidad: no puede estar vacía --
            string loc = Texto(fila, COL_LOCALIDAD);
            bool locOk = !string.IsNullOrWhiteSpace(loc);
            MarcarCelda(fila, COL_LOCALIDAD, locOk, "Falta la localidad.");
            if (!locOk) errores.Add($"Fila {n}: la Localidad está vacía. Completala.");

            // -- Motivo: no puede estar vacío --
            string mot = Texto(fila, COL_MOTIVO);
            bool motOk = !string.IsNullOrWhiteSpace(mot);
            MarcarCelda(fila, COL_MOTIVO, motOk, "Elegí un motivo de la lista.");
            if (!motOk) errores.Add($"Fila {n}: falta el Motivo de ingreso. Elegí una opción de la lista.");

            return errores;
        }

        // Pinta una celda: rojo + tooltip con el motivo si está mal; normal si está bien.
        private static void MarcarCelda(DataGridViewRow fila, int col, bool ok, string mensaje)
        {
            var celda = fila.Cells[col];
            if (ok)
            {
                // Color.Empty hace que la celda vuelva a heredar el color normal de la grilla.
                celda.Style.BackColor = Color.Empty;
                celda.Style.ForeColor = Color.Empty;
                celda.Style.SelectionBackColor = Color.Empty;
                celda.ToolTipText = "";
            }
            else
            {
                celda.Style.BackColor = Tema.CeldaCorrupta;
                celda.Style.ForeColor = Tema.TextoCorrupto;
                celda.Style.SelectionBackColor = Tema.CeldaCorrupta;
                celda.ToolTipText = mensaje;
            }
        }

        // ============================================================
        //  NORMALIZAR
        // ============================================================

        // Normaliza toda la grilla (localidad capitalizada + celular formateado).
        public static void NormalizarGrilla(DataGridView tabla)
        {
            foreach (DataGridViewRow fila in tabla.Rows)
            {
                if (!fila.IsNewRow) NormalizarFila(fila);
            }
        }

        // Normaliza UNA fila: capitaliza nombre y localidad, y formatea el celular si se puede.
        public static void NormalizarFila(DataGridViewRow fila)
        {
            // Nombre -> "Juan Perez" (misma regla de capitalización que la localidad).
            string nombre = Texto(fila, COL_NOMBRE);
            if (!string.IsNullOrWhiteSpace(nombre))
                SetCelda(fila, COL_NOMBRE, Validaciones.Capitalizar(nombre));

            // Localidad -> "Vicente Lopez"
            string loc = Texto(fila, COL_LOCALIDAD);
            if (!string.IsNullOrWhiteSpace(loc))
                SetCelda(fila, COL_LOCALIDAD, Validaciones.Capitalizar(loc));

            // Celular -> "(11 XXXX-XXXX)" (solo si es válido; si no, lo dejo para marcarlo en rojo)
            string cel = Texto(fila, COL_CELULAR);
            if (Validaciones.TryFormatearCelular(cel, out string formateado))
                SetCelda(fila, COL_CELULAR, formateado);
        }

        // ============================================================
        //  FILTRO DE TECLADO (columna Edad)
        // ============================================================

        // Bloquea cualquier tecla que no sea un dígito (deja backspace y demás
        // teclas de control). El formulario lo engancha al cuadro de edición
        // cuando la celda que se está editando es la de Edad.
        public static void SoloNumeros(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        // ============================================================
        //  HELPERS DE CELDA
        // ============================================================

        // Lee el texto de una celda, ya recortado de espacios (nunca null).
        public static string Texto(DataGridViewRow fila, int col)
            => fila.Cells[col].Value?.ToString()?.Trim() ?? "";

        // Escribe un valor en una celda solo si cambió (evita disparar eventos de más).
        private static void SetCelda(DataGridViewRow fila, int col, string valor)
        {
            if ((fila.Cells[col].Value?.ToString() ?? "") != valor)
                fila.Cells[col].Value = valor;
        }
    }
}
