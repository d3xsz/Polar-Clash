using UnityEngine;
using System.Collections.Generic;

// Miknatis gorselligi (oynanisa dokunmaz):
// - zemine dusen yumusak golge, govdede ust kenar parlakligi / alt kenar golgesi
// - nefes alan takim isigi; ates ederken parlar ve manyetik dalga halkalari yayar
// - dil kopukken (kilit) govde grilesir ve titrer
public class MagnetFx : MonoBehaviour
{
    static Sprite ringSprite;
    static Material unlit;

    PlayerController pc;
    Transform body;
    SpriteRenderer bodySr, glow, shadow, rimL, rimD;
    Vector3 glowScale;
    Color glowBase, team;
    float ringTimer;

    class Ring { public SpriteRenderer sr; public float t; }
    readonly List<Ring> rings = new List<Ring>();

    void Start()
    {
        pc = GetComponent<PlayerController>();
        body = transform.Find("Govde");
        if (body == null) { enabled = false; return; }
        bodySr = body.GetComponent<SpriteRenderer>();
        team = bodySr.color;
        if (unlit == null) unlit = WallFx.UnlitSprite();
        if (ringSprite == null) ringSprite = MakeRing();

        var g = transform.Find("Isik");
        if (g != null) { glow = g.GetComponent<SpriteRenderer>(); glowScale = g.localScale; glowBase = glow.color; }

        // Golge: ayri nesne, boylece ofset dunya uzayinda sabit kalir
        var sgo = new GameObject(name + "_Golge");
        shadow = sgo.AddComponent<SpriteRenderer>();
        shadow.sharedMaterial = unlit;
        shadow.color = new Color(0f, 0f, 0.02f, 0.45f);
        shadow.sortingOrder = 2;

        rimL = Overlay("Parlama", Cosmetics.RimLightSprite(), new Color(1f, 1f, 1f, 0.42f));
        rimD = Overlay("Koyu", Cosmetics.RimShadeSprite(), new Color(0f, 0f, 0.05f, 0.30f));
    }

    SpriteRenderer Overlay(string n, Sprite s, Color c)
    {
        var go = new GameObject(n);
        go.transform.SetParent(body, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.color = c; sr.sharedMaterial = unlit;
        sr.sortingOrder = bodySr.sortingOrder;
        sr.sortingLayerID = bodySr.sortingLayerID;
        // govdenin hemen ustunde ciz
        go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        sr.sortingOrder = bodySr.sortingOrder + 1;
        return sr;
    }

    void OnDestroy() { if (shadow != null) Destroy(shadow.gameObject); }

    void LateUpdate()
    {
        if (bodySr == null) return;
        float t = Time.time;
        bool firing = pc != null && pc.Firing;
        bool locked = pc != null && pc.Locked;

        // golge
        shadow.sprite = bodySr.sprite;
        shadow.transform.position = body.position + new Vector3(0.07f, -0.11f, 0f);
        shadow.transform.rotation = body.rotation;
        shadow.transform.localScale = body.lossyScale * 1.02f;

        // kilitliyken grilesme
        if (locked)
        {
            float f = 0.55f + 0.2f * Mathf.Sin(t * 22f);
            bodySr.color = Color.Lerp(team, new Color(0.45f, 0.45f, 0.5f), f);
        }
        else bodySr.color = Color.Lerp(bodySr.color, team, Time.deltaTime * 10f);

        // takim isigi
        if (glow != null)
        {
            float a = glowBase.a * (1f + 0.25f * Mathf.Sin(t * 2.2f));
            float sc = 1f + 0.04f * Mathf.Sin(t * 2.2f);
            if (firing) { a = 0.34f + 0.06f * Mathf.Sin(t * 18f); sc = 1.18f; }
            if (locked) { a = 0.06f; sc = 0.9f; }
            var gc = locked ? new Color(0.6f, 0.6f, 0.65f) : team;
            gc.a = Mathf.Lerp(glow.color.a, a, Time.deltaTime * 12f);
            glow.color = gc;
            glow.transform.localScale = Vector3.Lerp(glow.transform.localScale, glowScale * sc, Time.deltaTime * 10f);
        }

        // manyetik dalga halkalari
        if (firing)
        {
            ringTimer -= Time.deltaTime;
            if (ringTimer <= 0f) { ringTimer = 0.28f; SpawnRing(); }
        }
        else ringTimer = 0f;

        Color rc = pc != null && pc.Pulling ? new Color(0.45f, 1f, 0.65f) : Color.Lerp(team, Color.white, 0.35f);
        for (int i = rings.Count - 1; i >= 0; i--)
        {
            var r = rings[i];
            r.t += Time.deltaTime / 0.55f;
            if (r.t >= 1f) { r.sr.gameObject.SetActive(false); continue; }
            float e = 1f - (1f - r.t) * (1f - r.t);
            r.sr.transform.position = body.position;
            r.sr.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.1f, e);
            rc.a = 0.55f * (1f - r.t);
            r.sr.color = rc;
        }
    }

    void SpawnRing()
    {
        Ring r = null;
        foreach (var x in rings) if (!x.sr.gameObject.activeSelf) { r = x; break; }
        if (r == null)
        {
            var go = new GameObject("ManyetikDalga");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite; sr.sharedMaterial = unlit; sr.sortingOrder = 3;
            r = new Ring { sr = sr };
            rings.Add(r);
        }
        r.t = 0f;
        r.sr.gameObject.SetActive(true);
    }

    static Sprite MakeRing()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float h = n * 0.5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(h, h)) / h;
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.07f);
            a = a * a;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
