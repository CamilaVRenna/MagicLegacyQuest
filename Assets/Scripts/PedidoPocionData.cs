using UnityEngine;
using System.Collections.Generic;

// Cambiamos el nombre del menú para que quede más claro que define una receta/resultado
[CreateAssetMenu(fileName = "NuevaRecetaResultado", menuName = "Pociones/Receta y Resultado")]
public class PedidoPocionData : ScriptableObject // Mantenemos el nombre de la clase por compatibilidad
{
    [Header("Identificación y Pedido NPC")]
    [Tooltip("Nombre interno para identificar esta receta/pedido (ej: 'CurativaSimple').")]
    public string nombreIdentificador = "RecetaGenerica"; // Antes era nombrePedido
    [Tooltip("Ingredientes EXACTOS que se requieren para esta receta (y que el NPC podría pedir).")]
    public List<DatosIngrediente> ingredientesRequeridos;
    /*[Tooltip("Texto opcional que podría decir el NPC al pedir esto.")]
    public string dialogoPedido = "¿Podrías prepararme una poción con...?";*/
    [Tooltip("Frases genéricas que un NPC puede usar para pedir esta poción. Elige una al azar.")]
    public List<string> dialogosPedidoGenericos; // <<--- NUEVA LISTA
    [Tooltip("Nombre corto o palabra clave para referencia interna (ej: 'Curación', 'Fuerza'). Opcional.")]
    public string clavePocion; // <<--- NUEVO OPCIONAL

    [Header("Resultado y Detalles de la Receta")] // <<--- NUEVA SECCIÓN ---
    [Tooltip("Nombre que se mostrará en la UI cuando se cree esta poción.")]
    public string nombreResultadoPocion = "Poción Desconocida"; // <<--- NUEVO
    [Tooltip("Material que se aplicará al frasco y al caldero al crear esta poción.")]
    public Material materialResultado; // <<--- NUEVO
    [Tooltip("Imagen que se mostrará en el libro de recetas (página izquierda).")]
    public Sprite imagenPocion; // <<--- NUEVO
    [TextArea(5, 10)] // Para que el campo sea más grande en el Inspector
    [Tooltip("Descripción, historia o instrucciones de la poción (página derecha).")]
    public string descripcionPocion = "Nadie sabe exactamente qué hace..."; // <<--- NUEVO
}