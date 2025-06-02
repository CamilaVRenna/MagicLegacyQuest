using UnityEngine;
using TMPro;

public class FuenteIngredientes : MonoBehaviour
{
    public DatosIngrediente datosIngrediente;
    public GameObject objetoModelo;

    [Header("UI Informaci�n (Opcional)")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;

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

        bool consumidoConExito = false;
        if (GestorJuego.Instance != null)
        {
            consumidoConExito = GestorJuego.Instance.ConsumirStockTienda(datosIngrediente);
        }
        else
        {
            Debug.LogError($"No se encontr� GestorJuego para consumir stock de {datosIngrediente.nombreIngrediente}");
            return null;
        }

        if (consumidoConExito)
        {
            Debug.Log($"Recogido {datosIngrediente.nombreIngrediente} de la fuente (Stock Global).");
            ActualizarVisuales();
            return datosIngrediente;
        }
        else
        {
            Debug.Log($"�No queda {datosIngrediente.nombreIngrediente} en el stock!");
            return null;
        }
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