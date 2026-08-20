using System.Collections.Generic;
using UnityEngine;

public class GeneradorTorreJenga : MonoBehaviour
{
    private static readonly Color[] ColoresMadera =
    {
        new Color(0.10f, 0.40f, 0.95f, 1f),
        new Color(0.90f, 0.12f, 0.16f, 1f),
        new Color(0.10f, 0.68f, 0.28f, 1f)
    };

    [Header("Configuración de la Torre")]
    public GameObject bloquePrefab;
    [Min(3)] public int numeroNiveles = 10;
    [Header("Dimensiones del Bloque")]
    public float largoBloque = 0.075f;
    public float altoBloque = 0.015f;
    public float anchoBloque = 0.025f;

    private readonly List<BloqueJenga> bloques = new List<BloqueJenga>();
    public IReadOnlyList<BloqueJenga> Bloques => bloques;

    private void Start() => GenerarTorre();

    [ContextMenu("Generar Torre")]
    public void GenerarTorre()
    {
        if (bloquePrefab == null) { Debug.LogError("Asigna bloquePrefab en el Inspector."); return; }

        // Start ya crea la torre. Vuforia también invoca este método al detectar
        // la imagen; evitamos superponer una segunda torre durante la partida.
        if (Application.isPlaying && bloques.Count > 0) return;

        LimpiarTorre();
        AplicarColorBase();
        float mitadAlto = altoBloque * 0.5f;

        for (int nivel = 0; nivel < numeroNiveles; nivel++)
        {
            bool nivelPar = nivel % 2 == 0;
            for (int indice = -1; indice <= 1; indice++)
            {
                GameObject objeto = Instantiate(bloquePrefab, transform);
                objeto.transform.localPosition = nivelPar
                    ? new Vector3(0f, nivel * altoBloque + mitadAlto, indice * anchoBloque)
                    : new Vector3(indice * anchoBloque, nivel * altoBloque + mitadAlto, 0f);
                objeto.transform.localRotation = nivelPar ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                objeto.transform.localScale = new Vector3(largoBloque, altoBloque, anchoBloque);
                objeto.name = $"Bloque_Nivel_{nivel + 1}_{indice + 2}";

                BloqueJenga bloque = objeto.GetComponent<BloqueJenga>();
                if (bloque == null) bloque = objeto.AddComponent<BloqueJenga>();
                bloque.nivel = nivel;
                bloque.colocadoEnLaCima = false;
                bloque.indiceColor = nivel % ColoresMadera.Length;
                bloque.EstablecerColor(ColoresMadera[bloque.indiceColor]);
                bloques.Add(bloque);
                Rigidbody cuerpo = objeto.GetComponent<Rigidbody>();
                if (cuerpo != null) cuerpo.isKinematic = true;
            }
        }
    }

    private void AplicarColorBase()
    {
        Transform baseMesa = transform.Find("BaseMesa");
        if (baseMesa == null) return;
        Renderer renderizador = baseMesa.GetComponent<Renderer>();
        if (renderizador == null) return;

        Color color = new Color(0.06f, 0.16f, 0.22f, 1f);
        MaterialPropertyBlock propiedades = new MaterialPropertyBlock();
        renderizador.GetPropertyBlock(propiedades);
        propiedades.SetColor("_BaseColor", color);
        propiedades.SetColor("_Color", color);
        renderizador.SetPropertyBlock(propiedades);
    }

    public int NivelSuperiorActual()
    {
        int superior = -1;
        foreach (BloqueJenga bloque in bloques)
            if (bloque != null && !bloque.colocadoEnLaCima) superior = Mathf.Max(superior, bloque.nivel);
        return superior;
    }

    public bool TorreSinSoporte()
    {
        int nivelSuperior = NivelSuperiorActual();
        for (int nivel = 0; nivel < nivelSuperior; nivel++)
        {
            int bloquesEnNivel = 0;
            foreach (BloqueJenga bloque in bloques)
                if (bloque != null && !bloque.colocadoEnLaCima && bloque.nivel == nivel)
                    bloquesEnNivel++;

            if (bloquesEnNivel == 0) return true;
        }
        return false;
    }

    public void EstablecerFisica(bool activa)
    {
        foreach (BloqueJenga bloque in bloques)
        {
            if (bloque == null) continue;
            Rigidbody cuerpo = bloque.GetComponent<Rigidbody>();
            if (cuerpo == null) continue;
            cuerpo.isKinematic = !activa;
            if (!activa)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
        }
    }

    public Vector3 PosicionEnCima(BloqueJenga bloque)
    {
        int colocados = 0;
        foreach (BloqueJenga otro in bloques)
            if (otro != null && otro != bloque && otro.colocadoEnLaCima) colocados++;

        int ranura = colocados % 3 - 1;
        int capaNueva = colocados / 3;
        float posicionY = numeroNiveles * altoBloque + capaNueva * altoBloque + altoBloque * 0.5f;
        bool orientarEnX = (numeroNiveles + capaNueva) % 2 == 0;
        bloque.transform.localRotation = orientarEnX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        bloque.EstablecerColor(ColoresMadera[bloque.indiceColor]);
        return orientarEnX
            ? new Vector3(0f, posicionY, ranura * anchoBloque)
            : new Vector3(ranura * anchoBloque, posicionY, 0f);
    }

    public void LimpiarTorre()
    {
        bloques.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (!hijo.name.StartsWith("Bloque_Nivel_")) continue;
            if (Application.isPlaying) Destroy(hijo.gameObject); else DestroyImmediate(hijo.gameObject);
        }
    }
}
