using UnityEngine;
using TMPro; // Asegúrate de tener TextMeshPro

// Nombre de clase ya estaba bien
public class LibroRecetasInteractuable : MonoBehaviour
{
    // --- AÑADIDO: Variables para UI Flotante ---
    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrará al mirar el libro.")]
    public string textoIndicador = "Leer Libro [E]"; // Puedes cambiar este texto
    [Tooltip("Arrastra aquí el prefab de Canvas flotante (Prefab_InfoCanvas).")]
    public GameObject prefabCanvasInfo; // <<--- ASIGNAR EN INSPECTOR
    private GameObject canvasInfoActual = null;
    // --- FIN AÑADIDO ---

    // --- NUEVO: Métodos para Mostrar/Ocultar Indicador ---
    public void MostrarInformacion()
    {
        // Salir si no hay prefab asignado
        if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"Libro {gameObject.name} no tiene prefabCanvasInfo asignado.");
            return;
        }

        // Instanciar el canvas si no existe
        if (canvasInfoActual == null)
        {
            Vector3 offset = Vector3.up * 0.75f; // Ajusta offset Y para el libro
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            // Usar Quaternion.identity para rotación inicial
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);

            // Configurar texto (Asume script InfoCanvasUI o busca TMP)
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                {
                    uiScript.textoNombre.text = textoIndicador;
                    uiScript.textoNombre.gameObject.SetActive(true); // Asegurar visibilidad
                }
                else { Debug.LogWarning("InfoCanvasUI en prefab no tiene 'textoNombre' asignado."); }

                if (uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.gameObject.SetActive(false); // Ocultar cantidad
                }
            }
            else
            {
                // Fallback si no hay InfoCanvasUI
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) { tmp.text = textoIndicador; }
                else { Debug.LogWarning("No se encontró TextMeshProUGUI en prefabCanvasInfo para el Libro."); }
            }
        }
        // Asegurarse de que esté activo y con texto correcto
        if (canvasInfoActual != null)
        {
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

    // Limpiar el canvas si el libro se destruye
    void OnDestroy() { if (canvasInfoActual != null) { Destroy(canvasInfoActual); } }
    // --- FIN NUEVOS MÉTODOS ---
}