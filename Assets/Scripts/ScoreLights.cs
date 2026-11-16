using UnityEngine;

// Puan aninda: sahanin o yarisi takim renginde parlar, neon cerceve nabiz atar,
// IsikMavi / IsikSicak isiklari puan alan tarafa kayip parlar.
// P1 (kuzey, sol, kirmizi)  -> IsikSicak
// P2 (guney, sag, mavi)     -> IsikMavi
public class ScoreLights : MonoBehaviour
{
    static ScoreLights inst;
    const float Dur = 0.9f;
    static readonly Color[] TeamCol = { Color.white, new Color(1f, 0.25f, 0.25f), new Color(0.3f, 0.6f, 1f) };

    readonly float[] age = { 99f, 99f, 99f };
    readonly SpriteRenderer[] half = new SpriteRenderer[3];
    readonly SpriteRenderer[] glow = new SpriteRenderer[3];
    readonly Color[] glowBase = new Color[3];
    readonly Vector3[] glowPos = new Vector3[3];
    readonly System.Collections.Generic.List<SpriteRenderer>[] neon =
        { null, new System.Collections.Generic.List<SpriteRenderer>(), new System.Collections.Generic.List<SpriteRenderer>() };
    readonly System.Collections.Generic.List<Vector3>[] neonScale =
        { null, new System.Collections.Generic.List<Vector3>(), new System.Collections.Generic.List<Vector3>() };

    public static void Pulse(int playerId)
    {
        if (playerId < 1 || playerId > 2) return;
        if (inst == null) inst = new GameObject("ScoreLights").AddComponent<ScoreLights>();
        inst.age[playerId] = 0f;
    }

    void Awake()
    {
        inst = this;
        var unlit = new Material(Shader.Find("Sprites/Default"));

        // neon cizgiler: adi Sol ile bitenler P1, Sag ile bitenler P2
        Sprite square = null;
        var nc = GameObject.Find("NeonCerceve");
        if (nc != null)
        {
            foreach (Transform c in nc.transform)
            {
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                if (square == null) square = sr.sprite;
                int id = c.name.EndsWith("Sol") ? 1 : (c.name.EndsWith("Sag") ? 2 : 0);
                if (id == 0) continue;
                sr.sharedMaterial = unlit;
                neon[id].Add(sr);
                neonScale[id].Add(c.localScale);
            }
        }

        // yari saha parlamasi
        var saha = GameObject.Find("Saha");
        for (int id = 1; id <= 2; id++)
        {
            var go = new GameObject("YarimIsik_P" + id);
            if (saha != null) go.transform.SetParent(saha.transform, true);
            go.transform.position = new Vector3(id == 1 ? -5f : 5f, 0f, 0f);
            go.transform.localScale = new Vector3(10f, 10f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.sharedMaterial = unlit;
            sr.sortingOrder = -6;
            sr.color = new Color(1f, 1f, 1f, 0f);
            half[id] = sr;
        }

        // mevcut isik sprite'lari
        var warm = GameObject.Find("IsikSicak"); var blue = GameObject.Find("IsikMavi");
        if (warm != null) glow[1] = warm.GetComponent<SpriteRenderer>();
        if (blue != null) glow[2] = blue.GetComponent<SpriteRenderer>();
        for (int id = 1; id <= 2; id++)
            if (glow[id] != null) { glowBase[id] = glow[id].color; glowPos[id] = glow[id].transform.position; }
    }

    static float Env(float a)
    {
        const float atk = 0.08f;
        if (a < atk) return a / atk;
        return Mathf.Pow(Mathf.Clamp01(1f - (a - atk) / (Dur - atk)), 1.6f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int id = 1; id <= 2; id++)
        {
            bool on = age[id] < Dur;
            if (on) age[id] += dt;
            float env = on ? Env(age[id]) : 0f;
            float beat = 0.65f + 0.35f * Mathf.Cos(age[id] * 16f);
            Color tc = TeamCol[id];

            if (half[id] != null)
                half[id].color = new Color(tc.r, tc.g, tc.b, 0.28f * env);

            if (glow[id] != null)
            {
                var b = glowBase[id];
                glow[id].color = new Color(Mathf.Lerp(b.r, tc.r, env), Mathf.Lerp(b.g, tc.g, env), Mathf.Lerp(b.b, tc.b, env), b.a + 0.4f * env);
                var p = glowPos[id];
                glow[id].transform.position = new Vector3(Mathf.Lerp(p.x, id == 1 ? -5f : 5f, env), p.y, p.z);
            }

            for (int i = 0; i < neon[id].Count; i++)
            {
                var sr = neon[id][i];
                if (sr == null) continue;
                bool vis = env > 0.01f;
                if (sr.gameObject.activeSelf != vis) sr.gameObject.SetActive(vis);
                if (!vis) continue;
                sr.color = new Color(tc.r, tc.g, tc.b, Mathf.Clamp01(env * beat * 1.3f));
                Vector3 s = neonScale[id][i];
                float k = 1f + 2.5f * env * beat;
                if (s.x < s.y) s.x *= k; else s.y *= k;   // ince ekseni kalinlastir
                sr.transform.localScale = s;
            }
        }
    }
}
