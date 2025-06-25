using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MensajeLobo : MonoBehaviour
{
    public string nombreEscena = "Bosque";
    public float tiempoEspera = 3f;
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;

    private bool yaActivado = false;

    void OnTriggerEnter(Collider other)
    {
        if (!yaActivado && other.CompareTag("Player"))
        {
            yaActivado = true;

            MostrarMensaje("Ten cuidado, hay un lobo. Busca una poción para dormirlo");
            Invoke(nameof(CambiarEscena), tiempoEspera);
        }
    }

    void MostrarMensaje(string mensaje)
    {
        if (panelMensaje != null && textoMensaje != null)
        {
            panelMensaje.SetActive(true);
            textoMensaje.text = mensaje;
        }
    }

    void CambiarEscena()
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
