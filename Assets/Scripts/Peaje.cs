using UnityEngine;
using TMPro;

public class Peaje : MonoBehaviour
{
    public GameObject objetoBloqueador;
    public GameObject textoFlotante;
    public int costoPeaje = 15;

    private bool jugadorCerca = false;
    private bool yaInteractuo = false;

    private GestorUI gestorUI; // Referencia al gestor de monedas

    void Start()
    {
        gestorUI = FindObjectOfType<GestorUI>();
        if (gestorUI == null)
        {
            Debug.LogError("No se encontró el GestorUI en la escena.");
        }
    }

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E) && !yaInteractuo)
        {
            IntentarLiberarPaso();
        }
    }

    void IntentarLiberarPaso()
    {
        if (gestorUI != null && gestorUI.IntentarGastarDinero(costoPeaje))
        {
            LiberarPaso();
        }
        else
        {
            Debug.Log("No tienes suficiente dinero para pagar el peaje.");
            // Aquí podrías mostrar algún feedback visual si querés.
        }
    }

    void LiberarPaso()
    {
        Debug.Log("Peaje pagado. Te ha dejado pasar.");
        yaInteractuo = true;
        objetoBloqueador.SetActive(false);
        textoFlotante.SetActive(false);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (!yaInteractuo)
                textoFlotante.SetActive(true);
        }
    }

    void OnTriggerExit(Collider otro)
    {
        if (otro.CompareTag("Player"))
        {
            jugadorCerca = false;
            textoFlotante.SetActive(false);
        }
    }
}
