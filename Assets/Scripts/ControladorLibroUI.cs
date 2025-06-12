using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

public class ControladorLibroUI : MonoBehaviour
{
    [Header("Referencias Cat�logo")]
    public CatalogoRecetas catalogo;

    [Header("Referencias UI - P�gina Izquierda")]
    public Image imagenRecetaIzquierda;
    [Header("Referencias UI - P�gina Derecha")]
    public TextMeshProUGUI textoNombreDerecha; // Podr�a ser redundante si lo pones a la izq.
    public TextMeshProUGUI textoDescripcionDerecha;
    public TextMeshProUGUI textoIngredientesDerecha;

    [Header("Referencias UI - Navegaci�n")]
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

    private int paginaActual = 0; // <<< AHORA representa el �NDICE de la RECETA mostrada
    private List<PedidoPocionData> recetasMostrables;

    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;

    public static bool LibroAbierto { get; private set; } = false;

    private GameObject canvasPrincipalRef = null; // <<--- A�ADE ESTA L�NEA

    void Start()
    {

        controladorJugador = FindObjectOfType<ControladorJugador>();
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        gameObject.SetActive(false);

        if (botonAnterior) botonAnterior.onClick.AddListener(PaginaAnterior);
        if (botonSiguiente) botonSiguiente.onClick.AddListener(PaginaSiguiente);
        if (botonCerrar) botonCerrar.onClick.AddListener(CerrarLibro);

        Camera camPrincipal = Camera.main; // Asumiendo que la c�mara est� etiquetada
        if (camPrincipal != null)
        {
            camaraVolume = camPrincipal.GetComponent<PostProcessVolume>();
        }
        if (camaraVolume == null)
        {
            Camera cualquierCamara = FindObjectOfType<Camera>();
            if (cualquierCamara != null) camaraVolume = cualquierCamara.GetComponent<PostProcessVolume>();
        }

        if (camaraVolume == null)
        {
            Debug.LogError("ControladorLibroUI no encontr� PostProcessVolume en la c�mara!", gameObject);
        }
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

        if (canvasPrincipalRef == null)
        {
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
        }
        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(false);
            Debug.Log("CanvasPrincipal ocultado por Libro.");
        }
        else { Debug.LogWarning("ControladorLibroUI: No se encontr� CanvasPrincipal para ocultar."); }

        if (camaraVolume != null) camaraVolume.profile = perfilLibro;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(false);
        // Ocultar HUD de item sostenido si est� visible
        if (interaccionJugador == null) interaccionJugador = FindObjectOfType<InteraccionJugador>(); // Buscar si no la ten�amos
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

        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(true);
            Debug.Log("CanvasPrincipal mostrado al Cerrar Libro.");
        }
        else
        {
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
            if (canvasPrincipalRef != null) canvasPrincipalRef.SetActive(true);
            else Debug.LogWarning("ControladorLibroUI: No se encontr� CanvasPrincipal para mostrar al cerrar.");
        }

        if (camaraVolume != null) camaraVolume.profile = perfilNormal;
        if (GestorAudio.Instancia != null && sonidoCerrarLibro != null) { GestorAudio.Instancia.ReproducirSonido(sonidoCerrarLibro); }

        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(true);

        if (interaccionJugador == null) interaccionJugador = FindObjectOfType<InteraccionJugador>(); // Buscar si no la ten�amos
        if (interaccionJugador != null && interaccionJugador.JugadorSostieneAlgo && interaccionJugador.panelItemSostenido != null)
        {
            interaccionJugador.panelItemSostenido.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PaginaSiguiente()
    {
        if (paginaActual + 1 < recetasMostrables.Count)
        {
            paginaActual++; // Avanzar al �ndice de la siguiente receta
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null) // Asume GestorAudio
            {
                GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    void PaginaAnterior()
    {
        if (paginaActual > 0)
        {
            paginaActual--; // Retroceder al �ndice de la receta anterior
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null) // Asume GestorAudio
            {
                GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    void MostrarPaginaActual()
    {
        if (recetasMostrables == null || recetasMostrables.Count == 0)
        {
            Debug.LogWarning("No hay recetas para mostrar.");
            if (imagenRecetaIzquierda != null) imagenRecetaIzquierda.enabled = false;
            if (textoNombreDerecha != null) textoNombreDerecha.text = "Libro Vac�o";
            if (textoDescripcionDerecha != null) textoDescripcionDerecha.text = "";
            if (textoIngredientesDerecha != null) textoIngredientesDerecha.text = "";
            if (botonAnterior) botonAnterior.interactable = false;
            if (botonSiguiente) botonSiguiente.interactable = false;
            return;
        }

        paginaActual = Mathf.Clamp(paginaActual, 0, recetasMostrables.Count - 1);

        PedidoPocionData recetaActual = recetasMostrables[paginaActual];

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

        if (botonAnterior) botonAnterior.interactable = (paginaActual > 0); // Activo si no es la primera
        if (botonSiguiente) botonSiguiente.interactable = (paginaActual + 1 < recetasMostrables.Count); // Activo si hay una receta siguiente
    }

    void Update()
    {
        if (!LibroAbierto) return;
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            PaginaSiguiente();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PaginaAnterior();
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarLibro();
        }
    }
}