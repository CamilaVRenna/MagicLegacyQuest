using UnityEngine;
using TMPro;

public class FuenteIngredientes : MonoBehaviour
{
    public DatosIngrediente datosIngrediente;
    public GameObject objetoModelo;

    [Header("UI Informaci�n (Opcional)")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;

    // --- NUEVO: Llevar registro de cuántos ingredientes entregó esta fuente al jugador ---
    private int cantidadEntregadaAlJugador = 0;
    // ----------------------------------------------------------

    public void MostrarInformacion()
    {
        if (canvasInfoActual == null && prefabCanvasInfo != null)
        {
            canvasInfoActual = Instantiate(prefabCanvasInfo, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            canvasInfoActual.transform.LookAt(Camera.main.transform);
            canvasInfoActual.transform.forward *= -1;

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                {
                    uiScript.textoNombre.text = datosIngrediente.nombreIngrediente;
                }
                int stockActual = 0;
                if (GestorJuego.Instance != null)
                {
                    stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente);
                }
                uiScript.textoCantidad.text = $"Disp.: {stockActual}";
                uiScript.textoCantidad.gameObject.SetActive(true);
            }
        }
        else if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(true);
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoCantidad != null)
            {
                int stockActual = 0;
                if (GestorJuego.Instance != null) { stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente); }
                uiScript.textoCantidad.text = $"Disp.: {stockActual}";
                uiScript.textoCantidad.gameObject.SetActive(true);
            }
        }
        else if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"PrefabCanvasInfo no asignado en {gameObject.name}, no se puede mostrar info.");
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    public DatosIngrediente IntentarRecoger()
    {
        if (datosIngrediente == null)
        {
            Debug.LogError($"Fuente {gameObject.name} no tiene DatosIngrediente asignado!");
            return null;
        }

        int cantidadEnStock = 0;
        if (GestorJuego.Instance != null)
        {
            cantidadEnStock = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente);
        }
        else
        {
            Debug.LogError($"No se encontró GestorJuego para consumir stock de {datosIngrediente.nombreIngrediente}");
            return null;
        }

        if (cantidadEnStock > 0)
        {
            // Consumir todo el stock disponible de ese ingrediente
            for (int i = 0; i < cantidadEnStock; i++)
            {
                GestorJuego.Instance.ConsumirStockTienda(datosIngrediente);
                // Añadir al inventario visual y lógico
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItemVisual(datosIngrediente.icono, -1); // Usa -1 para slot automático
                    InventoryManager.Instance.AddItem(datosIngrediente.nombreIngrediente); // <-- Usa SIEMPRE el nombre del ingrediente actual
                }
            }
            cantidadEntregadaAlJugador += cantidadEnStock; // <<--- REGISTRA CUÁNTOS ENTREGÓ ESTA FUENTE
            Debug.Log($"Recogidos {cantidadEnStock} de {datosIngrediente.nombreIngrediente} de la fuente (Stock Global).");
            ActualizarVisuales();
            return datosIngrediente;
        }
        else
        {
            Debug.Log($"¡No queda {datosIngrediente.nombreIngrediente} en el stock!");
            return null;
        }
    }

    public void DevolverIngrediente()
    {
        if (datosIngrediente == null) return;
        if (GestorJuego.Instance != null && InventoryManager.Instance != null)
        {
            int cantidadEnInventario = InventoryManager.Instance.ContarItem(datosIngrediente.nombreIngrediente);
            // --- NUEVO: Solo permite devolver hasta la cantidad que entregó esta fuente ---
            int cantidadADevolver = Mathf.Min(cantidadEnInventario, cantidadEntregadaAlJugador);
            if (cantidadADevolver > 0)
            {
                // Eliminar todas las unidades del inventario lógico y visual
                for (int i = 0; i < cantidadADevolver; i++)
                {
                    // InventoryManager.Instance.RemoveItem(datosIngrediente.nombreIngrediente, 1); // <-- Agregar cantidad
                    // InventoryManager.Instance.RemoveItemVisual(datosIngrediente.icono);
                }
                InventoryManager.Instance.RemoveItem(datosIngrediente.nombreIngrediente, cantidadADevolver);
                for (int i = 0; i < cantidadADevolver; i++)
                {
                    InventoryManager.Instance.RemoveItemVisual(datosIngrediente.icono, -1); // <-- slotIndex -1
                }

                // Devolver todas al stock de la fuente
                GestorJuego.Instance.AnadirStockTienda(datosIngrediente, cantidadADevolver);
                cantidadEntregadaAlJugador -= cantidadADevolver; // <<--- RESTA LAS DEVUELTAS
                ActualizarVisuales();

                UIMessageManager.Instance?.MostrarMensaje($"Devolviste {cantidadADevolver} {datosIngrediente.nombreIngrediente}(s) a la fuente.");
            }
            else
            {
                // Ya no tienes más para devolver o ya devolviste todo lo que te dio esta fuente
                UIMessageManager.Instance?.MostrarMensaje($"No tienes más {datosIngrediente.nombreIngrediente} para devolver aquí.");
            }
        }
    }

    // --- NUEVO: Permite sumar 1 cuando se agrega al caldero ---
    public void RegistrarIngredienteDevueltoDesdeCaldero()
    {
        cantidadEntregadaAlJugador += 1;
    }

    void ActualizarVisuales()
    {
        if (canvasInfoActual != null && canvasInfoActual.activeSelf)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoCantidad != null)
                {
                    int stockActual = 0;
                    if (GestorJuego.Instance != null)
                    {
                        stockActual = GestorJuego.Instance.ObtenerStockTienda(datosIngrediente);
                    }
                    uiScript.textoCantidad.text = $"Disp.: {stockActual}";
                }
            }
        }
    }

    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }
}