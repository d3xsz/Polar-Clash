using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Duvarlar cift calisir: ilki ana, ikincisi onun orta noktaya gore aynasi.
// Boylece iki oyuncu da duvarlardan ayni sekilde etkilenir.
public class ArenaWall : MonoBehaviour
{
    public float minDown = 6f, maxDown = 12f;
    public float minUp = 6f, maxUp = 10f;
    public float firstDelayMin = 3f, firstDelayMax = 8f;
    public float warnTime = 1.2f;
    public float moveTime = 0.5f;
    public float lowHeight = 0.04f;
    public float hitSpeed = 7f;

    [HideInInspector] public ArenaWall master;   // doluysa bu duvar aynadir
    [HideInInspector] public int pairIndex;
    [HideInInspector] public bool pushing;
    [HideInInspector] public bool active;        // uyari/yukselme/yukarida: yeri dolu sayilir

    // Yerlesim sinirlari ve bosluklar
    const float FieldX = 6.8f, FieldY = 4.6f;   // kale bolgelerine ve kenar duvarlara girmesin
    const float GapWalls = 0.5f;                // iki tuzak arasi en az bosluk
    const float GapMover = 0.4f;                // hareketli duvarin supurdugu alana bosluk

    SpriteRenderer sr;
    Collider2D col;
    Vector3 fullScale;
    Color baseColor;
    readonly HashSet<Rigidbody2D> hitThisRise = new HashSet<Rigidbody2D>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // R ile LoadScene sonrasi da tekrar eklensin diye her sahne yuklenisinde calistir
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
        var p = GameObject.Find("OlayDuvarlari");
        if (p == null) return;
        var walls = new List<ArenaWall>();
        foreach (Transform t in p.transform)
        {
            var w = t.GetComponent<ArenaWall>();
            if (w == null) w = t.gameObject.AddComponent<ArenaWall>();
            walls.Add(w);
        }
        for (int i = 0; i + 1 < walls.Count; i += 2)
        {
            walls[i].pairIndex = i / 2;
            walls[i + 1].master = walls[i];
        }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        fullScale = transform.localScale;
        baseColor = sr.color;
        if (master == null) StartCoroutine(Run());
        else SetHeight(lowHeight, 0f);
    }

    void SetHeight(float f, float alpha)
    {
        transform.localScale = new Vector3(fullScale.x, fullScale.y * f, 1f);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        col.enabled = f > 0.3f;
    }

    // Ayna duvar: anayi nokta simetrisiyle birebir izler
    void LateUpdate()
    {
        if (master == null || master.sr == null || sr == null) return;
        transform.position = new Vector3(-master.transform.position.x, -master.transform.position.y, 0f);
        transform.rotation = master.transform.rotation * Quaternion.Euler(0f, 0f, 180f);
        transform.localScale = master.transform.localScale;
        sr.color = master.sr.color;
        col.enabled = master.col.enabled;
        if (!master.pushing) hitThisRise.Clear();
        else if (col.enabled) PushBalls();
    }

    void PushBalls()
    {
        Physics2D.SyncTransforms();
        Bounds b = col.bounds;
        var hits = Physics2D.OverlapBoxAll(b.center, new Vector2(b.size.x + 0.1f, b.size.y + 0.1f), 0f);
        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb == null || h.GetComponent<Ball>() == null || hitThisRise.Contains(rb)) continue;
            hitThisRise.Add(rb);
            Vector2 d = (Vector2)h.transform.position - (Vector2)b.center;
            float sx = Mathf.Abs(d.x) < 0.05f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(d.x);
            float sy = Mathf.Clamp(d.y / Mathf.Max(0.1f, b.extents.y), -1f, 1f) * 0.35f;
            Vector2 dir = new Vector2(sx, sy).normalized;
            rb.linearVelocity = dir * (hitSpeed / Mathf.Sqrt(Mathf.Max(1f, rb.mass)));
        }
    }

    // Collider noktalarindan, verilen aci ve olcekte eksen hizali yari boyut (tam yukseklikte)
    public static Vector2 HalfExtents(GameObject go, float rotZ, Vector3 scale)
    {
        var poly = go.GetComponent<PolygonCollider2D>();
        var q = Quaternion.Euler(0f, 0f, rotZ);
        Vector2 mx = Vector2.zero;
        if (poly != null)
        {
            for (int p = 0; p < poly.pathCount; p++)
                foreach (var pt in poly.GetPath(p))
                {
                    Vector2 v = q * new Vector3((pt.x + poly.offset.x) * scale.x, (pt.y + poly.offset.y) * scale.y, 0f);
                    mx = Vector2.Max(mx, new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y)));
                }
            return mx;
        }
        var sr = go.GetComponent<SpriteRenderer>();
        Vector2 h = sr != null ? (Vector2)sr.sprite.bounds.extents : Vector2.one * 0.5f;
        h = new Vector2(h.x * Mathf.Abs(scale.x), h.y * Mathf.Abs(scale.y));
        float c = Mathf.Abs(Mathf.Cos(rotZ * Mathf.Deg2Rad)), sn = Mathf.Abs(Mathf.Sin(rotZ * Mathf.Deg2Rad));
        return new Vector2(h.x * c + h.y * sn, h.x * sn + h.y * c);
    }

    static bool Overlap(Vector2 a, Vector2 ha, Vector2 b, Vector2 hb, float gap)
    {
        return Mathf.Abs(a.x - b.x) < ha.x + hb.x + gap && Mathf.Abs(a.y - b.y) < ha.y + hb.y + gap;
    }

    ArenaWall Root { get { return master != null ? master : this; } }
    Vector3 FullScale { get { return Root.fullScale; } }

    // Bu tuzak (ve aynasi) c noktasinda, rot acisinda duruyorsa hicbir seyle cakisiyor mu?
    public bool SpotFree(Vector2 c, float rot)
    {
        Vector2 h = HalfExtents(gameObject, rot, FullScale);
        // saha icinde kalsin (ayna da simetrik oldugu icin ayni kosul yeter)
        if (Mathf.Abs(c.x) + h.x > FieldX || Mathf.Abs(c.y) + h.y > FieldY) return false;
        // kendi aynasiyla cakismasin (aynanin acisi +180, kutu ayni)
        if (Overlap(c, h, -c, h, GapWalls)) return false;

        // diger aktif tuzaklar ve aynalari
        foreach (Transform o in transform.parent)
        {
            var ow = o.GetComponent<ArenaWall>();
            if (ow == null || ow.Root == this) continue;     // kendim ve aynam
            if (!ow.Root.active) continue;                    // asagidakiler kalkarken kendileri kontrol eder
            Vector2 oh = HalfExtents(o.gameObject, o.eulerAngles.z, ow.FullScale);
            Vector2 op = o.position;
            if (Overlap(c, h, op, oh, GapWalls) || Overlap(-c, h, op, oh, GapWalls)) return false;
        }

        // hareketli duvarlarin tum gidip gelme alani
        var movers = GameObject.Find("HareketliDuvarlar");
        if (movers != null)
            foreach (Transform m in movers.transform)
            {
                var mw = m.GetComponent<MovingWall>();
                float amp = mw != null ? mw.amplitude : 0f;
                Vector2 mh = HalfExtents(m.gameObject, m.eulerAngles.z, m.localScale);
                Vector2 sweep = new Vector2(mh.x, mh.y + amp);
                Vector2 mc = new Vector2(m.position.x, 0f);
                if (Overlap(c, h, mc, sweep, GapMover) || Overlap(-c, h, mc, sweep, GapMover)) return false;
                if (Overlap(c, h, -mc, sweep, GapMover) || Overlap(-c, h, -mc, sweep, GapMover)) return false;
            }
        return true;
    }

    // 0. cift: ust-alt bolge, 1. cift: sol-sag orta bolge. Ayna otomatik karsi tarafa gecer.
    // Bos yer bulunamazsa false doner ve tuzak bu tur kalkmaz (asla ic ice gecmez).
    bool Relocate()
    {
        float xMin, xMax, yMin, yMax;
        if (pairIndex == 0) { xMin = -5.5f; xMax = 5.5f; yMin = 1.2f; yMax = 3.8f; }
        else { xMin = 1.5f; xMax = 5.8f; yMin = -1.6f; yMax = 1.6f; }
        for (int k = 0; k < 400; k++)
        {
            // son denemelerde tum sahaya ac
            bool wide = k >= 250;
            Vector2 c = wide
                ? new Vector2(Random.Range(-FieldX, FieldX), Random.Range(-FieldY, FieldY))
                : new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
            float rot = 90f * Random.Range(0, 4);
            if (!SpotFree(c, rot)) continue;
            transform.position = new Vector3(c.x, c.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, rot);
            return true;
        }
        return false;
    }

    IEnumerator Run()
    {
        SetHeight(lowHeight, 0f);
        yield return new WaitForSeconds(Random.Range(firstDelayMin, firstDelayMax));
        while (true)
        {
            if (!Relocate())
            {
                yield return new WaitForSeconds(2f);   // yer acilinca tekrar dene
                continue;
            }
            active = true;
            SoundFX.Play("wallwarn", 0.5f);
            for (float t = 0f; t < warnTime; t += Time.deltaTime)
            {
                float a = 0.25f + 0.5f * Mathf.Abs(Mathf.Sin(t * 8f));
                SetHeight(0.1f, a);
                yield return null;
            }
            hitThisRise.Clear();
            pushing = true;
            SoundFX.Play("wallrise");
            FX.Burst(transform.position, baseColor, 28, 5f, 0.13f, 0.6f);
            FX.Burst(-transform.position, baseColor, 28, 5f, 0.13f, 0.6f);
            for (float t = 0f; t < moveTime; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / moveTime);
                SetHeight(Mathf.Lerp(0.1f, 1f, k), Mathf.Lerp(0.5f, 1f, k));
                if (col.enabled) PushBalls();
                yield return null;
            }
            SetHeight(1f, 1f);
            PushBalls();
            yield return null;
            pushing = false;
            yield return new WaitForSeconds(Random.Range(minUp, maxUp));
            for (float t = 0f; t < moveTime; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / moveTime);
                SetHeight(Mathf.Lerp(1f, lowHeight, k), Mathf.Lerp(1f, 0f, k));
                yield return null;
            }
            SetHeight(lowHeight, 0f);
            active = false;
            yield return new WaitForSeconds(Random.Range(minDown, maxDown));
        }
    }
}
