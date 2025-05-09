using UnityEngine;
using TMPro; // <<--- AÑADE ESTO

public class FuenteFrascos : MonoBehaviour
{
    [Tooltip("Arrastra aquí el asset 'DatosFrasco' que define este tipo de frasco.")]
    public DatosFrasco datosFrasco;

    [Tooltip("Cuántos frascos hay disponibles inicialmente en esta fuente.")]
    public int cantidad = 10;

    // --- Añadido: UI de Información Flotante ---
    [Header("UI Información (Opcional)")]
    [Tooltip("Arrastra aquí el MISMO prefab de Canvas que usas para los ingredientes.")]
    public GameObject prefabCanvasInfo; // <<--- AÑADIDO
    private GameObject canvasInfoActual = null; // <<--- AÑADIDO
    // -------------------------------------------

    // --- Añadido: Mostrar/Ocultar Información ---
    public void MostrarInformacion()
    {
        if (datosFrasco == null) return; // Salir si no hay datos

        // Si no tenemos canvas, créalo y configúralo
        if (canvasInfoActual == null && prefabCanvasInfo != null)
        {
            // Debug.Log($"INSTANCIANDO canvas para frasco: {gameObject.name}"); // Debug opcional
            canvasInfoActual = Instantiate(prefabCanvasInfo, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            canvasInfoActual.transform.LookAt(Camera.main.transform);
            canvasInfoActual.transform.forward *= -1;

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                    uiScript.textoNombre.text = datosFrasco.nombreItem; // Usa el nombre del DatosFrasco
                if (uiScript.textoCantidad != null)
                    uiScript.textoCantidad.text = $"Quedan: {cantidad}";
            }
        }

        // Asegúrate de que esté activo (sea nuevo o existente)
        if (canvasInfoActual != null)
        {
            // Re-actualiza la cantidad por si cambió mientras no mirábamos
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoCantidad != null)
            {
                uiScript.textoCantidad.text = $"Quedan: {cantidad}";
            }
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
    void OnDestroy()
    { // Limpia si el objeto se destruye
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }
    // ---------------------------------------------

    // Modificado para actualizar UI si existe
    public DatosFrasco IntentarRecoger()
    {
        if (datosFrasco == null)
        {
            Debug.LogError("¡FuenteFrascos no tiene asignado un DatosFrasco!", this.gameObject);
            return null;
        }
        if (cantidad > 0)
        {
            cantidad--;
            // Actualizar UI flotante si la tienes y está visible
            if (canvasInfoActual != null && canvasInfoActual.activeSelf)
            {
                InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
                if (uiScript != null && uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.text = $"Quedan: {cantidad}";
                }
            }
            Debug.Log($"Recogido {datosFrasco.nombreItem}. Quedan: {cantidad}");
            return datosFrasco;
        }
        else
        {
            Debug.Log($"¡No quedan más {datosFrasco.nombreItem}!");
            return null;
        }
    }
}