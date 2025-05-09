using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para LINQ (GroupBy, OrderBy, etc.)

public class GestorRecoleccionBosque : MonoBehaviour
{
    // --- Clase interna para definir reglas por ingrediente ---
    [System.Serializable] // Para que se vea en el Inspector
    public class ConfigSpawnIngrediente
    {
        [Tooltip("El tipo de ingrediente para esta regla.")]
        public DatosIngrediente ingrediente;
        [Tooltip("Cuántos de ESTE ingrediente aparecerán como MÁXIMO cada día.")]
        public int maxPorDia;
        [Tooltip("Cuántos días deben pasar desde la recolección para que pueda volver a aparecer (1=al día sig., 2=esperar 1 día).")]
        public int diasCooldown = 1;
        [Range(0f, 1f)] // Slider de 0 a 1
        [Tooltip("Probabilidad (0=0%, 1=100%) de que aparezca en un punto disponible.")]
        public float probabilidadSpawn = 1.0f;
    }
    // --- Fin clase interna ---

    [Header("Configuración de Spawn")]
    [Tooltip("Define aquí las reglas para cada tipo de ingrediente que quieras que aparezca en el bosque.")]
    public List<ConfigSpawnIngrediente> configuracionSpawns; // Configura esto en el Inspector

    [Header("Puntos de Spawn (Opcional)")]
    [Tooltip("Puedes dejarla vacía para que busque todos los puntos automáticamente al iniciar, o arrastrarlos manualmente.")]
    public List<PuntoSpawnRecoleccion> todosLosPuntos;

    // Awake se llama una vez cuando el objeto se crea/activa
    void Awake()
    {
        // Buscar todos los puntos de spawn en la escena si no se asignaron manualmente
        if (todosLosPuntos == null || todosLosPuntos.Count == 0)
        {
            todosLosPuntos = FindObjectsOfType<PuntoSpawnRecoleccion>().ToList();
            Debug.Log($"[GestorRecoleccion] Encontrados {todosLosPuntos.Count} Puntos de Spawn de Recolección.");
        }
        else { Debug.Log($"[GestorRecoleccion] Usando {todosLosPuntos.Count} Puntos de Spawn asignados manualmente."); }
    }

    // Start se llama después de Awake
    void Start()
    {
        // Generar los ingredientes correspondientes al estado actual del juego
        GenerarIngredientesDelDia();
    }

    // Método principal que decide qué y dónde spawnear
    void GenerarIngredientesDelDia()
    {
        if (GestorJuego.Instance == null) { Debug.LogError("GestorRecoleccion: No se encontró GestorJuego."); return; }
        if (todosLosPuntos == null || todosLosPuntos.Count == 0) { Debug.LogWarning("GestorRecoleccion: No hay puntos de spawn definidos en la escena."); return; }

        if (GestorJuego.Instance.horaActual != HoraDelDia.Tarde) 
        {
             Debug.Log($"[GestorRecoleccion] No es de Tarde ({GestorJuego.Instance.horaActual}), no se generan ingredientes.");
             LimpiarObjetosInstanciados(); // Limpiar por si acaso
             return;
        }

        int diaActual = GestorJuego.Instance.diaActual;
        Debug.Log($"--- [GestorRecoleccion] Iniciando generación para el Día {diaActual} ---");

        // 1. Limpiar cualquier objeto que pudiera haber quedado del día anterior
        LimpiarObjetosInstanciados();

        // 2. Agrupar puntos por tipo de ingrediente
        var puntosAgrupados = todosLosPuntos
                              .Where(p => p != null && p.ingredienteParaSpawnear != null)
                              .GroupBy(p => p.ingredienteParaSpawnear);

        // 3. Iterar por cada tipo de ingrediente encontrado en los puntos
        foreach (var grupo in puntosAgrupados)
        {
            DatosIngrediente tipoIngrediente = grupo.Key;
            List<PuntoSpawnRecoleccion> puntosParaEsteTipo = grupo.ToList();

            // Buscar la configuración específica para este ingrediente
            ConfigSpawnIngrediente config = configuracionSpawns.FirstOrDefault(c => c.ingrediente == tipoIngrediente);

            if (config == null)
            {
                Debug.LogWarning($"No hay configuración de spawn para '{tipoIngrediente.nombreIngrediente}'. No aparecerá.");
                continue; // Pasar al siguiente tipo
            }

            // Filtrar los puntos que están listos para reaparecer (cooldown cumplido Y sin objeto actual)
            List<PuntoSpawnRecoleccion> puntosDisponibles = puntosParaEsteTipo
                .Where(p => p.objetoInstanciadoActual == null &&
                            diaActual >= p.diaUltimaRecoleccion + config.diasCooldown)
                .ToList();

            // Debug.Log($"Ingrediente: {tipoIngrediente.nombreIngrediente} - Puntos Totales: {puntosParaEsteTipo.Count}, Puntos Disponibles Hoy: {puntosDisponibles.Count}");

            // Calcular cuántos vamos a intentar spawnear hoy
            int maxASpawnearEsteTipo = Mathf.Min(config.maxPorDia, puntosDisponibles.Count);
            int spawneadosEsteTipo = 0;

            // Mezclar los puntos disponibles para que la aparición sea aleatoria entre ellos
            System.Random rng = new System.Random();
            puntosDisponibles = puntosDisponibles.OrderBy(p => rng.Next()).ToList();

            // Intentar spawnear en los puntos disponibles
            foreach (PuntoSpawnRecoleccion punto in puntosDisponibles)
            {
                if (spawneadosEsteTipo >= maxASpawnearEsteTipo) break; // Ya llegamos al máximo por día para este tipo

                // Comprobar probabilidad
                if (Random.value <= config.probabilidadSpawn)
                {
                    GameObject prefab = tipoIngrediente.prefabRecolectable;
                    if (prefab != null)
                    {
                        Quaternion rotacion = punto.rotacionAleatoriaY ?
                                            Quaternion.Euler(0, Random.Range(0f, 360f), 0) * prefab.transform.rotation : // Rota Y + base prefab
                                            prefab.transform.rotation; // Usa rotación base prefab

                        GameObject instanciado = Instantiate(prefab, punto.transform.position, rotacion);
                        // Hacerlo hijo del punto ayuda a organizar la jerarquía (opcional)
                        // instanciado.transform.SetParent(punto.transform);

                        // Guardar referencias importantes
                        punto.objetoInstanciadoActual = instanciado;
                        IngredienteRecolectable recolectable = instanciado.GetComponent<IngredienteRecolectable>();
                        if (recolectable != null) { recolectable.puntoOrigen = punto; }

                        spawneadosEsteTipo++;
                    }
                    else { Debug.LogWarning($"Ingrediente '{tipoIngrediente.name}' no tiene 'Prefab Recolectable' asignado."); }
                }
                // else -> Falló chequeo de probabilidad
            }
            Debug.Log($"-> Spawneados {spawneadosEsteTipo} de '{tipoIngrediente.nombreIngrediente}' (Máx Diario: {config.maxPorDia}, Puntos Disponibles Hoy: {puntosDisponibles.Count})");
        }
        Debug.Log("--- [GestorRecoleccion] Generación de ingredientes terminada ---");
    }

    // Limpia (destruye) cualquier objeto de ingrediente que esté actualmente instanciado en los puntos
    void LimpiarObjetosInstanciados()
    {
        // Debug.Log("[GestorRecoleccion] Limpiando objetos instanciados del día anterior..."); // Log opcional
        int cont = 0;
        foreach (var punto in todosLosPuntos)
        {
            if (punto != null && punto.objetoInstanciadoActual != null)
            {
                Destroy(punto.objetoInstanciadoActual);
                punto.objetoInstanciadoActual = null;
                cont++;
            }
        }
        if (cont > 0) Debug.Log($"[GestorRecoleccion] Limpiados {cont} objetos.");
    }
}