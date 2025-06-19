using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

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

    public CatalogoRecetas catalogoRecetas;
    public List<DatosIngrediente> todosLosIngredientes;
    public List<DatosFrasco> todosLosFrascos;

    private Image[] slots; // Se asignan en runtime desde la UI
    private Text[] cantidadTexts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetVisualReferences(Image[] newSlots, Text[] newCantidadTexts)
    {
        slots = newSlots;
        cantidadTexts = newCantidadTexts;
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
        }

        UIMessageManager.Instance?.MostrarMensaje("Agregado al inventario: " + item);
        ActualizarUIVisual(slots, cantidadTexts);

        // --- NUEVO: Selecciona automáticamente el slot recién agregado si es un frasco lleno ---
        if (item == "FrascoLleno")
        {
            selectedIndex = items.FindIndex(i => i.nombre == item);
            ActualizarSeleccionVisual();
        }
        // --- FIN NUEVO ---
    }

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
                items.Remove(stack);
            }
            UIMessageManager.Instance?.MostrarMensaje($"Removiste {item} del inventario.");
        }
        if (selectedIndex >= items.Count)
            selectedIndex = Mathf.Max(0, items.Count - 1);

        ActualizarUIVisual(slots, cantidadTexts);
    }

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
                items.Remove(stack);
            }
            UIMessageManager.Instance?.MostrarMensaje($"Eliminado del inventario: {item} x{eliminados}");
        }
        ActualizarUIVisual(slots, cantidadTexts);
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

    public void ActualizarUIVisual(Image[] slots, Text[] cantidadTexts)
    {
        // Solo actualizar referencias si no son null
        if (slots != null) this.slots = slots;
        if (cantidadTexts != null) this.cantidadTexts = cantidadTexts;

        if (this.slots == null) return;

        for (int i = 0; i < this.slots.Length; i++)
        {
            if (i < items.Count)
            {
                Sprite icono = BuscarIconoPorNombre(items[i].nombre);
                this.slots[i].sprite = icono;
                this.slots[i].enabled = true;
                if (this.cantidadTexts != null && i < this.cantidadTexts.Length)
                    this.cantidadTexts[i].text = items[i].cantidad > 1 ? items[i].cantidad.ToString() : "";
            }
            else
            {
                this.slots[i].sprite = null;
                this.slots[i].enabled = false;
                if (this.cantidadTexts != null && i < this.cantidadTexts.Length)
                    this.cantidadTexts[i].text = "";
            }
        }

        ActualizarSeleccionVisual();
    }

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
        if (slots == null) return;

        for (int i = 0; i < Mathf.Min(slots.Length, items.Count); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (slots[i].sprite != null && i < items.Count && items[i].cantidad > 0)
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
        // --- NUEVO: Si el usuario presiona un número mayor que la cantidad de items, deselecciona ---
        for (int i = items.Count; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = -1;
                UIMessageManager.Instance?.MostrarMensaje("Slot vacío.");
                ActualizarSeleccionVisual();
            }
        }

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
        }

        if (Input.GetKeyDown(KeyCode.T) && selectedIndex >= 0 && selectedIndex < items.Count)
        {
            UIMessageManager.Instance?.MostrarMensaje("Deseleccionaste el item actual.");
            selectedIndex = -1;
            ActualizarSeleccionVisual();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            inventarioAbierto = !inventarioAbierto;
            if (inventarioAbierto)
            {
                string lista = "Q para soltar item actual\nT para tirar item actual\n";
                for (int i = 0; i < items.Count; i++)
                {
                    string itemStr = $"{i + 1}. {items[i].nombre} x{items[i].cantidad}";
                    if (i == selectedIndex) itemStr += " *";
                    lista += itemStr + "\n";
                }
                UIMessageManager.Instance?.MostrarMensaje(lista);
            }
            else
            {
                UIMessageManager.Instance?.MostrarMensaje("");
            }
        }
    }
private void ActualizarSeleccionVisual()
{
    if (slots == null) return;

    for (int i = 0; i < slots.Length; i++)
    {
        if (i == selectedIndex && slots[i].sprite != null)
        {
            // Blanco brillante para el slot seleccionado
            slots[i].color = new Color(1f, 1f, 1f, 1f); // blanco puro y opaco
        }
        else
        {
            // Blanco más opaco para los demás (como si estuvieran deshabilitados)
            slots[i].color = new Color(0.7f, 0.7f, 0.7f, 1f); // gris claro
        }
    }
}

}
