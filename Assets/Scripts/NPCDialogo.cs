using UnityEngine;
public class NPCDialogo : MonoBehaviour
{
    public void IniciarDialogo()
    {
        Debug.Log($"Iniciando diálogo con {gameObject.name}");
        // AQUÍ iría tu lógica para mostrar la ventana de diálogo
        // Puedes usar un sistema de UI, mostrar notificaciones, etc.
        // Por ahora, solo un mensaje en consola.
        FindObjectOfType<InteraccionJugador>()?.MostrarNotificacion($"{gameObject.name}: Hola, viajero.", 3f); // Ejemplo
    }
}