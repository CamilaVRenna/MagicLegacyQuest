using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum TipoItem { Nada, Ingrediente, FrascoVacio, FrascoLleno }

[RequireComponent(typeof(AudioSource))]
public class InteraccionJugador : MonoBehaviour
{
    public float distanciaInteraccion = 3.0f;
    public Camera camaraJugador;
    public LayerMask capaInteraccion;

    public Image uiIconoItemSostenido;
    public TextMeshProUGUI uiNombreItemSostenido;
    public GameObject panelItemSostenido;

    public TextMeshProUGUI textoNotificacion;
    public float tiempoNotificacion = 2.5f;
    private float temporizadorNotificacion = 0f;

    public Transform puntoAnclajeItem3D;

    public AudioClip sonidoRecogerItem;
    public AudioClip sonidoRecogerPocion;
    public AudioClip sonidoTirarItem;
    public AudioClip sonidoError;

    private AudioSource audioSourceJugador;

    private ScriptableObject itemSostenido = null;
    private TipoItem tipoItemSostenido = TipoItem.Nada;
    public bool JugadorSostieneAlgo => tipoItemSostenido != TipoItem.Nada;
    private List<DatosIngrediente> contenidoFrascoLleno = null;
    private GameObject instanciaItem3D = null;

    private FuenteIngredientes fuenteIngredientesMirada = null;
    private Caldero calderoMiradoActual = null;
    private FuenteFrascos fuenteFrascosMirada = null;
    private NPCComprador npcMiradoActual = null;
    private LibroRecetasInteractuable libroMiradoActual = null;
    private PuertaCambioEscena puertaMiradaActual = null;
    private CamaInteractuable camaMiradaActual = null;
    private IngredienteRecolectable ingredienteRecolectableMirado = null;

    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    public GestorCompradores gestorNPC;
    public ControladorLibroUI controladorLibroUI;

    public TextMeshProUGUI textoInventario; // Arrástralo desde el inspector
    private bool inventarioVisible = false;

    void Start()
    {
        audioSourceJugador = GetComponent<AudioSource>();
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
        if (puntoAnclajeItem3D == null) Debug.LogWarning("FALTA ASIGNAR 'Punto Anclaje Item 3D'!", this.gameObject);
        if (gestorNPC == null) Debug.LogError("FALTA ASIGNAR 'Gestor NPC'!", this.gameObject);
        if (catalogoRecetas == null) Debug.LogError("FALTA ASIGNAR 'Catalogo Recetas'!", this.gameObject);
        if (materialPocionDesconocida == null) Debug.LogWarning("Material Pocion Desconocida no asignado.");
        if (controladorLibroUI == null) Debug.LogWarning("FALTA ASIGNAR 'Controlador Libro UI'!", this.gameObject);
    }

    void Update()
    {
        if (controladorLibroUI != null && controladorLibroUI.gameObject.activeInHierarchy) return;
        ManejarInteraccionMirada();
        ManejarEntradaAccion();
        ManejarNotificaciones();
        if (Input.GetKeyDown(KeyCode.Q) && tipoItemSostenido != TipoItem.Nada) { ReproducirSonidoJugador(sonidoTirarItem); LimpiarItemSostenido(); }
        if (Input.GetMouseButtonDown(1) && tipoItemSostenido == TipoItem.FrascoLleno) MostrarContenidoFrascoLleno();

        // Mostrar/ocultar inventario al apretar I
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventarioVisible = !inventarioVisible;
            if (textoInventario != null)
            {
                if (inventarioVisible)
                {
                    string contenido = (InventoryManager.Instance != null && InventoryManager.Instance.items.Count > 0)
                        ? "Inventario:\n- " + string.Join("\n- ", InventoryManager.Instance.items)
                        : "Inventario vacío";
                    textoInventario.text = contenido;
                    textoInventario.gameObject.SetActive(true);
                }
                else
                {
                    textoInventario.gameObject.SetActive(false);
                }
            }
        }
    }

    void ManejarInteraccionMirada()
    {
        RaycastHit hit;
        bool golpeoAlgo = Physics.Raycast(camaraJugador.transform.position, camaraJugador.transform.forward, out hit, distanciaInteraccion, capaInteraccion);
        GameObject objetoGolpeado = golpeoAlgo ? hit.collider.gameObject : null;

        if (fuenteIngredientesMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteIngredientesMirada.gameObject))
        {
            fuenteIngredientesMirada.OcultarInformacion();
            fuenteIngredientesMirada = null;
        }
        if (calderoMiradoActual != null && (!golpeoAlgo || objetoGolpeado != calderoMiradoActual.gameObject))
        {
            calderoMiradoActual = null;
        }
        if (fuenteFrascosMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteFrascosMirada.gameObject))
        {
            fuenteFrascosMirada.OcultarInformacion();
            fuenteFrascosMirada = null;
        }
        if (npcMiradoActual != null && (!golpeoAlgo || objetoGolpeado != npcMiradoActual.gameObject))
        {
            npcMiradoActual = null;
        }
        if (libroMiradoActual != null && (!golpeoAlgo || objetoGolpeado != libroMiradoActual.gameObject))
        {
            libroMiradoActual.OcultarInformacion();
            libroMiradoActual = null;
        }
        if (puertaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != puertaMiradaActual.gameObject))
        {
            puertaMiradaActual.OcultarInformacion();
            puertaMiradaActual = null;
        }
        if (camaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != camaMiradaActual.gameObject))
        {
            camaMiradaActual.OcultarInformacion();
            camaMiradaActual = null;
        }
        if (ingredienteRecolectableMirado != null && (!golpeoAlgo || objetoGolpeado != ingredienteRecolectableMirado.gameObject))
        {
            ingredienteRecolectableMirado.OcultarInformacion();
            ingredienteRecolectableMirado = null;
        }

        if (golpeoAlgo)
        {
            bool yaLoMiraba = (fuenteIngredientesMirada != null && objetoGolpeado == fuenteIngredientesMirada.gameObject) ||
                              (calderoMiradoActual != null && objetoGolpeado == calderoMiradoActual.gameObject) ||
                              (fuenteFrascosMirada != null && objetoGolpeado == fuenteFrascosMirada.gameObject) ||
                              (npcMiradoActual != null && objetoGolpeado == npcMiradoActual.gameObject) ||
                              (libroMiradoActual != null && objetoGolpeado == libroMiradoActual.gameObject) ||
                              (puertaMiradaActual != null && objetoGolpeado == puertaMiradaActual.gameObject) ||
                              (camaMiradaActual != null && objetoGolpeado == camaMiradaActual.gameObject);

            if (!yaLoMiraba)
            {
                if (objetoGolpeado.TryGetComponent(out FuenteIngredientes ingSrc)) { fuenteIngredientesMirada = ingSrc; ingSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out FuenteFrascos fraSrc)) { fuenteFrascosMirada = fraSrc; fraSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out Caldero caldSrc)) { calderoMiradoActual = caldSrc; return; }
                if (objetoGolpeado.GetComponentInParent<NPCComprador>() is NPCComprador npcCtrl) { npcMiradoActual = npcCtrl; return; }
                if (objetoGolpeado.TryGetComponent(out LibroRecetasInteractuable libroCtrl)) { libroMiradoActual = libroCtrl; libroCtrl.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out PuertaCambioEscena puertaCtrl)) { puertaMiradaActual = puertaCtrl; puertaMiradaActual.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out CamaInteractuable camaCtrl)) { camaMiradaActual = camaCtrl; camaMiradaActual.MostrarInformacion(); }
                if (objetoGolpeado.TryGetComponent(out IngredienteRecolectable ingRecCtrl))
                {
                    if (puertaMiradaActual != null) { puertaMiradaActual.OcultarInformacion(); puertaMiradaActual = null; }
                    if (camaMiradaActual != null) { camaMiradaActual.OcultarInformacion(); camaMiradaActual = null; }
                    ingredienteRecolectableMirado = ingRecCtrl;
                    ingredienteRecolectableMirado.MostrarInformacion();
                }
            }
        }
    }

    void ManejarEntradaAccion()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (fuenteIngredientesMirada != null) InteractuarConFuenteIngredientes();
        else if (fuenteFrascosMirada != null) InteractuarConFuenteFrascos();
        else if (calderoMiradoActual != null) InteractuarConCaldero();
        else if (npcMiradoActual != null) InteractuarConNPC();
        else if (libroMiradoActual != null) InteractuarConLibro();
        else if (puertaMiradaActual != null) InteractuarConPuerta();
        else if (camaMiradaActual != null) InteractuarConCama();
        else if (ingredienteRecolectableMirado != null) InteractuarConIngredienteRecolectable();
    }

    void InteractuarConFuenteIngredientes()
    {
        if (tipoItemSostenido == TipoItem.Nada)
        {
            DatosIngrediente r = fuenteIngredientesMirada.IntentarRecoger();
            if (r != null) EstablecerItemSostenido(r, TipoItem.Ingrediente);
            else MostrarNotificacion($"¡No quedan más {fuenteIngredientesMirada.datosIngrediente.nombreIngrediente}!", -1f, true);
        }
        else MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
    }

    void InteractuarConFuenteFrascos()
    {
        if (tipoItemSostenido == TipoItem.Nada)
        {
            DatosFrasco r = fuenteFrascosMirada.IntentarRecoger();
            if (r != null) EstablecerItemSostenido(r, TipoItem.FrascoVacio);
            else MostrarNotificacion($"¡No quedan más {fuenteFrascosMirada.datosFrasco.nombreItem}!", -1f, true);
        }
        else MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
    }

    void InteractuarConCaldero()
    {
        switch (tipoItemSostenido)
        {
            case TipoItem.Ingrediente:
                if (itemSostenido is DatosIngrediente i && calderoMiradoActual.AnadirIngrediente(i)) LimpiarItemSostenido();
                break;
            case TipoItem.FrascoVacio:
                if (calderoMiradoActual.EstaPocionLista())
                {
                    DatosIngrediente[] c = calderoMiradoActual.RecogerPocion();
                    if (c != null) LlenarFrascoSostenido(c);
                }
                else if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover)
                    MostrarNotificacion("Debes remover la mezcla primero.", -1f, true);
                else MostrarNotificacion("Caldero vacío o poción no lista.", -1f, true);
                break;
            case TipoItem.Nada:
                if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover)
                    calderoMiradoActual.IntentarIniciarRemovido();
                else if (calderoMiradoActual.EstaPocionLista())
                    MostrarNotificacion("Necesitas frasco vacío.", -1f, true);
                break;
            case TipoItem.FrascoLleno:
                MostrarNotificacion("Ya tienes una poción.", -1f, true);
                break;
        }
    }

    void InteractuarConNPC()
    {
        if (npcMiradoActual.TryGetComponent<NPCComprador>(out NPCComprador clienteTienda))
        {
            if (clienteTienda.EstaEsperandoAtencion()) clienteTienda.IniciarPedidoYTimer();
            else if (clienteTienda.EstaEsperandoEntrega() && tipoItemSostenido == TipoItem.FrascoLleno)
            {
                if (contenidoFrascoLleno != null) { clienteTienda.IntentarEntregarPocion(contenidoFrascoLleno); LimpiarItemSostenido(); }
                else MostrarNotificacion("Error interno frasco.", -1f, true);
            }
            else if (clienteTienda.EstaEsperandoEntrega())
                MostrarNotificacion("Necesitas la poción que pidió.", -1f, true);
            else MostrarNotificacion("Parece ocupado ahora mismo...", 2f, false);
        }
        else if (npcMiradoActual.TryGetComponent<NPCVendedor>(out NPCVendedor vendedor))
            vendedor.AbrirInterfazTienda();
        else if (npcMiradoActual.TryGetComponent<NPCDialogo>(out NPCDialogo dialogo))
            dialogo.IniciarDialogo();
        else MostrarNotificacion("No parece querer nada ahora...", 2f, false);
    }

    void InteractuarConLibro()
    {
        if (controladorLibroUI != null)
        {
            controladorLibroUI.AbrirLibro();
            fuenteIngredientesMirada = null; calderoMiradoActual = null; fuenteFrascosMirada = null; npcMiradoActual = null; puertaMiradaActual = null;
        }
        else { Debug.LogError("ControladorLibroUI no asignado!"); MostrarNotificacion("Error al abrir libro.", -1f, true); }
    }

    void InteractuarConPuerta()
    {
        if (GestorJuego.Instance != null)
        {
            if (GestorJuego.Instance.horaActual != HoraDelDia.Noche)
                puertaMiradaActual.CambiarEscena();
            else
                MostrarNotificacion("Será mejor que no salga ahora, podría encontrarme con un troll...", 3f, false);
        }
        else
        {
            Debug.LogError("No se encontró GestorJuego al interactuar con la puerta.");
            MostrarNotificacion("Error del sistema de tiempo.", 2f, true);
        }
    }

    void InteractuarConCama()
    {
        if (GestorJuego.Instance != null)
        {
            if (GestorJuego.Instance.PuedeDormir()) GestorJuego.Instance.IrADormir();
            else MostrarNotificacion("Solo puedes dormir por la noche...", 2f, false);
        }
        else Debug.LogError("No se encontró GestorJuego para intentar dormir.");
    }

    void InteractuarConIngredienteRecolectable()
    {
        if (ingredienteRecolectableMirado != null && ingredienteRecolectableMirado.datosIngrediente != null)
        {
            if (ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente.ToLower() == "miel"
                && InventoryManager.Instance != null
                && InventoryManager.Instance.HasItem("palita"))
            {
                if (Random.value < 0.85f) // 85% de probabilidad de abejas
                {
                    ingredienteRecolectableMirado.IniciarMinijuegoAbejas();
                    return; // Espera a que termine el minijuego
                }
                else
                {
                    Debug.Log("No hay abejas");
                    ingredienteRecolectableMirado.Recolectar();
                    return;
                }
            }
        }

        if (tipoItemSostenido == TipoItem.Nada)
        {
            ingredienteRecolectableMirado.Recolectar();
        }
        else MostrarNotificacion("Tienes las manos llenas para recolectar.", 2f, true);
    }

    void EstablecerItemSostenido(ScriptableObject itemData, TipoItem tipo)
    {
        itemSostenido = itemData;
        tipoItemSostenido = tipo;
        contenidoFrascoLleno = null;
        LimpiarInstanciaItem3D();

        string nombreMostrar = "Desconocido";
        Sprite iconoMostrar = null;
        GameObject prefab3D = null;
        Material matVacio = null;
        Vector3 rotacionItemEspecifica = Vector3.zero;

        if (tipo == TipoItem.Ingrediente && itemData is DatosIngrediente ingData)
        {
            nombreMostrar = ingData.nombreIngrediente;
            iconoMostrar = ingData.icono;
            prefab3D = ingData.prefabModelo3D;
            rotacionItemEspecifica = ingData.rotacionEnMano;
        }
        else if (tipo == TipoItem.FrascoVacio && itemData is DatosFrasco fraData)
        {
            nombreMostrar = fraData.nombreItem;
            iconoMostrar = fraData.icono;
            prefab3D = fraData.prefabModelo3D;
            matVacio = fraData.materialVacio;
            rotacionItemEspecifica = fraData.rotacionEnMano;
        }

        if (panelItemSostenido != null)
        {
            if (uiIconoItemSostenido != null && iconoMostrar != null) { uiIconoItemSostenido.sprite = iconoMostrar; uiIconoItemSostenido.enabled = true; }
            else if (uiIconoItemSostenido != null) { uiIconoItemSostenido.enabled = false; }
            if (uiNombreItemSostenido != null) { uiNombreItemSostenido.text = nombreMostrar; }
            panelItemSostenido.SetActive(true);
        }
        if (prefab3D != null && puntoAnclajeItem3D != null)
        {
            instanciaItem3D = Instantiate(prefab3D, puntoAnclajeItem3D);
            instanciaItem3D.transform.localPosition = Vector3.zero;
            instanciaItem3D.transform.localRotation = Quaternion.identity;

            if (rotacionItemEspecifica != Vector3.zero)
                instanciaItem3D.transform.localRotation *= Quaternion.Euler(rotacionItemEspecifica);

            if (tipo == TipoItem.FrascoVacio)
            {
                FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
                if (scriptFrasco != null)
                {
                    if (itemData is DatosFrasco dFr) { scriptFrasco.materialVacio = dFr.materialVacio; scriptFrasco.materialLleno = dFr.materialLleno; }
                    scriptFrasco.EstablecerApariencia(false);
                }
                else
                {
                    MeshRenderer rend = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
                    if (rend != null && matVacio != null) { rend.material = matVacio; }
                }
            }
        }
        ReproducirSonidoJugador(sonidoRecogerItem);
    }

    void LlenarFrascoSostenido(DatosIngrediente[] contenidoArray)
    {
        if (tipoItemSostenido != TipoItem.FrascoVacio || !(itemSostenido is DatosFrasco frascoData)) { Debug.LogError("Error llenar frasco: No se sostiene frasco vacío."); return; }
        if (contenidoArray == null || contenidoArray.Length == 0) { Debug.LogError("Error llenar frasco: Sin ingredientes."); return; }

        List<DatosIngrediente> contenidoLista = new List<DatosIngrediente>(contenidoArray);
        tipoItemSostenido = TipoItem.FrascoLleno;
        contenidoFrascoLleno = contenidoLista;

        string nombrePocion = "Poción Desconocida";
        Material materialAplicar = materialPocionDesconocida;
        PedidoPocionData recetaEncontrada = null;

        if (catalogoRecetas != null)
        {
            recetaEncontrada = catalogoRecetas.BuscarRecetaPorIngredientes(contenidoLista);
            if (recetaEncontrada != null)
            {
                nombrePocion = recetaEncontrada.nombreResultadoPocion;
                if (recetaEncontrada.materialResultado != null)
                    materialAplicar = recetaEncontrada.materialResultado;
            }
        }

        if (panelItemSostenido != null)
        {
            if (uiIconoItemSostenido != null && frascoData.icono != null) { uiIconoItemSostenido.sprite = frascoData.icono; uiIconoItemSostenido.enabled = true; }
            if (uiNombreItemSostenido != null) uiNombreItemSostenido.text = nombrePocion;
            panelItemSostenido.SetActive(true);
        }

        if (instanciaItem3D != null)
        {
            MeshRenderer rendererFrasco = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
            if (rendererFrasco != null && materialAplicar != null)
                rendererFrasco.material = Instantiate(materialAplicar);

            FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
            if (scriptFrasco != null) scriptFrasco.Llenar(contenidoArray);
        }
        ReproducirSonidoJugador(sonidoRecogerPocion);
    }

    public void LimpiarItemSostenido()
    {
        itemSostenido = null;
        tipoItemSostenido = TipoItem.Nada;
        contenidoFrascoLleno = null;
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        LimpiarInstanciaItem3D();
    }

    void LimpiarInstanciaItem3D()
    {
        if (instanciaItem3D != null)
        {
            Destroy(instanciaItem3D);
            instanciaItem3D = null;
        }
    }

    public void MostrarNotificacion(string mensaje, float duracion = -1f, bool conSonidoError = false)
    {
        if (textoNotificacion != null)
        {
            textoNotificacion.text = mensaje;
            textoNotificacion.gameObject.SetActive(true);
            temporizadorNotificacion = (duracion > 0) ? duracion : tiempoNotificacion;
            if (conSonidoError) ReproducirSonidoJugador(sonidoError);
        }
    }

    void ManejarNotificaciones()
    {
        if (temporizadorNotificacion > 0)
        {
            temporizadorNotificacion -= Time.deltaTime;
            if (temporizadorNotificacion <= 0)
            {
                if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
            }
        }
    }

    void MostrarContenidoFrascoLleno()
    {
        if (contenidoFrascoLleno != null && contenidoFrascoLleno.Count > 0)
        {
            string t = "Contenido: ";
            for (int i = 0; i < contenidoFrascoLleno.Count; i++)
            {
                t += (contenidoFrascoLleno[i]?.nombreIngrediente ?? "???");
                t += (i < contenidoFrascoLleno.Count - 1 ? ", " : "");
            }
            MostrarNotificacion(t, 4f);
        }
        else
        {
            MostrarNotificacion("Frasco lleno pero sin contenido registrado.", 2f, true);
        }
    }

    void ReproducirSonidoJugador(AudioClip clip)
    {
        if (audioSourceJugador != null && clip != null)
            audioSourceJugador.PlayOneShot(clip);
    }
} // Fin de la clase