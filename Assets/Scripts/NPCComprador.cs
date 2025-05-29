using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// --- Clase interna para diálogos específicos ---
[System.Serializable] // Para verlo en el Inspector
public class DialogoEspecificoNPC
{
    [Tooltip("La receta para la cual este NPC dirá algo único.")]
    public PedidoPocionData receta; // Podrías usar la 'clavePocion' si la añadiste para buscar más fácil
    [Tooltip("La frase exacta que dirá este NPC para esa receta.")]
    public string dialogoUnico;
}
// --- Fin clase interna ---

public class NPCComprador : MonoBehaviour
{
    // --- Estados del NPC ---
    private enum EstadoNPC { MoviendoAVentana, EsperandoAtencion, EnVentanaEsperando, ProcesandoEntrega, EsperandoParaSalir, MoviendoASalida, Inactivo }
    private EstadoNPC estadoActual = EstadoNPC.Inactivo;

    // --- Referencias ---
    [HideInInspector] public GestorCompradores gestor;

    // --- Configuración Movimiento ---
    [Header("Movimiento Simple")]
    public float velocidadMovimiento = 4.0f;
    public float velocidadRotacion = 360f;

    // --- Configuración Pedidos ---
    [Header("Pedidos Posibles")]
    public List<PedidoPocionData> pedidosPosibles;
    public List<PedidoPocionData> listaPedidosEspecificos;
    public bool usarListaEspecifica = false;

    [Header("Diálogos Personalizados (Opcional)")]
    [Tooltip("Define aquí si quieres que ESTE NPC diga frases únicas para ciertas pociones.")]
    public List<DialogoEspecificoNPC> dialogosEspecificos; // <<--- NUEVA LISTA

    // --- Configuración Feedback y Sonidos ---
    [Header("Feedback y Sonidos")]
    public string mensajeFeedbackCorrecto = "¡Muchas gracias!";
    public string mensajeFeedbackIncorrecto = "¡No sirves para nada!";
    public string mensajeSegundoFallo = "¡Nah! ¡Me voy de aquí!";
    public AudioClip sonidoPocionCorrecta;
    public AudioClip sonidoPocionIncorrecta;

    // --- Configuración UI Bocadillo ---
    [Header("UI Bocadillo Pedido")]
    public GameObject prefabBocadilloUI;
    public Transform puntoAnclajeBocadillo;
    public float duracionFeedback = 3.0f;

    // --- Estado Interno ---
    private PedidoPocionData pedidoActual = null;
    private int intentosFallidos = 0;
    private Vector3 destinoActual;
    private float tiempoRestanteEspera; // <<--- AÑADE ESTA LÍNEA
    private float tiempoRestanteEsperaAtencion; // <<--- NUEVO: Timer inicial
    private bool mirandoVentana = false;

    // --- Variables Bocadillo y Corutinas ---
    private GameObject instanciaBocadilloActual = null;
    private TextMeshProUGUI textoBocadilloActual = null;
    private TextMeshProUGUI textoTemporizadorActual = null; // <<--- AÑADE ESTA LÍNEA
    private Coroutine coroutineOcultarBocadillo = null;
    private Coroutine coroutineRetrasarSalida = null;

    // --- NUEVO: Configuración Temporizador ---
    [Header("Temporizador Espera")]
    [Tooltip("Segundos máximos que el NPC espera ANTES de ser atendido (Mostrará '...')")]
    public float tiempoMaximoEsperaAtencion = 10.0f; // <<--- NUEVO: Tiempo para atenderlo
    [Tooltip("Mensaje si se acaba el tiempo de espera atencion.")]
    public string mensajeTiempoEsperaAgotado = "¡Por lo que veo no tienen empleados, adiós!";
    [Tooltip("Segundos máximos que el NPC espera antes de irse enfadado.")]
    public float tiempoMaximoEspera = 30.0f;
    [Tooltip("Mensaje si se acaba el tiempo de espera.")]
    public string mensajeTiempoAgotado = "¡Eres demasiado lento, adiós!";
    [Tooltip("Sonido opcional si se acaba el tiempo (si es None, usa el de poción incorrecta).")]
    public AudioClip sonidoTiempoAgotado;
    // --- FIN NUEVO ---

    void Awake()
    {
        estadoActual = EstadoNPC.Inactivo;
        tiempoRestanteEspera = tiempoMaximoEspera; // <<--- AÑADE ESTA LÍNEA
        tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion; // <<--- AÑADIDO
    }

    void Update()
    {
        
        // --- LOG DE ESTADO ---
        //Debug.Log($"Update Loop - Estado Actual: {estadoActual}");
        // ---------------------

        if (estadoActual == EstadoNPC.MoviendoAVentana || estadoActual == EstadoNPC.MoviendoASalida)
        {
            MoverHaciaDestino(); // Mueve Y Gira mientras camina
        }
        // --- NUEVO BLOQUE: Lógica cuando espera ser atendido ---
        else if (estadoActual == EstadoNPC.EsperandoAtencion)
        {
            // 1. Asegurar visibilidad del bocadillo "..." (si ya existe)
            if (instanciaBocadilloActual != null && !instanciaBocadilloActual.activeSelf)
            {
                MostrarBocadillo("[E]", false); // Reactivar si se ocultó
            }
            else if (instanciaBocadilloActual == null)
            {
                MostrarBocadillo("[E]", false); // Crear si no existe
            }

            // 2. Girar hacia la ventana (igual que antes)
            if (!mirandoVentana && gestor != null && gestor.puntoMiradaVentana != null)
            {
                // ... (Código de giro con RotateTowards) ...
                Vector3 dirTarget = gestor.puntoMiradaVentana.position - transform.position;
                Vector3 dirHoriz = new Vector3(dirTarget.x, 0, dirTarget.z);
                if (dirHoriz.sqrMagnitude > 0.001f)
                {
                    Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, velocidadRotacion * Time.deltaTime);
                    if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)
                    {
                        transform.rotation = rotObj;
                        mirandoVentana = true;
                    }
                }
                else { mirandoVentana = true; }
            }

            // 3. Contar tiempo de espera para ATENCIÓN (Solo si ya está mirando o no necesita girar)
            if (mirandoVentana || gestor?.puntoMiradaVentana == null)
            {
                if (tiempoRestanteEsperaAtencion > 0)
                {
                    tiempoRestanteEsperaAtencion -= Time.deltaTime;
                    ActualizarUITemporizador();
                    // NO actualizamos UI de timer aquí (mostramos "...")

                    if (tiempoRestanteEsperaAtencion <= 0)
                    {
                        TiempoAgotadoEsperandoAtencion(); // Se cansó de esperar atención
                    }
                }
            }
        }
        // --- FIN NUEVO BLOQUE ---
        // --- Lógica para cuando está ESPERANDO en la ventana ---
        else if (estadoActual == EstadoNPC.EnVentanaEsperando)
        {
            // --- 1. Asegurar Visibilidad de UI de Espera ---
            if (instanciaBocadilloActual != null)
            {
                // Reactivar bocadillo si está inactivo
                if (!instanciaBocadilloActual.activeSelf)
                {
                    string textoOriginalPedido = ObtenerTextoOriginalPedido();
                    MostrarBocadillo(textoOriginalPedido, false);
                    Debug.LogWarning($"Reactivado bocadillo para {gameObject.name} (estaba inactivo)."); // Aviso útil

                }
                // Reactivar texto del timer si está inactivo
                if (textoTemporizadorActual != null && !textoTemporizadorActual.gameObject.activeSelf)
                {
                    textoTemporizadorActual.gameObject.SetActive(true);
                    Debug.LogWarning($"Reactivado TextoTemporizador para {gameObject.name}."); // Aviso útil

                }
                // Restaurar texto del pedido si mostró feedback antes
                /*if (textoBocadilloActual != null && textoBocadilloActual.text != ObtenerTextoOriginalPedido())
                {
                    textoBocadilloActual.text = ObtenerTextoOriginalPedido();
                }*/
            }
            else
            {
                // Si no hay bocadillo, forzar que lo pida de nuevo
                Debug.LogError($"NPC {gameObject.name} en EnVentanaEsperando sin bocadillo. Forzando SolicitarPocion.");
                SolicitarPocion();
                return; // Salir este frame
            }
            // --- Fin Asegurar Visibilidad ---


            // --- 2. Girar hacia la ventana HASTA que esté orientado --- <<<--- LÓGICA RESTAURADA
            if (!mirandoVentana && gestor != null && gestor.puntoMiradaVentana != null)
            {
                Vector3 direccionHaciaTarget = gestor.puntoMiradaVentana.position - transform.position;
                Vector3 direccionHorizontal = new Vector3(direccionHaciaTarget.x, 0, direccionHaciaTarget.z);

                if (direccionHorizontal.sqrMagnitude > 0.001f)
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHorizontal);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);

                    if (Quaternion.Angle(transform.rotation, rotacionObjetivo) < 1.0f) // Si ya casi está
                    {
                        transform.rotation = rotacionObjetivo; // Clavar rotación
                        mirandoVentana = true; // <<--- Marcar como terminado
                        Debug.Log($"{gameObject.name} terminó de girar hacia la ventana.");
                    }
                }
                else { mirandoVentana = true; } // Ya está en el punto
            }
            // --- Fin Girar ---


            // --- 3. Contar tiempo de espera (Solo si ya giró o no necesita girar) ---
            // Usamos mirandoVentana para saber si ya terminó el giro inicial

            if (tiempoRestanteEspera > 0)
            {
                tiempoRestanteEspera -= Time.deltaTime;

                ActualizarUITemporizador();
                if (tiempoRestanteEspera <= 0) { TiempoAgotado(); }
            }

            /*if (mirandoVentana || gestor?.puntoMiradaVentana == null) // Contar si ya mira o si no hay punto para mirar
            {
                if (tiempoRestanteEspera > 0)
                {
                    tiempoRestanteEspera -= Time.deltaTime;
                    ActualizarUITemporizador();
                    if (tiempoRestanteEspera <= 0) { TiempoAgotado(); }
                }
            }*/
            // ----------------------------------------------------------------
        }
    }

    void MoverHaciaDestino()
    {
        // --- 1. Calcular Dirección ---
        // Vector desde la posición actual hacia el destino
        Vector3 direccionHaciaDestino = destinoActual - transform.position;

        // Ignorar el eje Y para la rotación (evita que el NPC se incline)
        Vector3 direccionHorizontal = new Vector3(direccionHaciaDestino.x, 0, direccionHaciaDestino.z);

        // --- 2. Calcular Rotación Objetivo ---
        // Solo calcular si hay una dirección válida (no estamos ya en el destino)
        if (direccionHorizontal.sqrMagnitude > 0.001f) // Usar un umbral pequeño
        {
            // Crear la rotación que mira hacia la dirección horizontal
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHorizontal);

            // --- 3. Aplicar Rotación Suavemente ---
            // Girar desde la rotación actual hacia la objetivo, a una velocidad máxima
            transform.rotation = Quaternion.RotateTowards(
                                     transform.rotation,          // Rotación actual
                                     rotacionObjetivo,            // Rotación a la que queremos llegar
                                     velocidadRotacion * Time.deltaTime // Máximos grados a girar este frame
                                 );
        }
        // --- FIN ROTACIÓN ---

        // --- 4. Mover la Posición (como antes) ---
        float paso = velocidadMovimiento * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destinoActual, paso);
        // --- FIN MOVER ---

        // --- 5. Comprobar si Llegó (como antes) ---
        if (Vector3.Distance(transform.position, destinoActual) < 0.05f)
        {
            transform.position = destinoActual; // Ajustar posición final

            if (estadoActual == EstadoNPC.MoviendoAVentana)
            {
                Debug.Log($"{gameObject.name} llegó a la ventana.");
                estadoActual = EstadoNPC.EsperandoAtencion;
                mirandoVentana = false;
                tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion; // <<--- RESETEAR TIMER INICIAL

                // Mostrar bocadillo inicial simple
                MostrarBocadillo("[E]", false); // <<--- MUESTRA "..."
                // Ocultar el texto del timer (si estuviera visible de antes)
                if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);

                if (GestorAudio.Instancia != null && gestor != null && gestor.sonidoNuevoPedido != null) { /* Reproducir sonido */ }
                SolicitarPocion();
            }
            else if (estadoActual == EstadoNPC.MoviendoASalida)
            {
                Debug.Log($"{gameObject.name} llegó a la salida. Destruyendo...");
                estadoActual = EstadoNPC.Inactivo;
                Destroy(gameObject);
            }
        }
        // --- FIN COMPROBAR LLEGADA ---
    }

    public void IrAVentana(Vector3 posVentana)
    {
        if (estadoActual != EstadoNPC.Inactivo) return;
        destinoActual = posVentana;
        estadoActual = EstadoNPC.MoviendoAVentana;
        gameObject.SetActive(true);
        Debug.Log($"{gameObject.name} yendo a la ventana (movimiento simple)...");
    }

    // Dentro de NPCComprador.cs
    void SolicitarPocion()
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando) return;

        List<PedidoPocionData> listaAUsar = null;
        // ... (Lógica para elegir listaAUsar) ...
        if (usarListaEspecifica && listaPedidosEspecificos != null && listaPedidosEspecificos.Count > 0) { listaAUsar = listaPedidosEspecificos; } else if (pedidosPosibles != null && pedidosPosibles.Count > 0) { listaAUsar = pedidosPosibles; } else if (gestor != null && gestor.listaMaestraPedidos != null && gestor.listaMaestraPedidos.Count > 0) { listaAUsar = gestor.listaMaestraPedidos; }


        if (listaAUsar != null && listaAUsar.Count > 0)
        {
            pedidoActual = listaAUsar[Random.Range(0, listaAUsar.Count)];

            // Obtener el texto del pedido usando la nueva función
            string textoPedido = ObtenerTextoOriginalPedido();
            Debug.Log($"NPC ({gameObject.name}) PIDE: {textoPedido}");
            MostrarBocadillo(textoPedido, false); // Mostrar pedido inicial, sin auto-ocultar

            // Iniciar/Resetear Temporizador
            //tiempoRestanteEspera = tiempoMaximoEspera;
            // Buscar referencia a textoTemporizadorActual (como antes)
            if (instanciaBocadilloActual != null)
            {
                Debug.Log($"--- Buscando 'CanvasBocadillo/FondoBocadillo/TextoTemporizador' en '{instanciaBocadilloActual.name}' ---");
                // Buscar usando la ruta desde la raíz del bocadillo instanciado
                Transform timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");

                if (timerTransform != null) // ¿Encontró el objeto?
                {
                    Debug.Log($"Objeto '{timerTransform.name}' ENCONTRADO con Find!");
                    textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>(); // Obtener componente
                    if (textoTemporizadorActual != null)
                    {
                        Debug.Log("   ¡Componente TextMeshProUGUI ENCONTRADO y asignado!");
                    }
                    else
                    {
                        Debug.LogError($"   ¡ERROR! Objeto '{timerTransform.name}' encontrado, pero NO tiene componente TextMeshProUGUI!", timerTransform.gameObject);
                    }
                }
                else // Si Find no encontró la ruta
                {
                    Debug.LogError("==> ERROR CRÍTICO: transform.Find NO encontró la ruta 'CanvasBocadillo/Fondo]Bocadillo/TextoTemporizador'. Revisa nombres EXACTOS y jerarquía del prefab 'Prefab_BocadilloNPC'. <==");
                }
                Debug.Log("--- Búsqueda de 'TextoTemporizador' terminada ---");
            }
            else
            {
                Debug.LogError("¡ERROR CRÍTICO! instanciaBocadilloActual es NULL al intentar buscar el temporizador.");
            }

            // Actualizar UI y Activar (SOLO si se encontró la referencia)
            ActualizarUITemporizador(); // Llama a actualizar (mostrará tiempo si textoTemporizadorActual no es null)
            if (textoTemporizadorActual != null)
            {
                textoTemporizadorActual.gameObject.SetActive(true); // Activar el objeto de texto
                Debug.LogWarning("    ¡Componente TextMeshProUGUI ENCONTRADO y asignado!");
                Debug.LogError($"SolicitarPocion: ENCONTRADO Timer (ID Comp: {textoTemporizadorActual.GetInstanceID()}) en Objeto '{textoTemporizadorActual.gameObject.name}' (ID Obj: {textoTemporizadorActual.gameObject.GetInstanceID()}) dentro de Bocadillo (ID Inst: {instanciaBocadilloActual.GetInstanceID()})");
            }
            else
            {
                Debug.LogWarning("No se pudo activar TextoTemporizador porque la referencia es NULL.");
            }

        }
        else { /* ... Error si no hay recetas ... */ }
    }

    // ... (resto de métodos de NPCComprador) ...

    public void IntentarEntregarPocion(List<DatosIngrediente> pocionEntregada)
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando)
        {
            Debug.LogWarning($"Se intentó entregar poción a {gameObject.name} pero no estaba esperando (Estado: {estadoActual})");
            return;
        }
        if (pedidoActual == null)
        {
            Debug.LogWarning($"Se intentó entregar poción a {gameObject.name} pero no tenía pedido activo.");
            return;
        }

        Debug.Log($"NPC ({gameObject.name}) recibe poción. Comprobando...");
        estadoActual = EstadoNPC.ProcesandoEntrega;

        if (CompararListasIngredientes(pedidoActual.ingredientesRequeridos, pocionEntregada))
        {
            // Correcta
            GiveFeedback(mensajeFeedbackCorrecto, sonidoPocionCorrecta);
            // --- AÑADIR DINERO --- <<<--- AÑADIDO
            if (GestorJuego.Instance != null)
            { // <<--- Usar GestorJuego
              // Usar el método traducido 'AnadirDinero' y el valor del GestorJuego
                GestorJuego.Instance.AnadirDinero(GestorJuego.Instance.valorPocionCorrecta); // <<--- CORREGIDO
            }
            else { Debug.LogError("¡GestorJuego no encontrado para añadir dinero!"); }
            // ---------------------
            Irse(); // Iniciar secuencia de salida (con retraso)
        }
        else
        {
            intentosFallidos++;
            Debug.Log($"Intento fallido #{intentosFallidos}");
            if (intentosFallidos >= 2)
            {
                GiveFeedback(mensajeSegundoFallo, sonidoPocionIncorrecta);
                Irse();
            }
            else
            {
                GiveFeedback(mensajeFeedbackIncorrecto, sonidoPocionIncorrecta);
                estadoActual = EstadoNPC.EnVentanaEsperando;
                StartCoroutine(RestaurarPedidoDespuesDeFeedback());
            }
        }
    }

    void Irse()
    {
        if (estadoActual == EstadoNPC.MoviendoASalida || estadoActual == EstadoNPC.Inactivo || coroutineRetrasarSalida != null || estadoActual == EstadoNPC.EsperandoParaSalir)
        {
            return;
        }
        if (estadoActual != EstadoNPC.ProcesandoEntrega && pedidoActual != null)
        {
            Debug.LogWarning($"Irse() llamado desde estado {estadoActual}. Forzando a ProcesandoEntrega.");
            estadoActual = EstadoNPC.ProcesandoEntrega;
        }

        estadoActual = EstadoNPC.EsperandoParaSalir;
        Debug.Log($"{gameObject.name} iniciando secuencia de salida (con retraso)...");
        coroutineRetrasarSalida = StartCoroutine(RetrasarSalidaCoroutine());
    }

    IEnumerator RetrasarSalidaCoroutine()
    {
        // --- MOVIDO: La notificación al gestor se hará DESPUÉS de esperar ---
        // if (gestor != null) { gestor.NPCTermino(this); }

        Debug.Log($"{gameObject.name}: Mostrando feedback final, esperando {duracionFeedback}s...");
        yield return new WaitForSeconds(duracionFeedback);

        coroutineRetrasarSalida = null;

        if (estadoActual == EstadoNPC.EsperandoParaSalir)
        {
            Debug.Log($"{gameObject.name}: Tiempo de espera terminado. Iniciando movimiento a salida.");

            // --- NOTIFICAR AL GESTOR AQUÍ ---
            // Notifica JUSTO ANTES de empezar a moverse físicamente.
            if (gestor != null)
            {
                gestor.NPCTermino(this);
                Debug.Log($"NPCTermino notificado para {gameObject.name}");
            }
            else
            {
                Debug.LogError("¡RetrasarSalidaCoroutine: El NPC no tiene referencia a su gestor!");
            }
            // --------------------------------

            IniciarMovimientoHaciaSalida();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Estado cambió mientras esperaba para salir ({estadoActual}). No se iniciará movimiento.");
            // Aún así, notificar al gestor si no se hizo antes, por si acaso
            if (gestor != null) gestor.NPCTermino(this);
        }
    }

    void IniciarMovimientoHaciaSalida()
    {
        OcultarBocadillo();

        // --- MOVIDO: La notificación al gestor ahora se hace en la corutina ---
        // if (gestor != null) { gestor.NPCTermino(this); }

        if (gestor != null && gestor.puntoSalidaNPC != null)
        {
            destinoActual = gestor.puntoSalidaNPC.position;
            estadoActual = EstadoNPC.MoviendoASalida;
            Debug.Log($"{gameObject.name} se va hacia {destinoActual} (movimiento simple)... Estado: {estadoActual}");
        }
        else
        {
            // Log de errores y fallback igual que antes
            if (gestor == null) Debug.LogError("IniciarMovimientoHaciaSalida: gestor es null.");
            else if (gestor.puntoSalidaNPC == null) Debug.LogError("IniciarMovimientoHaciaSalida: gestor.puntoSalidaNPC es null. ¡Asigna el punto de salida en el Inspector del GestorNPCs!");
            Debug.LogError($"Punto de salida no config. o gestor no encontrado para NPC {gameObject.name}. Destruyendo.", this.gameObject);
            estadoActual = EstadoNPC.Inactivo; // Asegurar estado antes de destruir
            Destroy(gameObject);
        }
    }


    // --- Métodos Bocadillo UI (Sin cambios) ---
    void MostrarBocadillo(string texto, bool autoOcultar = false)
    {
        // --- LOG INICIAL ---
        Debug.LogWarning($"MostrarBocadillo INICIADO. Texto recibido: '{texto}', autoOcultar: {autoOcultar}");
        // ------------------
        if (coroutineOcultarBocadillo != null) {
            Debug.LogWarning("...Deteniendo corutina OcultarBocadillo anterior."); StopCoroutine(coroutineOcultarBocadillo); coroutineOcultarBocadillo = null; }
        if (instanciaBocadilloActual == null)
        {
            Debug.LogWarning("...Instancia de bocadillo es NULL, intentando crearla...");
            if (prefabBocadilloUI != null && puntoAnclajeBocadillo != null)
            {
                instanciaBocadilloActual = Instantiate(prefabBocadilloUI, puntoAnclajeBocadillo.position, puntoAnclajeBocadillo.rotation, puntoAnclajeBocadillo);
                textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>();
                if (textoBocadilloActual == null)
                {
                    Debug.LogError("¡Prefab Bocadillo UI sin TextMeshProUGUI!", instanciaBocadilloActual);
                    Destroy(instanciaBocadilloActual);
                    instanciaBocadilloActual = null; return;
                }
                else
                {
                    Debug.LogWarning($"...Instancia creada. Texto principal encontrado: '{textoBocadilloActual.gameObject.name}'");
                }
            }
            else
            {
                if (prefabBocadilloUI == null) Debug.LogError("¡Falta asignar 'Prefab Bocadillo UI'!", this.gameObject);
                if (puntoAnclajeBocadillo == null) Debug.LogError("¡Falta asignar 'Punto Anclaje Bocadillo'!", this.gameObject);
                return;
            }
        }
        if (instanciaBocadilloActual != null)
        {
            // Asegurarse de tener la referencia al texto principal (podría haberse perdido?)
            if (textoBocadilloActual == null)
            {
                textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>(true);
                Debug.LogWarning("...Referencia a textoBocadilloActual era null, re-buscando."); // Log
            }

            if (textoBocadilloActual != null)
            {
                // --- LOG ANTES DE ASIGNAR TEXTO ---
                Debug.Log($"...Asignando texto: '{texto}' a componente TMP: {textoBocadilloActual.GetInstanceID()} en objeto '{textoBocadilloActual.gameObject.name}'");
                // ----------------------------------
                textoBocadilloActual.text = texto; // ASIGNAR TEXTO
                instanciaBocadilloActual.SetActive(true); // Asegurar que el bocadillo esté activo

                // --- LOG DESPUÉS DE ACTIVAR BOCADILLO ---
                Debug.Log($"...instanciaBocadilloActual activado (SetActive(true)). AutoOcultar = {autoOcultar}");
                // --------------------------------------

                if (autoOcultar && duracionFeedback > 0)
                {
                    // --- LOG ANTES DE INICIAR CORUTINA OCULTAR ---
                    Debug.Log($"...Iniciando corutina OcultarBocadilloDespuesDe({duracionFeedback}s)");
                    // -------------------------------------------
                    coroutineOcultarBocadillo = StartCoroutine(OcultarBocadilloDespuesDe(duracionFeedback));
                }
            }
            else
            {
                Debug.LogError("MostrarBocadillo: No se pudo encontrar/asignar textoBocadilloActual incluso después de instanciar.");
            }
        }
        Debug.Log("MostrarBocadillo TERMINADO."); // Log final
        if (instanciaBocadilloActual != null && textoBocadilloActual != null)
        {
            textoBocadilloActual.text = texto;
            instanciaBocadilloActual.SetActive(true);
            if (autoOcultar && duracionFeedback > 0) { coroutineOcultarBocadillo = StartCoroutine(OcultarBocadilloDespuesDe(duracionFeedback)); }
        }
    }
    IEnumerator OcultarBocadilloDespuesDe(float segundos) { yield return new WaitForSeconds(segundos); OcultarBocadillo(); coroutineOcultarBocadillo = null; }
    void OcultarBocadillo()
    {
        if (coroutineOcultarBocadillo != null) { StopCoroutine(coroutineOcultarBocadillo); coroutineOcultarBocadillo = null; }
        // Ocultar también el texto del temporizador <<--- AÑADIDO
        if (textoTemporizadorActual != null) { textoTemporizadorActual.gameObject.SetActive(false); }
        if (instanciaBocadilloActual != null) { instanciaBocadilloActual.SetActive(false); }
    }
    // --- Fin Métodos Bocadillo UI ---

    // GiveFeedback y CompararListasIngredientes (Sin cambios)
    void GiveFeedback(string message, AudioClip sound)
    {
        if (GestorAudio.Instancia != null) { GestorAudio.Instancia.ReproducirSonido(sound); }
        MostrarBocadillo(message, true);
    }
    bool CompararListasIngredientes(List<DatosIngrediente> lista1, List<DatosIngrediente> lista2)
    {
        if (lista1 == null || lista2 == null || lista1.Count != lista2.Count) return false;
        var tempLista1 = new List<DatosIngrediente>(lista1);
        var tempLista2 = new List<DatosIngrediente>(lista2);
        foreach (var item1 in tempLista1) { bool found = false; for (int i = 0; i < tempLista2.Count; i++) { if (item1 == tempLista2[i]) { tempLista2.RemoveAt(i); found = true; break; } } if (!found) return false; }
        return tempLista2.Count == 0;
    }

    void OnDestroy()
    {
        if (instanciaBocadilloActual != null) { Destroy(instanciaBocadilloActual); }
        if (coroutineOcultarBocadillo != null) { StopCoroutine(coroutineOcultarBocadillo); }
        if (coroutineRetrasarSalida != null) { StopCoroutine(coroutineRetrasarSalida); }
    }

    // --- NUEVO: Método llamado cuando se agota el tiempo ---
    void TiempoAgotado()
    {
        Debug.Log($"{gameObject.name}: ¡Se acabó el tiempo de espera!");
        if (estadoActual != EstadoNPC.EnVentanaEsperando) return; // Doble check

        estadoActual = EstadoNPC.ProcesandoEntrega; // Poner en estado intermedio

        // Ocultar temporizador explícitamente
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);

        // Mostrar mensaje de enfado y reproducir sonido
        AudioClip sonidoAUsar = (sonidoTiempoAgotado != null) ? sonidoTiempoAgotado : sonidoPocionIncorrecta;
        GiveFeedback(mensajeTiempoAgotado, sonidoAUsar);

        // Iniciar secuencia de salida (con el retraso de feedback normal)
        Irse();
    }

    // --- NUEVO: Método para actualizar la UI del temporizador ---
    void ActualizarUITemporizador()
    {
        // Salir si no estamos en un estado de espera relevante
        if (estadoActual != EstadoNPC.EnVentanaEsperando && estadoActual != EstadoNPC.EsperandoAtencion)
        {
            // Opcionalmente, ocultar el texto si no estamos esperando
            if (textoTemporizadorActual != null && textoTemporizadorActual.gameObject.activeSelf)
            {
                // textoTemporizadorActual.gameObject.SetActive(false);
            }
            return;
        }

        // Re-buscar referencia si es nula (como antes)
        if (textoTemporizadorActual == null)
        {
            if (instanciaBocadilloActual != null)
            {
                // Debug.LogWarning("ActualizarUITemporizador: Timer NULL. Buscando...");
                Transform timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");
                if (timerTransform != null)
                {
                    textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();
                    if (textoTemporizadorActual != null) { textoTemporizadorActual.gameObject.SetActive(true); }
                    else { Debug.LogError("Objeto timer encontrado SIN componente TMP!"); return; }
                }
                else { Debug.LogError("No se encontró RUTA a TextoTemporizador"); return; }
            }
            else { return; } // No hay bocadillo para buscar
        }

        // --- Decidir qué tiempo mostrar basado en el estado actual ---
        float tiempoParaMostrar = 0f;
        if (estadoActual == EstadoNPC.EsperandoAtencion)
        {
            tiempoParaMostrar = tiempoRestanteEsperaAtencion;
        }
        else if (estadoActual == EstadoNPC.EnVentanaEsperando)
        {
            tiempoParaMostrar = tiempoRestanteEspera;
        }
        // ---------------------------------------------------------

        // Asegurarse de que esté activo (redundante si la búsqueda anterior funciona)
        if (!textoTemporizadorActual.gameObject.activeSelf)
        {
            textoTemporizadorActual.gameObject.SetActive(true);
        }

        // Calcular y asignar el texto
        int tiempoInt = Mathf.CeilToInt(tiempoParaMostrar);
        // Evitar mostrar 0 o negativos, mostrar 1 como mínimo mientras corre
        //tiempoInt = Mathf.Max(1, tiempoInt);
        textoTemporizadorActual.text = tiempoInt.ToString();

        // Opcional: Cambiar color si queda poco tiempo (usando el timer correspondiente)
        // float tiempoRef = (estadoActual == EstadoNPC.EsperandoAtencion) ? tiempoRestanteEsperaAtencion : tiempoRestanteEspera;
        // if (tiempoRef < 5.5f) { textoTemporizadorActual.color = Color.red; }
        // else { textoTemporizadorActual.color = Color.white; } // O color original del timer
    }

    // --- NUEVA FUNCIÓN AUXILIAR ---
    // Devuelve el texto correcto para el pedido actual (específico, genérico o nombre)
    private string ObtenerTextoOriginalPedido()
    {
        string textoResult = ""; // Texto a devolver

        // Comprobar si hay un pedido activo
        if (pedidoActual == null) return "¿Necesitas algo?"; // Texto por defecto si no hay pedido

        // 1. Buscar diálogo específico
        DialogoEspecificoNPC especifico = null;
        if (dialogosEspecificos != null)
        {
            especifico = dialogosEspecificos.FirstOrDefault(d => d != null && d.receta == pedidoActual);
        }
        if (especifico != null && !string.IsNullOrEmpty(especifico.dialogoUnico))
        {
            textoResult = especifico.dialogoUnico;
        }
        // 2. Buscar diálogo genérico si no hay específico
        else if (pedidoActual.dialogosPedidoGenericos != null && pedidoActual.dialogosPedidoGenericos.Count > 0)
        {
            List<string> opcionesValidas = pedidoActual.dialogosPedidoGenericos.Where(d => !string.IsNullOrEmpty(d)).ToList();
            if (opcionesValidas.Count > 0) { textoResult = opcionesValidas[Random.Range(0, opcionesValidas.Count)]; }
        }
        // 3. Usar nombre como fallback si todo falla
        if (string.IsNullOrEmpty(textoResult))
        {
            textoResult = $"Mmm... ¿Tendrías una {pedidoActual.nombreResultadoPocion}?";
        }
        return textoResult;
    }
    // --- FIN FUNCIÓN AUXILIAR ---

    // --- NUEVA CORUTINA ---
    // Espera a que termine el feedback de error y restaura la UI del pedido original
    private IEnumerator RestaurarPedidoDespuesDeFeedback()
    {
        // Esperar un poquito más que la duración del feedback para asegurar que el bocadillo
        // se ocultó o que estamos listos para sobrescribir el texto.
        yield return new WaitForSeconds(duracionFeedback + 0.1f);

        // Doble comprobación: ¿Sigue el NPC esperando ESTE pedido?
        // (Podría haber recibido otra poción o haberse ido mientras esperábamos)
        if (estadoActual == EstadoNPC.EnVentanaEsperando && pedidoActual != null)
        {
            Debug.Log($"Restaurando bocadillo del pedido original para {gameObject.name} después de feedback incorrecto.");
            // Obtener el texto del pedido original
            string textoOriginalPedido = ObtenerTextoOriginalPedido();
            // Volver a mostrar el bocadillo con el pedido, SIN auto-ocultar
            MostrarBocadillo(textoOriginalPedido, false);

            // Reactivar y actualizar el temporizador si existe
            if (textoTemporizadorActual != null)
            {
                textoTemporizadorActual.gameObject.SetActive(true); // Asegurar que esté activo
                ActualizarUITemporizador(); // Poner el tiempo restante actual
            }
        }
        else
        {
            Debug.Log($"No se restaura bocadillo para {gameObject.name}, estado cambió a {estadoActual} durante feedback.");
        }
    }
    // --- FIN NUEVA CORUTINA ---

    // Se llama cuando se agota el tiempo de espera para ATENCIÓN inicial
    void TiempoAgotadoEsperandoAtencion()
    {
        Debug.Log($"{gameObject.name}: ¡Se cansó de esperar atención!");
        if (estadoActual != EstadoNPC.EsperandoAtencion) return; // Doble check

        estadoActual = EstadoNPC.ProcesandoEntrega; // Cambiar estado para que se vaya

        // Ocultar bocadillo "..." o lo que hubiera
        //OcultarBocadillo();

        // Mostrar mensaje de enfado y sonido (usamos los mismos de tiempo agotado del pedido)
        AudioClip sonidoAUsar = (sonidoTiempoAgotado != null) ? sonidoTiempoAgotado : sonidoPocionIncorrecta;
        GiveFeedback(mensajeTiempoEsperaAgotado, sonidoAUsar);

        // Iniciar secuencia de salida
        Irse();

        // Opcional: Penalizar reputación
        // if(GestorJuego.Instance != null) GestorJuego.Instance.ModificarReputacion(-2); // Penalización por ignorar
    }

    // Método público llamado por InteraccionJugador cuando pulsa 'E' sobre este NPC esperando atención
    public void IniciarPedidoYTimer()
    {
        // Solo proceder si realmente estaba esperando atención
        if (estadoActual != EstadoNPC.EsperandoAtencion)
        {
            Debug.LogWarning($"Se intentó iniciar pedido para {gameObject.name} pero su estado era {estadoActual}");
            return;
        }

        Debug.Log($"Iniciando pedido y timer principal para {gameObject.name}");
        estadoActual = EstadoNPC.EnVentanaEsperando; // Cambiar al estado de espera de poción
        SolicitarPocion(); // Genera el pedido, muestra el bocadillo correcto, busca TextoTemporizador
                           // Asegurar que el timer principal se resetee (aunque SolicitarPocion ya lo hacía, doble check)
        tiempoRestanteEspera = tiempoMaximoEspera;
        ActualizarUITemporizador(); // Mostrar el tiempo inicial
        // Asegurar que el texto del timer esté visible (SolicitarPocion también lo intenta)
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);
    }

    // Funciones helper para InteraccionJugador
    public bool EstaEsperandoAtencion() { return estadoActual == EstadoNPC.EsperandoAtencion; }
    public bool EstaEsperandoEntrega() { return estadoActual == EstadoNPC.EnVentanaEsperando; }

}