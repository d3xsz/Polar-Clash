using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Gorsel dil: koyu mor derinlik, yuvarlak kose paneller, ince beyaz/renkli cizgiler, yumusak isik huzmeleri, kucuk parilti yildizlari
public static class UIKit
{
    static Font fontMed, fontBold;
    static Sprite round, roundLine, pillBig, pillBigLine, pillSmall, pillSmallLine, glowShape, circle, sparkle, soft, vGrad;

    // Assets/Resources/Fonts/ icine Fredoka-Medium.ttf ve Fredoka-Bold.ttf konursa onlari kullanir, yoksa sistem fontuna duser
    static Font LoadFont(string res, string[] os)
    {
        var f = Resources.Load<Font>(res);
        return f != null ? f : Font.CreateDynamicFontFromOSFont(os, 48);
    }

    public static Font Medium
    {
        get
        {
            if (fontMed == null) fontMed = LoadFont("Fonts/Fredoka-Medium", new[] { "Arial Rounded MT Bold", "Nunito", "Segoe UI", "Helvetica Neue", "Arial" });
            return fontMed;
        }
    }

    public static Font Bold
    {
        get
        {
            if (fontBold == null) fontBold = LoadFont("Fonts/Fredoka-Bold", new[] { "Arial Rounded MT Bold", "Nunito ExtraBold", "Segoe UI Black", "Segoe UI Semibold", "Helvetica Neue", "Arial" });
            return fontBold;
        }
    }

    // Font Turkce'ye ozel harfleri desteklemiyorsa true birak: ç ş ğ ö ü ı İ -> c s g o u i I
    public static bool AsciiOnly = true;
    public static string T(string s)
    {
        if (!AsciiOnly || string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case 'ç': sb.Append('c'); break;
                case 'Ç': sb.Append('C'); break;
                case 'ğ': sb.Append('g'); break;
                case 'Ğ': sb.Append('G'); break;
                case 'ı': sb.Append('i'); break;
                case 'İ': sb.Append('I'); break;
                case 'ö': sb.Append('o'); break;
                case 'Ö': sb.Append('O'); break;
                case 'ş': sb.Append('s'); break;
                case 'Ş': sb.Append('S'); break;
                case 'ü': sb.Append('u'); break;
                case 'Ü': sb.Append('U'); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    // ---------- Sprite uretimi ----------
    static Texture2D Tex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return t;
    }

    static float SdRound(float px, float py, float hx, float hy, float r)
    {
        float dx = Mathf.Abs(px) - (hx - r), dy = Mathf.Abs(py) - (hy - r);
        float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
    }

    static Sprite Finish(Texture2D t, Vector4 border, float ppu)
    {
        t.Apply();
        var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    static Sprite Shape(int n, float r, int border, bool outline, float lineW)
    {
        var t = Tex(n, n); float h = n * 0.5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d = SdRound(x + 0.5f - h, y + 0.5f - h, h, h, r);
            float a = Mathf.Clamp01(0.5f - d);
            if (outline) a *= Mathf.Clamp01(d + lineW);
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        return Finish(t, new Vector4(border, border, border, border), 100f);
    }

    static void EnsureSprites()
    {
        if (round != null) return;
        round = Shape(64, 22f, 24, false, 0f);
        roundLine = Shape(64, 22f, 24, true, 2.5f);
        pillBig = Shape(64, 31f, 31, false, 0f);
        pillBigLine = Shape(64, 31f, 31, true, 2.5f);
        pillSmall = Shape(32, 15.5f, 15, false, 0f);
        pillSmallLine = Shape(32, 15.5f, 15, true, 2f);

        // Panel arkasi yumusak isilti
        var g = Tex(128, 128);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            float dd = SdRound(x + 0.5f - 64f, y + 0.5f - 64f, 24f, 24f, 22f);
            float a = dd <= 0f ? 1f : Mathf.Exp(-dd / 11f) * Mathf.Clamp01(1f - dd / 40f);
            g.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        glowShape = Finish(g, new Vector4(56, 56, 56, 56), 100f);

        var c = Tex(32, 32);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
            c.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(16f - d)));
        }
        circle = Finish(c, Vector4.zero, 100f);

        var s = Tex(32, 32);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float px = (x + 0.5f - 16f) / 16f, py = (y + 0.5f - 16f) / 16f;
            float v = Mathf.Sqrt(Mathf.Abs(px)) + Mathf.Sqrt(Mathf.Abs(py));
            s.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1.1f - v) * 1.4f)));
        }
        sparkle = Finish(s, Vector4.zero, 100f);

        var so = Tex(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
            so.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2f)));
        }
        soft = Finish(so, Vector4.zero, 100f);

        var v2 = Tex(4, 256);
        for (int y = 0; y < 256; y++)
        {
            Color col = Color.Lerp(new Color(0.13f, 0.09f, 0.30f), new Color(0.045f, 0.035f, 0.12f), y / 255f);
            for (int x = 0; x < 4; x++) v2.SetPixel(x, y, col);
        }
        vGrad = Finish(v2, Vector4.zero, 100f);
    }

    // ---------- Temel yapi taslari ----------
    static RectTransform MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(Transform parent, string name, Vector2 pos, Vector2 size, Sprite s, Color c, bool sliced)
    {
        var rt = MakeRect(name, parent, pos, size);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = s; im.color = c; im.raycastTarget = false;
        if (sliced) im.type = Image.Type.Sliced;
        return im;
    }

    public static GameObject Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    public static Image Block(Transform parent, string name, Vector2 pos, Vector2 size, Color col)
    {
        return Img(parent, name, pos, size, null, col, false);
    }

    // Yuvarlak kose cam panel: yumusak isilti + dolgu + ince cizgi
    public static RectTransform Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color fill, Color line, Color glowCol)
    {
        EnsureSprites();
        var root = MakeRect(name, parent, pos, size);
        if (glowCol.a > 0.001f) Img(root, "Isik", Vector2.zero, size + new Vector2(80f, 80f), glowShape, glowCol, true);
        Img(root, "Dolgu", Vector2.zero, size, round, fill, true);
        Img(root, "Cizgi", Vector2.zero, size, roundLine, line, true);
        return root;
    }

    // Hap seklinde etiket / dugme (uzun olanlar tam hap, kisa olanlar kucuk yaricapli)
    public static RectTransform Pill(Transform parent, string name, Vector2 pos, Vector2 size, Color fill, Color line)
    {
        EnsureSprites();
        bool big = size.y >= 62f;
        var root = MakeRect(name, parent, pos, size);
        Img(root, "Dolgu", Vector2.zero, size, big ? pillBig : pillSmall, fill, true);
        if (line.a > 0.001f) Img(root, "Cizgi", Vector2.zero, size, big ? pillBigLine : pillSmallLine, line, true);
        return root;
    }

    public static Image Dot(Transform parent, string name, Vector2 pos, float size, Color col)
    {
        EnsureSprites();
        return Img(parent, name, pos, new Vector2(size, size), circle, col, false);
    }

    public static Image Soft(Transform parent, string name, Vector2 pos, Vector2 size, Color col, float rotZ)
    {
        EnsureSprites();
        var im = Img(parent, name, pos, size, soft, col, false);
        im.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        return im;
    }

    public static Text Label(Transform parent, string name, string text, int size, Color col, Vector2 pos, Vector2 box, TextAnchor align, bool bold)
    {
        var rt = MakeRect(name, parent, pos, box);
        var tx = rt.gameObject.AddComponent<Text>();
        tx.font = bold ? Bold : Medium;
        tx.fontSize = size; tx.alignment = align; tx.color = col; tx.text = T(text);
        tx.raycastTarget = false; tx.supportRichText = false;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        return tx;
    }

    // Yuvarlak kose tus kapagi
    public static RectTransform KeyCap(Transform parent, string key, Vector2 pos, float size)
    {
        EnsureSprites();
        var root = MakeRect("Tus_" + key, parent, pos, new Vector2(size, size));
        Img(root, "Golge", new Vector2(0f, -4f), new Vector2(size, size), round, new Color(0.02f, 0.015f, 0.08f, 0.85f), true);
        Img(root, "Yuz", Vector2.zero, new Vector2(size, size), round, new Color(0.24f, 0.21f, 0.46f, 1f), true);
        Img(root, "Cizgi", Vector2.zero, new Vector2(size, size), roundLine, new Color(1f, 1f, 1f, 0.28f), true);
        Label(root, "Harf", key, (int)(size * 0.5f), Color.white, Vector2.zero, new Vector2(size, size), TextAnchor.MiddleCenter, true);
        return root;
    }

    // Fare ile tiklanabilir yap: dolgu gorseli isin hedefi olur (yoksa saydam bir alan eklenir)
    public static UIButton Clickable(RectTransform r, System.Action click, System.Action rightClick = null, float hoverScale = 1.05f)
    {
        var fill = r.Find("Dolgu");
        Image hit = fill != null ? fill.GetComponent<Image>() : r.GetComponent<Image>();
        if (hit == null) { hit = r.gameObject.AddComponent<Image>(); hit.color = new Color(1f, 1f, 1f, 0f); }
        hit.raycastTarget = true;
        var b = r.gameObject.GetComponent<UIButton>();
        if (b == null) b = r.gameObject.AddComponent<UIButton>();
        b.onClick = click; b.onRightClick = rightClick; b.hoverScale = hoverScale;
        return b;
    }

    // Saydam tiklama alani (kucuk ogeler icin genis hedef)
    public static RectTransform HitArea(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rt = MakeRect(name, parent, pos, size);
        var im = rt.gameObject.AddComponent<Image>();
        im.color = new Color(1f, 1f, 1f, 0f);
        return rt;
    }

    // Kirpilmis alan (onizleme icin)
    public static RectTransform Clip(Transform parent, string name, Vector2 size)
    {
        var rt = MakeRect(name, parent, Vector2.zero, size);
        rt.gameObject.AddComponent<RectMask2D>();
        return rt;
    }

    public static RectTransform Pivot(Transform parent, string name)
    {
        return MakeRect(name, parent, Vector2.zero, new Vector2(10f, 10f));
    }

    // Ekran arka plani: koyu mor gradyan + iki yumusak isik huzmesi + parilti yildizlari
    public static Image[] Background(Transform parent)
    {
        EnsureSprites();
        var g = Stretch("Zemin", parent);
        var gi = g.AddComponent<Image>();
        gi.sprite = vGrad; gi.raycastTarget = false;
        Soft(parent, "HuzmeMavi", new Vector2(-380f, 230f), new Vector2(2000f, 320f), new Color(0.35f, 0.5f, 1f, 0.20f), -32f);
        Soft(parent, "HuzmeSicak", new Vector2(520f, -250f), new Vector2(2000f, 280f), new Color(1f, 0.7f, 0.3f, 0.16f), -32f);
        var list = new List<Image>();
        var rng = new System.Random(11);
        for (int i = 0; i < 46; i++)
        {
            float x = (float)(rng.NextDouble() * 1880.0 - 940.0), y = (float)(rng.NextDouble() * 1040.0 - 520.0);
            float sz = 8f + (float)rng.NextDouble() * 14f;
            float a = 0.25f + (float)rng.NextDouble() * 0.55f;
            list.Add(Img(parent, "Yildiz", new Vector2(x, y), new Vector2(sz, sz), sparkle, new Color(1f, 1f, 1f, a), false));
        }
        return list.ToArray();
    }

    // Yildizlari yavasca parildat
    public static void Twinkle(Image[] stars, float[] baseA)
    {
        if (stars == null) return;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            float k = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (1f + (i % 3) * 0.7f) + i);
            var c = stars[i].color; c.a = baseA[i] * k; stars[i].color = c;
        }
    }
}
