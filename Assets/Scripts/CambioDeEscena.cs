using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class CambioDeEscena : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Marca la flag en GestorJuego
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.interactuoConCueva = true;
            }

            StartCoroutine(ChangeScene());
        }
    }

     IEnumerator ChangeScene()
    {
            panel.gameObject.SetActive(true); 
            yield return new WaitForSeconds(8f); 
            SceneManager.LoadScene("TiendaDeMagia");
    }
}