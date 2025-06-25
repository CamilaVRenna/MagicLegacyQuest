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
            StartCoroutine(ChangeScene());
        }
    }

     IEnumerator ChangeScene()
    {
                    panel.gameObject.SetActive(true); 
            yield return new WaitForSeconds(6f); 
            SceneManager.LoadScene("TiendaDeMagia");
    }
}