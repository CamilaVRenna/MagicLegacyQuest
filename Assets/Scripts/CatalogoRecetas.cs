using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CatalogoDeRecetas", menuName = "Pociones/Catalogo de Recetas")]
public class CatalogoRecetas : ScriptableObject
{
    [Tooltip("Arrastra aqu� TODOS los assets de RecetaResultado (PedidoPocionData modificados) que definen pociones crafteables.")]
    public List<PedidoPocionData> todasLasRecetas; // Usamos PedidoPocionData aqu�

    public PedidoPocionData BuscarRecetaPorIngredientes(List<DatosIngrediente> ingredientesPocion)
    {
        if (todasLasRecetas == null || ingredientesPocion == null) return null;

        foreach (PedidoPocionData receta in todasLasRecetas)
        {
            if (CompararListasIngredientes(receta.ingredientesRequeridos, ingredientesPocion))
            {
                return receta; // �Encontrada!
            }
        }
        return null; // No encontrada
    }

    private bool CompararListasIngredientes(List<DatosIngrediente> lista1, List<DatosIngrediente> lista2)
    {
        if (lista1 == null || lista2 == null || lista1.Count != lista2.Count) return false;
        var tempLista1 = new List<DatosIngrediente>(lista1);
        var tempLista2 = new List<DatosIngrediente>(lista2);
        foreach (var item1 in tempLista1)
        {
            bool encontrado = false;
            for (int i = 0; i < tempLista2.Count; i++)
            {
                if (item1 == tempLista2[i])
                {
                    tempLista2.RemoveAt(i);
                    encontrado = true;
                    break;
                }
            }
            if (!encontrado) return false;
        }
        return tempLista2.Count == 0;
    }
}