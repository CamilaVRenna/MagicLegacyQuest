using UnityEngine;

public class BocadilloUI : MonoBehaviour
{
    private Transform camTransform = null; // Inicializar a null
    //private bool busquedaInicialHecha = false; // Para evitar logs repetitivos

    // Awake o Start pueden usarse para configuraciones iniciales que NO dependen de la cámara
    void Awake()
    {
        // Podrías configurar otras cosas aquí si fuera necesario
    }

    // Usamos LateUpdate para asegurarnos de que la cámara ya se haya movido/actualizado
    void LateUpdate()
    {
        // --- BÚSQUEDA DINÁMICA DE CÁMARA ---
        // Comprobar si no tenemos cámara O si la que teníamos está inactiva
        if (camTransform == null || !camTransform.gameObject.activeInHierarchy)
        {
            // Si no tenemos cámara válida, intentar encontrar una CADA frame
            // Debug.LogWarning($"BocadilloUI ({gameObject.name}): Buscando cámara..."); // Log Opcional (puede ser ruidoso)

            // Intento 1: Camera.main (la forma preferida y más eficiente)
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
            else
            {
                // Fallback: Buscar CUALQUIER cámara activa si MainCamera falla
                // Esto podría coger la cámara del minijuego temporalmente
                Camera cualquierCamaraActiva = FindObjectOfType<Camera>();
                if (cualquierCamaraActiva != null)
                {
                    camTransform = cualquierCamaraActiva.transform;
                    // Ya no necesitamos mostrar el warning aquí, es esperado si la MainCamera está inactiva
                }
                else
                {
                    // Si no hay NINGUNA cámara activa, poner camTransform a null
                    camTransform = null;
                }
            }
            // Log opcional para saber qué encontró
            // if(camTransform != null) Debug.Log($"BocadilloUI ({gameObject.name}) encontró/usa cámara: {camTransform.name}");
            // else Debug.LogWarning($"BocadilloUI ({gameObject.name}) no encontró cámara activa este frame.");
        }
        // --- FIN BÚSQUEDA ---


        // --- LÓGICA DE ORIENTACIÓN (Billboard) ---
        // Si DESPUÉS de la búsqueda tenemos una cámara válida, orientar hacia ella
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward,
                             camTransform.rotation * Vector3.up);
        }
        // --- FIN ORIENTACIÓN ---
    }// --- FIN ORIENTACIÓN ---
}