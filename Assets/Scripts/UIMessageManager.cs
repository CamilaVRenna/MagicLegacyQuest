using UnityEngine;
using TMPro;

public class UIMessageManager : MonoBehaviour
{
    public static UIMessageManager Instance { get; private set; }
    public TextMeshProUGUI textoMensajesUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void MostrarMensaje(string mensaje)
    {
        if (textoMensajesUI != null)
            textoMensajesUI.text = mensaje;
    }
}