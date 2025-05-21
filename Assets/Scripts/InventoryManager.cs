using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<string> items = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Si ya existe, elimina el duplicado
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persiste entre escenas
    }

    public void AddItem(string item)
    {
        items.Add(item);
        Debug.Log("Agregado al inventario: " + item);
    }

    public void RemoveItem(string item)
    {
        items.Remove(item);
        Debug.Log("Eliminado del inventario: " + item);
    }

    public bool HasItem(string item)
    {
        return items.Contains(item);
    }
}
