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

    public void EstablecerColor(Color color)
    {
        colorBase = color;
        AplicarColor(colorBase);
        CrearBordes();
    }

    public void Resaltar(bool activo)
    {
        AplicarColor(activo ? new Color(1f, 0.82f, 0.10f, 1f) : colorBase);
    }

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

        foreach (Vector3 posicion in posiciones)
        {
            GameObject borde = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borde.name = "Borde";
            borde.transform.SetParent(transform, false);
            borde.transform.localPosition = posicion;
            borde.transform.localRotation = Quaternion.identity;
            borde.transform.localScale = new Vector3(1.015f, 0.055f, 0.055f);

            Collider colliderBorde = borde.GetComponent<Collider>();
            if (colliderBorde != null) Destroy(colliderBorde);

            Renderer rendererBorde = borde.GetComponent<Renderer>();
            rendererBorde.sharedMaterial = renderizador.sharedMaterial;
            MaterialPropertyBlock propiedades = new MaterialPropertyBlock();
            Color oscuro = new Color(0.015f, 0.025f, 0.045f, 1f);
            propiedades.SetColor("_BaseColor", oscuro);
            propiedades.SetColor("_Color", oscuro);
            rendererBorde.SetPropertyBlock(propiedades);
            rendererBorde.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GameManager.Instance == null || GameManager.Instance.juegoTerminado) return;
        if (collision.gameObject.CompareTag("Suelo") || collision.gameObject.CompareTag("Mesa"))
            GameManager.Instance.JugadorPerdio();
    }
}
