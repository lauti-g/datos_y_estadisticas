using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;   // GraphicsState (para guardar/restaurar la rotación)
using System.Drawing.Text;        // TextRenderingHint (suavizado del texto)
using System.Windows.Forms;

namespace datos_y_estadisticas
{
    // ============================================================================
    //  TabControlOscuro — solapas VERTICALES a la izquierda, texto rotado
    //  y SIN el borde claro que dibuja Windows.
    // ============================================================================
    //
    // ¿POR QUÉ EXISTE ESTO? (misma historia que DateTimePickerOscuro y AutocompletadoOscuro)
    // --------------------------------------------------------------------------
    // El TabControl es un control NATIVO: lo dibuja Windows con el tema del sistema.
    // Eso trae TRES problemas que no se arreglan con propiedades:
    //
    //   1) NO EXISTE BorderStyle. El marco clarito que se ve alrededor de las páginas
    //      lo pinta el propio control (es parte de su "tema visual") y no hay ninguna
    //      propiedad para sacarlo. Por eso no la encontraste: no está.
    //
    //   2) La "TIRA" donde viven las solapas (con Alignment = Left, toda la columna
    //      izquierda, incluido el sobrante debajo de las solapas) se pinta con el color
    //      del sistema (gris clarito) e IGNORA BackColor.
    //
    //   3) Con las solapas a la izquierda, Windows NO rota el texto. Y el evento
    //      DrawItem que usábamos antes solo deja dibujar ADENTRO del rectángulo de cada
    //      solapa: la tira y el borde siguen siendo del sistema, hagas lo que hagas.
    //
    // LA SOLUCIÓN: UserPaint = "el dibujo del control lo hago YO, ENTERO".
    // --------------------------------------------------------------------------
    // SetStyle(ControlStyles.UserPaint, true) le dice a WinForms: "no dejes que el
    // control nativo se pinte; llamá a MI OnPaint". Ahí pintamos TODO el rectángulo
    // del control (tira + borde + solapas) con nuestros colores. Las páginas (los
    // TabPage) son ventanas hijas que se pintan solas ENCIMA de lo nuestro, así que
    // no hay que tocarlas: siguen mostrando su contenido normalmente.
    //
    // Como pintamos hasta el último píxel del control, el borde claro "desaparece":
    // esos píxeles ahora son del color oscuro que elijamos (ColorFondoControl).
    //
    // OJO IMPORTANTE: con UserPaint activado, el evento DrawItem YA NO SE DISPARA
    // (era parte del mecanismo de dibujo nativo, que acabamos de apagar). Por eso la
    // lógica de colores que estaba en Form1.tab_DrawItem se mudó acá adentro, al
    // método DibujarSolapa(). Tampoco hace falta DrawMode = OwnerDrawFixed: si quedó
    // esa línea en el Designer, se puede borrar.
    //
    // CÓMO SE USA (ver Form1.Designer.cs y ConfigurarTabsYEventos en Form1.cs)
    // --------------------------------------------------------------------------
    //   tab = new TabControlOscuro();          // en el Designer, en lugar de TabControl
    //   tab.Paletas.Add(...);                  // colores de la solapa 0, luego la 1, etc.
    //   tab.ColorFondoControl = Tema.FondoDatos; // color de la tira y del marco
    //
    // Las propiedades llevan [Browsable(false)] y [DesignerSerializationVisibility(Hidden)]
    // por el mismo motivo que en DateTimePickerOscuro: para que el DISEÑADOR de Visual
    // Studio no intente serializarlas (las manejamos por código y evitamos errores raros
    // al abrir el formulario en el diseñador).
    // ============================================================================
    public class TabControlOscuro : TabControl
    {
        // --------------------------------------------------------------------
        //  Paleta de UNA solapa: sus 4 colores (fondo/texto × normal/seleccionada).
        // --------------------------------------------------------------------
        // Es una clasecita "bolsa de datos" (solo campos, sin lógica). La hago anidada
        // (adentro de TabControlOscuro) porque solo tiene sentido junto a este control;
        // desde afuera se usa como "TabControlOscuro.PaletaSolapa".
        public sealed class PaletaSolapa
        {
            public Color FondoNormal;        // fondo cuando la solapa NO está seleccionada
            public Color FondoSeleccionada;  // fondo cuando SÍ está seleccionada
            public Color TextoNormal;        // texto cuando NO está seleccionada
            public Color TextoSeleccionada;  // texto cuando SÍ está seleccionada
        }

        // Una paleta por solapa, EN ORDEN: Paletas[0] es la primera solapa, [1] la segunda...
        // En esta app: 0 = Datos (azul/dorado), 1 = Estadísticas (rojo/gris). Se llenan
        // desde Form1 para que los colores sigan viviendo en un solo lugar (la clase Tema).
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<PaletaSolapa> Paletas { get; } = new();

        // Red de seguridad: si alguna solapa no tiene paleta configurada (por ejemplo,
        // se agrega una tercera pestaña y nadie carga sus colores), usamos esta para
        // que igual se vea algo coherente en vez de explotar o quedar invisible.
        private static readonly PaletaSolapa PaletaPorDefecto = new()
        {
            FondoNormal = Tema.PanelDatos,
            FondoSeleccionada = Tema.AzulAccent,
            TextoNormal = Tema.Blanco,
            TextoSeleccionada = Tema.Dorado
        };

        // Color de la TIRA: la columna izquierda completa (lo que sobra debajo de las
        // solapas) MÁS el marquito que rodea a las páginas. Es una propiedad "con cuerpo"
        // (no automática) a propósito: al asignarle un color nuevo llama a Invalidate(),
        // que le pide a Windows "repintame", y así el cambio se ve al instante sin que
        // quien la use tenga que acordarse de refrescar nada.
        private Color _colorFondoControl = Tema.FondoDatos;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ColorFondoControl
        {
            get => _colorFondoControl;
            set
            {
                _colorFondoControl = value;
                Invalidate(); // repintar con el color nuevo
            }
        }

        // Fuente de los títulos de las solapas (la misma que usaba tab_DrawItem).
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font FuenteSolapas { get; set; } = new Font("Segoe UI Semibold", 12F);

        public TabControlOscuro()
        {
            // UserPaint          = el dibujo lo hago yo en OnPaint (apaga el dibujo nativo).
            // AllPaintingInWmPaint = también el fondo se pinta en OnPaint (menos parpadeo).
            // OptimizedDoubleBuffer = dibuja primero en memoria y recién después en pantalla
            //                         (evita el "flasheo" al repintar).
            // ResizeRedraw       = si el control cambia de tamaño, repintar TODO
            //                      (si no, al agrandar la ventana quedan zonas sin pintar).
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            // Solapas paradas sobre el borde izquierdo.
            Alignment = TabAlignment.Left;

            // OBLIGATORIO para Left/Right: si Multiline es false, Windows directamente
            // ignora la alineación lateral y deja las solapas arriba.
            Multiline = true;

            // Fixed = "respetá EXACTAMENTE el tamaño que te doy en ItemSize"
            // (sin esto, el ancho de cada solapa lo decide el largo de su texto).
            SizeMode = TabSizeMode.Fixed;

            // ¡LA TRAMPA DE ItemSize! Con Alignment Left/Right, Windows INTERCAMBIA los ejes:
            //   ItemSize.Width  pasa a ser el ALTO de cada solapa en pantalla (su largo vertical)
            //   ItemSize.Height pasa a ser el ANCHO de cada solapa en pantalla (su grosor)
            // Por eso tu Size(170, 34) daba solapas de 170 de ALTO y 34 de ANCHO: el texto
            // horizontal no entraba en 34 píxeles de ancho y se veía cortado a dos letras.
            // Acá: 170 de alto y 44 de grosor, cómodo para el texto rotado. Si querés
            // solapas más largas o más finitas, tocá estos dos números.
            ItemSize = new Size(10, 0);
        }

        // --------------------------------------------------------------------
        //  El dibujo completo del control
        // --------------------------------------------------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 1) Pinto TODO el fondo del control de un solo color oscuro.
            //    Acá es donde "mueren" el borde clarito y la tira gris del sistema:
            //    esos píxeles ahora los pintamos nosotros. (Las páginas se dibujan
            //    solas encima de esto, así que no las tapamos.)
            g.Clear(ColorFondoControl);

            // El texto rotado lo dibuja GDI+ (DrawString); sin esta línea sale un poco
            // "peludo". ClearTypeGridFit es el mismo suavizado que usa Windows.
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 2) Dibujo cada solapa encima del fondo, una por una.
            //    TabCount es la cantidad de pestañas; el índice coincide con Paletas.
            for (int i = 0; i < TabCount; i++)
                DibujarSolapa(g, i);
        }

        // Dibuja UNA solapa: su fondo y su texto rotado 90°.
        private void DibujarSolapa(Graphics g, int indice)
        {
            // GetTabRect devuelve el rectángulo de esa solapa. Aunque el dibujo nativo
            // está apagado, Windows sigue calculando la POSICIÓN de cada solapa (y sigue
            // atendiendo los clics), así que este rectángulo es siempre el correcto.
            Rectangle r = GetTabRect(indice);
            bool seleccionada = (indice == SelectedIndex);

            // Paleta de ESTA solapa (o la de emergencia si no configuraron una).
            PaletaSolapa p = indice < Paletas.Count ? Paletas[indice] : PaletaPorDefecto;
            Color fondo = seleccionada ? p.FondoSeleccionada : p.FondoNormal;
            Color texto = seleccionada ? p.TextoSeleccionada : p.TextoNormal;

            // Fondo de la solapa.
            using (var pincelFondo = new SolidBrush(fondo))
                g.FillRectangle(pincelFondo, r);

            // ---------- Texto VERTICAL (rotado 90°) ----------
            // OJO: TextRenderer (lo que usaba el viejo tab_DrawItem) acá NO sirve.
            // TextRenderer dibuja con GDI "clásico", que IGNORA las rotaciones del
            // Graphics: por más que rotes, el texto sale derecho. Para texto rotado hay
            // que usar Graphics.DrawString (GDI+), que SÍ respeta las transformaciones.
            //
            // La receta, paso a paso:
            //   a) GUARDO el estado del Graphics (posición del origen, rotación, etc.)
            //      para poder deshacer todo al final.
            //   b) MUEVO el origen (el punto (0,0) del dibujo) al CENTRO de la solapa.
            //   c) ROTO el sistema de coordenadas 90° alrededor de ese origen.
            //   d) DIBUJO el texto centrado en el origen -> queda centrado en la solapa
            //      y girado.
            //   e) RESTAURO el estado guardado. Si me salteo esto, TODO lo que se dibuje
            //      después (las otras solapas) saldría corrido y rotado también.
            GraphicsState estadoPrevio = g.Save();                          // (a)

            g.TranslateTransform(r.X + r.Width / 2f, r.Y + r.Height / 2f); // (b)

            // (c) 90 = gira en sentido horario: el texto se lee de ARRIBA hacia ABAJO
            //     (como los lomos de los libros). Si lo preferís de abajo hacia arriba,
            //     cambiá 90 por -90: no hay que tocar nada más, el centrado sigue andando.
            g.RotateTransform(90);

            // Cómo acomodar el texto dentro de su rectángulo:
            using var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,          // centrado a lo largo del texto
                LineAlignment = StringAlignment.Center,      // centrado a lo "gordo" del texto
                Trimming = StringTrimming.EllipsisCharacter, // si no entra, corta con "..."
                FormatFlags = StringFormatFlags.NoWrap       // una sola línea (sin partir palabras)
            };

            // El rectángulo del texto, expresado en el sistema YA ROTADO y centrado en el
            // origen. Como giramos 90°, los ejes quedaron cruzados: el LARGO disponible
            // para el texto es el ALTO de la solapa (r.Height) y el grosor es su ANCHO
            // (r.Width). Arranca en menos-la-mitad de cada lado para quedar centrado en (0,0).
            var rectTexto = new RectangleF(-r.Height / 2f, -r.Width / 2f, r.Height, r.Width);

            using var pincelTexto = new SolidBrush(texto);
            g.DrawString(TabPages[indice].Text, FuenteSolapas, pincelTexto,
                         rectTexto, formato);                               // (d)

            g.Restore(estadoPrevio);                                        // (e)
        }

        // Al cambiar de solapa hay que repintar: la que estaba "prendida" se apaga y la
        // nueva se prende. Con el dibujo nativo esto lo hacía Windows solo; ahora que
        // pintamos nosotros, el repintado también lo pedimos nosotros.
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e); // que el evento siga llegando a quien esté suscripto
            Invalidate();                   // "repintame entero" -> se dispara OnPaint
        }

        // Liberar recursos: la fuente es un recurso del sistema (IDisposable), igual que
        // hace AutocompletadoOscuro con la suya. Dispose(bool) es el patrón estándar de
        // los controles de WinForms; disposing = true significa "liberá lo administrado".
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                FuenteSolapas?.Dispose();

            base.Dispose(disposing);
        }
    }
}