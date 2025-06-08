using UnityEngine;
using TMPro;

public class Comerciante : MonoBehaviour
{
    public GameObject panelTienda; // UI de la tienda
    public GameObject textoInteractuar; // Texto flotante 3D
    private bool jugadorCerca = false;
    private bool tiendaAbierta = false;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            AlternarTienda();
        }
    }

    void AlternarTienda()
    {
        tiendaAbierta = !tiendaAbierta;
        panelTienda.SetActive(tiendaAbierta);
        textoInteractuar.SetActive(!tiendaAbierta);
        
        // Opcional: detener movimiento del jugador mientras está en la tienda
        
        Time.timeScale = tiendaAbierta ? 0f : 1f; // Pausar juego si querés
        Debug.Log("Tienda abierta: " + tiendaAbierta);
        Cursor.lockState = tiendaAbierta ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = tiendaAbierta;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (!tiendaAbierta)
                textoInteractuar.SetActive(true);
        }
    }

    void OnTriggerExit(Collider otro)
    {
        if (otro.CompareTag("Player"))
        {
            jugadorCerca = false;
            textoInteractuar.SetActive(false);

            if (tiendaAbierta)
                AlternarTienda(); // Cierra la tienda si te alejás
        }
    }
    public void ComprarItem(string nombreItem)
{
    InventoryManager.Instance?.AddItem(nombreItem);
    UIMessageManager.Instance?.MostrarMensaje("¡Compraste: " + nombreItem + "!");
}
}
