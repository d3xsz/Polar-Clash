using UnityEngine;

// Engel gorselligi (carpismaya dokunmaz): neon hale (bulanik kopyalar), zemine dusen golge ve hafif nabiz.
// Engel rengi/alfasi (ArenaWall'un indirip kaldirmasi) her karede takip edilir.
public class WallFx : MonoBehaviour
{
    static Material unlit;
    static readonly Vector2[] Dirs =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    SpriteRenderer sr, shadow, core;
    SpriteRenderer[] halo;
    float phase;

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
        foreach (var n in new[] { "OlayDuvarlari", "HareketliDuvarlar" })
        {
            var p = GameObject.Find(n);
            if (p == null) continue;
            foreach (Transform t in p.transform)
                if (t.GetComponent<SpriteRenderer>() != null && t.GetComponent<WallFx>() == null) t.gameObject.AddComponent<WallFx>();
        }
    }

    // URP 2D ile uyumlu isiksiz sprite materyali (eski Sprites/Default, URP'de farkli dokulu
    // sprite'lari yanlis dokuyla cizebiliyordu). Resources'ta oldugu icin build'e de girer.
    public static Material UnlitSprite()
    {
        var m = Resources.Load<Material>("SpriteUnlit");
        return m != null ? m : new Material(Shader.Find("Sprites/Default"));
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (unlit == null) unlit = UnlitSprite();
        phase = Random.value * 6.28f;

        halo = new SpriteRenderer[Dirs.Length];
        for (int i = 0; i < Dirs.Length; i++)
        {
            var go = new GameObject("Hale");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)(Dirs[i] * 0.07f);
            halo[i] = go.AddComponent<SpriteRenderer>();
            halo[i].sprite = sr.sprite; halo[i].sharedMaterial = unlit;
            halo[i].sortingOrder = sr.sortingOrder - 1;
        }

        // Isiktan etkilenmeyen parlak cekirdek (neon cizgiler daha canli gorunsun)
        var cgo = new GameObject("Cekirdek");
        cgo.transform.SetParent(transform, false);
        core = cgo.AddComponent<SpriteRenderer>();
        core.sprite = sr.sprite; core.sharedMaterial = unlit;
        core.sortingOrder = sr.sortingOrder + 1;

        var sgo = new GameObject("Golge");
        sgo.transform.SetParent(transform, false);
        shadow = sgo.AddComponent<SpriteRenderer>();
        shadow.sprite = sr.sprite; shadow.sharedMaterial = unlit;
        shadow.sortingOrder = sr.sortingOrder - 2;
    }

    void LateUpdate()
    {
        if (sr == null) return;
        Color c = sr.color;
        float a = c.a;
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.4f + phase);

        for (int i = 0; i < halo.Length; i++)
            halo[i].color = new Color(c.r, c.g, c.b, a * 0.16f * pulse);

        core.color = new Color(Mathf.Lerp(c.r, 1f, 0.25f), Mathf.Lerp(c.g, 1f, 0.25f), Mathf.Lerp(c.b, 1f, 0.25f), a * 0.55f);

        // golge dunya uzayinda sag-asagi
        shadow.transform.position = transform.position + new Vector3(0.08f, -0.12f, 0f);
        shadow.color = new Color(0f, 0f, 0.03f, a * 0.5f);
    }
}
