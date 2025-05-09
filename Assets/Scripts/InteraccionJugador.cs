using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// El enum ahora está fuera para ser accesible globalmente
public enum TipoItem { Nada, Ingrediente, FrascoVacio, FrascoLleno }

[RequireComponent(typeof(AudioSource))]
public class InteraccionJugador : MonoBehaviour
{
    // --- Variables públicas y privadas (igual que tu código) ---
    [Header("Configuración General")]
    public float distanciaInteraccion = 3.0f;
    public Camera camaraJugador;
    public LayerMask capaInteraccion;

    [Header("UI Item Sostenido")]
    public Image uiIconoItemSostenido;
    public TextMeshProUGUI uiNombreItemSostenido;
    public GameObject panelItemSostenido;

    [Header("UI Notificaciones")]
    public TextMeshProUGUI textoNotificacion;
    public float tiempoNotificacion = 2.5f;
    private float temporizadorNotificacion = 0f;

    [Header("Anclaje Item 3D")]
    public Transform puntoAnclajeItem3D;

    [Header("Sonidos Jugador")]
    public AudioClip sonidoRecogerItem;
    public AudioClip sonidoRecogerPocion;
    public AudioClip sonidoTirarItem;
    public AudioClip sonidoError;

    private AudioSource audioSourceJugador;

    // --- Variables para gestionar qué item se sostiene ---
    private ScriptableObject itemSostenido = null;
    private TipoItem tipoItemSostenido = TipoItem.Nada;
    public bool JugadorSostieneAlgo => tipoItemSostenido != TipoItem.Nada;
    private List<DatosIngrediente> contenidoFrascoLleno = null;
    private GameObject instanciaItem3D = null;

    // --- Referencias a Objetos Mirados ---
    private FuenteIngredientes fuenteIngredientesMirada = null;
    private Caldero calderoMiradoActual = null;
    private FuenteFrascos fuenteFrascosMirada = null;
    private NPCComprador npcMiradoActual = null;
    private LibroRecetasInteractuable libroMiradoActual = null;
    private PuertaCambioEscena puertaMiradaActual = null; // <<--- Variable ya presente
    private CamaInteractuable camaMiradaActual = null; // <<--- NUEVO
    private IngredienteRecolectable ingredienteRecolectableMirado = null; // <<--- NUEVO

    // --- Referencias a Sistemas Externos ---
    [Header("Configuración Pociones y Recetas")]
    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    [Header("Configuración NPCs")]
    public GestorCompradores gestorNPC;
    [Header("Interfaz Libro Recetas")]
    public ControladorLibroUI controladorLibroUI;


    void Start()
    {
        // ... (Start igual que tu código) ...
        audioSourceJugador = GetComponent<AudioSource>();
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
        if (puntoAnclajeItem3D == null) Debug.LogWarning("¡FALTA ASIGNAR 'Punto Anclaje Item 3D'!", this.gameObject);
        if (gestorNPC == null) Debug.LogError("¡FALTA ASIGNAR 'Gestor NPC'!", this.gameObject);
        if (catalogoRecetas == null) Debug.LogError("¡FALTA ASIGNAR 'Catalogo Recetas'!", this.gameObject);
        if (materialPocionDesconocida == null) Debug.LogWarning("Material Pocion Desconocida no asignado.");
        if (controladorLibroUI == null) Debug.LogWarning("¡FALTA ASIGNAR 'Controlador Libro UI'!", this.gameObject);
    }

    void Update()
    {
        // ... (Update igual que tu código, incluyendo check libro y lógica Q) ...
        if (controladorLibroUI != null && controladorLibroUI.gameObject.activeInHierarchy) { return; }
        ManejarInteraccionMirada();
        ManejarEntradaAccion();
        ManejarNotificaciones();
        if (Input.GetKeyDown(KeyCode.Q)) { if (tipoItemSostenido != TipoItem.Nada) { ReproducirSonidoJugador(sonidoTirarItem); LimpiarItemSostenido(); } }
        if (Input.GetMouseButtonDown(1) && tipoItemSostenido == TipoItem.FrascoLleno) { MostrarContenidoFrascoLleno(); }
    }

    // --- ManejarInteraccionMirada (REVISADO Y CORREGIDO para Cama) ---
    // --- ManejarInteraccionMirada (CON RESET CORREGIDO) ---
    void ManejarInteraccionMirada()
    {
        RaycastHit hit;
        bool golpeoAlgo = Physics.Raycast(camaraJugador.transform.position, camaraJugador.transform.forward, out hit, distanciaInteraccion, capaInteraccion);
        GameObject objetoGolpeado = golpeoAlgo ? hit.collider.gameObject : null;
        // La variable hayNuevoObjetivo la quitamos de aquí, la manejaremos diferente

        // --- 1. Resetear el objeto que YA NO se está mirando ---
        // La condición ahora es: Si NO golpeamos nada O golpeamos algo DIFERENTE

        if (fuenteIngredientesMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteIngredientesMirada.gameObject))
        {
            // Debug.Log("Dejando de mirar Fuente Ingredientes"); // Log Opcional
            if (fuenteIngredientesMirada != null) fuenteIngredientesMirada.OcultarInformacion();
            fuenteIngredientesMirada = null;
        }
        if (calderoMiradoActual != null && (!golpeoAlgo || objetoGolpeado != calderoMiradoActual.gameObject))
        {
            // Debug.Log("Dejando de mirar Caldero"); // Log Opcional
            calderoMiradoActual = null;
        }
        if (fuenteFrascosMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteFrascosMirada.gameObject))
        {
            // Debug.Log("Dejando de mirar Fuente Frascos"); // Log Opcional
            if (fuenteFrascosMirada != null) fuenteFrascosMirada.OcultarInformacion();
            fuenteFrascosMirada = null;
        }
        if (npcMiradoActual != null && (!golpeoAlgo || objetoGolpeado != npcMiradoActual.gameObject))
        {
            // Debug.Log("Dejando de mirar NPC"); // Log Opcional
            npcMiradoActual = null;
        }
        if (libroMiradoActual != null && (!golpeoAlgo || objetoGolpeado != libroMiradoActual.gameObject))
        {
            // Debug.Log("Dejando de mirar Libro"); // Log Opcional
            if (libroMiradoActual != null) libroMiradoActual.OcultarInformacion();
            libroMiradoActual = null;
        }
        if (puertaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != puertaMiradaActual.gameObject))
        {
            // Debug.Log("Dejando de mirar Puerta"); // Log Opcional
            if (puertaMiradaActual != null) puertaMiradaActual.OcultarInformacion();
            puertaMiradaActual = null;
        }
        if (camaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != camaMiradaActual.gameObject))
        {
            // Debug.Log("Dejando de mirar Cama"); // Log Opcional
            if (camaMiradaActual != null) camaMiradaActual.OcultarInformacion();
            camaMiradaActual = null;
        }
        // --- AÑADIR RESET INGREDIENTE RECOLECTABLE ---
        if (ingredienteRecolectableMirado != null && (!golpeoAlgo || objetoGolpeado != ingredienteRecolectableMirado.gameObject))
        {
            if (ingredienteRecolectableMirado != null) ingredienteRecolectableMirado.OcultarInformacion(); // Llama a ocultar
            ingredienteRecolectableMirado = null;
        }
        // --- FIN RESET INGREDIENTE RECOLECTABLE ---



        // --- 2. Si golpeamos algo, intentar identificarlo y actualizar referencia ---
        if (golpeoAlgo)
        {
            // Comprobamos si el objeto golpeado YA es el que estamos mirando actualmente.
            // Si ya lo es, no necesitamos hacer nada más en esta sección.
            bool yaLoMiraba = (fuenteIngredientesMirada != null && objetoGolpeado == fuenteIngredientesMirada.gameObject) ||
                              (calderoMiradoActual != null && objetoGolpeado == calderoMiradoActual.gameObject) ||
                              (fuenteFrascosMirada != null && objetoGolpeado == fuenteFrascosMirada.gameObject) ||
                              (npcMiradoActual != null && objetoGolpeado == npcMiradoActual.gameObject) ||
                              (libroMiradoActual != null && objetoGolpeado == libroMiradoActual.gameObject) ||
                              (puertaMiradaActual != null && objetoGolpeado == puertaMiradaActual.gameObject) ||
                              (camaMiradaActual != null && objetoGolpeado == camaMiradaActual.gameObject);

            if (!yaLoMiraba) // ¡Es un objetivo NUEVO! Intentar identificarlo.
            {
                // Prioridad: En cuanto encuentra uno, guarda referencia, muestra info (si aplica) y sale.
                FuenteIngredientes ingSrc = objetoGolpeado.GetComponent<FuenteIngredientes>(); if (ingSrc != null) { fuenteIngredientesMirada = ingSrc; ingSrc.MostrarInformacion(); return; }
                FuenteFrascos fraSrc = objetoGolpeado.GetComponent<FuenteFrascos>(); if (fraSrc != null) { fuenteFrascosMirada = fraSrc; fraSrc.MostrarInformacion(); return; }
                Caldero caldSrc = objetoGolpeado.GetComponent<Caldero>(); if (caldSrc != null) { calderoMiradoActual = caldSrc; return; }
                NPCComprador npcCtrl = objetoGolpeado.GetComponentInParent<NPCComprador>(); if (npcCtrl != null) { npcMiradoActual = npcCtrl; return; }
                LibroRecetasInteractuable libroCtrl = objetoGolpeado.GetComponent<LibroRecetasInteractuable>(); if (libroCtrl != null) { libroMiradoActual = libroCtrl; libroCtrl.MostrarInformacion(); return; }
                PuertaCambioEscena puertaCtrl = objetoGolpeado.GetComponent<PuertaCambioEscena>(); if (puertaCtrl != null) { puertaMiradaActual = puertaCtrl; puertaMiradaActual.MostrarInformacion(); return; }
                CamaInteractuable camaCtrl = objetoGolpeado.GetComponent<CamaInteractuable>(); if (camaCtrl != null) { camaMiradaActual = camaCtrl; camaMiradaActual.MostrarInformacion(); }
                IngredienteRecolectable ingRecCtrl = objetoGolpeado.GetComponent<IngredienteRecolectable>();
                if (ingRecCtrl != null)
                {
                    // Ocultar otros indicadores por si acaso
                    if (puertaMiradaActual != null) { puertaMiradaActual.OcultarInformacion(); puertaMiradaActual = null; }
                    if (camaMiradaActual != null) { camaMiradaActual.OcultarInformacion(); camaMiradaActual = null; }
                    // ... etc, si fuera necesario limpiar otros ...

                    ingredienteRecolectableMirado = ingRecCtrl;
                    ingredienteRecolectableMirado.MostrarInformacion();
                    // No ponemos return porque es el último
                }
            }
            // Si ya lo miraba, no hacemos nada aquí.
        }
        // Si no golpeamos nada (golpeoAlgo == false), el bloque de reseteo al principio ya limpió todo.
    }

    // --- ManejarEntradaAccion (CON LÓGICA COMPLETA RESTAURADA) ---
    void ManejarEntradaAccion()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Log opcional para ver qué se mira al pulsar E
            // Debug.Log($"Tecla E - Estado Mirada: Ingrediente={(fuenteIngredientesMirada != null)}, Frasco={(fuenteFrascosMirada != null)}, Caldero={(calderoMiradoActual != null)}, NPC={(npcMiradoActual != null)}, Libro={(libroMiradoActual != null)}, Puerta={(puertaMiradaActual != null)}");

            // Prioridad de interacción
            if (fuenteIngredientesMirada != null) // 1. FUENTE DE INGREDIENTES
            {
                if (tipoItemSostenido == TipoItem.Nada)
                { // Solo recoger si no tenemos nada
                    DatosIngrediente r = fuenteIngredientesMirada.IntentarRecoger();
                    if (r != null) EstablecerItemSostenido(r, TipoItem.Ingrediente);
                    else MostrarNotificacion($"¡No quedan más {fuenteIngredientesMirada.datosIngrediente.nombreIngrediente}!", -1f, true);
                }
                else MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
            }
            else if (fuenteFrascosMirada != null) // 2. FUENTE DE FRASCOS
            {
                if (tipoItemSostenido == TipoItem.Nada)
                { // Solo recoger si no tenemos nada
                    DatosFrasco r = fuenteFrascosMirada.IntentarRecoger();
                    if (r != null) EstablecerItemSostenido(r, TipoItem.FrascoVacio);
                    else MostrarNotificacion($"¡No quedan más {fuenteFrascosMirada.datosFrasco.nombreItem}!", -1f, true);
                }
                else MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
            }
            else if (calderoMiradoActual != null) // 3. CALDERO
            {
                switch (tipoItemSostenido)
                {
                    case TipoItem.Ingrediente: // Echar ingrediente
                        if (itemSostenido is DatosIngrediente i) { if (calderoMiradoActual.AnadirIngrediente(i)) LimpiarItemSostenido(); }
                        break;
                    case TipoItem.FrascoVacio: // Recoger poción
                        if (calderoMiradoActual.EstaPocionLista()) { DatosIngrediente[] c = calderoMiradoActual.RecogerPocion(); if (c != null) LlenarFrascoSostenido(c); }
                        else if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover) { MostrarNotificacion("Debes remover la mezcla primero.", -1f, true); }
                        else { MostrarNotificacion("Caldero vacío o poción no lista.", -1f, true); }
                        break;
                    case TipoItem.Nada: // Iniciar remover o aviso
                        if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover) { calderoMiradoActual.IntentarIniciarRemovido(); }
                        else if (calderoMiradoActual.EstaPocionLista()) { MostrarNotificacion("Necesitas frasco vacío.", -1f, true); }
                        // Si está Ocioso o ya RemovidoFallido, no hacer nada con E y sin item
                        break;
                    case TipoItem.FrascoLleno: // Aviso
                        MostrarNotificacion("Ya tienes una poción.", -1f, true);
                        break;
                }
            }
            // --- Prioridad 4: ¿NPC? (LÓGICA REESTRUCTURADA) --- <<<--- REEMPLAZAR ESTE BLOQUE
            else if (npcMiradoActual != null)
            {
                Debug.Log($"Interactuando con NPC: {npcMiradoActual.gameObject.name}");

                // Intentar obtener el script específico del cliente
                if (npcMiradoActual.TryGetComponent<NPCComprador>(out NPCComprador clienteTienda))
                {
                    // Sub-Prioridad 4.1: ¿Está esperando ATENCIÓN inicial?
                    if (clienteTienda.EstaEsperandoAtencion())
                    {
                        clienteTienda.IniciarPedidoYTimer(); // El jugador lo atiende
                    }
                    // Sub-Prioridad 4.2: ¿Está esperando ENTREGA y llevamos frasco lleno?
                    else if (clienteTienda.EstaEsperandoEntrega() && tipoItemSostenido == TipoItem.FrascoLleno)
                    {
                        // Intentar entregar la poción
                        if (contenidoFrascoLleno != null) { clienteTienda.IntentarEntregarPocion(contenidoFrascoLleno); LimpiarItemSostenido(); }
                        else { MostrarNotificacion("Error interno frasco.", -1f, true); }
                    }
                    // Sub-Prioridad 4.3: ¿Está esperando ENTREGA pero NO llevamos frasco lleno?
                    else if (clienteTienda.EstaEsperandoEntrega())
                    {
                        MostrarNotificacion("Necesitas la poción que pidió.", -1f, true);
                    }
                    // Otro estado del cliente? (Moviendose, etc.)
                    else { MostrarNotificacion("Parece ocupado ahora mismo...", 2f, false); }
                }
                // Sub-Prioridad 4.X: ¿Es un Vendedor?
                else if (npcMiradoActual.TryGetComponent<NPCVendedor>(out NPCVendedor vendedor))
                {
                    vendedor.AbrirInterfazTienda();
                }
                // Sub-Prioridad 4.Y: ¿Es de Diálogo?
                else if (npcMiradoActual.TryGetComponent<NPCDialogo>(out NPCDialogo dialogo))
                {
                    dialogo.IniciarDialogo();
                }
                // Añade más 'else if (TryGetComponent...)' para otros tipos de NPC
                // ...

                // Si no es ninguno de los tipos conocidos
                else { MostrarNotificacion("No parece querer nada ahora...", 2f, false); }
            }
            else if (libroMiradoActual != null) // 5. LIBRO
            {
                Debug.Log("Interactuando con el libro...");
                if (controladorLibroUI != null)
                {
                    controladorLibroUI.AbrirLibro();
                    // Limpiar otras referencias al abrir libro
                    fuenteIngredientesMirada = null; calderoMiradoActual = null; fuenteFrascosMirada = null; npcMiradoActual = null; puertaMiradaActual = null;
                }
                else { Debug.LogError("¡ControladorLibroUI no asignado!"); MostrarNotificacion("Error al abrir libro.", -1f, true); }
            }
            // --- INTERACCIÓN CON PUERTA (CONDICIONAL) --- <<<--- MODIFICADO
            else if (puertaMiradaActual != null)
            {
                // Comprobar la hora ANTES de intentar cambiar de escena
                if (GestorJuego.Instance != null)
                {
                    if (GestorJuego.Instance.horaActual != HoraDelDia.Noche)
                    {
                        // NO es de noche: Proceder normalmente
                        Debug.Log($"Interactuando con puerta -> {puertaMiradaActual.nombreEscenaDestino}");
                        puertaMiradaActual.CambiarEscena(); // Llama al método normal
                    }
                    else
                    {
                        // SÍ es de noche: Mostrar notificación y NO cambiar escena
                        Debug.Log("Intento de usar puerta de noche bloqueado.");
                        MostrarNotificacion("Será mejor que no salga ahora, podría encontrarme con un troll...", 3f, false); // Tu mensaje de excusa
                        // Opcional: Reproducir sonido de puerta cerrada/bloqueada
                        // ReproducirSonidoJugador(sonidoPuertaBloqueada);
                    }
                }
                else
                {
                    // Error si no encontramos el GestorJuego
                    Debug.LogError("No se encontró GestorJuego al interactuar con la puerta.");
                    MostrarNotificacion("Error del sistema de tiempo.", 2f, true);
                }
            }
            else if (camaMiradaActual != null) // 7. CAMA
            {
                Debug.Log("Interactuando con la cama...");
                if (GestorJuego.Instance != null)
                {
                    // 1. Preguntar al Gestor si se puede dormir
                    if (GestorJuego.Instance.PuedeDormir())
                    {
                        // 2. Si sí, decirle al gestor que inicie la secuencia
                        GestorJuego.Instance.IrADormir();
                    }
                    else
                    {
                        // 3. Si no, mostrar notificación DESDE AQUÍ
                        MostrarNotificacion("Solo puedes dormir por la noche...", 2f, false);
                    }
                }
                else { Debug.LogError("No se encontró GestorJuego para intentar dormir."); }
            }

            // --- INTERACCIÓN CON INGREDIENTE RECOLECTABLE --- <<<--- AÑADE ESTE BLOQUE ENTERO
            else if (ingredienteRecolectableMirado != null) // 8. INGREDIENTE RECOLECTABLE (Bosque)
            {
                // Solo permitir recolectar si el jugador no tiene nada en las manos
                if (tipoItemSostenido == TipoItem.Nada)
                {
                    Debug.Log($"Intentando recolectar: {ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente}");
                    ingredienteRecolectableMirado.Recolectar(); // Llama al método del script del objeto
                                                                // Al recolectar, el objeto se destruye y añade stock al GestorJuego.
                                                                // No necesitamos hacer EstablecerItemSostenido aquí.
                }
                else
                {
                    // Si el jugador tiene algo, mostrar notificación
                    MostrarNotificacion("Tienes las manos llenas para recolectar.", 2f, true);
                }
            }
            // --- FIN INTERACCIÓN INGREDIENTE RECOLECTABLE ---

            // --- FIN INTERACCIÓN CAMA ---
        }
    }


    // --- Métodos para gestionar el item sostenido y la UI ---
    // --- EstablecerItemSostenido (CON ROTACIÓN ESPECÍFICA) --- <<<--- EDITADO
    void EstablecerItemSostenido(ScriptableObject itemData, TipoItem tipo)
    {
        itemSostenido = itemData;
        tipoItemSostenido = tipo;
        contenidoFrascoLleno = null; // Limpiar contenido anterior
        LimpiarInstanciaItem3D();    // Limpiar modelo 3D anterior

        // Variables para datos del item
        string nombreMostrar = "Desconocido";
        Sprite iconoMostrar = null;
        GameObject prefab3D = null;
        Material matVacio = null;
        Vector3 rotacionItemEspecifica = Vector3.zero; // <<--- NUEVO: Variable para guardar rotación

        // Extraer datos generales Y la rotación específica del ScriptableObject
        if (tipo == TipoItem.Ingrediente && itemData is DatosIngrediente ingData)
        {
            nombreMostrar = ingData.nombreIngrediente;
            iconoMostrar = ingData.icono;
            prefab3D = ingData.prefabModelo3D;
            rotacionItemEspecifica = ingData.rotacionEnMano; // <<--- Leer rotación del ingrediente
        }
        else if (tipo == TipoItem.FrascoVacio && itemData is DatosFrasco fraData)
        {
            nombreMostrar = fraData.nombreItem;
            iconoMostrar = fraData.icono;
            prefab3D = fraData.prefabModelo3D;
            matVacio = fraData.materialVacio;
            rotacionItemEspecifica = fraData.rotacionEnMano; // <<--- Leer rotación del frasco
        }

        // Actualizar UI del HUD (sin cambios aquí)
        if (panelItemSostenido != null)
        {
            if (uiIconoItemSostenido != null && iconoMostrar != null) { uiIconoItemSostenido.sprite = iconoMostrar; uiIconoItemSostenido.enabled = true; }
            else if (uiIconoItemSostenido != null) { uiIconoItemSostenido.enabled = false; }
            if (uiNombreItemSostenido != null) { uiNombreItemSostenido.text = nombreMostrar; }
            panelItemSostenido.SetActive(true);
        }

        // Instanciar y configurar el modelo 3D en la mano
        if (prefab3D != null && puntoAnclajeItem3D != null)
        {
            instanciaItem3D = Instantiate(prefab3D, puntoAnclajeItem3D);
            instanciaItem3D.transform.localPosition = Vector3.zero;         // Resetear pos local
            instanciaItem3D.transform.localRotation = Quaternion.identity; // Resetear rotación local a la del anclaje

            // --- APLICAR ROTACIÓN ESPECÍFICA DEL ITEM --- <<<--- AÑADIDO
            // Si definimos una rotación en el ScriptableObject (no es 0,0,0), la aplicamos
            if (rotacionItemEspecifica != Vector3.zero)
            {
                // Multiplica la rotación actual (identity) por la rotación adicional leída del asset
                instanciaItem3D.transform.localRotation *= Quaternion.Euler(rotacionItemEspecifica);
                Debug.Log($"Aplicando rotación en mano {rotacionItemEspecifica} a {nombreMostrar}"); // Log opcional
            }
            // -------------------------------------------

            // Configuración específica si es un frasco vacío (sin cambios aquí)
            if (tipo == TipoItem.FrascoVacio)
            {
                FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
                if (scriptFrasco != null)
                {
                    if (itemData is DatosFrasco dFr) { scriptFrasco.materialVacio = dFr.materialVacio; scriptFrasco.materialLleno = dFr.materialLleno; }
                    scriptFrasco.EstablecerApariencia(false);
                }
                else
                {
                    MeshRenderer rend = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
                    if (rend != null && matVacio != null) { rend.material = matVacio; }
                    else if (rend == null) { Debug.LogWarning($"Prefab {prefab3D.name} sin MeshRenderer."); }
                    else if (matVacio == null && itemData is DatosFrasco) { Debug.LogWarning($"DatosFrasco {((DatosFrasco)itemData).name} sin material vacío."); }
                }
            }
        }
        else if (tipo != TipoItem.Nada) { /* ... Advertencias si falta config ... */ }

        ReproducirSonidoJugador(sonidoRecogerItem);
    }

    void LlenarFrascoSostenido(DatosIngrediente[] contenidoArray)
    {
        if (tipoItemSostenido != TipoItem.FrascoVacio || !(itemSostenido is DatosFrasco frascoData)) { Debug.LogError("Error llenar frasco: No se sostiene frasco vacío."); return; }
        if (contenidoArray == null || contenidoArray.Length == 0) { Debug.LogError("Error llenar frasco: Sin ingredientes."); return; }

        List<DatosIngrediente> contenidoLista = new List<DatosIngrediente>(contenidoArray);
        tipoItemSostenido = TipoItem.FrascoLleno;
        contenidoFrascoLleno = contenidoLista;

        // Valores por defecto
        string nombrePocion = "Poción Desconocida";
        Material materialAplicar = materialPocionDesconocida;
        PedidoPocionData recetaEncontrada = null; // Inicializar

        // Buscar en el catálogo
        if (catalogoRecetas != null)
        {
            recetaEncontrada = catalogoRecetas.BuscarRecetaPorIngredientes(contenidoLista);

            // --- LOG 1: QUÉ RECETA SE ENCONTRÓ ---
            if (recetaEncontrada != null)
            {
                nombrePocion = recetaEncontrada.nombreResultadoPocion;
                Debug.Log($"Frasco - Receta Encontrada: {nombrePocion}, Material Asignado: {(recetaEncontrada.materialResultado != null ? recetaEncontrada.materialResultado.name : "NINGUNO")}");
                if (recetaEncontrada.materialResultado != null)
                {
                    materialAplicar = recetaEncontrada.materialResultado;
                }
                else { Debug.LogWarning($"Receta '{nombrePocion}' sin Material Resultado."); }
            }
            else { Debug.Log("Frasco - No se encontró receta."); }
            // --- FIN LOG 1 ---

        }
        else { Debug.LogError("¡Catalogo Recetas no asignado!"); }

        // --- LOG 2: QUÉ MATERIAL SE VA A APLICAR ---
        Debug.Log($"Frasco - Material final a aplicar: {(materialAplicar != null ? materialAplicar.name : "NINGUNO (Usando Desconocido o Falló)")}");
        // --- FIN LOG 2 ---

        // --- Actualizar UI del HUD ---
        if (panelItemSostenido != null) { /* ... */ if (uiIconoItemSostenido != null && frascoData.icono != null) { uiIconoItemSostenido.sprite = frascoData.icono; uiIconoItemSostenido.enabled = true; } if (uiNombreItemSostenido != null) uiNombreItemSostenido.text = nombrePocion; panelItemSostenido.SetActive(true); }

        // --- Actualizar Modelo 3D Frasco ---
        if (instanciaItem3D != null)
        {
            MeshRenderer rendererFrasco = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
            if (rendererFrasco != null && materialAplicar != null)
            {
                rendererFrasco.material = Instantiate(materialAplicar);
                // Debug.Log($"Aplicado material '{materialAplicar.name}' al frasco en mano."); // Log original, puedes dejarlo
            }
            else if (rendererFrasco != null) { Debug.LogWarning("No se pudo aplicar material al frasco (Material nulo?)."); }
            else { Debug.LogWarning($"No se encontró MeshRenderer en {instanciaItem3D.name} para aplicar material."); }

            FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
            if (scriptFrasco != null) { scriptFrasco.Llenar(contenidoArray); }
        }

        Debug.Log($"Frasco llenado! Nombre: '{nombrePocion}'. Ingredientes: {contenidoLista.Count}.");
        ReproducirSonidoJugador(sonidoRecogerPocion);
    }

    // Limpia el item sostenido actual
    public void LimpiarItemSostenido()
    {
        itemSostenido = null;
        tipoItemSostenido = TipoItem.Nada;
        contenidoFrascoLleno = null;
        if (panelItemSostenido != null) { panelItemSostenido.SetActive(false); } // Ocultar HUD
        LimpiarInstanciaItem3D(); // Destruir modelo 3D
    }

    // Destruye el modelo 3D que se tiene en la mano
    void LimpiarInstanciaItem3D()
    {
        if (instanciaItem3D != null)
        {
            Destroy(instanciaItem3D);
            instanciaItem3D = null;
        }
    }

    // Muestra un mensaje temporal en la UI de notificaciones
    public void MostrarNotificacion(string mensaje, float duracion = -1f, bool conSonidoError = false)
    {
        if (textoNotificacion != null)
        {
            textoNotificacion.text = mensaje;
            textoNotificacion.gameObject.SetActive(true);
            // Usar duración pasada o la por defecto
            temporizadorNotificacion = (duracion > 0) ? duracion : tiempoNotificacion;
            if (conSonidoError) ReproducirSonidoJugador(sonidoError);
        }
        else { Debug.LogWarning("UI Texto Notificaciones no asignada!"); }
    }

    // Controla el temporizador para ocultar notificaciones
    void ManejarNotificaciones()
    {
        if (temporizadorNotificacion > 0)
        {
            temporizadorNotificacion -= Time.deltaTime;
            if (temporizadorNotificacion <= 0)
            {
                if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
            }
        }
    }

    // Muestra los ingredientes del frasco lleno (llamado con clic derecho)
    void MostrarContenidoFrascoLleno()
    {
        if (contenidoFrascoLleno != null && contenidoFrascoLleno.Count > 0)
        {
            string t = "Contenido: ";
            for (int i = 0; i < contenidoFrascoLleno.Count; i++)
            {
                // Añadir "?" si un ingrediente es nulo por alguna razón
                t += (contenidoFrascoLleno[i]?.nombreIngrediente ?? "???");
                t += (i < contenidoFrascoLleno.Count - 1 ? ", " : "");
            }
            MostrarNotificacion(t, 4f); // Mostrar por más tiempo
        }
        else
        {
            // Esto no debería pasar si tipoItemSostenido es FrascoLleno, pero por si acaso
            MostrarNotificacion("Frasco lleno pero sin contenido registrado.", 2f, true);
        }
    }

    // Reproduce un sonido usando el AudioSource del jugador
    void ReproducirSonidoJugador(AudioClip clip)
    {
        if (audioSourceJugador != null && clip != null)
        {
            audioSourceJugador.PlayOneShot(clip);
        }
    }

} // Fin de la clase