using UnityEngine;

[CreateAssetMenu(fileName = "Nuevo Frasco", menuName = "Alquimia/Datos Frasco")]
public class DatosFrasco : ScriptableObject
{
    [Header("Información Básica")]
    public string nombreItem = "Frasco Vacío";
    public Sprite icono;

    [Header("Visualización 3D (En Mano)")]
    public GameObject prefabModelo3D;
    public Material materialVacio;
    public Material materialLleno;

    // --- AÑADIDO ---
    [Header("Ajustes Visuales en Mano")]
    [Tooltip("Rotación adicional en Euler(X,Y,Z) para que se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero; // <<--- NUEVO CAMPO
    // ---------------
}