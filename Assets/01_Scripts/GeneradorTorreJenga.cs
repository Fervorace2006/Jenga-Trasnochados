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

    //Construye la torre desde cero. Previene duplicados si existen en escena
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

    //Se aplica el color al prefab de la base de la mesa usando MaterialPropertyBlock para evitar crear instancias de materiales.
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

    // Devuelve el nivel más alto de la torre que aún tiene bloques sin colocar en la cima.
    public int NivelSuperiorActual()
    {
        int superior = -1;
        foreach (BloqueJenga bloque in bloques)
            if (bloque != null && !bloque.colocadoEnLaCima) superior = Mathf.Max(superior, bloque.nivel);
        return superior;
    }

    // Indica si el bloque pertenece al piso físicamente más alto que existe
    // en este momento. Se usa la altura real y no el nivel original, porque
    // una pieza colocada en la cima deja de ser el último piso cuando después
    // se construye otro nivel encima.
    public bool EstaEnPisoSuperior(BloqueJenga bloqueConsultado)
    {
        if (bloqueConsultado == null) return false;

        float alturaSuperior = float.NegativeInfinity;
        foreach (BloqueJenga bloque in bloques)
        {
            if (bloque == null) continue;
            alturaSuperior = Mathf.Max(alturaSuperior, bloque.transform.localPosition.y);
        }

        // La tolerancia agrupa como un mismo piso los bloques cuyas alturas
        // puedan diferir unos milímetros por redondeo o por la física.
        float tolerancia = altoBloque * 0.35f;
        return bloqueConsultado.transform.localPosition.y >= alturaSuperior - tolerancia;
    }

    // Comprueba si el peso de la torre perdió su apoyo. Un nivel es inestable
    // cuando queda vacío o cuando solo conserva un bloque situado a un lado.
    // Un único bloque central todavía puede sostener el centro de la torre.
    public bool TorreInestable()
    {
        int nivelSuperior = NivelSuperiorActual();
        for (int nivel = 0; nivel < nivelSuperior; nivel++)
        {
            int bloquesEnNivel = 0;
            BloqueJenga unicoBloque = null;

            foreach (BloqueJenga bloque in bloques)
            {
                if (bloque != null && !bloque.colocadoEnLaCima && bloque.nivel == nivel)
                {
                    bloquesEnNivel++;
                    unicoBloque = bloque;
                }
            }

            // No existe ninguna superficie que sostenga los niveles superiores.
            if (bloquesEnNivel == 0) return true;

            if (bloquesEnNivel == 1 && unicoBloque != null)
            {
                // Los niveles pares están distribuidos sobre el eje Z y los
                // impares sobre X. El bloque central tiene desplazamiento 0;
                // los laterales están aproximadamente a +/- anchoBloque.
                float desplazamiento = nivel % 2 == 0
                    ? unicoBloque.transform.localPosition.z
                    : unicoBloque.transform.localPosition.x;

                // Si el único apoyo está fuera del centro, el centro de masa
                // de todo lo que queda arriba ya no descansa sobre ese bloque.
                if (Mathf.Abs(desplazamiento) > anchoBloque * 0.5f)
                    return true;
            }
        }
        return false;
    }

    // Se conserva este nombre por compatibilidad con posibles botones,
    // eventos o scripts antiguos que todavía lo utilicen.
    public bool TorreSinSoporte() => TorreInestable();

    // Activa o desactiva la física de todos los bloques de la torre. Si se desactiva, también se detienen sus velocidades.
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

    // Busca una ranura realmente libre en el piso superior. Esto evita que dos
    // bloques terminen superpuestos cuando se vuelve a mover una pieza que ya
    // había sido colocada en la cima durante un turno anterior.
    public Vector3 PosicionEnCima(BloqueJenga bloque)
    {
        float mitadAlto = altoBloque * 0.5f;
        float alturaSuperior = float.NegativeInfinity;

        // Primero localizamos el piso más alto, ignorando la pieza que se está
        // moviendo porque dejará de ocupar su posición anterior.
        foreach (BloqueJenga otro in bloques)
        {
            if (otro == null || otro == bloque) continue;
            alturaSuperior = Mathf.Max(alturaSuperior, otro.transform.localPosition.y);
        }

        int pisoSuperior = alturaSuperior > float.NegativeInfinity
            ? Mathf.RoundToInt((alturaSuperior - mitadAlto) / altoBloque)
            : -1;
        float toleranciaAltura = altoBloque * 0.35f;
        int bloquesEnPisoSuperior = 0;

        foreach (BloqueJenga otro in bloques)
        {
            if (otro == null || otro == bloque) continue;
            if (Mathf.Abs(otro.transform.localPosition.y - alturaSuperior) <= toleranciaAltura)
                bloquesEnPisoSuperior++;
        }

        // Si arriba ya hay tres piezas, comenzamos un piso nuevo. Si hay una
        // o dos, completamos primero las ranuras libres de ese mismo piso.
        int pisoDestino = bloquesEnPisoSuperior >= 3 ? pisoSuperior + 1 : pisoSuperior;
        bool orientarEnX = pisoDestino % 2 == 0;
        bool[] ranurasOcupadas = new bool[3];

        if (pisoDestino == pisoSuperior)
        {
            foreach (BloqueJenga otro in bloques)
            {
                if (otro == null || otro == bloque) continue;
                if (Mathf.Abs(otro.transform.localPosition.y - alturaSuperior) > toleranciaAltura) continue;

                float desplazamiento = orientarEnX
                    ? otro.transform.localPosition.z
                    : otro.transform.localPosition.x;
                int indiceRanura = Mathf.RoundToInt(desplazamiento / anchoBloque) + 1;
                if (indiceRanura >= 0 && indiceRanura < ranurasOcupadas.Length)
                    ranurasOcupadas[indiceRanura] = true;
            }
        }

        int ranuraLibre = 0;
        while (ranuraLibre < ranurasOcupadas.Length && ranurasOcupadas[ranuraLibre])
            ranuraLibre++;

        // Protección adicional ante posiciones inesperadas: si las tres
        // ranuras aparecen ocupadas, iniciamos el siguiente piso.
        if (ranuraLibre >= ranurasOcupadas.Length)
        {
            pisoDestino++;
            orientarEnX = pisoDestino % 2 == 0;
            ranuraLibre = 0;
        }

        int ranura = ranuraLibre - 1;
        float posicionY = pisoDestino * altoBloque + mitadAlto;
        bloque.transform.localRotation = orientarEnX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        bloque.EstablecerColor(ColoresMadera[bloque.indiceColor]);
        return orientarEnX
            ? new Vector3(0f, posicionY, ranura * anchoBloque)
            : new Vector3(ranura * anchoBloque, posicionY, 0f);
    }

    // Limpia la lista de bloques y destruye los objetos de bloque existentes en la escena.
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
