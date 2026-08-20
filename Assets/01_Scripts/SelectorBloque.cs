using UnityEngine;
using UnityEngine.EventSystems;

public class SelectorBloque : MonoBehaviour
{
    [SerializeField] private float sensibilidadArrastre = 0.0004f;
    private Camera camaraAR;
    private BloqueJenga seleccionado;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Vector3 ultimoPuntero;

    private void Start() => camaraAR = Camera.main;

    private void Update()
    {
        GameManager juego = GameManager.Instance;
        if (juego == null || juego.juegoTerminado) return;
        if (!juego.arSeguimientoEstable) { CancelarMovimiento(); return; }
        if (PunteroPresionado()) Seleccionar(PosicionPuntero());
        else if (seleccionado != null && PunteroMantenido()) Arrastrar(PosicionPuntero());
        else if (seleccionado != null && PunteroLiberado()) ColocarEnCima();
    }

    private void Seleccionar(Vector3 puntero)
    {
        if (PunteroSobreInterfaz()) return;
        if (camaraAR == null) camaraAR = Camera.main;
        if (camaraAR == null || !Physics.Raycast(camaraAR.ScreenPointToRay(puntero), out RaycastHit hit)) return;
        BloqueJenga bloque = hit.collider.GetComponentInParent<BloqueJenga>();
        GeneradorTorreJenga torre = GameManager.Instance.generadorTorre;
        if (bloque == null || torre == null) return;
        if (!GameManager.Instance.ValidarColor(bloque, out string motivoColor))
        {
            GameManager.Instance.MovimientoInvalido(motivoColor);
            return;
        }
        if (bloque.colocadoEnLaCima || bloque.nivel >= torre.NivelSuperiorActual())
        {
            GameManager.Instance.MovimientoInvalido("No se puede retirar un bloque del nivel superior.");
            return;
        }
        seleccionado = bloque;
        seleccionado.Resaltar(true);
        posicionInicial = bloque.transform.localPosition;
        rotacionInicial = bloque.transform.localRotation;
        ultimoPuntero = puntero;
        // Durante la extracción la torre se mantiene estable. La física se
        // reactiva únicamente después de colocar la pieza en la cima.
        torre.EstablecerFisica(false);
        Rigidbody cuerpo = bloque.GetComponent<Rigidbody>();
        if (cuerpo != null) cuerpo.isKinematic = true;
    }

    private void Arrastrar(Vector3 puntero)
    {
        Vector3 delta = puntero - ultimoPuntero;
        Transform referencia = camaraAR.transform;
        seleccionado.transform.position += (referencia.right * delta.x + referencia.up * delta.y) * sensibilidadArrastre;
        ultimoPuntero = puntero;
    }

    private void ColocarEnCima()
    {
        GeneradorTorreJenga torre = GameManager.Instance.generadorTorre;
        if (torre == null) { CancelarMovimiento(); return; }
        seleccionado.transform.localPosition = torre.PosicionEnCima(seleccionado);
        seleccionado.colocadoEnLaCima = true;
        seleccionado.Resaltar(false);
        Rigidbody cuerpo = seleccionado.GetComponent<Rigidbody>();
        if (cuerpo != null) cuerpo.isKinematic = true;
        seleccionado = null;
        GameManager.Instance.MovimientoValido();
    }

    private void CancelarMovimiento()
    {
        if (seleccionado == null) return;
        seleccionado.transform.localPosition = posicionInicial;
        seleccionado.transform.localRotation = rotacionInicial;
        seleccionado.Resaltar(false);
        Rigidbody cuerpo = seleccionado.GetComponent<Rigidbody>();
        if (cuerpo != null) cuerpo.isKinematic = true;
        seleccionado = null;
    }

    private static Vector3 PosicionPuntero() => Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
    private static bool PunteroSobreInterfaz()
    {
        if (EventSystem.current == null) return false;
        return Input.touchCount > 0
            ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
    private static bool PunteroPresionado() => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);
    private static bool PunteroMantenido() => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary : Input.GetMouseButton(0);
    private static bool PunteroLiberado() => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled : Input.GetMouseButtonUp(0);
}
