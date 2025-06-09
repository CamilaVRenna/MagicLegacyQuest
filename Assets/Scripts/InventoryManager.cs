using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<string> items = new List<string>();

    private int selectedIndex = -1;
    private bool inventarioAbierto = false; // NUEVO

    [Header("UI")]
    public GameObject panelInventario;
    public Transform contenidoInventario; // Donde se generan los botones
    public GameObject prefabBotonItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(string item)
    {
        items.Add(item);
        ActualizarUIInventario();
        UIMessageManager.Instance?.MostrarMensaje("Agregado al inventario: " + item);
    }

    public void RemoveItem(string item)
    {
        items.Remove(item);
                ActualizarUIInventario();

        UIMessageManager.Instance?.MostrarMensaje("Eliminado del inventario: " + item);
    }

    public bool HasItem(string item)
    {
        return items.Contains(item);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventarioAbierto = !inventarioAbierto; // Alterna abierto/cerrado
            bool activo = !panelInventario.activeSelf;
             panelInventario.SetActive(activo);
            Cursor.lockState = activo ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = activo;
            if (activo){
                ActualizarUIInventario();
            }
            if (inventarioAbierto)
            {
                string lista = "Q para soltar item actual\nT para tirar item actual\n";
                if (items.Count == 0)
                {
                  //      lista += "Inventario vacío";
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        string itemStr = $"{i + 1}. {items[i]}";
                        if (i == selectedIndex) itemStr += " *";
                        lista += itemStr + "\n";
                    }
                }
                UIMessageManager.Instance?.MostrarMensaje(lista);
            }
            else
            {
                UIMessageManager.Instance?.MostrarMensaje(""); // Limpia mensaje al cerrar
            }
        }

        // Solo permite seleccionar si el inventario está abierto
        if (inventarioAbierto)
        {
            for (int i = 0; i < Mathf.Min(items.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    selectedIndex = i;
                    UIMessageManager.Instance?.MostrarMensaje($"Seleccionaste: {items[selectedIndex]}");
                }
            }

            // Soltar item actual con Q (lo pasa al baúl y lo elimina del inventario)
            if (Input.GetKeyDown(KeyCode.Q) && selectedIndex >= 0 && selectedIndex < items.Count)
            {
                string item = items[selectedIndex];
                Baul baul = FindObjectOfType<Baul>();
                if (baul != null)
                {
                    baul.AgregarAlBaul(item);
                    UIMessageManager.Instance?.MostrarMensaje($"Soltaste el elemento \"{item}\" y lo guardaste en el baúl");
                }
                else
                {
                    Debug.LogWarning("No se encontró el baúl en la escena.");
                }
                items.RemoveAt(selectedIndex);
                if (items.Count == 0)
                    selectedIndex = -1;
                else if (selectedIndex >= items.Count)
                    selectedIndex = items.Count - 1;
            }

            // Tirar item actual con T (solo deselecciona, NO elimina)
            if (Input.GetKeyDown(KeyCode.T) && selectedIndex >= 0 && selectedIndex < items.Count)
            {
                UIMessageManager.Instance?.MostrarMensaje("Deseleccionaste el item actual.");
                selectedIndex = -1;
            }
        }
    }
   
      void ActualizarUIInventario()
    {
        foreach (Transform hijo in contenidoInventario)
        {
            Destroy(hijo.gameObject);
        }

        foreach (string item in items)
        {
            GameObject boton = Instantiate(prefabBotonItem, contenidoInventario);
            boton.GetComponentInChildren<TMP_Text>().text = item;

            // Cuando hacés click en el botón
            boton.GetComponent<Button>().onClick.AddListener(() => {
                RemoveItem(item);
            });
        }
    }
}