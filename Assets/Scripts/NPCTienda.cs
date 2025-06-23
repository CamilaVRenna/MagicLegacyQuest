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

    public DatosIngrediente flor;
    public DatosIngrediente hongo;
    public DatosIngrediente hueso;
    public DatosIngrediente miel;
    public DatosIngrediente pluma;
    public DatosIngrediente mariposa;

    private Animator animator;
    private bool mirandoVentana = false;

    private int pasoDialogo = 0; // 0: espera E para palita, 1: espera E para ingredientes, 2: se va

    void Start()
    {
        animator = GetComponent<Animator>();
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
            animator?.SetBool("Caminata", true);
            animator?.SetBool("Idle", false);
            mirandoVentana = false;
            MoverHaciaDestino();
        }
        else
        {
            animator?.SetBool("Idle", true);
            animator?.SetBool("Caminata", false);
            GirarHaciaVentana();
            if (esperandoInteraccion)
            {
                if (Input.GetKeyDown(KeyCode.E) && JugadorCerca())
                {
                    esperandoInteraccion = false;
                    if (pasoDialogo == 0)
                    {
                        StartCoroutine(DialogoPalita());
                    }
                    else if (pasoDialogo == 1)
                    {
                        StartCoroutine(DialogoIngredientes());
                    }
                }
            }
        }
    }

    void GirarHaciaVentana()
    {
        // Usar el punto de mirada del gestor, igual que NPCComprador
        if (mirandoVentana || gestor == null || gestor.puntoMiradaVentana == null) return;
        Vector3 dir = gestor.puntoMiradaVentana.position - transform.position;
        Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);
        if (dirHoriz.sqrMagnitude > 0.001f)
        {
            Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, velocidadRotacion * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)
            {
                transform.rotation = rotObj;
                mirandoVentana = true;
                // Debug.Log($"{gameObject.name} terminó de girar hacia la ventana.");
            }
        }
        else mirandoVentana = true;
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
            mirandoVentana = false; // <--- IMPORTANTE: igual que NPCComprador

            if (!yaEntrego)
            {
                esperandoInteraccion = true;
                pasoDialogo = 0;
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
        if (pasoDialogo == 0)
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
            pasoDialogo = 1;
            yield return new WaitForSeconds(duracionDialogo);

            OcultarBocadillo();

            // Preguntar por ingredientes
            MostrarBocadillo("¿Tienes ingredientes? (E para sí, F para no)");
            esperandoInteraccion = true;
        }
        else if (pasoDialogo == 1)
        {
            // Aquí puedes manejar la lógica si el jugador tiene ingredientes
            // Por ahora, solo vamos a destruir el NPC después de un diálogo
            MostrarBocadillo("Gracias por tu tiempo!");
            pasoDialogo = 2;
            yield return new WaitForSeconds(duracionDialogo);

            OcultarBocadillo();
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
        else
        {
            // Destruir NPC si ya no hay más diálogos
            if (gestor != null)
            {
                gestor.NPCTiendaTermino();
            }
            Destroy(gameObject);
        }
    }

    IEnumerator DialogoPalita()
    {
        MostrarBocadillo("Toma: te servirá para recolectar miel");

        // Añadir la palita al inventario
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItemByName("palita");
            // Mostrar mensaje en la interfaz
            InteraccionJugador jugador = FindObjectOfType<InteraccionJugador>();
            if (jugador != null)
                jugador.MostrarNotificacion("¡Has incorporado una palita para matar abejas!", 3f, false);
        }
        else
        {
            Debug.LogWarning("NPCTienda: No se encontró InventoryManager para añadir la palita.");
        }

        yaEntrego = true;
        pasoDialogo = 1;
        yield return new WaitForSeconds(duracionDialogo);

        MostrarBocadillo("Y aquí te dejo unos ingredientes que te servirán para tus pociones, luego los tendrás que recolectar por tu cuenta");
        esperandoInteraccion = true; // Espera E de nuevo
    }

    IEnumerator DialogoIngredientes()
    {
        // Añadir 5 de cada ingrediente al stock de la tienda
        if (GestorJuego.Instance != null)
        {
            if (flor != null) GestorJuego.Instance.AnadirStockTienda(flor, 5);
            if (hongo != null) GestorJuego.Instance.AnadirStockTienda(hongo, 5);
            if (hueso != null) GestorJuego.Instance.AnadirStockTienda(hueso, 5);
            if (miel != null) GestorJuego.Instance.AnadirStockTienda(miel, 5);
            if (pluma != null) GestorJuego.Instance.AnadirStockTienda(pluma, 5);
            if (mariposa != null) GestorJuego.Instance.AnadirStockTienda(mariposa, 5);

            // Mostrar mensaje en la interfaz
            InteraccionJugador jugador = FindObjectOfType<InteraccionJugador>();
            if (jugador != null)
                jugador.MostrarNotificacion("¡El vendedor te ha dado ingredientes para tus pociones!", 3f, false);
        }
        else
        {
            Debug.LogWarning("NPCTienda: No se encontró GestorJuego para añadir ingredientes.");
        }

        pasoDialogo = 2;
        yield return new WaitForSeconds(duracionDialogo);

        OcultarBocadillo();

        // En vez de irse, se convierte en NPCComprador
        var comprador = gameObject.AddComponent<NPCComprador>();
        comprador.gestor = this.gestor;

        // --- AQUI EL CAMBIO PARA PEDIR LA POCION ESPECÍFICA ---
        // El usuario indicó que es el "Pedido posible 4", que corresponde al índice 3.
        int indicePocionEspecifica = 3; 

        if (gestor != null && gestor.listaMaestraPedidos != null && gestor.listaMaestraPedidos.Count > indicePocionEspecifica)
        {
            PedidoPocionData recetaEspecifica = gestor.listaMaestraPedidos[indicePocionEspecifica];
            
            // Creamos una lista solo con esa receta y la configuramos en el comprador.
            comprador.listaPedidosEspecificos = new System.Collections.Generic.List<PedidoPocionData> { recetaEspecifica };
            comprador.usarListaEspecifica = true;
        }
        else
        {
            // Si algo falla (no hay gestor, la lista es muy corta), pedirá una aleatoria como antes.
            Debug.LogWarning("NPCTienda: No se pudo encontrar la receta específica (Pedido 4). Se usará la lista de pedidos aleatorios.");
            comprador.pedidosPosibles = gestor.listaMaestraPedidos;
        }
        // --- FIN DEL CAMBIO ---

        comprador.prefabBocadilloUI = this.prefabBocadilloUI;
        comprador.puntoAnclajeBocadillo = this.puntoAnclajeBocadillo;
        
        // Inicia el comportamiento del comprador (moverse a la ventana y esperar)
        comprador.IrAVentana(puntoVentana.position);
        
        // Destruimos este script para que no interfiera.
        Destroy(this);
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
