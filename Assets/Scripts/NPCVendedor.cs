using UnityEngine;

public class NPCVendedor : MonoBehaviour
{
    // public GameObject panelTiendaVendedor; // Referencia a la UI de su tienda

    public void AbrirInterfazTienda()
    {
        Debug.Log($"Abriendo tienda de {gameObject.name}");
        // AQUÍ iría tu lógica para activar el panel de UI de la tienda de este NPC
        // if(panelTiendaVendedor != null) panelTiendaVendedor.SetActive(true);
        // Bloquear movimiento jugador, etc.
        FindObjectOfType<InteraccionJugador>()?.MostrarNotificacion($"Tienda de {gameObject.name} abierta.", 2f); // Ejemplo
    }
}