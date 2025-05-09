using UnityEngine;
using UnityEngine.SceneManagement;

public class Arranque : MonoBehaviour
{
    // Nombre de la PRIMERA escena real a cargar (tu menú principal)
    public string primeraEscena = "MenuPrincipal";

    void Start()
    {
        // Llama inmediatamente al método estático para cargar el menú a través de la pantalla de carga
        GestorJuego.CargarEscenaConPantallaDeCarga(primeraEscena);
    }
}