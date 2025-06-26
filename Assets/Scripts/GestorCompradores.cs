using UnityEngine;
using System.Collections.Generic;

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuraci�n General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aqu� TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles; 

    [Tooltip("N�mero m�ximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5;

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

    [Header("NPC Especial (Palita)")]
    public GameObject prefabNPCTienda; // Asigna tu prefab especial en el inspector
    private bool npctiendaEntregadoHoy = false;
    private bool npctiendaActivo = false; // <-- NUEVO

    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizador = 0f;

    private const string PREF_NPCTIENDA_ENTREGADO = "NPCTiendaEntregado";

    // Devuelve true si ya apareció alguna vez
    private bool NPCTiendaYaEntregado
    {
        get => PlayerPrefs.GetInt(PREF_NPCTIENDA_ENTREGADO, 0) == 1;
        set => PlayerPrefs.SetInt(PREF_NPCTIENDA_ENTREGADO, value ? 1 : 0);
    }

    void Update()
    {
        if (!tiendaAbierta || !compradoresHabilitados) return; // <--- Cambiado

        temporizador += Time.deltaTime;
        if (temporizador >= intervaloAparicion && PuedeGenerarMasNPCs()) 
        {
            temporizador = 0f;
            GenerarNPC();
        }

        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    private bool PuedeGenerarMasNPCs()
    {
        int totalNPCsActivos = colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0);
        bool limiteConcurrenteOk = totalNPCsActivos < maximoNPCsActivos;

        bool limiteDiarioOk = false;
        bool esDeNoche = false; 

        if (GestorJuego.Instance != null) 
        {
            limiteDiarioOk = GestorJuego.Instance.ObtenerNPCsGeneradosHoy() < GestorJuego.Instance.limiteNPCsPorDia;
            esDeNoche = GestorJuego.Instance.horaActual == HoraDelDia.Noche; 
        }
        else
        {
            Debug.LogError("GestorJuego no encontrado para verificar l�mite diario!");
            return false; 
        }

        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
        return limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
    }

    void GenerarNPC()
    {
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("�La lista 'prefabsNPCsPosibles' est� vac�a o no asignada en GestorCompradores! No se pueden generar NPCs.");
            return; 
        }
        if (puntoAparicion == null)
        {
            Debug.LogError("�Falta asignar Punto Aparicion en GestorCompradores!");
            return;
        }

        int indicePrefab = Random.Range(0, prefabsNPCsPosibles.Count); 
        GameObject prefabAUsar = prefabsNPCsPosibles[indicePrefab];

        if (prefabAUsar == null)
        {
            Debug.LogError($"El elemento {indicePrefab} en la lista 'prefabsNPCsPosibles' est� vac�o (None).");
            return;
        }

        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);

        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;
            colaNPCs.Enqueue(controladorNPC);

            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.RegistrarNPCGeneradoHoy();
            }
            else { Debug.LogWarning("GenerarNPC: No se encontr� GestorJuego para registrar NPC diario."); }

            Debug.Log($"NPC {objetoNPC.name} (Tipo: {prefabAUsar.name}) generado y a�adido a la cola. (Total en cola: {colaNPCs.Count}, Total activos: {colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0)})");

        }
        else
        {
            Debug.LogError($"�El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC);
        }
    }

    void AsignarSiguienteNPC()
    {
        if (npcActualEnVentana != null) return;
        npcActualEnVentana = colaNPCs.Dequeue();
        Debug.Log($"Asignando a {npcActualEnVentana.gameObject.name} a la ventana. ({colaNPCs.Count} restantes en cola)");
        npcActualEnVentana.gameObject.SetActive(true);
        npcActualEnVentana.IrAVentana(posicionVentana.position);
    }

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intent� notificar t�rmino.");
            if (npcActualEnVentana == npcQueTermino) { npcActualEnVentana = null; }
        }
    }

    public NPCComprador ObtenerNPCActual()
    {
        return npcActualEnVentana;
    }

    public void ReiniciarParaNuevoDia()
    {
        Debug.Log("GestorCompradores: Reiniciando para nuevo d�a...");

        if (npcActualEnVentana != null)
        {
            Debug.Log($"Destruyendo NPC en ventana: {npcActualEnVentana.gameObject.name}");
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue(); 
            if (npcEnCola != null)
            {
                Debug.Log($"- Destruyendo NPC en cola: {npcEnCola.gameObject.name}");
                Destroy(npcEnCola.gameObject); 
            }
        }
        colaNPCs.Clear();

        temporizador = 0f;
        Debug.Log("GestorCompradores: Reinicio completado. Temporizador a 0.");

        // --- NUEVO: Reiniciar flag para NPCTienda ---
        npctiendaEntregadoHoy = false;
    }

    public void DespawnTodosNPCsPorNoche()
    {
        Debug.LogWarning("GestorCompradores: Se hizo de noche. Despachando a todos los NPCs...");

        if (npcActualEnVentana != null)
        {
            Debug.Log($"- Despawneando NPC en ventana: {npcActualEnVentana.gameObject.name}");
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        Debug.Log($"- Vaciando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                Debug.Log($"- Despawneando NPC en cola: {npcEnCola.gameObject.name}");
                Destroy(npcEnCola.gameObject);
            }
        }
        colaNPCs.Clear(); 
        
        temporizador = 0f;
        Debug.Log("GestorCompradores: NPCs despawneados por noche.");
    }

    // --- NUEVO: Método para notificar que el NPCTienda terminó ---
    public void NPCTiendaTermino()
    {
        npctiendaActivo = false;
        NPCTiendaYaEntregado = true;
        compradoresHabilitados = true; // <--- Habilita compradores recién ahora
    }

    // --- NUEVO: Funci�n para generar el NPC especial de la tienda ---
    public void GenerarNPCTienda()
    {
        if (puntoAparicion == null || posicionVentana == null || puntoSalidaNPC == null)
        {
            Debug.LogError("Faltan puntos de aparición, ventana o salida para NPCTienda.");
            return;
        }

        GameObject obj = Instantiate(prefabNPCTienda, puntoAparicion.position, puntoAparicion.rotation);
        NPCTienda npcTienda = obj.GetComponent<NPCTienda>();
        if (npcTienda != null)
        {
            npcTienda.puntoVentana = posicionVentana;
            npcTienda.puntoSalida = puntoSalidaNPC;
            npcTienda.gestor = this; // <-- Asigna referencia al gestor
        }
        else
        {
            Debug.LogError("El prefabNPCTienda no tiene el script NPCTienda.");
            Destroy(obj);
        }
    }

    [HideInInspector]
    public bool tiendaAbierta = false;

    [HideInInspector]
    public bool compradoresHabilitados = false;
}