using UnityEngine;
using TMPro;

// Asegura que tenga un collider para ser detectado
[RequireComponent(typeof(Collider))]
public class IngredienteRecolectable : MonoBehaviour
{
    [Tooltip("Asigna aquí el ScriptableObject del ingrediente que este objeto representa.")]
    public DatosIngrediente datosIngrediente; // <<--- ASIGNAR EN EL PREFAB

    [Header("Interacción y UI")]
    public string textoIndicador = "Recolectar [E]";
    [Tooltip("Arrastra el prefab InfoCanvas.")]
    public GameObject prefabCanvasInfo; // <<--- ASIGNAR EN EL PREFAB
    private GameObject canvasInfoActual = null;

    // Referencia al punto de spawn (lo asigna el GestorRecoleccionBosque al crearlo)
    [HideInInspector] public PuntoSpawnRecoleccion puntoOrigen = null;

    // --- Métodos para Mostrar/Ocultar Indicador ---
    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null || datosIngrediente == null) return;
        if (canvasInfoActual == null)
        {
            Vector3 offset = Vector3.up * 0.5f; // Ajustar altura
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);
        }
        if (canvasInfoActual != null)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null) uiScript.textoNombre.text = $"{datosIngrediente.nombreIngrediente}\n[{textoIndicador}]";
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false);
            }
            else { TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>(); if (tmp != null) tmp.text = $"{datosIngrediente.nombreIngrediente}\n[{textoIndicador}]"; }
            canvasInfoActual.SetActive(true);
        }
    }
    public void OcultarInformacion() { if (canvasInfoActual != null) { canvasInfoActual.SetActive(false); } }
    void OnDestroy() { if (canvasInfoActual != null) { Destroy(canvasInfoActual); } }
    // --- Fin Métodos Indicador ---


    // --- Método llamado por InteraccionJugador al presionar E ---
    public void Recolectar()
    {
        if (datosIngrediente == null) return;

        Debug.Log($"Recolectado: {datosIngrediente.nombreIngrediente}");
        bool anadido = false;
        if (GestorJuego.Instance != null)
        {
            // Añadir al stock global
            GestorJuego.Instance.AnadirStockTienda(datosIngrediente, 1); // Añade 1
            anadido = true;

            // Marcar el punto de origen como recolectado hoy
            if (puntoOrigen != null)
            {
                puntoOrigen.diaUltimaRecoleccion = GestorJuego.Instance.diaActual;
                puntoOrigen.objetoInstanciadoActual = null; // El punto queda libre
            }
        }
        else { Debug.LogError("No se encontró GestorJuego para añadir al stock."); }

        // Destruir el objeto SOLO si se pudo añadir al stock
        if (anadido)
        {
            // Sonido opcional de recolección
            // if(GestorAudio.Instancia != null && SONIDO_RECOLECCION != null) GestorAudio.Instancia.ReproducirSonido(SONIDO_RECOLECCION);

            OcultarInformacion();
            Destroy(gameObject);
        }
    }
}