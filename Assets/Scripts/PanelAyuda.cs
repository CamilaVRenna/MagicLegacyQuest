using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelAyuda : MonoBehaviour
{ 
    public GameObject helpPanel; // Referencia al panel de ayuda en la UI
    private bool isPanelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que choca es el jugador
        if (other.CompareTag("Player") && helpPanel != null)
        {
            helpPanel.SetActive(true);
            isPanelOpen = true;
            // Destruye el objeto del trigger después de activar el panel
        }
    }

    private void Update()
    {
        // Verifica si se presiona E para cerrar el panel
        if (isPanelOpen && Input.GetKeyDown(KeyCode.E))
        {
            helpPanel.SetActive(false);
            isPanelOpen = false;
            Destroy(gameObject);

        }
    }
}
