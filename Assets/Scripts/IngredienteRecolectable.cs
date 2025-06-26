using UnityEngine;
using TMPro;
using System.Collections; // Agrega esto arriba

[RequireComponent(typeof(Collider))]
public class IngredienteRecolectable : MonoBehaviour
{
    public DatosIngrediente datosIngrediente;
    public string textoIndicador = "Recolectar [E]";
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    [HideInInspector] public PuntoSpawnRecoleccion puntoOrigen = null;

    // --- NUEVO: Prefab y lógica de abejas ---
    public GameObject prefabAbeja; // Asigna en el inspector
    public Transform puntoSalidaAbejas; // Opcional, si quieres controlar el punto de aparición
    private int abejasDerrotadas = 0;
    private bool minijuegoAbejasActivo = false;
    private int abejasRestantes = 0;
    private bool minijuegoTerminado = false; // <-- NUEVO

    [SerializeField] public TextMeshProUGUI mensajeTemporalUI; // Asigna en el inspector

    private Coroutine mensajeCoroutine;

    public void IniciarMinijuegoAbejas()
    {
        abejasDerrotadas = 0;
        if (prefabAbeja == null)
        {
            Debug.LogError("No se asignó el prefab de abeja en IngredienteRecolectable.");
            return;
        }

        minijuegoAbejasActivo = true;
        minijuegoTerminado = false; // <-- NUEVO
        abejasRestantes = 5;

        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = (puntoSalidaAbejas != null) ? puntoSalidaAbejas.position : transform.position + Random.insideUnitSphere * 0.5f;
            GameObject abeja = Instantiate(prefabAbeja, spawnPos, Quaternion.identity);
            AbejaMinijuego abejaScript = abeja.GetComponent<AbejaMinijuego>();
            if (abejaScript != null)
            {
                abejaScript.SetObjetivoJugador(Camera.main.transform);
                abejaScript.onAbejaMuerta = OnAbejaMuerta;
            }
        }

        MostrarMensajeTemporal("¡Han salido 5 abejas! Mátalas haciendo click para recolectar la miel.");
    }

    // Callback cuando una abeja muere
    private void OnAbejaMuerta()
    {
        if (minijuegoTerminado) return; // <-- NUEVO

        abejasRestantes--;
        abejasDerrotadas++; // Suma una abeja derrotada

        // Actualiza el texto en pantalla
        if (GestorJuego.Instance != null)
            GestorJuego.Instance.SumarAbejaMatada();

        if (abejasRestantes <= 0)
        {
            minijuegoAbejasActivo = false;
            minijuegoTerminado = true;
            MostrarMensajeTemporal("¡Has derrotado a todas las abejas y puedes recolectar la miel!");
            Recolectar();
        }
    }

    void Update()
    {
        if (minijuegoAbejasActivo)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                abejasRestantes--;
                Debug.Log($"¡Rápido! Quedan {abejasRestantes} pulsaciones de 'P' para espantar las abejas.");
                if (abejasRestantes <= 0)
                {
                    minijuegoAbejasActivo = false;
                    Debug.Log("¡Has espantado a las abejas y recolectado la miel!");
                    Recolectar();
                }
            }
        }
    }

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
        // Ya NO agregamos nada al inventario aquí

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
            Debug.LogError("No se encontró GestorJuego para añadir al stock.");
        }

        if (anadido)
        {
            OcultarInformacion();
            Destroy(gameObject);
        }
    }

    // Llama a esto en vez de Debug.Log
    private void MostrarMensajeTemporal(string mensaje, float duracion = 2f)
    {
        if (mensajeCoroutine != null)
            StopCoroutine(mensajeCoroutine);
        mensajeCoroutine = StartCoroutine(MostrarMensajeCoroutine(mensaje, duracion));
    }

    private IEnumerator MostrarMensajeCoroutine(string mensaje, float duracion)
    {
        if (mensajeTemporalUI != null)
        {
            mensajeTemporalUI.text = mensaje;
            mensajeTemporalUI.gameObject.SetActive(true);
            yield return new WaitForSeconds(duracion);
            mensajeTemporalUI.gameObject.SetActive(false);
        }
    }
}