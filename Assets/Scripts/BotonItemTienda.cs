using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonItemTienda : MonoBehaviour
{
    public string nombreItem;
    public int precioItem;

    private Comerciante comerciante;
    private TextMeshProUGUI textoBoton;
    private int cantidad;

    public void Inicializar(string nombre, int precio, int cantidad, Comerciante comerciante)
    {
        nombreItem = nombre;
        precioItem = precio;
        this.cantidad = cantidad;
        this.comerciante = comerciante;

        textoBoton = GetComponentInChildren<TextMeshProUGUI>();
        textoBoton.text = $"{nombreItem} - {precioItem}$";

        GetComponent<Button>().onClick.AddListener(() => comerciante.ComprarItem(nombre, precio, cantidad));
    }

    public void Comprar()
    {
        comerciante.ComprarItem(nombreItem, precioItem, cantidad);
    }
}
