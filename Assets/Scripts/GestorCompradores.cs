using UnityEngine;
using System.Collections.Generic;

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuración General")]
    //public GameObject prefabNPC;
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aquí TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles; // <<--- CAMBIADO A LISTA

    [Tooltip("Número máximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5; // <<--- NUEVA VARIABLE PARA EL LÍMITE

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

    // --- Estado Interno ---
    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizador = 0f;

    void Update()
    {
        // Incrementa el temporizador
        temporizador += Time.deltaTime;

        // Si ha pasado suficiente tiempo Y AÚN NO HEMOS ALCANZADO EL LÍMITE...
        if (temporizador >= intervaloAparicion && PuedeGenerarMasNPCs()) // <<--- CONDICIÓN MODIFICADA
        {
            temporizador = 0f; // Reinicia temporizador SOLO si generamos o intentamos generar
            GenerarNPC();
        }
        // Si no se cumple la condición (tiempo o límite), el temporizador sigue contando,
        // pero no se reinicia ni se genera NPC, esperando al siguiente ciclo.

        // Asigna al siguiente si la ventana está libre (sin cambios)
        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    // --- NUEVA FUNCIÓN PRIVADA ---
    // Comprueba si se pueden generar más NPCs según el límite
    private bool PuedeGenerarMasNPCs()
    {
        // Límite concurrente (el que ya teníamos)
        int totalNPCsActivos = colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0);
        bool limiteConcurrenteOk = totalNPCsActivos < maximoNPCsActivos;

        // Límite diario (NUEVO)
        bool limiteDiarioOk = false;
        bool esDeNoche = false; // <<<--- DECLARACIÓN DE LA VARIABLE

        if (GestorJuego.Instance != null) // Acceder via Singleton
        {
            // Comprobar si los generados HOY son menores que el límite diario
            limiteDiarioOk = GestorJuego.Instance.ObtenerNPCsGeneradosHoy() < GestorJuego.Instance.limiteNPCsPorDia;
            esDeNoche = GestorJuego.Instance.horaActual == HoraDelDia.Noche; // <<--- NUEVA COMPROBACIÓN
        }
        else
        {
            Debug.LogError("GestorJuego no encontrado para verificar límite diario!");
            return false; // No generar si no podemos verificar
        }

        // Solo generar si se cumplen TODOS los límites Y NO es de noche
        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk && !esDeNoche; // <<--- AÑADIDO !esDeNoche

        // Solo generar si AMBOS límites están OK
        if (!limiteConcurrenteOk) // Log opcional
        {
            // Debug.Log("No se genera NPC: Límite concurrente alcanzado.");
        }
        if (!limiteDiarioOk) // Log opcional
        {
            // Debug.Log("No se genera NPC: Límite diario alcanzado.");
        }

        return limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
    }
    // --- FIN NUEVA FUNCIÓN ---

    // GenerarNPC (sin cambios internos, pero ahora solo se llama si PuedeGenerarMasNPCs es true)
    void GenerarNPC()
    {
        // --- MODIFICADO: Comprobar la LISTA de prefabs ---
        // Asegurarse de que la lista exista y tenga al menos un prefab asignado
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("¡La lista 'prefabsNPCsPosibles' está vacía o no asignada en GestorCompradores! No se pueden generar NPCs.");
            return; // Salir si no hay prefabs para elegir
        }
        // Comprobar el punto de aparición (igual que antes)
        if (puntoAparicion == null)
        {
            Debug.LogError("¡Falta asignar Punto Aparicion en GestorCompradores!");
            return;
        }
        // --- FIN COMPROBACIÓN ---

        // --- NUEVO: Elegir un Prefab al Azar de la Lista ---
        int indicePrefab = Random.Range(0, prefabsNPCsPosibles.Count); // Elige un índice aleatorio
        GameObject prefabAUsar = prefabsNPCsPosibles[indicePrefab]; // Obtiene el prefab de esa posición en la lista

        // Comprobar si el prefab elegido es válido (por si un elemento de la lista quedó vacío)
        if (prefabAUsar == null)
        {
            Debug.LogError($"El elemento {indicePrefab} en la lista 'prefabsNPCsPosibles' está vacío (None).");
            return; // No instanciar si el prefab elegido es nulo
        }
        // --- FIN ELEGIR PREFAB ---

        // --- MODIFICADO: Instanciar el prefab ELEGIDO ---
        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);
        // ----------------------------------------------

        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;
            colaNPCs.Enqueue(controladorNPC); // Añade a la cola

            // Registrar NPC generado hoy (igual que antes)
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.RegistrarNPCGeneradoHoy();
            }
            else { Debug.LogWarning("GenerarNPC: No se encontró GestorJuego para registrar NPC diario."); }


            // Log más informativo indicando qué tipo de NPC se generó
            Debug.Log($"NPC {objetoNPC.name} (Tipo: {prefabAUsar.name}) generado y añadido a la cola. (Total en cola: {colaNPCs.Count}, Total activos: {colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0)})");

        }
        else
        {
            // Log de error mencionando el prefab específico que falló
            Debug.LogError($"¡El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC); // Destruir la instancia creada incorrectamente
        }
    }

    // AsignarSiguienteNPC (sin cambios)
    void AsignarSiguienteNPC()
    {
        // ... (código igual que antes) ...
        if (npcActualEnVentana != null) return;
        npcActualEnVentana = colaNPCs.Dequeue();
        Debug.Log($"Asignando a {npcActualEnVentana.gameObject.name} a la ventana. ({colaNPCs.Count} restantes en cola)");
        npcActualEnVentana.gameObject.SetActive(true);
        npcActualEnVentana.IrAVentana(posicionVentana.position);
    }

    // NPCTermino (sin cambios)
    public void NPCTermino(NPCComprador npcQueTermino)
    {
        // ... (código igual que antes) ...
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intentó notificar término.");
            if (npcActualEnVentana == npcQueTermino) { npcActualEnVentana = null; }
        }
    }

    // ObtenerNPCActual (sin cambios)
    public NPCComprador ObtenerNPCActual()
    {
        // ... (código igual que antes) ...
        return npcActualEnVentana;
    }

    // --- NUEVO MÉTODO PÚBLICO ---
    public void ReiniciarParaNuevoDia()
    {
        Debug.Log("GestorCompradores: Reiniciando para nuevo día...");

        // 1. Destruir el NPC actual en la ventana (si hay uno)
        if (npcActualEnVentana != null)
        {
            Debug.Log($"Destruyendo NPC en ventana: {npcActualEnVentana.gameObject.name}");
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        // 2. Destruir todos los NPCs en la cola de espera
        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue(); // Saca el siguiente de la cola
            if (npcEnCola != null) // Comprobar por si acaso
            {
                Debug.Log($"- Destruyendo NPC en cola: {npcEnCola.gameObject.name}");
                Destroy(npcEnCola.gameObject); // Destruye su GameObject
            }
        }
        // Asegurarse de que la cola quede vacía (Dequeue ya lo hace, pero Clear() es explícito)
        colaNPCs.Clear();

        // 3. Reiniciar el temporizador de aparición
        temporizador = 0f; // Para que el primer NPC tarde 'intervaloAparicion' en aparecer
        Debug.Log("GestorCompradores: Reinicio completado. Temporizador a 0.");
    }
    // --- FIN NUEVO MÉTODO ---

    // Método para eliminar todos los NPCs activos cuando se hace de noche
    public void DespawnTodosNPCsPorNoche()
    {
        Debug.LogWarning("GestorCompradores: Se hizo de noche. Despachando a todos los NPCs...");

        // 1. NPC en la ventana: Simplemente lo destruimos para que desaparezca rápido
        if (npcActualEnVentana != null)
        {
            Debug.Log($"- Despawneando NPC en ventana: {npcActualEnVentana.gameObject.name}");
            // npcActualEnVentana.Irse(); // Podría usar Irse, pero quizás es más directo destruir
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        // 2. NPCs en la cola
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
        colaNPCs.Clear(); // Asegurar que quede vacía

        // 3. Resetear temporizador de spawn por si acaso
        temporizador = 0f;
        Debug.Log("GestorCompradores: NPCs despawneados por noche.");
    }

}