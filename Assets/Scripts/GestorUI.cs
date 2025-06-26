using UnityEngine;
using UnityEngine.UI; // Para Image
using TMPro;         // Para TextMeshProUGUI
using System.Collections; // Para Corutinas

// Nombre de clase cambiado
public class GestorUI : MonoBehaviour
{
    [Header("UI Persistente")] // Encabezados traducidos
    public TextMeshProUGUI textoDinero;
    public Image iconoMonedaDinero;
    public Sprite spriteMoneda;
    private int dineroActual = 50; // Dinero actual del jugador
public int DineroActual => dineroActual; // Propiedad para consultar desde otros scripts
public TextMeshProUGUI textoMielesRecolectadas; // Texto para mieles recolectadas

    [Header("UI Feedback D�a y Dinero")]
    public TextMeshProUGUI textoDia;
    [Tooltip("CanvasGroup del texto del d�a para controlar su alfa (fade).")] // Tooltips opcionales
    public CanvasGroup grupoCanvasTextoDia; // Nombre de variable cambiado
    public float duracionFadeDia = 1.0f;
    public float tiempoVisibleDia = 2.0f;

    public TextMeshProUGUI textoCambioDinero;
    public Image iconoMonedaCambio;
    [Tooltip("CanvasGroup del texto de cambio de dinero para controlar su alfa.")]
    public CanvasGroup grupoCanvasCambioDinero; // Nombre de variable cambiado
    public float duracionFadeCambioDinero = 0.5f;
    public float tiempoVisibleCambioDinero = 1.5f;

    [Header("UI Fundido Transici�n")] // Encabezado traducido
    [Tooltip("Una Imagen UI de color negro que cubra toda la pantalla.")]
    public Image panelFundidoNegro; // Nombre de variable cambiado
    public float duracionFundidoNegro = 0.75f; // Nombre de variable cambiado

    void Start()
    {
        
        // Asegurar estado inicial oculto/transparente
        if (grupoCanvasTextoDia != null) grupoCanvasTextoDia.alpha = 0;
        if (grupoCanvasCambioDinero != null) grupoCanvasCambioDinero.alpha = 0;
        if (panelFundidoNegro != null)
        {
            panelFundidoNegro.color = new Color(0, 0, 0, 0); // Transparente
            panelFundidoNegro.gameObject.SetActive(false); // Inactivo
        }

        // Asignar sprite moneda (sin cambios l�gicos)
        if (iconoMonedaDinero != null && spriteMoneda != null) iconoMonedaDinero.sprite = spriteMoneda;
        if (iconoMonedaCambio != null && spriteMoneda != null) iconoMonedaCambio.sprite = spriteMoneda;
    }

    // --- Actualizaci�n UI ---

    // Nombre de m�todo cambiado
    public void ActualizarUIDinero(int cantidad)
{
    dineroActual = cantidad;

    if (textoDinero != null)
    {
        textoDinero.text = cantidad.ToString();
    }
    if (iconoMonedaDinero != null && textoDinero != null)
    {
        iconoMonedaDinero.enabled = true;
    }
}

public bool IntentarGastarDinero(int cantidad)
{
    if (dineroActual >= cantidad)
    {
        dineroActual -= cantidad;
        // --- SINCRONIZAR CON GESTORJUEGO ---
        if (GestorJuego.Instance != null)
            GestorJuego.Instance.dineroActual = dineroActual;

        ActualizarUIDinero(dineroActual);
        MostrarCambioDinero(-cantidad); // Efecto visual
        return true;
    }
    else
    {
        Debug.Log("No hay suficiente dinero.");
        // Opcional: mostrar feedback visual o sonido de error
        return false;
    }
}

    // --- Efectos Visuales ---

    // Nombre de m�todo cambiado
    public void MostrarInicioDia(int dia)
    {
        if (textoDia != null && grupoCanvasTextoDia != null)
        {
            textoDia.text = $"DÍA {dia}"; // Mantenemos D�A en may�sculas por estilo
            grupoCanvasTextoDia.gameObject.SetActive(true);
            // Llamar a la corutina con nombre traducido
            StartCoroutine(FundidoEntradaSalidaElemento(grupoCanvasTextoDia, duracionFadeDia, tiempoVisibleDia));
        }
        else 
        {
            Debug.LogError("Falta TextoDia o GrupoCanvasTextoDia en GestorUI"); 
        } // Log de error
    }

    public void ActualizarTextoMieles(string texto)
    {
        if (textoMielesRecolectadas != null)
            textoMielesRecolectadas.text = texto;
        else
            Debug.LogWarning("No se asignó textoMielesRecolectadas en GestorUI.");
    }

    // Nombre de m�todo cambiado
    public void MostrarCambioDinero(int cantidad)
    {
        if (textoCambioDinero != null && grupoCanvasCambioDinero != null && iconoMonedaCambio != null)
        {
            string signo = (cantidad > 0) ? "+" : "";
            textoCambioDinero.text = $"{signo}{cantidad}";
            textoCambioDinero.color = (cantidad > 0) ? Color.green : Color.red;
            iconoMonedaCambio.color = textoCambioDinero.color;
            // Llamar a la corutina con nombre traducido
            StartCoroutine(FundidoEntradaSalidaElemento(grupoCanvasCambioDinero, duracionFadeCambioDinero, tiempoVisibleCambioDinero));
        }
    }

    // Nombre de m�todo cambiado
    public IEnumerator FundidoANegro() // Corutina p�blica para ser llamada desde GestorJuego
    {
        Debug.Log("Iniciando Fundido a Negro..."); // Mensaje traducido
        if (panelFundidoNegro == null) yield break;

        panelFundidoNegro.gameObject.SetActive(true); // Activar el panel
        // Llamar a la corutina auxiliar con nombre traducido
        yield return FundidoAlfaElemento(panelFundidoNegro, 0f, 1f, duracionFundidoNegro);
        Debug.Log("Fundido a Negro Completo."); // Mensaje traducido
    }

    // Nombre de m�todo cambiado
    public IEnumerator FundidoDesdeNegro() // Corutina p�blica
    {
        Debug.Log("Iniciando Fundido desde Negro..."); // Mensaje traducido
        if (panelFundidoNegro == null) yield break;

        // Llamar a la corutina auxiliar con nombre traducido
        yield return FundidoAlfaElemento(panelFundidoNegro, 1f, 0f, duracionFundidoNegro);
        panelFundidoNegro.gameObject.SetActive(false); // Desactivar al final
        Debug.Log("Fundido desde Negro Completo."); // Mensaje traducido
    }


    // --- Corutinas Auxiliares ---

    // Nombre de m�todo y par�metros cambiados
    private IEnumerator FundidoEntradaSalidaElemento(CanvasGroup gc, float durFade, float durVisible)
    {
        // Fade In
        float temporizador = 0f; // Variable renombrada
        while (temporizador < durFade)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(0f, 1f, temporizador / durFade);
            yield return null;
        }
        gc.alpha = 1f;

        // Esperar tiempo visible
        yield return new WaitForSeconds(durVisible);

        // Fade Out
        temporizador = 0f;
        while (temporizador < durFade)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(1f, 0f, temporizador / durFade);
            yield return null;
        }
        gc.alpha = 0f;
    }

    // Nombre de m�todo y par�metros cambiados
    private IEnumerator FundidoAlfaElemento(Image imagen, float alfaInicio, float alfaFinal, float duracion)
    {
        float temporizador = 0f; // Variable renombrada
        Color colorActual = imagen.color; // Variable renombrada
        while (temporizador < duracion)
        {
            temporizador += Time.deltaTime;
            float alfa = Mathf.Lerp(alfaInicio, alfaFinal, temporizador / duracion); // Variable renombrada
            imagen.color = new Color(colorActual.r, colorActual.g, colorActual.b, alfa);
            yield return null;
        }
        imagen.color = new Color(colorActual.r, colorActual.g, colorActual.b, alfaFinal);
    }
}