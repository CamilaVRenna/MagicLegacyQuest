using System.Collections;
using UnityEngine;
using TMPro;

public class AbrirTiendaMensaje : MonoBehaviour
{
    public string tagJugador = "Player";
    public string mensaje = "Presiona E para abrir la tienda";
    public TextMeshProUGUI textoMensaje; // Asigna este campo desde el inspector

    private Coroutine mensajeCoroutine;

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

            // Si usaste la corrutina, detenla aquí:
            // if (mensajeCoroutine != null)
            //     StopCoroutine(mensajeCoroutine);
        }
    }

    // Si quieres que desaparezca automáticamente después de un tiempo:
    // private IEnumerator DesaparecerMensajeDespuesDeTiempo(float segundos)
    // {
    //     yield return new WaitForSeconds(segundos);
    //     if (textoMensaje != null)
    //         textoMensaje.gameObject.SetActive(false);
    // }
}