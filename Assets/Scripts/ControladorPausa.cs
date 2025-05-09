using UnityEngine;
using UnityEngine.SceneManagement; // Para cambiar de escena
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class ControladorPausa : MonoBehaviour
{
    [Tooltip("Arrastra aquí el GameObject PanelPausa desde la jerarquía del prefab del jugador.")]
    public GameObject panelPausa; // Referencia al panel UI

    [Header("Post Procesado")]
    [Tooltip("Arrastra aquí el asset del perfil SIN desenfoque.")]
    public PostProcessProfile perfilNormal; // <<--- Asignar PP_Normal aquí
    [Tooltip("Arrastra aquí el asset del perfil CON desenfoque.")]
    public PostProcessProfile perfilPausa; // <<--- Asignar PP_Desenfocado aquí

    private PostProcessVolume camaraVolume; // Referencia al Volume en la cámara

    // Variable estática para saber si el juego está pausado (accesible desde otros scripts si es necesario)
    public static bool JuegoPausado { get; private set; } = false;

    private GameObject canvasPrincipalRef = null; // <<--- AÑADE ESTA LÍNEA

    [Header("BotonesEX")]
    [Tooltip("Arrastra aquí el boton para cargar la partida.")]
    public Button botonCargarPartida;

    void Start()
    {
        // Asegurarse de que el panel esté oculto al empezar la escena
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
        else
        {
            Debug.LogError("¡Panel de Pausa no asignado en ControladorPausa!", this.gameObject);
        }
        // Asegurar que el tiempo corra normal al inicio de la escena
        Time.timeScale = 1f;
        JuegoPausado = false; // Resetear estado al cargar escena
                              // Asegurar que el cursor esté bloqueado al inicio de escenas de juego
        if (SceneManager.GetActiveScene().name != "MenuPrincipal") // No bloquear en menú principal
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Buscar el Volume en la cámara del jugador
        // Asumiendo que este script está en el objeto Jugador raíz
        Camera cam = GetComponentInChildren<Camera>(); // Busca la cámara hija
        if (cam != null)
        {
            camaraVolume = cam.GetComponent<PostProcessVolume>();
        }
        if (camaraVolume == null)
        {
            Debug.LogError("ControladorPausa no encontró PostProcessVolume en la cámara hija!", gameObject);
        }
        else
        {
            // Asegurar que empieza con el perfil normal
            if (perfilNormal != null) camaraVolume.profile = perfilNormal;
        }

        bool hayGuardado = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;
        if (botonCargarPartida != null) botonCargarPartida.interactable = hayGuardado;

    }

    void Update()
    {
        // Detectar la tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (JuegoPausado)
            {
                ReanudarJuego();
            }
            else
            {
                PausarJuego();
            }
        }
    }

    void PausarJuego()
    {
        JuegoPausado = true;
        Time.timeScale = 0f;

        if (panelPausa != null)
            panelPausa.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cambiar el perfil de postprocesado si está asignado
        if (camaraVolume != null && perfilPausa != null)
            camaraVolume.profile = perfilPausa;

        // Desactivar movimiento del jugador
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
            jugador.HabilitarMovimiento(false);
    }

    // Método público para ser llamado por el botón
    void ReanudarJuego()
    {
        JuegoPausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (camaraVolume != null && perfilNormal != null)
            camaraVolume.profile = perfilNormal;

        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
            jugador.HabilitarMovimiento(true);
    }


    // Método público para el botón "Menú Principal"
    public void IrMenuPrincipal()
    {
        Debug.Log("Volviendo al Menú Principal...");
        // ¡MUY IMPORTANTE! Restaurar la escala de tiempo ANTES de cargar la escena
        Time.timeScale = 1f;
        JuegoPausado = false; // Resetear estado

        // --- MOSTRAR HUD ANTES DE SALIR (POR SI ACASO) --- <<<--- AÑADIDO
        if (canvasPrincipalRef != null) canvasPrincipalRef.SetActive(true);
        // -------------------------------------------------

        // Asegurar perfil normal antes de salir
        if (camaraVolume != null) camaraVolume.profile = perfilNormal;
        GestorJuego.CargarEscenaConPantallaDeCarga("MainMenu");

        // Usar el cargador de escenas con pantalla de carga
        // Asegúrate de que el nombre "MainMenu" es correcto
        GestorJuego.CargarEscenaConPantallaDeCarga("MenuPrincipal");
    }

    // Método público para el botón "Salir del Juego"
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        // Código para salir (igual que en MainMenuController)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Llamado por el botón "Cargar Partida" del menú de pausa
    public void BotonCargarPartidaPresionado()
    {
        Debug.Log("Cargando último guardado (Reiniciando día)...");
        Time.timeScale = 1f;
        JuegoPausado = false;
        // Recargar la escena actual hará que GestorJuego.CargarDatos se ejecute
        GestorJuego.CargarEscenaConPantallaDeCarga(SceneManager.GetActiveScene().name);
    }

}