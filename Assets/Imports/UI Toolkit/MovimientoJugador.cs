using UnityEngine;

public class MovimientoJugador : MonoBehaviour
{
    public float velocidadMovimiento = 5f;
    private CharacterController controlador;

    void Start()
    {
        controlador = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Debug.Log("Horizontal: " + horizontal + ", Vertical: " + vertical); // Añade esta línea

        Vector3 movimiento = new Vector3(horizontal, 0f, vertical) * velocidadMovimiento * Time.deltaTime;
        controlador.Move(transform.TransformDirection(movimiento));
    }
}