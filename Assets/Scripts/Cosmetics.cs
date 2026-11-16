using UnityEngine;

// Yalnizca gorsel secenekler: oynanisi, gucleri ve carpismalari etkilemez.
public static class Cosmetics
{
    public static readonly string[] DesignNames = { "Klasik", "Gümüş uçlu", "Çizgili", "Neon kontur", "Metalik", "Noktalı", "Kristal", "Zikzak" };
    public static readonly string[] TrailNames = { "Yok", "Kısa", "Uzun", "Gökkuşağı", "Kıvılcım" };
    public static readonly string[] FxNames = { "Patlama", "Halka", "Konfeti", "Yıldız", "Çift halka" };

    // Oyuncu numarasina gore: 1 = kuzey kutup, 2 = guney kutup
    public static int[] design = new int[3];
    public static int[] trail = new int[3];
    public static int[] fx = new int[3];

    static Sprite originalSprite, stripeSprite, tipSprite, contourSprite, metalSprite, dotSprite, crystalSprite, zigzagSprite, rimLight, rimShade;

    // Secimler oyun kapansa da hatirlanir
    public static void Save()
    {
        for (int id = 1; id <= 2; id++)
        {
            PlayerPrefs.SetInt("koz_tasarim_" + id, design[id]);
            PlayerPrefs.SetInt("koz_iz_" + id, trail[id]);
            PlayerPrefs.SetInt("koz_efekt_" + id, fx[id]);
        }
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        for (int id = 1; id <= 2; id++)
        {
            design[id] = Mathf.Clamp(PlayerPrefs.GetInt("koz_tasarim_" + id, design[id]), 0, DesignNames.Length - 1);
            trail[id] = Mathf.Clamp(PlayerPrefs.GetInt("koz_iz_" + id, trail[id]), 0, TrailNames.Length - 1);
            fx[id] = Mathf.Clamp(PlayerPrefs.GetInt("koz_efekt_" + id, fx[id]), 0, FxNames.Length - 1);
        }
    }

    static float Hash(float a, float b)
    {
        float h = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    // Ucgen yuzeyli kristal
    static float Crystal(float x, float y)
    {
        if (!Inner(x, y)) return 1f;
        float u = x * 7f + y * 3f, v = y * 7f - x * 2f;
        float fu = Mathf.Floor(u), fv = Mathf.Floor(v);
        float tri = (u - fu) > (v - fv) ? 1f : 0f;
        return 0.55f + 0.45f * Hash(fu + tri * 17.3f, fv);
    }

    // Kollar boyunca zikzak serit
    static float Zigzag(float x, float y)
    {
        if (!Inner(x, y)) return 1f;
        float z = Mathf.Repeat(y * 5f + Mathf.Abs(Mathf.Repeat(x * 5f, 1f) - 0.5f) * 1.4f, 1f);
        return z < 0.5f ? 1f : 0.5f;
    }
    static Material trailMat;

    static bool InO(float x, float y)
    {
        if (x <= 0f) return x * x / 0.2025f + y * y / 0.49f <= 1f;
        if (x <= 0.35f) return Mathf.Abs(y) <= 0.7f;
        float cy = y > 0 ? 0.55f : -0.55f; float dx = x - 0.35f, dy = y - cy;
        return dx * dx + dy * dy <= 0.0225f;
    }

    static bool InI(float x, float y)
    {
        if (x <= 0f) return x * x / 0.04f + y * y / 0.16f <= 1f;
        return Mathf.Abs(y) < 0.4f;
    }

    static bool InShape(float x, float y) { return InO(x, y) && !InI(x, y); }

    static bool Inner(float x, float y)
    {
        const float e = 0.035f;
        return InShape(x + e, y) && InShape(x - e, y) && InShape(x, y + e) && InShape(x, y - e);
    }

    static float Metal(float x, float y)
    {
        float v = Mathf.Clamp01(0.62f + 0.35f * (-x * 0.5f + y * 0.8f));
        if (Mathf.Abs(y - (0.45f - x * 0.3f)) < 0.025f) v = Mathf.Min(1f, v + 0.25f);
        if (!Inner(x, y)) v *= 0.7f;
        return v;
    }

    static float Dots(float x, float y)
    {
        float gx = Mathf.Repeat(x, 0.16f) - 0.08f, gy = Mathf.Repeat(y, 0.16f) - 0.08f;
        float v = (gx * gx + gy * gy) < 0.0012f ? 0.5f : 1f;
        if (!Inner(x, y)) v = 1f;
        return v;
    }

    static Gradient TrailGradient(int k)
    {
        var g = new Gradient();
        if (k == 3)
            g.SetKeys(new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.25f), new GradientColorKey(Color.green, 0.5f), new GradientColorKey(Color.cyan, 0.75f), new GradientColorKey(new Color(0.7f, 0.4f, 1f), 1f) },
                      new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
        else
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0.5f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    static Sprite Raster(System.Func<float, float, bool> include, System.Func<float, float, float> val)
    {
        int w = 192, h = 256; float ppu = 160f;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
        {
            int c = 0;
            for (int sy = 0; sy < 3; sy++) for (int sx = 0; sx < 3; sx++)
            {
                float wx = (i + (sx + 0.5f) / 3f) / ppu - 0.6f;
                float wy = (j + (sy + 0.5f) / 3f) / ppu - 0.8f;
                if (include(wx, wy)) c++;
            }
            float cx = (i + 0.5f) / ppu - 0.6f, cy = (j + 0.5f) / ppu - 0.8f;
            float v = c > 0 ? val(cx, cy) : 1f;
            t.SetPixel(i, j, new Color(v, v, v, c / 9f));
        }
        t.Apply();
        t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    static void EnsureSprites()
    {
        if (stripeSprite == null)
            stripeSprite = Raster((x, y) => InShape(x, y), (x, y) => (((int)Mathf.Floor((x + y) * 9f)) & 1) == 0 ? 1f : 0.6f);
        if (tipSprite == null)
            tipSprite = Raster((x, y) => x > 0.15f && InShape(x, y), (x, y) => 1f);
        if (contourSprite == null)
            contourSprite = Raster((x, y) => InShape(x, y), (x, y) => Inner(x, y) ? 0.16f : 1f);
        if (metalSprite == null)
            metalSprite = Raster((x, y) => InShape(x, y), (x, y) => Metal(x, y));
        if (dotSprite == null)
            dotSprite = Raster((x, y) => InShape(x, y), (x, y) => Dots(x, y));
        if (crystalSprite == null)
            crystalSprite = Raster((x, y) => InShape(x, y), (x, y) => Crystal(x, y));
        if (zigzagSprite == null)
            zigzagSprite = Raster((x, y) => InShape(x, y), (x, y) => Zigzag(x, y));
        // Hacim hissi icin ust kenar parlakligi ve alt kenar golgesi (sekil her tasarimda ayni)
        if (rimLight == null)
            rimLight = Raster((x, y) => InShape(x, y) && !InShape(x - 0.03f, y + 0.075f), (x, y) => 1f);
        if (rimShade == null)
            rimShade = Raster((x, y) => InShape(x, y) && !InShape(x + 0.03f, y - 0.09f), (x, y) => 1f);
    }

    static Material TrailMat()
    {
        if (trailMat == null)
        {
            trailMat = new Material(Shader.Find("Sprites/Default"));
            trailMat.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }
        return trailMat;
    }

    public static Sprite DesignSprite(int d)
    {
        EnsureSprites();
        switch (d)
        {
            case 2: return stripeSprite;
            case 3: return contourSprite;
            case 4: return metalSprite;
            case 5: return dotSprite;
            case 6: return crystalSprite;
            case 7: return zigzagSprite;
            default: return originalSprite;
        }
    }
    public static Sprite TipsSprite() { EnsureSprites(); return tipSprite; }
    public static Sprite RimLightSprite() { EnsureSprites(); return rimLight; }
    public static Sprite RimShadeSprite() { EnsureSprites(); return rimShade; }

    // Secimleri ilgili miknatisa uygular (collider'a dokunmaz)
    public static void Apply(GameObject p, int id)
    {
        var body = p.transform.Find("Govde");
        if (body == null) return;
        var sr = body.GetComponent<SpriteRenderer>();
        if (originalSprite == null) originalSprite = sr.sprite;
        EnsureSprites();

        int d = design[id];
        sr.sprite = DesignSprite(d);

        var tips = body.Find("Uclar");
        if (d == 1)
        {
            if (tips == null)
            {
                var go = new GameObject("Uclar");
                go.transform.SetParent(body, false);
                var o = go.AddComponent<SpriteRenderer>();
                o.sprite = tipSprite; o.color = new Color(0.86f, 0.89f, 0.93f); o.sortingOrder = 6;
                tips = go.transform;
            }
            tips.gameObject.SetActive(true);
        }
        else if (tips != null) tips.gameObject.SetActive(false);

        SetupTrails(p.transform, id, sr.color);
    }

    static void SetupTrails(Transform p, int id, Color baseCol)
    {
        foreach (var n in new[] { "Iz_Ust", "Iz_Alt" })
        {
            var old = p.Find(n);
            if (old != null) { old.name = "eski"; Object.Destroy(old.gameObject); }
        }
        int k = trail[id];
        if (k == 0) return;
        float time = k == 1 ? 0.35f : (k == 3 ? 0.7f : (k == 4 ? 0.5f : 0.9f));
        float width = k == 1 ? 0.07f : (k == 3 ? 0.09f : (k == 4 ? 0.05f : 0.11f));
        Color c = Color.Lerp(baseCol, Color.white, 0.4f);
        foreach (float sy in new[] { 1f, -1f })
        {
            var go = new GameObject(sy > 0 ? "Iz_Ust" : "Iz_Alt");
            go.transform.SetParent(p, false);
            go.transform.localPosition = new Vector3(0.475f, 0.52f * sy, 0f);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = time; tr.startWidth = width; tr.endWidth = 0f; tr.minVertexDistance = 0.03f;
            tr.material = TrailMat();
            tr.startColor = new Color(c.r, c.g, c.b, 0.9f);
            tr.endColor = new Color(c.r, c.g, c.b, 0f);
            if (k >= 3) tr.colorGradient = TrailGradient(k);
            tr.sortingOrder = 3;
        }
    }

    // Top toplama efekti secenekleri
    public static void CollectFx(int id, Vector2 pos, Color c, bool big)
    {
        switch (fx[id])
        {
            case 1: FX.Ring(pos, c, big ? 44 : 26, 7f, 0.13f, 0.55f); break;
            case 2: FX.Confetti(pos, big ? 60 : 36); break;
            case 3: FX.Stars(pos, c, big ? 9 : 6); break;
            case 4: FX.DoubleRing(pos, c, big ? 1.3f : 1f); break;
            default: FX.Burst(pos, c, big ? 40 : 18, 6f, 0.14f, 0.6f); break;
        }
    }
}
