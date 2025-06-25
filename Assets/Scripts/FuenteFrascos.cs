using UnityEngine;
using TMPro; // <<--- A�ADE ESTO

public class FuenteFrascos : MonoBehaviour
{
    [Tooltip("Arrastra aqu� el asset 'DatosFrasco' que define este tipo de frasco.")]
    public DatosFrasco datosFrasco;

    [Tooltip("Cu�ntos frascos hay disponibles inicialmente en esta fuente.")]
    public int cantidad = 10;

    // --- A�adido: UI de Informaci�n Flotante ---
    [Header("UI Informaci�n (Opcional)")]
    [Tooltip("Arrastra aqu� el MISMO prefab de Canvas que usas para los ingredientes.")]
    public GameObject prefabCanvasInfo; // <<--- A�ADIDO
    private GameObject canvasInfoActual = null; // <<--- A�ADIDO
    // -------------------------------------------

    // --- A�adido: Mostrar/Ocultar Informaci�n ---
    public void MostrarInformacion()
    {
        if (datosFrasco == null) return; // Salir si no hay datos

        // Si no tenemos canvas, cr�alo y config�ralo
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

        // Aseg�rate de que est� activo (sea nuevo o existente)
        if (canvasInfoActual != null)
        {
            // Re-actualiza la cantidad por si cambi� mientras no mir�bamos
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
            // Actualizar UI flotante si la tienes y est� visible
            if (canvasInfoActual != null && canvasInfoActual.activeSelf)
            {
                InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
                if (uiScript != null && uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.text = $"Quedan: {cantidad}";
                }
            }
            // AGREGA ESTA LÍNEA para sumar el frasco al inventario con el nombre limpio
            InventoryManager.Instance?.AddItem(datosFrasco.nombreItem.Trim());
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