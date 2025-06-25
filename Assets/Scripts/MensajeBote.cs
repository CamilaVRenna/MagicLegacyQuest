using UnityEngine;
using TMPro;

public class MensajeBote : MonoBehaviour
{
    public string tagJugador = "Player";
    public TextMeshPro texto3D; // Arrástralo desde el inspector
    public string mensaje = "¡El bote pronto estará terminado!";

    private void Start()
    {
        if (texto3D != null)
        {
            texto3D.gameObject.SetActive(false); // Oculta el mensaje al inicio
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador) && texto3D != null)
        {
            texto3D.text = mensaje;
            texto3D.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagJugador) && texto3D != null)
        {
            texto3D.gameObject.SetActive(false);
        }
    }
}