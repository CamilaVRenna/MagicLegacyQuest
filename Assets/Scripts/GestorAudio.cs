using UnityEngine;

// Asegura que este GameObject siempre tenga un componente AudioSource.
[RequireComponent(typeof(AudioSource))]
public class GestorAudio : MonoBehaviour
{
    // Variable privada para guardar la referencia al componente AudioSource.
    private AudioSource fuenteEfectos;

    [Header("Música/Ambiente")] // Nueva sección
    [Tooltip("Arrastra aquí un SEGUNDO componente AudioSource para la música/ambiente.")]
    public AudioSource fuenteMusicaFondo; // <<--- NUEVA VARIABLE

    // Propiedad estática para implementar el patrón Singleton.
    // Permite acceder a la instancia única de GestorAudio desde cualquier script.
    public static GestorAudio Instancia { get; private set; } // "Instance" es común mantenerlo así por el patrón Singleton

    // Awake se ejecuta antes que Start, ideal para inicializar Singletons.
    void Awake()
    {
        // Lógica del Singleton:
        // Si no existe ya una instancia...
        if (Instancia == null)
        {
            // ...esta se convierte en la instancia única.
            Instancia = this;
            // Opcional: Evita que este objeto se destruya al cargar una nueva escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, destruye este GameObject duplicado.
            Destroy(gameObject);
            // Salimos del método para evitar inicializar la fuente de audio en el duplicado.
            return;
        }

        // Obtenemos el componente AudioSource adjunto a este GameObject.
        fuenteEfectos = GetComponent<AudioSource>();

        // Configurar fuente para música/ambiente
        if (fuenteMusicaFondo != null)
        {
            fuenteMusicaFondo.loop = true;        // La música se repite
            fuenteMusicaFondo.playOnAwake = false; // No empieza sola
        }
        else
        {
            // Advertencia si no se asigna en el Inspector
            Debug.LogError("¡FuenteMusicaFondo no asignada en GestorAudio! No habrá música/ambiente.");
        }

    }

    // Método público para reproducir un sonido específico una vez.
    public void ReproducirSonido(AudioClip clip)
    {
        // Comprobamos que tanto el clip de audio como la fuente de audio no sean nulos.
        if (clip != null && fuenteEfectos != null)
        {
            // Reproduce el clip de audio proporcionado.
            fuenteEfectos.PlayOneShot(clip);
        }
        // Si el clip es nulo, muestra una advertencia en la consola.
        else if (clip == null)
        {
            Debug.LogWarning("Se intentó reproducir un AudioClip nulo.");
        }
        // Si la fuente de audio es nula (no debería pasar por RequireComponent y Awake), muestra una advertencia.
        else
        {
            Debug.LogWarning("El GestorAudio no tiene un componente AudioSource asignado o inicializado.");
        }
    }

    // --- NUEVO MÉTODO ---
    // Cambia la pista de fondo si es diferente a la actual
    public void CambiarMusicaFondo(AudioClip nuevoClip)
    {
        if (fuenteMusicaFondo == null) return; // Salir si no hay fuente asignada

        // Si el nuevo clip es nulo, detener música
        if (nuevoClip == null)
        {
            if (fuenteMusicaFondo.isPlaying) fuenteMusicaFondo.Stop();
            fuenteMusicaFondo.clip = null;
            Debug.Log("Música de fondo detenida (clip nulo).");
            return;
        }

        // Solo cambiar y reproducir si el clip es diferente al actual o si no está sonando
        if (fuenteMusicaFondo.clip != nuevoClip || !fuenteMusicaFondo.isPlaying)
        {
            Debug.Log($"Cambiando música/ambiente a: {nuevoClip.name}");
            fuenteMusicaFondo.clip = nuevoClip;
            fuenteMusicaFondo.Play(); // Play() respeta el loop = true
        }
    }
    // --- FIN NUEVO MÉTODO ---

}