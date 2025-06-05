using System.Collections.Generic;
using UnityEngine;

public class Baul : MonoBehaviour
{
    public List<string> objetosBaul = new List<string>();
    private int selectedIndex = -1;
    private bool baulAbierto = false; // NUEVO

    public void AgregarAlBaul(string item)
    {
        objetosBaul.Add(item);
        UIMessageManager.Instance?.MostrarMensaje($"Agregado al baúl: {item}");
    }

    public void RemoverDelBaul(string item)
    {
        objetosBaul.Remove(item);
        UIMessageManager.Instance?.MostrarMensaje($"Removido del baúl: {item}");
    }

    private void OnMouseDown()
    {
        baulAbierto = !baulAbierto; // Alterna abierto/cerrado
        if (baulAbierto)
        {
            string lista = "E para equipar item actual\nT para deseleccionar item actual\n";
            lista += "Objetos en el baúl:\n";
            if (objetosBaul.Count == 0)
            {
                lista += "Baúl vacío";
            }
            else
            {
                for (int i = 0; i < objetosBaul.Count; i++)
                {
                    string itemStr = $"{i + 1}. {objetosBaul[i]}";
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

    private void Update()
    {
        // Solo permite seleccionar si el baúl está abierto
        if (baulAbierto)
        {
            for (int i = 0; i < Mathf.Min(objetosBaul.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    selectedIndex = i;
                    UIMessageManager.Instance?.MostrarMensaje($"Seleccionaste: {objetosBaul[selectedIndex]}");
                }
            }

            // Transferir del baúl al inventario con E
            if (Input.GetKeyDown(KeyCode.E) && selectedIndex >= 0 && selectedIndex < objetosBaul.Count)
            {
                string item = objetosBaul[selectedIndex];
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(item);
                    UIMessageManager.Instance?.MostrarMensaje($"Sacaste del baúl: {item}");
                    objetosBaul.RemoveAt(selectedIndex);
                    selectedIndex = -1;
                }
            }

            // Deseleccionar item actual con T
            if (Input.GetKeyDown(KeyCode.T) && selectedIndex >= 0 && selectedIndex < objetosBaul.Count)
            {
                UIMessageManager.Instance?.MostrarMensaje("Deseleccionaste el item actual.");
                selectedIndex = -1;
            }
        }
    }
}