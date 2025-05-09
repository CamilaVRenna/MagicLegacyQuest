using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class FuenteIngredientes : MonoBehaviour
{
    public DatosIngrediente datosIngrediente; // Arrastra el ScriptableObject correspondiente aquí
    //public int cantidad = 5; // Cantidad inicial
    public GameObject objetoModelo; // Arrastra el objeto visual del orbe aquí

    [Header("UI Información (Opcional)")]
    public GameObject prefabCanvasInfo; // Prefab de un Canvas con TextMeshPro para mostrar info
    private GameObject canvasInfoActual = null;

    // Start se llama antes de la primera actualización del frame
    void Start()
    {
        // Podrías instanciar el canvas aquí si es necesario
    }

    // Llamado cuando el jugador mira este objeto
    public void MostrarInformacion()
    {
        // 1. Si el canvas NO existe AÚN, y TENEMOS un prefab para crearlo:
        if (canvasInfoActual == null && prefabCanvasInfo != null)
        {
            Debug.Log($"INSTANCIANDO nuevo canvas para: {gameObject.name}");
            canvasInfoActual = Instantiate(prefabCanvasInfo, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            canvasInfoActual.transform.LookAt(Camera.main.transform);
            canvasInfoActual.transform.forward *= -1;

            // Intenta obtener el script de UI recién creado
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                // Actualiza los textos usando las referencias del script de UI
                if (uiScript.textoNombre != null)
                {
                    uiScript.textoNombre.text = datosIngrediente.nombreIngrediente;
                }
                int stockActual = 0; // Valor por defecto si no encontramos el gestor
                if (GestorJuego.Instance != null)
                {
                    stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente);
                }
                uiScript.textoCantidad.text = $"Disp.: {stockActual}"; // Mostrar stock global (Disp. = Disponible)
                uiScript.textoCantidad.gameObject.SetActive(true); // Asegurar que se vea
            }
            // No hace falta SetActive(true) aquí, Instantiate ya lo hace visible.
        }
        // 2. Si el canvas YA EXISTE (fue creado antes):
        else if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(true);
            // Re-actualizar cantidad al mostrar de nuevo
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoCantidad != null)
            {
                int stockActual = 0;
                if (GestorJuego.Instance != null) { stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente); }
                uiScript.textoCantidad.text = $"Disp.: {stockActual}"; // <<<--- ACTUALIZAR AQUÍ TAMBIÉN
                uiScript.textoCantidad.gameObject.SetActive(true);
            }
            // Actualizar nombre también por si acaso (opcional)
            // if (uiScript != null && uiScript.textoNombre != null) uiScript.textoNombre.text = datosIngrediente.nombreIngrediente;
        }
        // 3. Si no existe y TAMPOCO tenemos prefab, no hacemos nada (o mostramos error)
        else if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"PrefabCanvasInfo no asignado en {gameObject.name}, no se puede mostrar info.");
        }
    }

    // Llamado cuando el jugador deja de mirar
    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    // Intenta dar el ingrediente al jugador
    public DatosIngrediente IntentarRecoger()
    {
        // Primero, comprobar si tenemos datos de ingrediente asignados
        if (datosIngrediente == null)
        {
            Debug.LogError($"Fuente {gameObject.name} no tiene DatosIngrediente asignado!");
            return null;
        }

        // Intentar consumir 1 unidad del stock global en GestorJuego
        bool consumidoConExito = false;
        if (GestorJuego.Instance != null)
        {
            consumidoConExito = GestorJuego.Instance.ConsumirStockTienda(datosIngrediente);
        }
        else
        {
            Debug.LogError($"No se encontró GestorJuego para consumir stock de {datosIngrediente.nombreIngrediente}");
            // Decidir si devolver null o el ingrediente igual (podría causar inconsistencias)
            // Devolver null es más seguro para indicar el fallo.
            return null;
        }

        // Si se pudo consumir (había stock)...
        if (consumidoConExito)
        {
            Debug.Log($"Recogido {datosIngrediente.nombreIngrediente} de la fuente (Stock Global).");
            ActualizarVisuales(); // Llama a actualizar la UI flotante para mostrar la nueva cantidad
            return datosIngrediente; // Devolver el ingrediente al jugador
        }
        // Si no se pudo consumir (no había stock)...
        else
        {
            Debug.Log($"¡No queda {datosIngrediente.nombreIngrediente} en el stock!");
            // InteraccionJugador mostrará la notificación al recibir null
            return null; // Indica que no se pudo recoger
        }
    }

    // Dentro de FuenteIngredientes.cs
    void ActualizarVisuales()
    {
        // Podrías cambiar el material, tamaño, etc. del orbe aquí si quisieras

        // Si el InfoCanvas está activo, intenta actualizar sus textos
        if (canvasInfoActual != null && canvasInfoActual.activeSelf) // Comprueba si el canvas existe Y está visible
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>(); // Obtiene el script del canvas

            if (uiScript != null) // Comprueba si el script existe en el canvas
            {
                // Actualiza la cantidad si el campo de texto existe en uiScript
                if (uiScript.textoCantidad != null)
                {
                    int stockActual = 0;
                    if (GestorJuego.Instance != null)
                    {
                        stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente);
                    }
                    uiScript.textoCantidad.text = $"Disp.: {stockActual}"; // <<<--- Usa GestorJuego
                }
                // Podrías actualizar el nombre también si fuera necesario, aunque normalmente no cambia
                // if(uiScript.textoNombre != null) {
                //    uiScript.textoNombre.text = datosIngrediente.nombreIngrediente;
                // }
            }
        }
    }

    // Limpia el Canvas si el objeto se destruye para evitar errores
    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }
}