using System.Collections;
using UnityEngine;
using TMPro;

public class NPCTienda : MonoBehaviour
{
    public Transform puntoVentana; // Asigna desde el inspector (igual que GestorCompradores.posicionVentana)
    public Transform puntoSalida;  // Asigna desde el inspector (igual que GestorCompradores.puntoSalidaNPC)
    public float velocidadMovimiento = 4.0f;
    public float velocidadRotacion = 360f;
    public GameObject prefabBocadilloUI;
    public Transform puntoAnclajeBocadillo;
    public float duracionDialogo = 2.5f;

    private Vector3 destinoActual;
    private bool enMovimiento = false;
    private bool yaEntrego = false;
    private GameObject instanciaBocadilloActual = null;
    private TextMeshProUGUI textoBocadilloActual = null;
    public GestorCompradores gestor; // <-- Agrega esto arriba
    private bool esperandoInteraccion = false;

    void Start()
    {
        // Comienza moviéndose a la ventana
        if (puntoVentana != null)
        {
            destinoActual = puntoVentana.position;
            enMovimiento = true;
        }
        else
        {
            Debug.LogError("NPCTienda: Falta asignar puntoVentana.");
        }
    }

    void Update()
    {
        if (enMovimiento)
        {
            MoverHaciaDestino();
        }
        else if (esperandoInteraccion)
        {
            // Detectar si el jugador presiona E y está cerca
            if (Input.GetKeyDown(KeyCode.E) && JugadorCerca())
            {
                esperandoInteraccion = false;
                StartCoroutine(DialogoYRegalo());
            }
        }
    }

    void MoverHaciaDestino()
    {
        Vector3 direccion = destinoActual - transform.position;
        Vector3 direccionHorizontal = new Vector3(direccion.x, 0, direccion.z);

        // Rotar suavemente hacia el destino
        if (direccionHorizontal.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHorizontal);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                velocidadRotacion * Time.deltaTime
            );
        }

        // Mover hacia el destino
        float paso = velocidadMovimiento * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destinoActual, paso);

        // Llegó al destino
        if (Vector3.Distance(transform.position, destinoActual) < 0.2f)
        {
            transform.position = destinoActual;
            enMovimiento = false;

            if (!yaEntrego)
            {
                esperandoInteraccion = true;
                MostrarBocadillo("Pulsa E para hablar");
            }
            else
            {
                if (gestor != null)
                {
                    gestor.NPCTiendaTermino();
                }
                Destroy(gameObject);
            }
        }
    }

    IEnumerator DialogoYRegalo()
    {
        MostrarBocadillo("Toma, te lo regalo");

        // Añadir la palita al inventario
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem("palita");
        }
        else
        {
            Debug.LogWarning("NPCTienda: No se encontró InventoryManager para añadir la palita.");
        }

        yaEntrego = true;
        yield return new WaitForSeconds(duracionDialogo);

        OcultarBocadillo();

        // Irse a la salida
        if (puntoSalida != null)
        {
            destinoActual = puntoSalida.position;
            enMovimiento = true;
        }
        else
        {
            Debug.LogError("NPCTienda: Falta asignar puntoSalida.");
            Destroy(gameObject);
        }
    }

    void MostrarBocadillo(string texto)
    {
        if (instanciaBocadilloActual == null && prefabBocadilloUI != null && puntoAnclajeBocadillo != null)
        {
            instanciaBocadilloActual = Instantiate(prefabBocadilloUI, puntoAnclajeBocadillo.position, puntoAnclajeBocadillo.rotation, puntoAnclajeBocadillo);
            textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (instanciaBocadilloActual != null && textoBocadilloActual != null)
        {
            textoBocadilloActual.text = texto;
            instanciaBocadilloActual.SetActive(true);
        }
    }

    void OcultarBocadillo()
    {
        if (instanciaBocadilloActual != null)
        {
            instanciaBocadilloActual.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (instanciaBocadilloActual != null)
        {
            Destroy(instanciaBocadilloActual);
        }
    }

    // Comprueba si el jugador está cerca (ajusta el radio si lo necesitas)
    bool JugadorCerca()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.transform.position);
            return distancia < 2.5f; // Puedes ajustar el rango
        }
        return false;
    }
}
