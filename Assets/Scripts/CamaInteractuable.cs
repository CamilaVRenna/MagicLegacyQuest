using UnityEngine;
using TMPro; // Asegúrate de tener TextMeshPro en tu proyecto

public class CamaInteractuable : MonoBehaviour
{
    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrará al mirar la cama. Ej: 'Dormir (E)'")]
    public string textoIndicador = "Dormir [E]"; // Texto personalizable
    [Tooltip("Arrastra aquí el MISMO prefab de Canvas flotante que usas para ingredientes/puertas.")]
    public GameObject prefabCanvasInfo; // <<--- ASIGNAR EN INSPECTOR
    private GameObject canvasInfoActual = null;

    // Necesitamos una referencia al GameManager para llamar a GoToSleep
    // No es estrictamente necesario aquí, ya que la llamada se hace desde InteraccionJugador,
    // pero podría ser útil para otras lógicas de la cama.

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null) return;
        if (canvasInfoActual == null)
        {
            Vector3 posicionUI = transform.position + Vector3.up * 1.0f; // Ajusta Y offset
            canvasInfoActual = Instantiate(prefabCanvasInfo, posicionUI, Quaternion.identity);

            // Configurar texto (Asume script InfoCanvasUI o busca TMP)
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null) uiScript.textoNombre.text = textoIndicador;
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false); // Ocultar cantidad
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = textoIndicador;
            }
        }
        if (canvasInfoActual != null)
        {
            // Re-actualizar texto por si acaso
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null) uiScript.textoNombre.text = textoIndicador;
            else { TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>(); if (tmp != null) tmp.text = textoIndicador; }
            canvasInfoActual.SetActive(true);
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    void OnDestroy() { if (canvasInfoActual != null) { Destroy(canvasInfoActual); } }
}