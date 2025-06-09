using UnityEngine;
using TMPro;

public class Comerciante : MonoBehaviour
{
    public GameObject panelTienda; // UI de la tienda
    public GameObject textoInteractuar; // Texto flotante 3D
    private bool jugadorCerca = false;
    private bool tiendaAbierta = false;
    public GameObject prefabBotonItem; // Prefab del botón con el script BotonItemTienda
public Transform contenedorBotones; // Dónde se instancian los botones en la UI

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

    Time.timeScale = tiendaAbierta ? 0f : 1f;
    Cursor.lockState = tiendaAbierta ? CursorLockMode.None : CursorLockMode.Locked;
    Cursor.visible = tiendaAbierta;

    if (tiendaAbierta)
        GenerarBotones();
    else
        LimpiarBotones();
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
void GenerarBotones()
{
    // Lista hardcodeada, podés reemplazarla luego por datos reales
    string[] nombres = { "Espada", "Poción", "Escudo" };
    int[] precios = { 100, 50, 150 };

    for (int i = 0; i < nombres.Length; i++)
    {
        GameObject nuevoBoton = Instantiate(prefabBotonItem, contenedorBotones);
        BotonItemTienda scriptBoton = nuevoBoton.GetComponent<BotonItemTienda>();
        scriptBoton.Inicializar(nombres[i], precios[i], this);
    }
}

void LimpiarBotones()
{
    foreach (Transform hijo in contenedorBotones)
    {
        Destroy(hijo.gameObject);
    }
}
}
