using UnityEngine;
using TMPro;

public class Peaje : MonoBehaviour
{
    public GameObject objetoBloqueador;
    public GameObject textoFlotante; // Referencia al texto encima del NPC

    private bool jugadorCerca = false;
    private bool yaInteractuo = false;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E) && !yaInteractuo)
        {
            LiberarPaso();
        }
    }

    void LiberarPaso()
    {
        Debug.Log("Te ha dejado pasar.");
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
