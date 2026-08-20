using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado AR")]
    public bool arSeguimientoEstable = false;

    [Header("Estado del Juego")]
    public int jugadorActual = 1;
    public bool juegoTerminado = false;
    private bool huboMovimiento;
    private int jugadorEnRiesgo = 1;
    private int colorDado = -1;
    private TextMeshProUGUI textoDado;
    private Button botonDado;
    // Arrays estáticos con los nombres y colores de los tres colores posibles.
    private static readonly string[] NombresColores = { "AZUL", "ROJO", "VERDE" };
    // Colores del dado en formato RGBA 

    // SONIDO
    [Header("Audio")]
    public AudioSource fuenteEfectos;
    public AudioSource fuenteMusica;

    public AudioClip sonidoCaida;
    public AudioClip sonidoDado;
    public AudioClip sonidoReinicio;
    public AudioClip musicaAmbiente;

    [Range(0f, 1f)]
    public float volumenMusica = 0.20f;

    [Range(0f, 1f)]
    public float volumenEfectos = 1f;

    private bool reiniciando;
    private static readonly Color[] ColoresDado =
    {
        new Color(0.10f, 0.40f, 0.95f, 1f),
        new Color(0.90f, 0.12f, 0.16f, 1f),
        new Color(0.10f, 0.68f, 0.28f, 1f)
    };

    [Header("Referencias")]
    public GeneradorTorreJenga generadorTorre;

    [Header("Interfaz de Usuario")]
    public TextMeshProUGUI textoTurno;
    public TextMeshProUGUI textoEstado;
    public GameObject panelJuegoTerminado;
    public TextMeshProUGUI textoGanadorPerdedor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PrepararFuentesAudio();
    }

    void Start()
    {
        juegoTerminado = false;
        jugadorActual = 1;
        if (panelJuegoTerminado != null) panelJuegoTerminado.SetActive(false);
        CrearInterfazDado();
        ActualizarInterfaz();
        ReproducirMusicaAmbiente();
    }
    private void ReproducirMusicaAmbiente()
    {
        if (fuenteMusica == null || musicaAmbiente == null)
            return;

        fuenteMusica.clip = musicaAmbiente;
        fuenteMusica.loop = true;
        fuenteMusica.volume = volumenMusica;
        fuenteMusica.Play();
    }

    // Crea y configura automáticamente las fuentes si no fueron asignadas
    // manualmente en el Inspector.
    private void PrepararFuentesAudio()
    {
        if (fuenteEfectos == null)
            fuenteEfectos = gameObject.AddComponent<AudioSource>();

        if (fuenteMusica == null)
            fuenteMusica = gameObject.AddComponent<AudioSource>();

        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.loop = false;
        fuenteEfectos.spatialBlend = 0f;

        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
    }

    // PlayOneShot permite reproducir un efecto sin asignarlo como música
    // principal ni interrumpir inmediatamente otros efectos cortos.
    private void ReproducirEfecto(AudioClip clip)
    {
        if (fuenteEfectos == null || clip == null) return;
        fuenteEfectos.PlayOneShot(clip, volumenEfectos);
    }
    //Se monitorea la torre para detectar caídas.
    private void Update()
    {
        if (!huboMovimiento || juegoTerminado || generadorTorre == null) return;

        int bloquesCaidos = 0;
        foreach (BloqueJenga bloque in generadorTorre.Bloques)
        {
            if (bloque != null && bloque.transform.localPosition.y < -0.05f)
                bloquesCaidos++;
        }

        if (bloquesCaidos >= 3) JugadorPerdio();
    }
    
    public void SetTrackingEstable(bool estado)
    {
        arSeguimientoEstable = estado;
        // Al perder el marcador congelamos la simulación. Al encontrarlo, la
        // física comienza recién cuando el jugador toma un bloque.
        if (!estado && generadorTorre != null) generadorTorre.EstablecerFisica(false);
        ActualizarInterfaz();
    }
    //Avanza al siguiente turno (1 - 2 - 3)
    public void SiguienteTurno()
    {
        if (juegoTerminado) return;

        jugadorActual = (jugadorActual % 3) + 1;
        ActualizarInterfaz();
    }

    public void MovimientoValido()
    {
        if (juegoTerminado) return;
        jugadorEnRiesgo = jugadorActual;
        huboMovimiento = true;

        // En AR no mantenemos la física activa continuamente porque los
        // reajustes del marcador pueden mover la torre. Después de cada jugada
        // comprobamos el soporte: un nivel vacío o un único bloque lateral
        // deja el centro de masa sin apoyo y activa la caída física.
        if (generadorTorre != null && generadorTorre.TorreInestable())
        {
            generadorTorre.EstablecerFisica(true);
            JugadorPerdio();
            return;
        }

        SiguienteTurno();
        ReiniciarDado();
    }

    //Valida si el bloque seleccionado cumple con el color que salió en el dado.
    public bool ValidarColor(BloqueJenga bloque, out string motivo)
    {
        if (colorDado < 0)
        {
            motivo = "Primero lanza el dado de color.";
            return false;
        }
        if (bloque.indiceColor != colorDado)
        {
            motivo = $"Debes retirar un bloque {NombresColores[colorDado]}.";
            return false;
        }
        motivo = "";
        return true;
    }


    //Lanza el dado: elige un color aleatorio y actualiza la UI.
    public void LanzarDado()
    {
        if (juegoTerminado || colorDado >= 0) return;
        ReproducirEfecto(sonidoDado);
        colorDado = Random.Range(0, NombresColores.Length);
        if (textoDado != null)
        {
            textoDado.text = $"Color: {NombresColores[colorDado]}";
            textoDado.color = ColoresDado[colorDado];
        }
        if (botonDado != null) botonDado.interactable = false;
        if (textoEstado != null) textoEstado.text = $"Retira un bloque {NombresColores[colorDado]}.";
    }

    //Reinicia el dado para el siguiente turno (limpia el color y reactiva el botón).

    private void ReiniciarDado()
    {
        colorDado = -1;
        if (textoDado != null) { textoDado.text = "Lanza el dado"; textoDado.color = Color.white; }
        if (botonDado != null) botonDado.interactable = true;
    }
    //Crea dinámicamente la interfaz del dado (panel, texto y botón) en el Canvas.
    // Se ejecuta en Start para asegurar que exista la UI.
    private void CrearInterfazDado()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("PanelDado", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rectPanel = panel.GetComponent<RectTransform>();
        rectPanel.anchorMin = rectPanel.anchorMax = new Vector2(1f, 1f);
        rectPanel.pivot = new Vector2(1f, 1f);
        rectPanel.anchoredPosition = new Vector2(-20f, -20f);
        rectPanel.sizeDelta = new Vector2(270f, 130f);
        panel.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.85f);

        GameObject resultado = new GameObject("TextoDado", typeof(RectTransform), typeof(TextMeshProUGUI));
        resultado.transform.SetParent(panel.transform, false);
        textoDado = resultado.GetComponent<TextMeshProUGUI>();
        textoDado.text = "Lanza el dado";
        textoDado.fontSize = 25f;
        textoDado.alignment = TextAlignmentOptions.Center;
        RectTransform rectTexto = resultado.GetComponent<RectTransform>();
        rectTexto.anchorMin = new Vector2(0f, 0.48f);
        rectTexto.anchorMax = new Vector2(1f, 1f);
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;

        GameObject objetoBoton = new GameObject("BotonLanzarDado", typeof(RectTransform), typeof(Image), typeof(Button));
        objetoBoton.transform.SetParent(panel.transform, false);
        RectTransform rectBoton = objetoBoton.GetComponent<RectTransform>();
        rectBoton.anchorMin = new Vector2(0.08f, 0.08f);
        rectBoton.anchorMax = new Vector2(0.92f, 0.48f);
        rectBoton.offsetMin = Vector2.zero;
        rectBoton.offsetMax = Vector2.zero;
        objetoBoton.GetComponent<Image>().color = new Color(0.12f, 0.55f, 0.72f, 1f);
        botonDado = objetoBoton.GetComponent<Button>();
        botonDado.onClick.AddListener(LanzarDado);

        GameObject etiqueta = new GameObject("Etiqueta", typeof(RectTransform), typeof(TextMeshProUGUI));
        etiqueta.transform.SetParent(objetoBoton.transform, false);
        TextMeshProUGUI textoBoton = etiqueta.GetComponent<TextMeshProUGUI>();
        textoBoton.text = "LANZAR DADO";
        textoBoton.fontSize = 22f;
        textoBoton.alignment = TextAlignmentOptions.Center;
        RectTransform rectEtiqueta = etiqueta.GetComponent<RectTransform>();
        rectEtiqueta.anchorMin = Vector2.zero;
        rectEtiqueta.anchorMax = Vector2.one;
        rectEtiqueta.offsetMin = rectEtiqueta.offsetMax = Vector2.zero;
    }

    //Notifica al jugador que su movimiento es inválido (por color incorrecto u otra razón).
    public void MovimientoInvalido(string motivo = "")
    {
        string mensaje = string.IsNullOrEmpty(motivo) ? "¡Movimiento inválido! Intenta con otro bloque." : motivo;
        if (textoEstado != null) textoEstado.text = mensaje;
        Debug.LogWarning(mensaje);
    }

    public void JugadorPerdio()
    {
        // Nunca puede existir una derrota antes del primer movimiento válido.
        if (!huboMovimiento || juegoTerminado) return;

        juegoTerminado = true;

        ReproducirEfecto(sonidoCaida);
        if (fuenteMusica != null) fuenteMusica.Stop();

        if (panelJuegoTerminado != null)
            panelJuegoTerminado.SetActive(true);

        int perdedor = huboMovimiento ? jugadorEnRiesgo : jugadorActual;
        if (textoGanadorPerdedor != null)
            textoGanadorPerdedor.text = $"¡La torre se derribó!\nEl Jugador {perdedor} pierde.";

        Debug.Log($"El Jugador {perdedor} ha perdido.");
    }

    public void TorreDerribada()
    {
        JugadorPerdio();
    }

    public void ReiniciarPartida()
    {
        if (!reiniciando) StartCoroutine(ReiniciarConSonido());
    }

    // Espera un momento antes de recargar la escena; de lo contrario, el
    // AudioSource se destruiría inmediatamente y el efecto quedaría cortado.
    private IEnumerator ReiniciarConSonido()
    {
        reiniciando = true;
        if (fuenteMusica != null) fuenteMusica.Stop();
        ReproducirEfecto(sonidoReinicio);

        float espera = sonidoReinicio != null
            ? Mathf.Min(sonidoReinicio.length, 1.5f)
            : 0f;
        yield return new WaitForSecondsRealtime(espera);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Conserva la conexión del botón creada previamente en la escena.
    public void ReiniciarJuego()
    {
        ReiniciarPartida();
    }

    private void ActualizarInterfaz()
    {
        if (textoTurno != null)
        {
            textoTurno.text = $"Turno: Jugador {jugadorActual}";
        }

        if (textoEstado != null)
        {
            textoEstado.text = arSeguimientoEstable ? "AR Estable" : "Buscando marcador AR...";
        }
    }
}
