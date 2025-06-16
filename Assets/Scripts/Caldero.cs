using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI; // Necesario para Image

[RequireComponent(typeof(AudioSource))]
public class Caldero : MonoBehaviour
{
    // --- Configuraci�n B�sica, Referencias, Ajustes Minijuego, Cursor (igual) ---
    [Header("Configuraci�n B�sica")]
    public int maximoIngredientes = 5;
    public List<DatosIngrediente> ingredientesActuales = new List<DatosIngrediente>();
    public enum EstadoCaldero { Ocioso, ListoParaRemover, Removiendo, PocionLista, RemovidoFallido }
    public EstadoCaldero estadoActual = EstadoCaldero.Ocioso;
    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;
    [Tooltip("C�mara espec�fica para la vista del minijuego. �Obligatoria!")]
    public Camera camaraMinijuego;
    [Tooltip("Objeto que contiene y rota la cuchara visualmente.")]
    public Transform pivoteRemover;
    [Tooltip("GameObject de la cuchara. Debe ser hijo de PivoteRemover y TENER UN COLLIDER.")]
    public GameObject objetoCuchara;
    [Header("Ajustes Minijuego Circular")]
    public float anguloObjetivoRemover = 1080f;
    public float radioMinimoGiro = 30f;
    public float sensibilidadGiro = 1.0f;
    public float velocidadVisualCucharaFija = 0f;
    [Header("Cursor Personalizado")]
    public Texture2D texturaCursorMinijuego;
    public Vector2 hotspotCursor = Vector2.zero;

    // --- UI Minijuego (Con Barra y Cursor Falso) ---
    [Header("UI Minijuego")] // Renombrado
    [Tooltip("Arrastra aqu� la Imagen UI configurada como Radial 360 (la que se llena).")]
    public Image barraProgresoCircular;
    [Tooltip("Arrastra aqu� el GameObject que act�a como FONDO de la barra de progreso.")]
    public GameObject fondoBarraProgreso;
    [Tooltip("Imagen UI que simula el cursor pegado a la cuchara. �Desactivar Raycast Target!")]
    public Image cursorEnJuegoUI; // Variable para el cursor falso

    // --- Sonidos, Recetas, Visual Caldero (igual) ---
    [Header("Sonidos Caldero")]
    public AudioClip sonidoAnadirIngrediente;
    public AudioClip sonidoRemoverBucle;
    public AudioClip sonidoPocionLista;
    public AudioClip sonidoPocionFallida;
    [Header("Recetas y Materiales")]
    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    [Header("Configuraci�n Visual Caldero")]
    public MeshRenderer rendererLiquidoCaldero;
    public int indiceMaterialLiquido = 2;
    public Material materialLiquidoVacio;

    // --- Variables Internas Minijuego ---
    private Vector2 centroPantallaMinijuego;
    private float anguloTotalRemovido = 0f;
    private Vector2 ultimaPosicionRaton;
    private bool botonRatonRemoverPresionado = false;
    private bool cucharaAgarrada = false;
    private AudioSource audioSourceCaldero;
    private Vector3 offsetAgarreLocal; // <<--- NUEVO: Guarda d�nde agarramos la cuchara (localmente)

    void Start()
    {
        controladorJugador = FindObjectOfType<ControladorJugador>();
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        audioSourceCaldero = GetComponent<AudioSource>();

        // Desactivaciones iniciales y comprobaciones
        if (camaraMinijuego) camaraMinijuego.gameObject.SetActive(false); else Debug.LogError("�CamaraMinijuego no asignada!", this.gameObject);
        if (objetoCuchara) objetoCuchara.SetActive(false); else Debug.LogError("�ObjetoCuchara no asignado!", this.gameObject);
        if (barraProgresoCircular != null) barraProgresoCircular.gameObject.SetActive(false); else Debug.LogWarning("BarraProgresoCircular no asignada.", this.gameObject);
        if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); else Debug.LogWarning("FondoBarraProgreso no asignado.", this.gameObject);
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false); else Debug.LogWarning("CursorEnJuegoUI no asignado.", this.gameObject); // Comprobaci�n
        if (audioSourceCaldero != null) audioSourceCaldero.loop = false;
        // Comprobaciones config
        if (objetoCuchara != null && pivoteRemover != null && objetoCuchara.transform.parent != pivoteRemover) { Debug.LogWarning("ADVERTENCIA: ObjetoCuchara NO es hijo de PivoteRemover.", this.gameObject); }
        if (objetoCuchara != null && objetoCuchara.GetComponent<Collider>() == null) { Debug.LogError("�ERROR CR�TICO! 'objetoCuchara' NO tiene Collider.", objetoCuchara); }
        if (pivoteRemover == null) { Debug.LogError("�Falta asignar 'pivoteRemover'!", this.gameObject); }
        if (catalogoRecetas == null) { Debug.LogError("�Falta asignar 'Catalogo Recetas'!", this.gameObject); }
        if (materialPocionDesconocida == null) { Debug.LogWarning("Material Pocion Desconocida no asignado."); }
        if (rendererLiquidoCaldero == null) { Debug.LogError("�Falta asignar 'Renderer Liquido Caldero'!", this.gameObject); }
    }

    void Update()
    {
        if (estadoActual == EstadoCaldero.Removiendo) { ManejarEntradaRemover(); }
    }

    // AnadirIngrediente, IntentarIniciarRemovido (SIN CAMBIOS)
    public bool AnadirIngrediente(DatosIngrediente ingrediente) { if (estadoActual != EstadoCaldero.Ocioso && estadoActual != EstadoCaldero.ListoParaRemover) { return false; } if (ingredientesActuales.Count >= maximoIngredientes) { return false; } ingredientesActuales.Add(ingrediente); ReproducirSonidoCaldero(sonidoAnadirIngrediente); if (ingredientesActuales.Count >= 2) { estadoActual = EstadoCaldero.ListoParaRemover; if (interaccionJugador) interaccionJugador.MostrarNotificacion("�Listo para remover! (E)"); } return true; }
    public void IntentarIniciarRemovido() { if (estadoActual == EstadoCaldero.ListoParaRemover) { IniciarMinijuegoRemover(); } }

    // IniciarMinijuegoRemover (SIN CAMBIOS respecto a la versi�n anterior con barra de progreso)
    void IniciarMinijuegoRemover() { estadoActual = EstadoCaldero.Removiendo; Debug.Log("Iniciando minijuego de remover (CIRCULAR)..."); if (controladorJugador != null) { controladorJugador.AlmacenarRotacionActual(); } anguloTotalRemovido = 0f; botonRatonRemoverPresionado = false; cucharaAgarrada = false; if (pivoteRemover != null) { pivoteRemover.transform.localRotation = Quaternion.identity; } if (camaraMinijuego != null) { if (!camaraMinijuego.gameObject.activeSelf) camaraMinijuego.gameObject.SetActive(true); centroPantallaMinijuego = new Vector2(camaraMinijuego.pixelWidth / 2.0f, camaraMinijuego.pixelHeight / 2.0f); } else { centroPantallaMinijuego = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f); } if (controladorJugador != null) controladorJugador.HabilitarMovimiento(false); if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(false); if (audioSourceCaldero != null && sonidoRemoverBucle != null) { audioSourceCaldero.clip = sonidoRemoverBucle; audioSourceCaldero.loop = true; audioSourceCaldero.Play(); } Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (texturaCursorMinijuego != null) { Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto); } else { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); } if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false); if (objetoCuchara != null) objetoCuchara.SetActive(true); if (barraProgresoCircular != null) { barraProgresoCircular.fillAmount = 0; barraProgresoCircular.gameObject.SetActive(true); if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(true); ActualizarBarraProgreso(); } }

    // --- ManejarEntradaRemover (Cursor Falso sigue al PUNTO DE AGARRE de la Cuchara) ---
    void ManejarEntradaRemover()
    {
        // --- Comprobar clic para AGARRAR ---
        if (Input.GetMouseButtonDown(0))
        {
            if (!cucharaAgarrada)
            {
                if (camaraMinijuego == null || objetoCuchara == null) return;
                Ray rayo = camaraMinijuego.ScreenPointToRay(Input.mousePosition); RaycastHit hit;
                if (Physics.Raycast(rayo, out hit, 100f))
                {
                    if (hit.collider.gameObject == objetoCuchara)
                    {
                        Debug.Log("Cuchara Agarrada!");
                        cucharaAgarrada = true; botonRatonRemoverPresionado = true; ultimaPosicionRaton = Input.mousePosition;

                        // --- CALCULAR Y GUARDAR OFFSET LOCAL --- <<<--- A�ADIDO
                        offsetAgarreLocal = objetoCuchara.transform.InverseTransformPoint(hit.point);
                        // -----------------------------------------

                        Cursor.visible = false; // Ocultar cursor sistema
                        if (cursorEnJuegoUI != null) { cursorEnJuegoUI.gameObject.SetActive(true); } // Mostrar cursor falso
                        // La posici�n se actualizar� en el bloque de abajo
                    }
                }
            }
            else { botonRatonRemoverPresionado = true; }
        }

        // --- Comprobar si se SUELTA ---
        if (Input.GetMouseButtonUp(0))
        {
            if (cucharaAgarrada)
            {
                Debug.Log("Cuchara Soltada.");
                Cursor.visible = true; // Mostrar cursor sistema
                if (cursorEnJuegoUI != null) { cursorEnJuegoUI.gameObject.SetActive(false); } // Ocultar cursor falso
                if (texturaCursorMinijuego != null) Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto); else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            botonRatonRemoverPresionado = false;
            cucharaAgarrada = false;
        }

        // --- Procesar movimiento SI est� agarrada Y presionando ---
        if (botonRatonRemoverPresionado && cucharaAgarrada)
        {

            // --- MOVER CURSOR FALSO AL PUNTO DE AGARRE EN PANTALLA --- <<<--- L�GICA MODIFICADA
            if (cursorEnJuegoUI != null && camaraMinijuego != null && objetoCuchara != null)
            {
                Vector3 puntoAgarreActualMundo = objetoCuchara.transform.TransformPoint(offsetAgarreLocal);
                Vector2 posicionCursorPantalla = camaraMinijuego.WorldToScreenPoint(puntoAgarreActualMundo);
                cursorEnJuegoUI.rectTransform.position = posicionCursorPantalla; // Cursor UI sigue el punto de agarre
            }
            // ------------------------------------------------------------

            // --- L�gica de c�lculo de �ngulo y rotaci�n (usa Input.mousePosition real) ---
            Vector2 posActual = Input.mousePosition; Vector2 vAnterior = ultimaPosicionRaton - centroPantallaMinijuego; Vector2 vActual = posActual - centroPantallaMinijuego;
            float dSqrAct = vActual.sqrMagnitude; float rMinSqr = radioMinimoGiro * radioMinimoGiro;
            if (dSqrAct > rMinSqr && (posActual - ultimaPosicionRaton).sqrMagnitude > 0.1f)
            {
                if (vAnterior.sqrMagnitude > rMinSqr)
                {
                    float deltaAngulo = Vector2.SignedAngle(vAnterior, vActual) * sensibilidadGiro;
                    if (deltaAngulo < -0.1f) { anguloTotalRemovido += deltaAngulo; }
                    if (pivoteRemover != null) { if (velocidadVisualCucharaFija <= 0) { pivoteRemover.transform.Rotate(Vector3.up, -deltaAngulo, Space.World); } else { pivoteRemover.transform.Rotate(Vector3.up, -velocidadVisualCucharaFija * Time.deltaTime, Space.World); } }
                    // Actualizar UI (Texto o Barra)
                    // if (textoVueltasInfo != null) ActualizarTextoProgresoRemover();
                    if (barraProgresoCircular != null) ActualizarBarraProgreso(); // Si usas barra
                    VerificarCompletadoRemover();
                }
            }
            ultimaPosicionRaton = posActual; // Actualizar para el siguiente frame
        }
    }

    // ActualizarBarraProgreso (SIN CAMBIOS)
    void ActualizarBarraProgreso() { if (barraProgresoCircular != null) { float progreso = Mathf.Clamp01(Mathf.Abs(anguloTotalRemovido) / anguloObjetivoRemover); barraProgresoCircular.fillAmount = progreso; } }
    // --- Opcional: M�todo para actualizar Texto (si lo prefieres) ---
    // void ActualizarTextoProgresoRemover() { if (textoVueltasInfo != null) { float anguloMostrado = Mathf.Abs(anguloTotalRemovido); anguloMostrado = Mathf.Min(anguloMostrado, anguloObjetivoRemover); textoVueltasInfo.text = $"Removiendo: {anguloMostrado:F0}� / {anguloObjetivoRemover}�"; } }

    // VerificarCompletadoRemover (SIN CAMBIOS)
    void VerificarCompletadoRemover() { if (estadoActual != EstadoCaldero.Removiendo) return; if (anguloTotalRemovido <= -anguloObjetivoRemover) { FinalizarMinijuegoRemover(true); } }

    // FinalizarMinijuegoRemover (Asegura restauraci�n cursor)
    void FinalizarMinijuegoRemover(bool exito)
    {
        if (estadoActual != EstadoCaldero.Removiendo) return;
        Debug.Log($"Minijuego terminado. �xito: {exito}");
        estadoActual = exito ? EstadoCaldero.PocionLista : EstadoCaldero.RemovidoFallido;

        // Detener sonido, ocultar elementos (igual)
        if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle) { audioSourceCaldero.Stop(); audioSourceCaldero.loop = false; }
        if (objetoCuchara != null) objetoCuchara.SetActive(false);
        // Ocultar UI minijuego
        // if (textoVueltasInfo != null) textoVueltasInfo.gameObject.SetActive(false);
        if (barraProgresoCircular != null) { barraProgresoCircular.gameObject.SetActive(false); if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); }
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false);
        // Restaurar Cursor, C�mara, Jugador (igual)
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        if (camaraMinijuego != null) camaraMinijuego.gameObject.SetActive(false);
        if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(true);
        if (controladorJugador != null) { controladorJugador.RestaurarRotacionAlmacenada(); }
        if (controladorJugador != null) controladorJugador.HabilitarMovimiento(true);

        // L�gica post-minijuego
        if (exito)
        {
            Debug.Log("�Poci�n lista!");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("�Poci�n lista! (E)");
            ReproducirSonidoCaldero(sonidoPocionLista);

            // --- L�GICA ACTUALIZAR MATERIAL CALDERO CON LOGS ---
            Material materialAAplicar = materialPocionDesconocida;
            string nombreRecetaDebug = "Desconocida";
            PedidoPocionData recetaEncontrada = null; // Inicializar a null

            if (catalogoRecetas != null)
            {
                recetaEncontrada = catalogoRecetas.BuscarRecetaPorIngredientes(ingredientesActuales);

                // --- LOG 1: QU� RECETA SE ENCONTR� ---
                if (recetaEncontrada != null)
                {
                    nombreRecetaDebug = recetaEncontrada.nombreResultadoPocion;
                    Debug.Log($"Caldero - Receta Encontrada: {nombreRecetaDebug}, Material Asignado: {(recetaEncontrada.materialResultado != null ? recetaEncontrada.materialResultado.name : "NINGUNO")}");
                    if (recetaEncontrada.materialResultado != null)
                    {
                        materialAAplicar = recetaEncontrada.materialResultado;
                    }
                    else
                    {
                        Debug.LogWarning($"Receta '{nombreRecetaDebug}' no tiene Material Resultado asignado en su asset.");
                    }
                }
                else
                {
                    Debug.Log("Caldero - No se encontr� receta para esta combinaci�n.");
                }
                // --- FIN LOG 1 ---

            }
            else { Debug.LogError("�Catalogo de Recetas no asignado en Caldero!"); }

            // --- LOG 2: QU� MATERIAL SE VA A APLICAR ---
            Debug.Log($"Caldero - Material final a aplicar: {(materialAAplicar != null ? materialAAplicar.name : "NINGUNO (Usando Desconocido o Fall�)")}");
            // --- FIN LOG 2 ---

            ActualizarMaterialLiquido(materialAAplicar); // Aplica el material al caldero
            // --- FIN L�GICA ACTUALIZAR MATERIAL ---

        }
        else
        { // Fallo
            Debug.Log("�Mezcla fallida!"); if (interaccionJugador) interaccionJugador.MostrarNotificacion("�Mezcla fallida!"); ReproducirSonidoCaldero(sonidoPocionFallida); ReiniciarCaldero();
        }
    }

    // --- M�todos Auxiliares (SIN CAMBIOS) ---
    void ReproducirSonidoCaldero(AudioClip clip) { if (audioSourceCaldero != null && clip != null) { audioSourceCaldero.PlayOneShot(clip); } }
    public bool EstaPocionLista() { return estadoActual == EstadoCaldero.PocionLista; }
    public DatosIngrediente[] RecogerPocion() { if (estadoActual == EstadoCaldero.PocionLista) { DatosIngrediente[] c = ingredientesActuales.ToArray(); ReiniciarCaldero(); return c; } return null; }
    public void ReiniciarCaldero() { ingredientesActuales.Clear(); estadoActual = EstadoCaldero.Ocioso; if (materialLiquidoVacio != null) { ActualizarMaterialLiquido(materialLiquidoVacio); } Debug.Log("Caldero reiniciado."); if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle) { audioSourceCaldero.Stop(); audioSourceCaldero.loop = false; } }
    public void ActualizarMaterialLiquido(Material nuevoMaterial) { if (rendererLiquidoCaldero == null) return; if (nuevoMaterial == null) return; Material[] mats = rendererLiquidoCaldero.materials; if (indiceMaterialLiquido >= 0 && indiceMaterialLiquido < mats.Length) { mats[indiceMaterialLiquido] = Instantiate(nuevoMaterial); rendererLiquidoCaldero.materials = mats; } else { Debug.LogError($"�ndice ({indiceMaterialLiquido}) fuera de rango ({mats.Length})", this.gameObject); } }

    // Alias para compatibilidad con InteraccionJugador
    public bool AgregarIngrediente(DatosIngrediente ingrediente)
    {
        return AnadirIngrediente(ingrediente);
    }

} // Fin de la clase