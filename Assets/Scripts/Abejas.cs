using UnityEngine;
using System;

public class AbejaMinijuego : MonoBehaviour
{
    public float velocidad = 3f;
    public float distanciaAtaque = 1.5f;
    public Action onAbejaMuerta;

    private Transform objetivo;

    private float cooldownRetroceso = 0.5f;
    private float tiempoUltimoRetroceso = -10f;

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
            if (Time.time - tiempoUltimoRetroceso < cooldownRetroceso)
                return;
            tiempoUltimoRetroceso = Time.time;

            Debug.Log("perdiste vida");

            // Retroceder la abeja al golpear al jugador
            Vector3 direccionRetroceso = (transform.position - other.transform.position).normalized;
            direccionRetroceso += new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0, UnityEngine.Random.Range(-0.2f, 0.2f));
            direccionRetroceso.Normalize();

            float distanciaRetroceso = 2f;
            Vector3 nuevaPos = transform.position + direccionRetroceso * distanciaRetroceso;

            // Desactivar collider temporalmente para evitar atascos
            Collider col = GetComponent<Collider>();
            if (col != null) StartCoroutine(DesactivarColliderTemporal(col, 0.5f));

            // Solo mover si hay espacio
            if (!Physics.CheckSphere(nuevaPos, 0.3f, LayerMask.GetMask("Default")))
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(direccionRetroceso * 200f, ForceMode.Impulse);
                }
                else
                {
                    transform.position = nuevaPos;
                }
            }
            else
            {
                // Si no hay espacio, solo rota la abeja
                transform.rotation = Quaternion.LookRotation(direccionRetroceso);
            }
        }
    }

    private System.Collections.IEnumerator DesactivarColliderTemporal(Collider col, float tiempo)
    {
        col.enabled = false;
        yield return new WaitForSeconds(tiempo);
        col.enabled = true;
    }
}