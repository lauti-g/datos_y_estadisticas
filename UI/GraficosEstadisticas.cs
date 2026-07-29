using datos_y_estadisticas.Modelos;            // la clase Alumno
using LiveChartsCore;                          // ISeries
using LiveChartsCore.SkiaSharpView;            // ColumnSeries, PieSeries, Axis
using LiveChartsCore.SkiaSharpView.Painting;   // SolidColorPaint
using LiveChartsCore.SkiaSharpView.WinForms;   // CartesianChart, PieChart
using SkiaSharp;                               // SKColor

namespace datos_y_estadisticas.UI
{
    // ============================================================
    //  GRÁFICOS DE LA PESTAÑA ESTADÍSTICAS (LiveCharts2)
    // ============================================================
    //
    // Todo el DIBUJO de estadísticas vive acá: crear los tres controles de
    // gráfico (dos de barras + la torta), contar los alumnos por localidad /
    // motivo / rango de edad, y volcar esas cuentas en las series.
    //
    // El formulario solo tiene que llamar a Mostrar(alumnos, descripcion):
    // no sabe nada de LiveCharts, de SkiaSharp ni de cómo se arma una serie.
    // Si mañana se cambia la librería de gráficos, se toca ÚNICAMENTE este
    // archivo.
    //
    // A diferencia de Validaciones o ImportadorExcel, esta clase NO es static:
    // guarda estado (los tres controles de gráfico y las etiquetas de título),
    // así que hay que crear UNA instancia en el arranque pasándole los paneles
    // donde meter cada gráfico.
    public class GraficosEstadisticas
    {
        // Los tres controles de gráfico (se crean una vez, en el constructor).
        private readonly CartesianChart cartesianLocalidades; // barras: localidades
        private readonly CartesianChart cartesianEdades;      // barras: rangos de edad
        private readonly PieChart pieMotivos;                 // torta: motivos

        // Las etiquetas de título de cada gráfico (las actualiza Mostrar con
        // el período y el total de alumnos).
        private readonly Label lblLocalidades;
        private readonly Label lblEdades;
        private readonly Label lblMotivos;

        // Paleta de rojos para las porciones de la torta de motivos.
        private static readonly SKColor[] ColoresTorta =
        {
            new SKColor(162, 63, 15),
            new SKColor(216, 60, 45),
            new SKColor(171, 111, 9)
        };

        // ---------- Rangos de edad para la estadística ----------
        // Etiquetas del eje X, EN ORDEN. El primer rango (0-29) es más ancho a
        // propósito; los demás van de 10 en 10. Si querés agregar "80-89", sumá
        // la etiqueta acá Y un "if" en IndiceRangoEdad: los dos lugares tienen
        // que coincidir.
        private static readonly string[] RangosEdad =
            { "0-29", "30-39", "40-49", "50-59", "60-119" };

        // Dada una edad, devuelve el ÍNDICE del rango al que pertenece,
        // o -1 si queda fuera. Uso "edad <= 29", "edad <= 39"... aprovechando
        // que si no entró en el primer if ya sé que es 30 o más, en el segundo
        // que es 40 o más, etc. (escalera de rangos).
        private static int IndiceRangoEdad(int edad)
        {
            if (edad <= 29) return 0; // 0  a 29
            if (edad <= 39) return 1; // 30 a 39
            if (edad <= 49) return 2; // 40 a 49
            if (edad <= 59) return 3; // 50 a 59
            if (edad <= 119) return 4; // 60 a 119
            return -1;                // 119+ => fuera de los rangos pedidos
        }

        // Convierte un Color de WinForms a un SKColor de SkiaSharp (para los gráficos).
        private static SKColor Sk(Color c) => new SKColor(c.R, c.G, c.B);

        // Crea los tres controles de gráfico y los mete en sus paneles.
        // Se llama UNA vez en el arranque del formulario.
        public GraficosEstadisticas(
            Panel panelLocalidades, Panel panelEdades, Panel panelMotivos,
            Label lblLocalidades, Label lblEdades, Label lblMotivos)
        {
            this.lblLocalidades = lblLocalidades;
            this.lblEdades = lblEdades;
            this.lblMotivos = lblMotivos;

            cartesianLocalidades = new CartesianChart
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.PanelEst
            };
            panelLocalidades.Controls.Add(cartesianLocalidades);

            // Gráfico de barras de edades (mismo armado que el de localidades).
            cartesianEdades = new CartesianChart
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.PanelEst
            };
            panelEdades.Controls.Add(cartesianEdades);

            pieMotivos = new PieChart
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.PanelEst,
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Right,
                LegendTextPaint = new SolidColorPaint(Sk(Tema.GrisTexto)),
                LegendTextSize = 13
            };
            panelMotivos.Controls.Add(pieMotivos);
        }

        // ============================================================
        //  EL MÉTODO QUE USA EL FORMULARIO
        // ============================================================

        // Dibuja las estadísticas de una lista YA TRAÍDA de alumnos (este
        // método NO toca la base). 'descripcion' es el texto que va en los
        // títulos (ej.: "Hoy", "Mes de junio", "Todos los alumnos").
        public void Mostrar(List<Alumno> alumnos, string descripcion)
        {
            // Quito los repetidos para no inflar las cuentas: si una misma persona
            // asiste varias veces, tiene que contar UNA sola vez en los gráficos.
            List<Alumno> unicos = QuitarDuplicados(alumnos);

            // --- Localidades: cantidad de alumnos por localidad (gráfico de barras) ---
            var porLocalidad = unicos
                .Where(a => !string.IsNullOrWhiteSpace(a.Localidad))
                .GroupBy(a => a.Localidad)
                .Select(g => new { Etiqueta = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            // --- Motivos: cantidad de alumnos por motivo (gráfico de torta) ---
            var porMotivo = unicos
                .Where(a => !string.IsNullOrWhiteSpace(a.MotivoIngreso))
                .GroupBy(a => a.MotivoIngreso)
                .Select(g => new { Etiqueta = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            // --- Edades: cantidad de alumnos por rango de edad (gráfico de barras) ---
            // Acá NO uso GroupBy como en los otros gráficos a propósito: quiero que
            // SIEMPRE aparezcan todos los rangos (aunque alguno tenga 0). Por eso armo
            // un arreglo de contadores (uno por rango, en el mismo orden que RangosEdad)
            // y los voy sumando.
            int[] cantidadesPorRango = new int[RangosEdad.Length]; // arranca todo en 0
            foreach (Alumno a in unicos)
            {
                int indice = IndiceRangoEdad(a.Edad); // a qué rango va esta edad (o -1)
                if (indice >= 0)                       // si es -1 (fuera de rango), no lo cuento
                    cantidadesPorRango[indice]++;      // sumo 1 en el casillero del rango
            }

            // Dibujo los tres gráficos.
            DibujarLocalidades(porLocalidad.Select(x => x.Etiqueta).ToArray(),
                               porLocalidad.Select(x => x.Cantidad).ToArray());
            DibujarMotivos(porMotivo.Select(x => x.Etiqueta).ToArray(),
                           porMotivo.Select(x => x.Cantidad).ToArray());
            DibujarEdades(RangosEdad, cantidadesPorRango);

            // Actualizo los títulos con el período y los totales.
            lblLocalidades.Text = $"Localidades — {descripcion}  ({unicos.Count} alumnos)";
            lblMotivos.Text = $"Motivos de ingreso — {descripcion}";
            lblEdades.Text = $"Edades — {descripcion}";
        }

        // Quita duplicados usando NOMBRE + CELULAR como clave: si una persona repite
        // asistencia, se la cuenta una sola vez en las estadísticas (pero igual aparece
        // en la grilla). ÚNICO lugar donde se decide qué es un "duplicado": cambiar acá
        // la regla si hiciera falta.
        private static List<Alumno> QuitarDuplicados(List<Alumno> alumnos)
        {
            var vistos = new HashSet<string>();
            var unicos = new List<Alumno>();
            foreach (Alumno a in alumnos)
            {
                string clave = a.Nombre.Trim().ToLowerInvariant() + "|" + a.Celular.Trim();

                if (vistos.Add(clave)) // Add devuelve false si la clave ya estaba.
                    unicos.Add(a);
            }
            return unicos;
        }

        // ============================================================
        //  DIBUJO DE CADA GRÁFICO
        // ============================================================

        // Dibuja el gráfico de barras de localidades (eje X: localidad, eje Y: cantidad).
        private void DibujarLocalidades(string[] localidades, int[] cantidades)
        {
            cartesianLocalidades.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "Cantidad",
                    Values = cantidades,
                    Fill = new SolidColorPaint(Sk(Tema.Rojo))
                }
            };

            cartesianLocalidades.XAxes = new[]
            {
                new Axis
                {
                    Labels = localidades,
                    LabelsPaint = new SolidColorPaint(Sk(Tema.GrisTexto)),
                    TextSize = 12,
                    // Si hay muchas localidades, inclino las etiquetas para que no se pisen.
                    LabelsRotation = localidades.Length > 4 ? 25 : 0
                }
            };

            cartesianLocalidades.YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(Sk(Tema.GrisTexto)),
                    SeparatorsPaint = new SolidColorPaint(Sk(Color.FromArgb(60, 60, 60))),
                    TextSize = 12,
                    MinLimit = 0 // el eje siempre arranca en 0.
                }
            };
        }

        // Dibuja el gráfico de barras de edades (eje X: rango, eje Y: cantidad).
        // Es prácticamente igual a DibujarLocalidades. La diferencia es que las etiquetas
        // del eje X son fijas (los rangos) y no hace falta rotarlas porque entran bien.
        private void DibujarEdades(string[] rangos, int[] cantidades)
        {
            // Una sola serie de columnas con las cantidades de cada rango.
            cartesianEdades.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "Cantidad",
                    Values = cantidades,
                    Fill = new SolidColorPaint(Sk(Tema.Rojo))
                }
            };

            // Eje X: las etiquetas de los rangos ("0-29", "30-39", ...).
            cartesianEdades.XAxes = new[]
            {
                new Axis
                {
                    Labels = rangos,
                    LabelsPaint = new SolidColorPaint(Sk(Tema.GrisTexto)),
                    TextSize = 12
                }
            };

            // Eje Y: cantidades. Arranca en 0 igual que el de localidades.
            cartesianEdades.YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(Sk(Tema.GrisTexto)),
                    SeparatorsPaint = new SolidColorPaint(Sk(Color.FromArgb(60, 60, 60))),
                    TextSize = 12,
                    MinLimit = 0
                }
            };
        }

        // Dibuja el gráfico de torta de motivos (una porción por motivo, en tonos de rojo).
        private void DibujarMotivos(string[] motivos, int[] cantidades)
        {
            // Total de alumnos del período (denominador del porcentaje de cada porción).
            int cantidadTotal = cantidades.Sum();

            var series = new List<ISeries>();
            for (int i = 0; i < motivos.Length; i++)
            {
                string nombre = motivos[i];     // copio a variables locales para el closure de la etiqueta.
                int cantidad = cantidades[i];

                // Porcentaje en DOUBLE: dividir enteros TRUNCA (100/3 = 33, no 33,33) y las
                // porciones sumarían 99 %. El cast a double conserva los decimales.
                double porcentaje = cantidadTotal == 0
                    ? 0
                    : (double)cantidad * 100 / cantidadTotal;

                series.Add(new PieSeries<int>
                {
                    Name = nombre,
                    Values = new[] { cantidad },
                    Fill = new SolidColorPaint(ColoresTorta[i % ColoresTorta.Length]),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    // "0.0" => un decimal (ej. 33,3 %). Usá "0.#" si querés ocultar el ",0".
                    DataLabelsFormatter = _ => $"{nombre} ({porcentaje:0.0} %)"
                });
            }
            pieMotivos.Series = series;
        }
    }
}
