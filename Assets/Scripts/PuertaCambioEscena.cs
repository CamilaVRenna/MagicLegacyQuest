using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Necesario si accedes a componentes TextMeshPro directamente

public class PuertaCambioEscena : MonoBehaviour
{
    [Header("Configuraci�n Destino")]
    public string nombreEscenaDestino = "";
    [Tooltip("Nombre del PuntoSpawn donde aparecer� el jugador en la escena destino.")]
    public string nombrePuntoSpawnDestino = "DefaultSpawn"; // <<--- NUEVO

    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrar� al mirar la puerta. Ej: 'Ir al bosque'")]
    public string textoIndicador = "Interactuar"; // Texto personalizable por puerta
    [Tooltip("Texto que se mostrar� al mirar la puerta SI ES DE NOCHE.")] // <<--- NUEVO
    public string textoIndicadorNoche = "Mejor no salir de noche, podria aparecer un ogro..."; // <<--- NUEVO
    [Tooltip("Arrastra aqu� el MISMO prefab de Canvas flotante que usas para los ingredientes.")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    
public void CambiarEscena()
{
    if (!string.IsNullOrEmpty(nombreEscenaDestino))
    {
        if (GestorJuego.Instance != null)
        {
                        // --- AGREGA ESTA LÍNEA ---
            GestorJuego.Instance.GuardarDatos();
            if (nombreEscenaDestino.ToLower().Contains("bosque"))
            {
                if (yaSalioAlBosque)
                {
                    InteraccionJugador jugador = FindObjectOfType<InteraccionJugador>();
                    if (jugador != null)
                        jugador.MostrarNotificacion("No puedes volver a salir al bosque.", 3f, false);
                    else
                        Debug.Log("No puedes volver a salir al bosque.");
                    return;
                }
                else
                {
                    yaSalioAlBosque = true;
                }
            }
            GestorJuego.Instance.SetSiguientePuntoSpawn(nombrePuntoSpawnDestino);
            GestorJuego.Instance.RegistrarViaje(nombreEscenaDestino);

        }
        else
        {
            Debug.LogError("Puerta.CambiarEscena: No se encontró GestorJuego.Instance para registrar el viaje!");
        }

        GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaDestino);
    }
    else // Si no hay nombre de escena destino configurado
    {
        Debug.LogError($"¿Puerta ({gameObject.name}) sin 'Nombre Escena Destino' configurado en el Inspector!", this.gameObject);
    }
}

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"Puerta {gameObject.name}: PrefabCanvasInfo no asignado.");
            return;
        }

        string textoAMostrar = textoIndicador; // Texto por defecto (d�a)
        if (GestorJuego.Instance != null && GestorJuego.Instance.horaActual == HoraDelDia.Noche)
        {
            textoAMostrar = textoIndicadorNoche; // Usar texto nocturno si aplica
        }

        if (canvasInfoActual == null)
        {
            Vector3 offset = Vector3.up * 1.0f;
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                {
                    uiScript.textoNombre.text = textoAMostrar; // <-- Usar textoAMostrar
                    uiScript.textoNombre.gameObject.SetActive(true);
                }
                if (uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.gameObject.SetActive(false); // Ocultar cantidad
                }
            }
            else // Fallback si no hay InfoCanvasUI
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) { tmp.text = textoAMostrar; } // <-- Usar textoAMostrar
                else { Debug.LogWarning($"No se encontr� TextMeshProUGUI en prefab para {gameObject.name}."); }
            }
        }
        if (canvasInfoActual != null)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null)
            {
                uiScript.textoNombre.text = textoAMostrar; // <-- Usar textoAMostrar
                uiScript.textoNombre.gameObject.SetActive(true); // Re-asegurar activaci�n
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = textoAMostrar; // <-- Usar textoAMostrar
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
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }

    private static bool yaSalioAlBosque = false;

    public static void ReiniciarRegistroSalidaBosque()
    {
        yaSalioAlBosque = false;
    }
}