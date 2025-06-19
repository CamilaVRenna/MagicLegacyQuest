using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Image[] slots;
    public Text[] cantidadTexts;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ActualizarUIVisual(slots, cantidadTexts);
        }
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ActualizarUIVisual(slots, cantidadTexts);
        }
    }
}
