using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Cambia la estructura para soportar stacking
    [System.Serializable]
    public class ItemStack
    {
        public string nombre;
        public int cantidad;
        public ItemStack(string nombre, int cantidad)
        {
            this.nombre = nombre;
            this.cantidad = cantidad;
        }
    }

    public List<ItemStack> items = new List<ItemStack>();

    private int selectedIndex = -1;
    private bool inventarioAbierto = false;

    [Header("Slots visuales del inventario")]
    public Image[] slots; // Asigna los slots en el inspector
    public Text[] cantidadTexts; // Asigna los textos de cantidad en el inspector (uno por slot)

    // NUEVO: Referencia al catálogo de ingredientes y frascos para buscar iconos por nombre
    public CatalogoRecetas catalogoRecetas;
    public List<DatosIngrediente> todosLosIngredientes; // Asignar en inspector o en Start
    public List<DatosFrasco> todosLosFrascos; // Asignar en inspector o en Start

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(string item)
    {
        var stack = items.Find(i => i.nombre == item);
        if (stack != null)
        {
            stack.cantidad++;
        }
        else
        {
            items.Add(new ItemStack(item, 1));
            Sprite icono = BuscarIconoPorNombre(item);
            if (icono != null)
                AddItemVisual(icono, items.Count - 1);
        }
        UIMessageManager.Instance?.MostrarMensaje("Agregado al inventario: " + item);
        ActualizarUIVisual();
    }

    // NUEVO: Agrega ítem y su icono visual si corresponde
    public void AddItemByName(string item)
    {
        AddItem(item);
    }

    public void RemoveItem(string item)
    {
        var stack = items.Find(i => i.nombre == item);
        if (stack != null)
        {
            stack.cantidad--;
            if (stack.cantidad <= 0)
            {
                int idx = items.IndexOf(stack);
                items.Remove(stack);
                Sprite icono = BuscarIconoPorNombre(item);
                if (icono != null)
                    RemoveItemVisual(icono, idx);
            }
            UIMessageManager.Instance?.MostrarMensaje($"Removiste {item} del inventario.");
        }
        if (selectedIndex >= items.Count)
            selectedIndex = Mathf.Max(0, items.Count - 1);
        ActualizarUIVisual();
    }

    // NUEVO: Eliminar varias unidades de un ítem
    public void RemoveItem(string item, int cantidad)
    {
        var stack = items.Find(i => i.nombre == item);
        int eliminados = 0;
        if (stack != null)
        {
            int quitar = Mathf.Min(stack.cantidad, cantidad);
            stack.cantidad -= quitar;
            eliminados = quitar;
            if (stack.cantidad <= 0)
            {
                int idx = items.IndexOf(stack);
                items.Remove(stack);
                Sprite icono = BuscarIconoPorNombre(item);
                if (icono != null)
                    RemoveItemVisual(icono, idx);
            }
            UIMessageManager.Instance?.MostrarMensaje($"Eliminado del inventario: {item} x{eliminados}");
        }
        ActualizarUIVisual();
    }

    public bool HasItem(string item)
    {
        return items.Exists(i => i.nombre == item && i.cantidad > 0);
    }

    public int ContarItem(string item)
    {
        var stack = items.Find(i => i.nombre == item);
        return stack != null ? stack.cantidad : 0;
    }

    public string GetSelectedItem()
    {
        if (items.Count == 0 || selectedIndex < 0 || selectedIndex >= items.Count)
            return null;
        return items[selectedIndex].nombre;
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    // Cambia para soportar stacking y cantidad visual
    public void AddItemVisual(Sprite icono, int slotIndex = -1)
    {
        if (slots == null) return;
        if (slotIndex == -1)
        {
            // Busca el primer slot vacío
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].enabled)
                {
                    slotIndex = i;
                    break;
                }
            }
        }
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex].sprite = icono;
        slots[slotIndex].enabled = true;
    }

    public void RemoveItemVisual(Sprite icono, int slotIndex = -1)
    {
        if (slots == null) return;
        if (slotIndex == -1)
        {
            // Busca el slot que tiene este icono
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].sprite == icono)
                {
                    slotIndex = i;
                    break;
                }
            }
        }
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex].sprite = null;
        slots[slotIndex].enabled = false;
        if (cantidadTexts != null && slotIndex < cantidadTexts.Length)
            cantidadTexts[slotIndex].text = "";
    }

    private void ActualizarUIVisual()
    {
        // Actualiza los iconos y cantidades en los slots
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Count)
            {
                Sprite icono = BuscarIconoPorNombre(items[i].nombre);
                slots[i].sprite = icono;
                slots[i].enabled = true;
                if (cantidadTexts != null && i < cantidadTexts.Length)
                    cantidadTexts[i].text = items[i].cantidad > 1 ? items[i].cantidad.ToString() : "";
            }
            else
            {
                slots[i].sprite = null;
                slots[i].enabled = false;
                if (cantidadTexts != null && i < cantidadTexts.Length)
                    cantidadTexts[i].text = "";
            }
        }
        ActualizarSeleccionVisual();
    }

    // Busca el icono correspondiente por nombre (ingrediente o frasco)
    private Sprite BuscarIconoPorNombre(string nombre)
    {
        if (todosLosIngredientes != null)
        {
            foreach (var ing in todosLosIngredientes)
                if (ing != null && ing.nombreIngrediente == nombre)
                    return ing.icono;
        }
        if (todosLosFrascos != null)
        {
            foreach (var fr in todosLosFrascos)
                if (fr != null && fr.nombreItem == nombre)
                    return fr.icono;
        }
        return null;
    }

    private void Update()
    {
        // Permitir seleccionar slots con teclas numéricas SIEMPRE
        for (int i = 0; i < Mathf.Min(slots.Length, items.Count); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (slots[i].sprite != null && i < items.Count)
                {
                    selectedIndex = i;
                    UIMessageManager.Instance?.MostrarMensaje($"Seleccionaste: {items[selectedIndex].nombre}");
                }
                else
                {
                    selectedIndex = -1;
                    UIMessageManager.Instance?.MostrarMensaje("Slot vacío.");
                }
                ActualizarSeleccionVisual();
            }
        }

        // Soltar item actual con Q (lo pasa al baúl y lo elimina del inventario)
        if (Input.GetKeyDown(KeyCode.Q) && selectedIndex >= 0 && selectedIndex < items.Count)
        {
            string item = items[selectedIndex].nombre;
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
            RemoveItem(item);
            if (items.Count == 0)
                selectedIndex = -1;
            else if (selectedIndex >= items.Count)
                selectedIndex = items.Count - 1;
            ActualizarSeleccionVisual();
        }

        // Tirar item actual con T (solo deselecciona, NO elimina)
        if (Input.GetKeyDown(KeyCode.T) && selectedIndex >= 0 && selectedIndex < items.Count)
        {
            UIMessageManager.Instance?.MostrarMensaje("Deseleccionaste el item actual.");
            selectedIndex = -1;
            ActualizarSeleccionVisual();
        }

        // Inventario visual (abrir/cerrar) solo muestra mensaje, no afecta selección
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventarioAbierto = !inventarioAbierto;
            if (inventarioAbierto)
            {
                string lista = "Q para soltar item actual\nT para tirar item actual\n";
                if (items.Count == 0)
                {
                  // lista += "Inventario vacío";
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        string itemStr = $"{i + 1}. {items[i].nombre} x{items[i].cantidad}";
                        if (i == selectedIndex) itemStr += " *";
                        lista += itemStr + "\n";
                    }
                }
                UIMessageManager.Instance?.MostrarMensaje(lista);
            }
            else
            {
                UIMessageManager.Instance?.MostrarMensaje("");
            }
        }
    }

    // --- NUEVO: Actualiza el color de los slots para mostrar cuál está seleccionado ---
    private void ActualizarSeleccionVisual()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == selectedIndex && slots[i].sprite != null)
            {
                slots[i].color = Color.Lerp(Color.white, Color.yellow, 0.25f); // Un poco más blanco/amarillo
            }
            else
            {
                slots[i].color = Color.white;
            }
        }
    }

      /*void ActualizarUIInventario()
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
    }*/
}