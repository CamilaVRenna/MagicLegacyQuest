using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Necesario si accedes a componentes TextMeshPro directamente

public class PuertaCambioEscena : MonoBehaviour
{
    [Header("Configuración Destino")]
    public string nombreEscenaDestino = "";
    [Tooltip("Nombre del PuntoSpawn donde aparecerá el jugador en la escena destino.")]
    public string nombrePuntoSpawnDestino = "DefaultSpawn"; // <<--- NUEVO

    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrará al mirar la puerta. Ej: 'Ir al bosque'")]
    public string textoIndicador = "Interactuar"; // Texto personalizable por puerta
    [Tooltip("Texto que se mostrará al mirar la puerta SI ES DE NOCHE.")] // <<--- NUEVO
    public string textoIndicadorNoche = "Mejor no salir de noche, podria aparecer un ogro..."; // <<--- NUEVO
    [Tooltip("Arrastra aquí el MISMO prefab de Canvas flotante que usas para los ingredientes.")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;



    // Podrías necesitar una referencia específica al TextMeshPro si tu prefab es complejo
    // private TextMeshProUGUI textoNombreEnCanvas;


    // --- Método llamado por InteraccionJugador ---
    /*public void CambiarEscena()
    {
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            Debug.Log($"Cambiando a escena: {nombreEscenaDestino}...");

            // --- Log y Registro de Viaje --- <<<--- AÑADE ESTE BLOQUE COMPLETO AQUÍ
            // Log para verificar si GestorJuego.Instance existe en este momento
            Debug.LogWarning($"Puerta.CambiarEscena: Verificando GestorJuego.Instance... ¿Es null? = {GestorJuego.Instance == null}");

            // Intentar registrar el viaje para que GestorJuego actualice la hora
            if (GestorJuego.Instance != null)
            {
                Debug.Log("Puerta.CambiarEscena: GestorJuego.Instance OK. Llamando a RegistrarViaje...");
                GestorJuego.Instance.RegistrarViaje(nombreEscenaDestino); // <<--- ¡LLAMADA IMPORTANTE AÑADIDA!
            }
            else
            {
                Debug.LogError("Puerta.CambiarEscena: No se encontró GestorJuego.Instance para registrar el viaje!");
            }
            // --- Fin Bloque Añadido ---

            // Ahora sí, cargar la escena con la pantalla de carga
            GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaDestino);
        }
        else
        {
            Debug.LogError($"¡Puerta ({gameObject.name}) sin 'Nombre Escena Destino'!", this.gameObject);
        }
    }*/

    // Dentro de PuertaCambioEscena.cs

    // Dentro de PuertaCambioEscena.cs
    public void CambiarEscena()
    {
        // Comprobar si hay nombre de escena destino
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            // Debug.Log($"Iniciando viaje a escena: {nombreEscenaDestino}..."); // Log opcional

            // --- Registrar Viaje (Versión Final Limpia) ---
            // Comprobar si la instancia existe antes de usarla (buena práctica)
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.SetSiguientePuntoSpawn(nombrePuntoSpawnDestino); // <<--- NUEVO
                GestorJuego.Instance.RegistrarViaje(nombreEscenaDestino); // Llamada a GestorJuego para cambiar hora
            }
            else
            {
                // Este error solo aparecería si algo muy raro pasa con el Singleton
                Debug.LogError("Puerta.CambiarEscena: No se encontró GestorJuego.Instance para registrar el viaje!");
            }
            // --- Fin Registro Viaje ---

            // Cargar la escena con pantalla de carga
            GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaDestino);
        }
        else // Si no hay nombre de escena destino configurado
        {
            Debug.LogError($"¡Puerta ({gameObject.name}) sin 'Nombre Escena Destino' configurado en el Inspector!", this.gameObject);
        }
    }

    // --- NUEVO: Lógica para Mostrar/Ocultar Indicador ---

    public void MostrarInformacion()
    {
        // 1. Salir si no hay prefab asignado
        if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"Puerta {gameObject.name}: PrefabCanvasInfo no asignado.");
            return;
        }

        // --- 2. ELEGIR TEXTO SEGÚN LA HORA --- <<<--- ESTO DEBE IR AQUÍ ARRIBA
        string textoAMostrar = textoIndicador; // Texto por defecto (día)
        if (GestorJuego.Instance != null && GestorJuego.Instance.horaActual == HoraDelDia.Noche)
        {
            textoAMostrar = textoIndicadorNoche; // Usar texto nocturno si aplica
        }
        // ----------------------------------

        // 3. Instanciar el canvas si no existe AÚN
        if (canvasInfoActual == null)
        {
            // Debug.Log($"Instanciando canvas para {gameObject.name}"); // Log Opcional
            Vector3 offset = Vector3.up * 1.0f;
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);

            // Intentar configurar los textos AHORA que sabemos qué texto poner
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
                else { Debug.LogWarning($"No se encontró TextMeshProUGUI en prefab para {gameObject.name}."); }
            }
        }

        // 4. Asegurarse de que esté activo y con el texto correcto (si ya existía o se acaba de crear)
        if (canvasInfoActual != null)
        {
            // Re-actualizar texto (por si acaso y para asegurar)
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null)
            {
                uiScript.textoNombre.text = textoAMostrar; // <-- Usar textoAMostrar
                uiScript.textoNombre.gameObject.SetActive(true); // Re-asegurar activación
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = textoAMostrar; // <-- Usar textoAMostrar
            }
            // Activar el canvas principal
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

    // Limpiar el canvas si la puerta se destruye
    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }
    // --- FIN NUEVA LÓGICA ---
}