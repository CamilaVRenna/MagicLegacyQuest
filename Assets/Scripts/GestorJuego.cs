using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Collections.Generic;
using System.Linq;

public enum HoraDelDia { Manana, Tarde, Noche }

[System.Serializable] 
public class StockInicialIngrediente
{
    public DatosIngrediente ingrediente;
    public int stockInicial = 5; 
}

[System.Serializable] // Necesario para que JsonUtility funcione
public class StockEntry
{
    public string ingredienteAssetName; // Guardamos el NOMBRE del asset ScriptableObject
    public int cantidad;
}

[System.Serializable]
public class StockDataWrapper
{
    public List<StockEntry> stockList = new List<StockEntry>();
}
public class GestorJuego : MonoBehaviour
{

    [Header("L�mites Diarios")] // Nuevo Header
    public int limiteNPCsPorDia = 5; // L�mite de NPCs a generar por d�a
    private int npcsGeneradosHoy = 0; // Contador interno

    [Header("Configuraci�n Guardado y Spawn")]
    [Tooltip("Punto donde aparece el jugador al INICIO DEL D�A (Empty GO cerca de la cama)")]
    private string nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // Nombre por DEFECTO o al cargar

    [Header("Inventario/Stock Ingredientes")]
    public List<StockInicialIngrediente> configuracionStockInicial; // Configurable en Inspector
    public Dictionary<DatosIngrediente, int> stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();

    private bool durmiendo = false; // <<--- NUEVA VARIABLE FLAG
    public static GestorJuego Instance { get; private set; }

    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar))
        {
            Debug.LogError("Se intent� cargar una escena con nombre vac�o.");
            return;
        }
        ControladorPantallaCarga.escenaACargar = nombreEscenaACargar;
        SceneManager.LoadScene("PantallaCarga");
    }

    void Awake()
    {
                Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CargarDatos(); // Carga datos existentes o inicializa por defecto
        }
        else { Destroy(gameObject); }
    }

    void OnEnable()
    {
        Debug.Log(">>> GESTOR JUEGO ON ENABLE - Suscribiendo a sceneLoaded <<<");
        SceneManager.sceneLoaded += EscenaCargada;
        Debug.Log("GestorJuego suscrito a sceneLoaded."); // Log para confirmar
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= EscenaCargada;
        Debug.Log("GestorJuego desuscrito de sceneLoaded."); // Log para confirmar
    }

    [Header("Estado del Juego")]
    public int diaActual = 1;
    public int dineroActual = 50;

    [Header("Ciclo D�a/Noche")]
    public HoraDelDia horaActual = HoraDelDia.Manana; // El d�a empieza por la ma�ana
    [Tooltip("Material Skybox para la ma�ana")]
    public Material skyboxManana; // <<--- Asigna tu Skybox de Ma�ana aqu�
    [Tooltip("Material Skybox para la tarde")]
    public Material skyboxTarde;   // <<--- Asigna tu Skybox de Tarde aqu�
    [Tooltip("Material Skybox para la noche")]
    public Material skyboxNoche;  // <<--- Asigna tu Skybox de Noche aqu�

    [Header("Econom�a")]
    public int valorPocionCorrecta = 5;
    public int costoRentaDiaria = 10;

    [Header("Referencias UI y Efectos")]
    public GestorUI gestorUI; // <<--- Usa el nombre de clase traducido
    public AudioClip sonidoGanarDinero;
    public AudioClip sonidoPerderDinero;
    public GestorCompradores gestorNPCs; // <<--- NUEVA REFERENCIA: Asigna el GestorNPCs aqu�

    [Header("Audio Ambiente")] // Puedes a�adir este encabezado para organizar
    [Tooltip("M�sica o sonido para el MenuPrincipal")]
    public AudioClip musicaMenu; // <<--- NUEVO
    [Tooltip("M�sica o sonido ambiente para el d�a (Ma�ana/Tarde)")]
    public AudioClip audioDia;      // <<--- A�ADE ESTA L�NEA
    [Tooltip("M�sica o sonido ambiente para la noche (grillos?)")]
    public AudioClip audioNoche;     // <<--- A�ADE ESTA L�NEA

    private Light luzDireccionalPrincipal = null; // <<--- A�ADE ESTA L�NEA

    void Start()
    {
        ActualizarAparienciaCiclo(true);
        Debug.Log("GestorJuego iniciado, Skybox inicial aplicado.");
    }

    void EscenaCargada(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == "Arranque" || escena.name == "LoadingScreen" || escena.name == "PantallaCarga")
        {
            Debug.Log($"EscenaCargada: Ignorando escena de utilidad '{escena.name}'.");
            return; // Salir si es una de estas escenas
        }

        Debug.Log($"---[EscenaCargada] Escena: '{escena.name}', Hora al entrar: {horaActual} ---");

        ActualizarAparienciaCiclo(true);

        Light[] luces = FindObjectsOfType<Light>();
        foreach (Light luz in luces)
        {
            if (luz.type == LightType.Directional)
            {
                luzDireccionalPrincipal = luz;
                Debug.Log($"Luz direccional encontrada: {luz.gameObject.name}");
                break;
            }
        }
        if (luzDireccionalPrincipal == null) Debug.LogWarning("No se encontr� luz direccional principal.");

        gestorUI = FindObjectOfType<GestorUI>(); // Busca el GestorUI en la escena reci�n cargada
        gestorNPCs = FindObjectOfType<GestorCompradores>(); // Busca el GestorNPCs

        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            PuntoSpawn[] puntos = FindObjectsOfType<PuntoSpawn>();

            Debug.Log($"EscenaCargada: Encontrados {puntos.Length} PuntoSpawn.");
            Debug.Log($"EscenaCargada: Buscando punto con nombre: '{nombrePuntoSpawnSiguiente}'");

            PuntoSpawn puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == nombrePuntoSpawnSiguiente); // A�adido p != null

            if (puntoDestino == null && nombrePuntoSpawnSiguiente != "SpawnInicialCama")
            {
                Debug.LogWarning($"No se encontr� '{nombrePuntoSpawnSiguiente}'. Buscando fallback 'SpawnInicialCama'...");
                puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");
            }

            if (puntoDestino != null)
            {
                Debug.Log($"Punto destino encontrado: '{puntoDestino.name}' en {puntoDestino.transform.position}. Intentando mover jugador...");
                CharacterController cc = jugador.GetComponent<CharacterController>();
                Vector3 posAntes = jugador.transform.position; // <<--- A�ADE ESTA L�NEA AQU�
                if (cc != null) cc.enabled = false; // Desactivar para teletransportar
                jugador.transform.position = puntoDestino.transform.position;
                jugador.transform.rotation = puntoDestino.transform.rotation; // Usar rotaci�n del punto
                if (cc != null) cc.enabled = true; // Reactivar

                Debug.Log($"Posici�n JUGADOR ANTES: {posAntes}, DESPU�S: {jugador.transform.position}"); // Verificar cambio

                jugador.ResetearVistaVertical();
            }
            else
            {
                Debug.LogError("�No se encontr� NING�N PuntoSpawn ('" + nombrePuntoSpawnSiguiente + "' o 'SpawnInicialCama') para posicionar al jugador!");
            }
        }
        else if (escena.name != "MenuPrincipal" && escena.name != "MainMenu") 
        {
            Debug.LogWarning("No se encontr� jugador en EscenaCargada.");
        } // No advertir en Men�

        AudioClip clipPoner = null; // Clip a reproducir por defecto (silencio)

        if (escena.name == "MenuPrincipal")
        { 
            clipPoner = musicaMenu;
            Debug.Log("EscenaCargada: Seleccionando m�sica del men�.");
        }
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque")
        { 
            switch (horaActual)
            {
                case HoraDelDia.Manana:
                case HoraDelDia.Tarde:
                    clipPoner = audioDia;
                    break;
                case HoraDelDia.Noche:
                    clipPoner = audioNoche;
                    break;
            }
            Debug.Log($"EscenaCargada: Seleccionando audio para {horaActual}: {(clipPoner != null ? clipPoner.name : "Ninguno")}");
        }

        if (GestorAudio.Instancia != null)
        {
            GestorAudio.Instancia.CambiarMusicaFondo(clipPoner);
        }
        else { Debug.LogWarning("GestorAudio no encontrado para cambiar m�sica."); }

        if (gestorUI != null)
        {
            Debug.Log("GestorUI encontrado en EscenaCargada. Actualizando UI.");
            gestorUI.ActualizarUIDinero(dineroActual);

            if (horaActual != HoraDelDia.Noche)
            {
                Debug.Log($"Mostrando UI del D�a {diaActual} porque es {horaActual}"); // Log opcional
                gestorUI.MostrarInicioDia(diaActual);
            }
            else
            {
                Debug.Log($"No se muestra UI del D�a porque es {horaActual}"); // Log opcional
            }
            // ------------------------------------------
        }
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque" || escena.name == "MainMenu")
        {
            Debug.LogWarning($"GestorUI no encontrado en la escena {escena.name}. �Falta el objeto UIManager o el script?");
        }

        if (gestorNPCs == null && escena.name == "TiendaDeMagia")
        {
            Debug.LogWarning($"GestorNPCs no encontrado en la escena {escena.name}.");
        }
    }

    public void AnadirDinero(int cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"Dinero a�adido: +{cantidad}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            // Llamadas traducidas
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(cantidad);
        }
        if (GestorAudio.Instancia != null && sonidoGanarDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoGanarDinero);
        }
        //GuardarDatos();
    }

    private void DeducirRenta()
    {
        dineroActual -= costoRentaDiaria;
        Debug.Log($"Renta diaria deducida: -{costoRentaDiaria}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(-costoRentaDiaria);
        }
        if (GestorAudio.Instancia != null && sonidoPerderDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoPerderDinero);
        }
    }

    public void IrADormir()
    {

        if (durmiendo)
        {
            Debug.Log("Ya est� en proceso de dormir, ignorando petici�n.");
            return; // Salir si ya estamos durmiendo
        }
        
        Debug.Log("Intentando ir a dormir (llamado desde interacci�n)..."); // Mensaje actualizado
        if (gestorUI != null)
        {
            StartCoroutine(SecuenciaDormir()); // Llama a la corutina directamente
        }
        else
        {
            Debug.LogError("No se puede dormir, falta GestorUI.");
        }
    }

    private IEnumerator SecuenciaDormir()
    {
        if (durmiendo) yield break; // Salir si ya est� corriendo
        durmiendo = true; // Marcar que empezamos a dormir

        try
        {
            Debug.Log("Iniciando secuencia de sue�o...");

            ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
            if (jugador != null) { jugador.HabilitarMovimiento(false); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontr� ControladorJugador para deshabilitar."); }

            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoANegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se har� fundido a negro."); }

            diaActual++;
            horaActual = HoraDelDia.Manana;
            GuardarDatos();
            npcsGeneradosHoy = 0;

            // REINICIAR la variable de salida al bosque
            PuertaCambioEscena.ReiniciarRegistroSalidaBosque();

            Debug.Log($"Comenzando D�A {diaActual} - Ma�ana");
            DeducirRenta();
            if (gestorNPCs != null) { gestorNPCs.ReiniciarParaNuevoDia(); }
            else { Debug.LogWarning("GestorNPCs no encontrado para reiniciar d�a."); } // Cambiado a Warning
            ActualizarAparienciaCiclo(true);
            if (GestorAudio.Instancia != null) { GestorAudio.Instancia.CambiarMusicaFondo(audioDia); } // Poner m�sica d�a

            Debug.Log("Forzando posici�n del jugador junto a la cama...");
            jugador = FindObjectOfType<ControladorJugador>(); // Buscar de nuevo por si acaso
            if (jugador != null)
            {
                PuntoSpawn puntoCama = FindObjectsOfType<PuntoSpawn>().FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");
                if (puntoCama != null)
                {
                    CharacterController cc = jugador.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    jugador.transform.position = puntoCama.transform.position;
                    jugador.transform.rotation = puntoCama.transform.rotation;
                    if (cc != null) cc.enabled = true;
                    jugador.GetComponent<ControladorJugador>()?.ResetearVistaVertical();
                }
                else { Debug.LogError("�No se encontr� 'SpawnInicialCama'!"); }
            }
            else { Debug.LogError("No se encontr� jugador para reposicionar."); }

            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoDesdeNegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se har� fundido desde negro."); }

            jugador = FindObjectOfType<ControladorJugador>(); // Buscar de nuevo
            if (jugador != null) { jugador.HabilitarMovimiento(true); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontr� ControladorJugador para habilitar."); }

            if (gestorUI != null) gestorUI.MostrarInicioDia(diaActual);

            // --- REACTIVAR EL CARTEL ---
            GameObject cartel = GameObject.Find("cartel");
            if (cartel != null)
                cartel.SetActive(true);

            Debug.Log("Secuencia de sueño completada.");

        } // Fin del try
        finally
        {
            durmiendo = false; // Permitir dormir de nuevo
            Debug.Log("Flag 'durmiendo' puesto a false.");
        }
    } // Fin de SecuenciaDormir 

    private void GuardarDatos()
    {
        Debug.LogWarning($"--- GUARDANDO DATOS --- D�a: {diaActual}, Hora: {horaActual}, Dinero: {dineroActual}");
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual); // Debe ser Manana (0) aqu�

        Debug.LogError($"GUARDANDO HoraActual como INT: {(int)horaActual} (Enum: {horaActual})");

        StockDataWrapper stockWrapper = new StockDataWrapper();
        foreach (var kvp in stockIngredientesTienda)
        {
            if (kvp.Key != null) stockWrapper.stockList.Add(new StockEntry { ingredienteAssetName = kvp.Key.name, cantidad = kvp.Value });
        }
        string stockJson = JsonUtility.ToJson(stockWrapper);
        PlayerPrefs.SetString("StockIngredientes", stockJson);
        Debug.Log($"Stock Guardado JSON: {stockJson.Substring(0, Mathf.Min(stockJson.Length, 100))}..."); // Mostrar solo inicio

        PlayerPrefs.Save();
        Debug.LogWarning("--- DATOS GUARDADOS ---");
    }

    private void CargarDatos()
    {
        if (!PlayerPrefs.HasKey("ExisteGuardado") || PlayerPrefs.GetInt("ExisteGuardado") == 0)
        {
            Debug.LogWarning("--- CARGANDO DATOS ---");
            InicializarValoresPorDefecto();
            return;
        }

        diaActual = PlayerPrefs.GetInt("DiaActual", 1);
        dineroActual = PlayerPrefs.GetInt("DineroActual", 50);
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana); // Carga la hora guardada (deber�a ser Manana)
        Debug.LogError($"CARGANDO HoraActual como INT: {PlayerPrefs.GetInt("HoraActual", -1)}, Convertido a Enum: {horaActual}"); // -1 si no existe
        Debug.LogError($"--- HORA CARGADA DE PLAYERPREFS: {horaActual} ---");

        stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();
        string stockJson = PlayerPrefs.GetString("StockIngredientes", "{}");
        StockDataWrapper stockWrapper = JsonUtility.FromJson<StockDataWrapper>(stockJson);
        if (stockWrapper?.stockList != null)
        {
            foreach (var entry in stockWrapper.stockList)
            {
                string resourcePath = $"Data/Ingredientes/{entry.ingredienteAssetName}"; // AJUSTA SI TU RUTA DENTRO DE RESOURCES ES DIFERENTE
                DatosIngrediente ingredienteAsset = Resources.Load<DatosIngrediente>(resourcePath);
                if (ingredienteAsset != null) stockIngredientesTienda[ingredienteAsset] = entry.cantidad;
                else Debug.LogWarning($"No se encontr� DatosIngrediente '{entry.ingredienteAssetName}' en 'Resources/{resourcePath}'.");
            }
            Debug.Log($"Stock cargado con {stockIngredientesTienda.Count} tipos.");
        }
        else { Debug.LogWarning("No se pudo deserializar stock."); }

        Debug.Log($"Datos Cargados - D�a: {diaActual}, Dinero: {dineroActual}, Hora: {horaActual}"); // Verifica la hora cargada

        nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // Usa el nombre de tu punto de spawn inicial
        Debug.LogWarning("--- DATOS CARGADOS ---");
    }

    private void InicializarValoresPorDefecto()
    {
        Debug.Log("Inicializando valores por defecto para Nueva Partida...");
        diaActual = 1;
        dineroActual = 50;
        horaActual = HoraDelDia.Manana;
        nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // <<--- A�ADE ESTA L�NEA
        npcsGeneradosHoy = 0;

        stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();
        if (configuracionStockInicial != null)
        {
            foreach (var config in configuracionStockInicial)
            {
                if (config?.ingrediente != null && !stockIngredientesTienda.ContainsKey(config.ingrediente))
                {
                    stockIngredientesTienda.Add(config.ingrediente, config.stockInicial);
                }
            }
        }
        Debug.Log($"Stock inicializado por defecto con {stockIngredientesTienda.Count} tipos.");
        PlayerPrefs.DeleteKey("ExisteGuardado");
        PlayerPrefs.Save(); // Guardar el borrado del flag
    }

    public int ObtenerNPCsGeneradosHoy()
    {
        return npcsGeneradosHoy;
    }

    public void RegistrarNPCGeneradoHoy()
    {
        if (npcsGeneradosHoy < limiteNPCsPorDia) // Seguridad extra
        {
            npcsGeneradosHoy++;
            Debug.Log($"NPC Registrado hoy. Total: {npcsGeneradosHoy}/{limiteNPCsPorDia}");
        }
        else
        {
            Debug.LogWarning("Se intent� registrar NPC pero ya se alcanz� el l�mite diario.");
        }
    }

    public bool PuedeDormir()
    {
        return horaActual == HoraDelDia.Noche;
    }

    void ActualizarAparienciaCiclo(bool instantaneo = false)
    {
        Debug.Log($"[ActualizarAparienciaCiclo] Ejecutando para Hora: {horaActual}");

        Material skyboxAplicar = null;
        Color luzAmbiente = new Color(0.5f, 0.5f, 0.5f, 1f); // Gris por defecto
        float intensidadSol = 1.0f; // Intensidad por defecto
        Quaternion rotacionSol = Quaternion.Euler(50f, -30f, 0f); // Ma�ana por defecto

        switch (horaActual)
        {
            case HoraDelDia.Manana:
                skyboxAplicar = skyboxManana;
                luzAmbiente = new Color(0.8f, 0.8f, 0.8f); // Claro
                rotacionSol = Quaternion.Euler(50f, -30f, 0f); // Sol alto
                intensidadSol = 1.0f;
                Debug.Log("[ActualizarAparienciaCiclo] Config Ma�ana.");
                break;
            case HoraDelDia.Tarde:
                skyboxAplicar = skyboxTarde;
                luzAmbiente = new Color(0.7f, 0.6f, 0.55f); // C�lido
                rotacionSol = Quaternion.Euler(20f, -150f, 0f); // Sol bajo
                intensidadSol = 0.75f; // Menos intenso
                Debug.Log("[ActualizarAparienciaCiclo] Config Tarde.");
                break;
            case HoraDelDia.Noche:
                skyboxAplicar = skyboxNoche;
                luzAmbiente = new Color(0.1f, 0.1f, 0.18f); // Oscuro azulado
                rotacionSol = Quaternion.Euler(-30f, -90f, 0f); // Posici�n de luna/bajo horizonte
                intensidadSol = 0.08f; // Muy tenue
                Debug.Log("[ActualizarAparienciaCiclo] Config Noche.");
                break;
        }

        Debug.Log($"[ActualizarAparienciaCiclo] Intentando aplicar Skybox: {(skyboxAplicar != null ? skyboxAplicar.name : "NINGUNO")}");

        if (skyboxAplicar != null) { RenderSettings.skybox = skyboxAplicar; DynamicGI.UpdateEnvironment(); }
        else { Debug.LogWarning($"Skybox NULO para {horaActual}."); }
        RenderSettings.ambientLight = luzAmbiente;
        Debug.Log($"[ActualizarAparienciaCiclo] Luz Ambiental aplicada: {luzAmbiente}");

        // Aplicar Luz Direccional
        if (luzDireccionalPrincipal != null)
        {
            luzDireccionalPrincipal.intensity = intensidadSol;
            luzDireccionalPrincipal.transform.rotation = rotacionSol;
            Debug.Log($"[ActualizarAparienciaCiclo] Luz Direccional - Intensidad: {intensidadSol}, Rot: {rotacionSol.eulerAngles}");
        }
    }

    public void RegistrarViaje(string escenaDestino)
    {
        HoraDelDia horaPrevia = horaActual;
        HoraDelDia nuevaHora = horaActual; // Por defecto no cambia

        Debug.Log($"[RegistrarViaje] Hora ANTES: {horaPrevia}, Viajando a: {escenaDestino}");

        if (horaActual == HoraDelDia.Manana && escenaDestino == "Bosque")
        {
            nuevaHora = HoraDelDia.Tarde;
        }
        else if (horaActual == HoraDelDia.Tarde && escenaDestino == "TiendaDeMagia")
        { // <-- Aseg�rate que este sea el nombre correcto
            nuevaHora = HoraDelDia.Noche;
        }

        if (nuevaHora != horaPrevia)
        {
            horaActual = nuevaHora;
            Debug.Log($"[RegistrarViaje] Hora CAMBIADA a: {horaActual}");
            if (horaActual == HoraDelDia.Noche)
            {
                Debug.Log("[RegistrarViaje] Se hizo de noche, despawneando NPCs...");
                gestorNPCs?.DespawnTodosNPCsPorNoche();
            }

            Debug.Log($"Viaje cambi� la hora de {horaPrevia} a {horaActual}");
        }
        else
        {
            Debug.Log($"Viaje a {escenaDestino} no cambi� la hora ({horaActual})");
        }
    }
    public int ObtenerStockTienda(DatosIngrediente tipo)
    {
        if (tipo != null && stockIngredientesTienda.TryGetValue(tipo, out int cantidad))
        {
            return cantidad;
        }
        return 0;
    }
    public bool ConsumirStockTienda(DatosIngrediente tipo)
    {
        if (tipo == null)
        {
            Debug.LogWarning("Intento de consumir ingrediente NULL.");
            return false;
        }

        if (stockIngredientesTienda.TryGetValue(tipo, out int cantidadActual) && cantidadActual > 0)
        {
            stockIngredientesTienda[tipo]--;
            Debug.Log($"Consumido 1 de {tipo.nombreIngrediente} del stock. Quedan: {stockIngredientesTienda[tipo]}");
            return true;
        }

        Debug.LogWarning($"No se pudo consumir '{tipo?.nombreIngrediente ?? "NULL"}'. Stock insuficiente o no encontrado.");
        return false;
    }
    public void AnadirStockTienda(DatosIngrediente tipo, int cantidadAAnadir)
    {
        if (tipo == null)
        {
            Debug.LogWarning("Intento de añadir stock para un tipo de ingrediente NULL.");
            return;
        }
        if (cantidadAAnadir <= 0)
        {
            Debug.LogWarning($"Intento de añadir una cantidad no positiva ({cantidadAAnadir}) de {tipo.nombreIngrediente}.");
            return;
        }
        if (stockIngredientesTienda.ContainsKey(tipo))
        {
            stockIngredientesTienda[tipo] += cantidadAAnadir;
        }
        else
        {
            stockIngredientesTienda.Add(tipo, cantidadAAnadir);
            Debug.LogWarning($"Ingrediente '{tipo.nombreIngrediente}' no estaba en el stock inicial, añadido ahora.");
        }
        Debug.Log($"Añadido +{cantidadAAnadir} de {tipo.nombreIngrediente} al stock. Nuevo total: {stockIngredientesTienda[tipo]}");
    }
    public void SetSiguientePuntoSpawn(string nombrePunto)
    {
        if (!string.IsNullOrEmpty(nombrePunto))
        {
            nombrePuntoSpawnSiguiente = nombrePunto;
            Debug.Log($"Siguiente punto de spawn fijado a: {nombrePuntoSpawnSiguiente}");
        }
        else
        {
            Debug.LogWarning("Se intent� fijar un nombre de punto de spawn vac�o. Se usar� el anterior o por defecto.");
        }
    }

} // Fin de la clase GestorJuego