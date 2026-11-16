using UnityEngine;

// Seyirciler: sol taraf KUZEY (kirmizi, P1), sag taraf GUNEY (mavi, P2).
// Kendi takimi puan alinca o takimin seyircileri ziplar, kollarini kaldirip sevinir.
public class CrowdBob : MonoBehaviour
{
    static CrowdBob inst;
    const int Variants = 6;
    const int N = 128;

    readonly float[] sulk = new float[3];     // rakip puan alinca basi one egme suresi
    readonly float[] bowAmt = new float[3];   // 0..1 yumusatilmis egilme miktari
    int winner;                               // 0 = yok/beraberlik, 1 = kuzey, 2 = guney
    readonly float[] cheer = new float[3];   // 1 = kuzey (kirmizi), 2 = guney (mavi)
    Transform[] kids;
    SpriteRenderer[] rends;
    Vector3[] basePos, up, baseScale;
    Quaternion[] baseRot;
    float[] phase;
    int[] team, variant;
    int[] pose;
    static Sprite[,,] sprites;   // [takim-1, varyant, poz]  poz: 0 normal, 1 sevinc, 2 uzgun

    // Rakip puan alinca o takimin seyircileri basini one egip bir sure surer
    public static void Sulk(int playerId)
    {
        if (inst == null || playerId < 1 || playerId > 2) return;
        inst.sulk[playerId] = 1.6f;
    }

    // Mac sonu: kazanan taraf ayaga kalkip zipliyor, kaybeden taraf basini egiyor. winner: 1 kuzey, 2 guney, 0 beraberlik
    public static void Finale(int winnerId)
    {
        if (inst == null) return;
        inst.winner = winnerId;
        if (winnerId == 0) { inst.cheer[1] = 1e5f; inst.cheer[2] = 1e5f; return; }
        int loser = winnerId == 1 ? 2 : 1;
        inst.cheer[winnerId] = 1e5f; inst.cheer[loser] = 0f;
        inst.sulk[loser] = 1e5f; inst.sulk[winnerId] = 0f;
    }

    public static void Cheer() { Cheer(1); Cheer(2); }

    public static void Cheer(int playerId)
    {
        if (inst == null || playerId < 1 || playerId > 2) return;
        inst.cheer[playerId] = 1.6f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneReloaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneReloaded;
        Attach();
    }

    static void OnSceneReloaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        Attach();
    }

    static void Attach()
    {
        var p = GameObject.Find("Seyirciler");
        if (p != null && p.GetComponent<CrowdBob>() == null) p.AddComponent<CrowdBob>();
    }

    void Awake()
    {
        inst = this;
        if (sprites == null || sprites[0, 0, 0] == null) sprites = BuildSprites();
        int n = transform.childCount;
        kids = new Transform[n]; rends = new SpriteRenderer[n];
        basePos = new Vector3[n]; up = new Vector3[n]; baseScale = new Vector3[n];
        baseRot = new Quaternion[n]; phase = new float[n];
        team = new int[n]; variant = new int[n]; pose = new int[n];
        for (int i = 0; i < n; i++)
        {
            kids[i] = transform.GetChild(i);
            rends[i] = kids[i].GetComponent<SpriteRenderer>();
            basePos[i] = kids[i].position;
            up[i] = kids[i].up;
            baseRot[i] = kids[i].rotation;
            baseScale[i] = kids[i].localScale;
            team[i] = basePos[i].x < 0f ? 1 : 2;
            variant[i] = Random.Range(0, Variants);
            phase[i] = (basePos[i].x + basePos[i].y) * 0.45f + Random.value * 1.2f;   // hafif dalga etkisi
            if (rends[i] != null) { rends[i].color = Color.white; rends[i].sprite = sprites[team[i] - 1, variant[i], 0]; }
        }
    }

    void Update()
    {
        // Mac bitince Time.timeScale = 0 oldugu icin unscaled zaman kullaniliyor
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        for (int g = 1; g <= 2; g++)
        {
            cheer[g] = Mathf.Max(0f, cheer[g] - dt);
            sulk[g] = Mathf.Max(0f, sulk[g] - dt);
            float target = (sulk[g] > 0f && cheer[g] <= 0.05f) ? 1f : 0f;
            bowAmt[g] = Mathf.MoveTowards(bowAmt[g], target, dt * (target > bowAmt[g] ? 5f : 2.5f));
        }
        for (int i = 0; i < kids.Length; i++)
        {
            int tm = team[i];
            float c = cheer[tm];
            float k = Mathf.Clamp01(c);
            bool happy = c > 0.05f;
            bool stand = happy && winner == tm;
            float bow = happy ? 0f : bowAmt[tm];
            float amp = 0.035f + 0.22f * k;
            float spd = 2.2f + 6f * k;
            if (stand) { amp += 0.12f; spd += 1.5f; }
            amp *= 1f - 0.85f * bow;
            float s = Mathf.Abs(Mathf.Sin(t * spd + phase[i]));
            kids[i].position = basePos[i] + up[i] * s * amp - up[i] * (0.07f * bow);
            float wob = happy ? Mathf.Sin(t * 14f + phase[i]) * 10f * k : Mathf.Sin(t * 1.3f + phase[i]) * 1.5f;
            wob += bow * Mathf.Sin(t * 2.5f + phase[i]) * 3f;
            kids[i].rotation = baseRot[i] * Quaternion.Euler(0f, 0f, wob);
            Vector3 sc = baseScale[i] * (1f + (happy ? 0.10f * k * s : 0f) + (stand ? 0.12f : 0f));
            sc.y *= 1f - 0.12f * bow;
            kids[i].localScale = sc;
            int ps = happy ? 1 : (bow > 0.4f ? 2 : 0);
            if (pose[i] != ps && rends[i] != null)
            {
                pose[i] = ps;
                rends[i].sprite = sprites[tm - 1, variant[i], ps];
            }
        }
    }

    // ---------- Prosedurel seyirci cizimi ----------
    static Sprite[,,] BuildSprites()
    {
        var red = new Color(1.00f, 0.42f, 0.45f);
        var blue = new Color(0.38f, 0.64f, 1.00f);
        var s = new Sprite[2, Variants, 3];
        for (int t = 0; t < 2; t++)
            for (int v = 0; v < Variants; v++)
                for (int p = 0; p < 3; p++)
                    s[t, v, p] = Paint(t == 0 ? red : blue, v, p);
        return s;
    }

    static Sprite Paint(Color team, int v, int pose)
    {
        bool armsUp = pose == 1, sad = pose == 2;
        var px = new Color[N * N];
        Color ink = Color.Lerp(team, Color.black, 0.55f);
        Color light = Color.Lerp(team, Color.white, 0.6f);
        Color visor = Color.Lerp(team, Color.black, 0.3f);
        Color[] skins = { new Color(0.96f, 0.78f, 0.62f), new Color(0.80f, 0.58f, 0.42f), new Color(0.55f, 0.37f, 0.27f) };
        Color[] hairs = { new Color(0.15f, 0.10f, 0.08f), new Color(0.42f, 0.26f, 0.14f), new Color(0.92f, 0.76f, 0.34f) };
        Color skin = skins[v % 3];
        Color hair = hairs[(v / 2) % 3];

        // kollar
        if (armsUp)
        {
            Arm(px, 26f, 46f, 14f, 100f, team, ink, skin);
            Arm(px, 102f, 46f, 114f, 100f, team, ink, skin);
        }
        else
        {
            Arm(px, 22f, 42f, 18f, 20f, team, ink, skin);
            Arm(px, 106f, 42f, 110f, 20f, team, ink, skin);
        }

        // govde + gogus seridi
        Ellipse(px, 64f, 40f, 47f, 28f, ink);
        Ellipse(px, 64f, 40f, 44f, 25f, team);
        Ellipse(px, 64f, 28f, 30f, 6f, light);

        // kulaklar + kafa
        Ellipse(px, 41f, 70f, 8f, 9f, ink);
        Ellipse(px, 87f, 70f, 8f, 9f, ink);
        Ellipse(px, 41f, 70f, 5f, 6f, skin);
        Ellipse(px, 87f, 70f, 5f, 6f, skin);
        Ellipse(px, 64f, 70f, 28f, 28f, ink);
        Ellipse(px, 64f, 70f, 25f, 25f, skin);

        if (v == 3 || v == 4)
        {
            // takim sapkasi: tepe + one bakan siper
            Ellipse(px, 64f, 92f, 30f, 20f, team, 64f, 70f, 25f);
            Ellipse(px, 64f, 92f, 4f, 4f, light);
            Capsule(px, 44f, 76f, 84f, 76f, 5f, ink);
            Capsule(px, 45f, 76f, 83f, 76f, 3f, visor);
        }
        else
        {
            Ellipse(px, 64f, 94f, 30f, 21f, hair, 64f, 70f, 25f);
            Ellipse(px, 52f, 78f, 7f, 5f, hair, 64f, 70f, 25f);   // perceme benzer kakul
            if (v == 5) Capsule(px, 42f, 78f, 86f, 78f, 3.5f, light);   // kafa bandi
        }

        Face(px, ink, sad, armsUp);

        // pon-ponlar
        if (armsUp)
        {
            PomPom(px, 14f, 111f, ink, light, team);
            PomPom(px, 114f, 111f, ink, light, team);
        }

        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, N, N), new Vector2(0.5f, 0.5f), 160f);
    }

    // Sahaya bakan yuz: gozler, yanaklar, agiz (sevincte acik, uzgunken asagi kivrik)
    static void Face(Color[] px, Color ink, bool sad, bool happy)
    {
        Color dark = new Color(0.16f, 0.08f, 0.08f);
        Color blush = new Color(1f, 0.45f, 0.45f, 0.45f);
        if (happy)
        {
            // keyifle kisilmis gozler
            Capsule(px, 52f, 66f, 56f, 69f, 1.8f, dark); Capsule(px, 56f, 69f, 60f, 66f, 1.8f, dark);
            Capsule(px, 68f, 66f, 72f, 69f, 1.8f, dark); Capsule(px, 72f, 69f, 76f, 66f, 1.8f, dark);
        }
        else
        {
            float ey = sad ? 64f : 66f;
            Ellipse(px, 56f, ey, 3.2f, 3.8f, dark);
            Ellipse(px, 72f, ey, 3.2f, 3.8f, dark);
            Ellipse(px, 57f, ey + 1.2f, 1.1f, 1.1f, Color.white);
            Ellipse(px, 73f, ey + 1.2f, 1.1f, 1.1f, Color.white);
            if (sad)
            {
                Capsule(px, 51f, 71f, 59f, 69f, 1.4f, dark);   // dusuk kaslar
                Capsule(px, 69f, 69f, 77f, 71f, 1.4f, dark);
            }
        }
        Ellipse(px, 49f, 58f, 4.5f, 3f, blush);
        Ellipse(px, 79f, 58f, 4.5f, 3f, blush);
        if (happy)
        {
            Ellipse(px, 64f, 55f, 7f, 6f, dark);                 // acik agiz
            Ellipse(px, 64f, 52f, 4f, 2.5f, new Color(0.95f, 0.45f, 0.5f));
        }
        else if (sad)
        {
            Capsule(px, 58f, 51f, 64f, 54f, 1.5f, dark);       // asagi kivrik agiz
            Capsule(px, 64f, 54f, 70f, 51f, 1.5f, dark);
        }
        else
        {
            Capsule(px, 58f, 56f, 64f, 53f, 1.5f, dark);       // gulumseme
            Capsule(px, 64f, 53f, 70f, 56f, 1.5f, dark);
        }
    }

    static void Arm(Color[] px, float sx, float sy, float hx, float hy, Color team, Color ink, Color skin)
    {
        Capsule(px, sx, sy, hx, hy, 12f, ink);
        Capsule(px, sx, sy, hx, hy, 9f, team);
        Ellipse(px, hx, hy, 10f, 10f, ink);
        Ellipse(px, hx, hy, 7f, 7f, skin);
    }

    static void PomPom(Color[] px, float cx, float cy, Color ink, Color light, Color team)
    {
        Ellipse(px, cx, cy, 15f, 13f, ink);
        Ellipse(px, cx, cy, 12f, 10f, light);
        Ellipse(px, cx - 3f, cy + 1f, 6f, 5f, Color.white);
        Ellipse(px, cx + 4f, cy - 2f, 4f, 4f, team);
    }

    static void Blend(Color[] px, int x, int y, Color c, float cov)
    {
        if (cov <= 0f) return;
        int i = y * N + x;
        Color d = px[i];
        float a = cov * c.a;
        float oa = a + d.a * (1f - a);
        if (oa <= 0f) return;
        px[i] = new Color(
            (c.r * a + d.r * d.a * (1f - a)) / oa,
            (c.g * a + d.g * d.a * (1f - a)) / oa,
            (c.b * a + d.b * d.a * (1f - a)) / oa,
            oa);
    }

    static void Ellipse(Color[] px, float cx, float cy, float rx, float ry, Color c, float clipX = 0f, float clipY = 0f, float clipR = 0f)
    {
        int x0 = Mathf.Max(0, (int)(cx - rx - 2f)), x1 = Mathf.Min(N - 1, (int)(cx + rx + 2f));
        int y0 = Mathf.Max(0, (int)(cy - ry - 2f)), y1 = Mathf.Min(N - 1, (int)(cy + ry + 2f));
        float m = Mathf.Min(rx, ry);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                float q = Mathf.Sqrt(dx * dx + dy * dy);
                float cov = Mathf.Clamp01(0.5f - (q - 1f) * m);
                if (clipR > 0f)
                {
                    float ex = x + 0.5f - clipX, ey = y + 0.5f - clipY;
                    cov *= Mathf.Clamp01(0.5f - (Mathf.Sqrt(ex * ex + ey * ey) - clipR));
                }
                Blend(px, x, y, c, cov);
            }
    }

    static void Capsule(Color[] px, float ax, float ay, float bx, float by, float r, Color c)
    {
        int x0 = Mathf.Max(0, (int)(Mathf.Min(ax, bx) - r - 2f)), x1 = Mathf.Min(N - 1, (int)(Mathf.Max(ax, bx) + r + 2f));
        int y0 = Mathf.Max(0, (int)(Mathf.Min(ay, by) - r - 2f)), y1 = Mathf.Min(N - 1, (int)(Mathf.Max(ay, by) + r + 2f));
        float abx = bx - ax, aby = by - ay;
        float len2 = abx * abx + aby * aby;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float qx = x + 0.5f - ax, qy = y + 0.5f - ay;
                float h = len2 > 0f ? Mathf.Clamp01((qx * abx + qy * aby) / len2) : 0f;
                float dx = qx - abx * h, dy = qy - aby * h;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Blend(px, x, y, c, Mathf.Clamp01(0.5f - (d - r)));
            }
    }
}
