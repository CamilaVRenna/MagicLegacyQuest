using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonItemTienda : MonoBehaviour
{
    public string nombreItem;
    public int precioItem;

    private Comerciante comerciante;
    private TextMeshProUGUI textoBoton;

    public void Inicializar(string nombre, int precio, Comerciante refComerciante)
    {
        nombreItem = nombre;
        precioItem = precio;
        comerciante = refComerciante;

        textoBoton = GetComponentInChildren<TextMeshProUGUI>();
        textoBoton.text = $"{nombreItem} - {precioItem}$";

        GetComponent<Button>().onClick.AddListener(Comprar);
    }

   void Comprar()
{
    comerciante.ComprarItem(nombreItem, precioItem);
}
}
