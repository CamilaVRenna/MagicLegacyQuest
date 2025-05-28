using UnityEngine;
using System;

public class AbejaMinijuego : MonoBehaviour
{
    public float velocidad = 3f;
    public float distanciaAtaque = 1.5f;
    public Action onAbejaMuerta;

    private Transform objetivo;

    public void SetObjetivoJugador(Transform jugador)
    {
        objetivo = jugador;
    }

    void Update()
    {
        if (objetivo != null)
        {
            Vector3 dir = (objetivo.position - transform.position).normalized;
            transform.position += dir * velocidad * Time.deltaTime;
        }
    }

    void OnMouseDown()
    {
        onAbejaMuerta?.Invoke();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("perdiste vida");
            Destroy(gameObject);
        }
    }
}