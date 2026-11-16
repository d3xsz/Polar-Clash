using UnityEngine;

// Atmosfer: yavasca kayan yildizlar, nebulalarin hafif hareketi ve
// zaman zaman tribunleri tarayan spot isiklari (disko topunun yerine).
// Sahnede 'Dekor' nesnesi bulununca kendini otomatik ekler.
public class Atmosphere : MonoBehaviour
{
    class Star { public Transform tr; public SpriteRenderer sr; public float baseY, speed, phase, twSpd; public Color col; }

    const float HalfW = 17.5f;   // yildizlarin dolasim siniri (Uzay 34 birim genis)

    Star[] stars;
    readonly Transform[] neb = new Transform[2];
    readonly SpriteRenderer[] nebR = new SpriteRenderer[2];
    readonly Vector3[] nebPos = new Vector3[2];
    readonly Vector3[] nebScale = new Vector3[2];
    readonly Color[] nebCol = new Color[2];
    readonly SpriteRenderer[] spot = new SpriteRenderer[2];
    static readonly Color[] SpotCol = { new Color(1f, 0.93f, 0.75f), new Color(0.7f, 0.85f, 1f) };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;
        Attach();
    }

    static void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) { Attach(); }

    static void Attach()
    {
        var d = GameObject.Find("Dekor");
        if (d != null && d.GetComponent<Atmosphere>() == null) d.AddComponent<Atmosphere>();
    }

    void Awake()
    {
        // yildizlar
        var list = new System.Collections.Generic.List<Star>();
        foreach (Transform c in transform)
        {
            if (c.name != "Yildiz") continue;
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            list.Add(new Star
            {
                tr = c, sr = sr, baseY = c.position.y, col = sr.color,
                speed = Random.Range(0.05f, 0.25f), phase = Random.value * 6.28f, twSpd = Random.Range(0.6f, 2.2f)
            });
        }
        stars = list.ToArray();

        // nebulalar
        string[] names = { "Nebula_Sol", "Nebula_Sag" };
        for (int i = 0; i < 2; i++)
        {
            var t = transform.Find(names[i]);
            if (t == null) continue;
            neb[i] = t; nebR[i] = t.GetComponent<SpriteRenderer>();
            nebPos[i] = t.position; nebScale[i] = t.localScale;
            if (nebR[i] != null) nebCol[i] = nebR[i].color;
        }

        // spot isiklari (nebulanin yumusak 'Glow' sprite'ini kullanir)
        Sprite glow = nebR[0] != null ? nebR[0].sprite : (nebR[1] != null ? nebR[1].sprite : null);
        if (glow != null)
        {
            var unlit = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Spot_" + (i + 1));
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(5f, 5f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = glow; sr.sharedMaterial = unlit; sr.sortingOrder = 5;
                sr.color = new Color(SpotCol[i].r, SpotCol[i].g, SpotCol[i].b, 0f);
                spot[i] = sr;
            }
        }
    }

    // Tribun halkasi (dikdortgen cevre) uzerinde konum
    static Vector2 OnRect(float s)
    {
        const float W = 23.2f, H = 12.8f;
        float P = 2f * (W + H);
        s = ((s % P) + P) % P;
        if (s < W) return new Vector2(-W / 2f + s, -H / 2f);
        s -= W; if (s < H) return new Vector2(W / 2f, -H / 2f + s);
        s -= H; if (s < W) return new Vector2(W / 2f - s, H / 2f);
        s -= W; return new Vector2(-W / 2f, H / 2f - s);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        // yildizlar: yavas kayma + hafif titresim
        if (stars != null) foreach (var s in stars)
        {
            if (s.tr == null) continue;
            var p = s.tr.position;
            p.x -= s.speed * dt;
            if (p.x < -HalfW) p.x += 2f * HalfW;
            p.y = s.baseY + Mathf.Sin(t * 0.2f + s.phase) * 0.08f;
            s.tr.position = p;
            var c = s.col; c.a = s.col.a * (0.7f + 0.3f * Mathf.Sin(t * s.twSpd + s.phase));
            s.sr.color = c;
        }

        // nebulalar: yavas salinim, nefes alma
        for (int i = 0; i < 2; i++)
        {
            if (neb[i] == null) continue;
            float ph = i * 2.1f;
            neb[i].position = nebPos[i] + new Vector3(Mathf.Cos(t * 0.07f + ph) * 0.7f, Mathf.Sin(t * 0.05f + ph) * 0.5f, 0f);
            neb[i].localScale = nebScale[i] * (1f + 0.04f * Mathf.Sin(t * 0.11f + ph));
            if (nebR[i] != null)
            {
                var c = nebCol[i]; c.a = nebCol[i].a * (1f + 0.18f * Mathf.Sin(t * 0.13f + ph * 2f));
                nebR[i].color = c;
            }
        }

        // spotlar: 16 sn'lik dongunun ilk ~7 sn'sinde belirip tribunleri tarar
        for (int i = 0; i < 2; i++)
        {
            if (spot[i] == null) continue;
            float tt = t + i * 8f;
            float u = (tt % 16f) / 16f;
            float env = u < 0.45f ? Mathf.Sin(u / 0.45f * Mathf.PI) : 0f;
            float dir = i == 0 ? 1f : -1f;
            Vector2 pos = OnRect(dir * tt * 1.6f);
            spot[i].transform.position = new Vector3(pos.x, pos.y, 0f);
            var c = SpotCol[i]; c.a = 0.32f * env;
            spot[i].color = c;
        }
    }
}
