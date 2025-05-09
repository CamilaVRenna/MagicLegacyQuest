using UnityEngine;

[CreateAssetMenu(fileName = "NuevoIngrediente", menuName = "Alquimia/Datos Ingrediente")]
public class DatosIngrediente : ScriptableObject
{
    public string nombreIngrediente = "Nuevo Ingrediente";
    public Sprite icono;
    public GameObject prefabModelo3D;

    // --- AÑADIDO ---
    [Header("Ajustes Recolección Bosque")]
    [Tooltip("Arrastra aquí el PREFAB del objeto que se instanciará en el bosque para ser recolectado (Debe tener el script IngredienteRecolectable).")]
    public GameObject prefabRecolectable; // <<--- NUEVO CAMPO
    // ---------------

    // --- AÑADIDO ---
    [Header("Ajustes Visuales en Mano")]
    [Tooltip("Rotación adicional en Euler(X,Y,Z) para que se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero; // <<--- NUEVO CAMPO
    // ---------------
}