using System.Collections;
using UnityEngine;
using TMPro;

public class CartelAyuda : MonoBehaviour
{
    public string tagJugador = "Player";
    public string mensaje = "Presiona E para ayuda";
    public TextMeshProUGUI textoMensaje; // Asigna este campo desde el inspector
    public GameObject helpPanel; // Referencia al panel de ayuda en la UI
    private bool isPanelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador) && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagJugador) && textoMensaje != null)
        {
            textoMensaje.gameObject.SetActive(false);
            // Cerrar el panel si está abierto
            if (isPanelOpen)
            {
                helpPanel.SetActive(false);
                isPanelOpen = false;
            }
        }
    }

    private void Update()
    {
        // Verifica si el jugador está en rango y presiona E
        if (textoMensaje != null && textoMensaje.gameObject.activeSelf && Input.GetKeyDown(KeyCode.E))
        {
            if (!isPanelOpen)
            {
                // Abrir el panel
                helpPanel.SetActive(true);
                isPanelOpen = true;
            }
            else
            {
                // Cerrar el panel
                helpPanel.SetActive(false);
                isPanelOpen = false;
            }
        }
    }
}
