namespace datos_y_estadisticas.Logica
{
    // ============================================================
    //  MÁQUINA DE ESTADOS DE LA PESTAÑA DATOS
    // ============================================================
    //
    // Antes el formulario manejaba el "modo" con dos banderas sueltas
    // (enModoBusqueda y hayCambiosSinGuardar) más una tercera (cargando)
    // repartidas por todo el archivo. El problema de las banderas sueltas
    // es que cada método tenía que acordarse de prender/apagar las que
    // correspondían, y era fácil dejar la interfaz a medio actualizar
    // (por ejemplo: salir de la búsqueda por cambio de fecha dejaba
    // escondido el botón "Buscar").
    //
    // Una MÁQUINA DE ESTADOS reemplaza esas banderas por UN solo dato:
    // "en qué estado estoy". Los cambios de estado (las TRANSICIONES)
    // son métodos con nombre (MarcarCambio, Guardado, EntrarBusqueda...),
    // así que queda escrito en un único lugar QUÉ puede pasar desde cada
    // estado. Y cada vez que el estado cambia se dispara el evento
    // EstadoCambiado: el formulario se suscribe una sola vez y ahí
    // sincroniza TODA la interfaz (textos de la columna del botón,
    // visibilidad de Buscar/Salir, fila de "agregar", etc.). Imposible
    // que quede algo a medio actualizar: siempre se refresca todo junto.

    // Los CUATRO estados posibles de la grilla. Son la combinación de
    // dos preguntas: ¿estoy viendo un día normal o resultados de búsqueda?
    // y ¿hay ediciones sin guardar?
    public enum EstadoDatos
    {
        Normal,             // viendo UN día, sin cambios pendientes
        ConCambios,         // viendo UN día, con ediciones sin guardar
        Busqueda,           // viendo resultados de búsqueda, sin cambios
        BusquedaConCambios  // resultados de búsqueda editados sin guardar
    }

    public class MaquinaEstadoDatos
    {
        // El estado actual. El 'private set' obliga a cambiarlo SOLO a través
        // de las transiciones de abajo: nadie de afuera puede pisarlo directo.
        public EstadoDatos Estado { get; private set; } = EstadoDatos.Normal;

        // Se dispara cada vez que el estado CAMBIA de verdad (no si la
        // transición dejó el mismo estado). El formulario se suscribe y
        // sincroniza la interfaz completa en un solo método.
        public event Action? EstadoCambiado;

        // ---------- La "compuerta" de carga ----------
        // Mientras el código llena la grilla (cargar un día, la búsqueda,
        // una importación), los eventos de la grilla se disparan igual que
        // si hubiera tipeado el usuario. Esta compuerta hace que
        // MarcarCambio() los ignore durante esos momentos.
        //
        // Es un CONTADOR y no un bool a propósito: si dos cargas se anidan
        // (ej.: fijar la fecha del selector mientras ya estoy cargando filas),
        // con un bool la de adentro apagaría la compuerta antes de tiempo.
        // Con el contador, la compuerta recién se abre cuando TODAS las
        // cargas anidadas terminaron (vuelve a 0).
        private int contadorCarga = 0;
        public bool Cargando => contadorCarga > 0;

        public void EmpezarCarga() => contadorCarga++;
        public void TerminarCarga()
        {
            if (contadorCarga > 0) contadorCarga--;
        }

        // ---------- Preguntas cómodas sobre el estado ----------
        // (Para que el formulario no tenga que comparar contra el enum a mano.)
        public bool EnBusqueda =>
            Estado == EstadoDatos.Busqueda || Estado == EstadoDatos.BusquedaConCambios;

        public bool HayCambiosSinGuardar =>
            Estado == EstadoDatos.ConCambios || Estado == EstadoDatos.BusquedaConCambios;

        // ============================================================
        //  TRANSICIONES (las únicas formas de cambiar de estado)
        // ============================================================

        // Se terminó de cargar un día en la grilla: lo recién cargado no son
        // cambios pendientes, y si venía de una búsqueda, ya no estoy en ella.
        public void TablaCargada() => CambiarA(EstadoDatos.Normal);

        // El usuario tocó algo en la grilla. Si estoy cargando por código, lo
        // ignoro (para eso está la compuerta). Si no, paso a la variante
        // "con cambios" del estado en el que esté.
        public void MarcarCambio()
        {
            if (Cargando) return;

            if (Estado == EstadoDatos.Normal)
                CambiarA(EstadoDatos.ConCambios);
            else if (Estado == EstadoDatos.Busqueda)
                CambiarA(EstadoDatos.BusquedaConCambios);
            // Si ya estaba en ConCambios / BusquedaConCambios, no hay nada que hacer.
        }

        // Se guardó correctamente: vuelvo a la variante "sin cambios" del
        // estado actual (sigo en búsqueda si estaba en búsqueda).
        public void Guardado()
        {
            if (Estado == EstadoDatos.ConCambios)
                CambiarA(EstadoDatos.Normal);
            else if (Estado == EstadoDatos.BusquedaConCambios)
                CambiarA(EstadoDatos.Busqueda);
        }

        // La grilla pasa a mostrar resultados de búsqueda (recién cargados,
        // así que sin cambios pendientes).
        public void EntrarBusqueda() => CambiarA(EstadoDatos.Busqueda);

        // Se abandona la búsqueda y se vuelve a la edición normal de un día.
        // (Quien llama se encarga después de cargar la fecha que corresponda.)
        public void SalirBusqueda() => CambiarA(EstadoDatos.Normal);

        // El único lugar que escribe 'Estado' y avisa a los suscriptores.
        private void CambiarA(EstadoDatos nuevo)
        {
            if (nuevo == Estado) return; // nada cambió: no molesto a nadie.
            Estado = nuevo;
            EstadoCambiado?.Invoke();
        }
    }
}
