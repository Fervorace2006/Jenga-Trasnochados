using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class BloqueJenga : MonoBehaviour
{
    public int nivel;
    public bool colocadoEnLaCima;
    public int indiceColor;

    private Renderer renderizador;
    private Color colorBase = Color.white;
    private bool bordesCreados;
    //Se guarda el color y se lo aplicará al bloque, además de crear los bordes del mismo.
    public void EstablecerColor(Color color)
    {
        colorBase = color;
        AplicarColor(colorBase);
        CrearBordes();
    }
    //Se resalta el bloque con un color amarillo si está activo, o se restaura su color base si no lo está.
    public void Resaltar(bool activo)
    {
        AplicarColor(activo ? new Color(1f, 0.82f, 0.10f, 1f) : colorBase);
    }

    //Se aplica el color al bloque usando MaterialPropertyBlock para evitar crear instancias de materiales.
    private void AplicarColor(Color color)
    {
        if (renderizador == null) renderizador = GetComponentInChildren<Renderer>();
        if (renderizador == null) return;

        MaterialPropertyBlock propiedades = new MaterialPropertyBlock();
        renderizador.GetPropertyBlock(propiedades);
        propiedades.SetColor("_BaseColor", color);
        propiedades.SetColor("_Color", color);
        renderizador.SetPropertyBlock(propiedades);
    }
    //Se crean los bordes del bloque como cubos pequeños y se les aplica un color oscuro.
    private void CrearBordes()
    {
        if (bordesCreados || renderizador == null) return;
        bordesCreados = true;

        Vector3[] posiciones =
        {
            new Vector3(0f, 0.485f, 0.485f),
            new Vector3(0f, 0.485f, -0.485f),
            new Vector3(0f, -0.485f, 0.485f),
            new Vector3(0f, -0.485f, -0.485f)
        };
        //

        foreach (Vector3 posicion in posiciones)
        {
            GameObject borde = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borde.name = "Borde";
            borde.transform.SetParent(transform, false);
            borde.transform.localPosition = posicion;
            borde.transform.localRotation = Quaternion.identity;
            borde.transform.localScale = new Vector3(1.015f, 0.055f, 0.055f);

            //Se elimina el collider del borde para que no interfiera con la física del bloque.
            Collider colliderBorde = borde.GetComponent<Collider>();
            if (colliderBorde != null) Destroy(colliderBorde);

            //Se aplica el mismo material del bloque al borde y se le asigna un color oscuro.
            Renderer rendererBorde = borde.GetComponent<Renderer>();
            rendererBorde.sharedMaterial = renderizador.sharedMaterial;
            MaterialPropertyBlock propiedades = new MaterialPropertyBlock();
            Color oscuro = new Color(0.015f, 0.025f, 0.045f, 1f);
            propiedades.SetColor("_BaseColor", oscuro);
            propiedades.SetColor("_Color", oscuro);
            rendererBorde.SetPropertyBlock(propiedades);
            //Se desactiva la proyección de sombras en los bordes para mejorar el rendimiento
            
            rendererBorde.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    //Se ejecuta automáticamente cuando este bloque colisiona con otro objeto.

    private void OnCollisionEnter(Collision collision)
    {
        if (GameManager.Instance == null || GameManager.Instance.juegoTerminado) return;
        //Si el bloque colisiona con el suelo o la mesa, se considera que el jugador ha perdido.
        if (collision.gameObject.CompareTag("Suelo") || collision.gameObject.CompareTag("Mesa"))
            GameManager.Instance.JugadorPerdio();
    }
}
