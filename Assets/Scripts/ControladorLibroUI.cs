using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

public class ControladorLibroUI : MonoBehaviour
{
    [Header("Referencias Catálogo")]
    public CatalogoRecetas catalogo;

    [Header("Referencias UI - Página Izquierda")]
    public Image imagenRecetaIzquierda;
    // Podrías añadir un texto para el nombre aquí también si quisieras:
    // public TextMeshProUGUI textoNombreIzquierda;

    [Header("Referencias UI - Página Derecha")]
    public TextMeshProUGUI textoNombreDerecha; // Podría ser redundante si lo pones a la izq.
    public TextMeshProUGUI textoDescripcionDerecha;
    public TextMeshProUGUI textoIngredientesDerecha;

    [Header("Referencias UI - Navegación")]
    public Button botonAnterior;
    public Button botonSiguiente;
    public Button botonCerrar;

    [Header("Sonidos Libro")] // Nuevo Header
    public AudioClip sonidoPasarPagina; // <<--- NUEVA VARIABLE
    public AudioClip sonidoCerrarLibro; // <<--- NUEVA VARIABLE
    public AudioClip sonidoAbrirLibro; // <<--- NUEVA VARIABLE

    [Header("Post Procesado")]
    public PostProcessProfile perfilNormal; // <<--- Asignar PP_Normal
    public PostProcessProfile perfilLibro; // <<--- Asignar PP_Desenfocado

    private PostProcessVolume camaraVolume;

    // --- Estado Interno ---
    private int paginaActual = 0; // <<< AHORA representa el ÍNDICE de la RECETA mostrada
    private List<PedidoPocionData> recetasMostrables;

    // Referencias jugador (sin cambios)
    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;

    // --- Variable estática para saber si el libro está abierto ---
    public static bool LibroAbierto { get; private set; } = false;
    // ----------------------------------------------------------

    private GameObject canvasPrincipalRef = null; // <<--- AÑADE ESTA LÍNEA

    void Start()
    {

        controladorJugador = FindObjectOfType<ControladorJugador>();
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        gameObject.SetActive(false);

        if (botonAnterior) botonAnterior.onClick.AddListener(PaginaAnterior);
        if (botonSiguiente) botonSiguiente.onClick.AddListener(PaginaSiguiente);
        if (botonCerrar) botonCerrar.onClick.AddListener(CerrarLibro);

        // Buscar el Volume en la cámara principal del jugador
        Camera camPrincipal = Camera.main; // Asumiendo que la cámara está etiquetada
        if (camPrincipal != null)
        {
            camaraVolume = camPrincipal.GetComponent<PostProcessVolume>();
        }
        // Fallback por si no hay MainCamera tag
        if (camaraVolume == null)
        {
            Camera cualquierCamara = FindObjectOfType<Camera>();
            if (cualquierCamara != null) camaraVolume = cualquierCamara.GetComponent<PostProcessVolume>();
        }

        if (camaraVolume == null)
        {
            Debug.LogError("ControladorLibroUI no encontró PostProcessVolume en la cámara!", gameObject);
        }
        // No cambiamos perfil aquí, solo al abrir/cerrar

        LibroAbierto = false; // Asegurar estado inicial

    }

    public void AbrirLibro()
    {
        if (GestorAudio.Instancia != null && sonidoAbrirLibro != null) { GestorAudio.Instancia.ReproducirSonido(sonidoAbrirLibro); }
        if (catalogo == null || catalogo.todasLasRecetas == null) { /* ... error ... */ return; }

        recetasMostrables = catalogo.todasLasRecetas;
        if (recetasMostrables.Count == 0) { /* ... warning ... */ }

        Debug.Log("Abriendo Libro...");
        gameObject.SetActive(true);
        LibroAbierto = true; // <--- MARCAR COMO ABIERTO
        paginaActual = 0;
        MostrarPaginaActual();

        // --- OCULTAR HUD PRINCIPAL --- <<<--- AÑADIDO
        if (canvasPrincipalRef == null)
        {
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
        }
        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(false);
            Debug.Log("CanvasPrincipal ocultado por Libro.");
        }
        else { Debug.LogWarning("ControladorLibroUI: No se encontró CanvasPrincipal para ocultar."); }
        // -----------------------------

        // Cambiar al perfil de post-procesado del libro
        if (camaraVolume != null) camaraVolume.profile = perfilLibro;

        // Liberar cursor y deshabilitar jugador
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(false);
        // Ocultar HUD de item sostenido si está visible
        if (interaccionJugador == null) interaccionJugador = FindObjectOfType<InteraccionJugador>(); // Buscar si no la teníamos
        if (interaccionJugador != null && interaccionJugador.panelItemSostenido != null)
        {
            interaccionJugador.panelItemSostenido.SetActive(false);
        }
    }

    public void CerrarLibro()
    {
        Debug.Log("Cerrando Libro...");
        gameObject.SetActive(false);
        LibroAbierto = false; // <--- MARCAR COMO CERRADO

        // --- MOSTRAR HUD PRINCIPAL --- <<<--- AÑADIDO
        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(true);
            Debug.Log("CanvasPrincipal mostrado al Cerrar Libro.");
        }
        else
        {
            // Intentar buscarlo de nuevo
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
            if (canvasPrincipalRef != null) canvasPrincipalRef.SetActive(true);
            else Debug.LogWarning("ControladorLibroUI: No se encontró CanvasPrincipal para mostrar al cerrar.");
        }
        // -----------------------------


        // Volver al perfil normal de post-procesado
        if (camaraVolume != null) camaraVolume.profile = perfilNormal;
        if (GestorAudio.Instancia != null && sonidoCerrarLibro != null) { GestorAudio.Instancia.ReproducirSonido(sonidoCerrarLibro); }

        // Restaurar jugador y cursor
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(true);

        // Volver a mostrar HUD de item sostenido SI el jugador tiene algo
        if (interaccionJugador == null) interaccionJugador = FindObjectOfType<InteraccionJugador>(); // Buscar si no la teníamos
        if (interaccionJugador != null && interaccionJugador.JugadorSostieneAlgo && interaccionJugador.panelItemSostenido != null)
        {
            interaccionJugador.panelItemSostenido.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- PaginaSiguiente CORREGIDO ---
    void PaginaSiguiente()
    {
        // Comprobar si hay una receta SIGUIENTE a la actual
        if (paginaActual + 1 < recetasMostrables.Count)
        {
            paginaActual++; // Avanzar al índice de la siguiente receta
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null) // Asume GestorAudio
            {
                GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    // --- PaginaAnterior CORREGIDO ---
    void PaginaAnterior()
    {
        // Comprobar si podemos retroceder (si no estamos en la primera receta, índice 0)
        if (paginaActual > 0)
        {
            paginaActual--; // Retroceder al índice de la receta anterior
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null) // Asume GestorAudio
            {
                GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    // --- MostrarPaginasActuales RENOMBRADO y CORREGIDO ---
    void MostrarPaginaActual()
    {
        if (recetasMostrables == null || recetasMostrables.Count == 0)
        {
            Debug.LogWarning("No hay recetas para mostrar.");
            // Limpiar UI por si acaso
            if (imagenRecetaIzquierda != null) imagenRecetaIzquierda.enabled = false;
            if (textoNombreDerecha != null) textoNombreDerecha.text = "Libro Vacío";
            if (textoDescripcionDerecha != null) textoDescripcionDerecha.text = "";
            if (textoIngredientesDerecha != null) textoIngredientesDerecha.text = "";
            if (botonAnterior) botonAnterior.interactable = false;
            if (botonSiguiente) botonSiguiente.interactable = false;
            return;
        }

        // Asegurarse de que paginaActual esté dentro de los límites (por si acaso)
        paginaActual = Mathf.Clamp(paginaActual, 0, recetasMostrables.Count - 1);

        // Obtener la ÚNICA receta para esta doble página
        PedidoPocionData recetaActual = recetasMostrables[paginaActual];

        // --- Página Izquierda (Imagen) ---
        if (imagenRecetaIzquierda != null)
        {
            if (recetaActual.imagenPocion != null)
            {
                imagenRecetaIzquierda.sprite = recetaActual.imagenPocion;
                imagenRecetaIzquierda.enabled = true;
                imagenRecetaIzquierda.preserveAspect = true;
            }
            else { imagenRecetaIzquierda.enabled = false; } // Ocultar si la receta no tiene imagen
        }

        // --- Página Derecha (Texto) ---
        if (textoNombreDerecha != null) { textoNombreDerecha.text = recetaActual.nombreResultadoPocion; textoNombreDerecha.gameObject.SetActive(true); }
        if (textoDescripcionDerecha != null) { textoDescripcionDerecha.text = recetaActual.descripcionPocion; textoDescripcionDerecha.gameObject.SetActive(true); }
        if (textoIngredientesDerecha != null)
        {
            string textoIng = "Ingredientes:\n";
            if (recetaActual.ingredientesRequeridos != null) // Comprobar si la lista existe
            {
                foreach (var ing in recetaActual.ingredientesRequeridos)
                {
                    if (ing != null) textoIng += $"- {ing.nombreIngrediente}\n"; // Comprobar si el ingrediente no es nulo
                    else textoIng += "- ???\n";
                }
            }
            else { textoIng += "- Desconocidos"; }

            textoIngredientesDerecha.text = textoIng;
            textoIngredientesDerecha.gameObject.SetActive(true);
        }

        // --- Actualizar botones ---
        if (botonAnterior) botonAnterior.interactable = (paginaActual > 0); // Activo si no es la primera
        if (botonSiguiente) botonSiguiente.interactable = (paginaActual + 1 < recetasMostrables.Count); // Activo si hay una receta siguiente
    }
}