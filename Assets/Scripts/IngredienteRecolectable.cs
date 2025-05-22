using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class IngredienteRecolectable : MonoBehaviour
{
    public DatosIngrediente datosIngrediente;
    public string textoIndicador = "Recolectar [E]";
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    [HideInInspector] public PuntoSpawnRecoleccion puntoOrigen = null;

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null || datosIngrediente == null) return;
        if (canvasInfoActual == null)
        {
            Vector3 offset = Vector3.up * 0.5f;
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);
        }
        if (canvasInfoActual != null)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null) uiScript.textoNombre.text = $"{datosIngrediente.nombreIngrediente}\n[{textoIndicador}]";
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false);
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"{datosIngrediente.nombreIngrediente}\n[{textoIndicador}]";
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

    public void Recolectar()
    {
        if (InventoryManager.Instance != null && datosIngrediente != null)
        {
            InventoryManager.Instance.AddItem(datosIngrediente.nombreIngrediente);
        }
        if (datosIngrediente == null) return;

        Debug.Log($"Recolectado: {datosIngrediente.nombreIngrediente}");
        bool anadido = false;
        if (GestorJuego.Instance != null)
        {
            GestorJuego.Instance.AnadirStockTienda(datosIngrediente, 1);
            anadido = true;

            if (puntoOrigen != null)
            {
                puntoOrigen.diaUltimaRecoleccion = GestorJuego.Instance.diaActual;
                puntoOrigen.objetoInstanciadoActual = null;
            }
        }
        else
        {
            Debug.LogError("No se encontr� GestorJuego para a�adir al stock.");
        }

        if (anadido)
        {
            OcultarInformacion();
            Destroy(gameObject);
        }
    }
}