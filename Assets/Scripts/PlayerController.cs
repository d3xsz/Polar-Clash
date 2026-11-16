using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public int playerId = 1;
    public float rotateSpeed = 140f;     // derece/saniye
    public float rotateAccel = 500f;
    public float maxAimAngle = 135f;     // +-135 = toplam 270 derece
    public float beamRange = 18f;
    public float beamRadius = 0.35f;
    public float beamForce = 11f;
    public float ropeSpeed = 45f;
    public float gapOffset = 0.14f;
    public float collectRadius = 0.26f;
    public float tongueLockTime = 3f;    // dil kopunca kac saniye kullanilamaz
    public float ropeTouchRadius = 0.25f;     // iplerin degme mesafesi
    public float ropeTipExtension = 0.3f;   // ip ucuna eklenen pay (top yaricapi kadar)
    public int score;
    public SpriteRenderer arrow;

    static readonly List<PlayerController> all = new List<PlayerController>();

    // Gorsel efektler icin durum
    public bool Firing { get { return fire && lockTimer <= 0f; } }
    public bool Locked { get { return lockTimer > 0f; } }
    public bool Pulling { get { return pulling; } }

    Rigidbody2D rb;
    LineRenderer rope;
    Vector2[] ropePts;
    Vector2 facing, aimEnd;
    float rotInput, baseAngle, aim, aimVel;
    float ropeLen, targetLen, lockTimer;
    bool fire, pulling, prevFire;
    float sparkTimer;
    Color ropeColor = Color.white;

    void Awake()
    {
        all.RemoveAll(x => x == null);
        all.Add(this);
        rb = GetComponent<Rigidbody2D>();
        baseAngle = playerId == 1 ? 0f : 180f;
        rb.rotation = baseAngle;
        UpdateFacing();
        BuildRope();
    }

    void OnDestroy() { all.Remove(this); }

    void BuildRope()
    {
        var go = new GameObject("Ip");
        go.transform.SetParent(transform, false);
        rope = go.AddComponent<LineRenderer>();
        rope.useWorldSpace = true;
        rope.positionCount = 28;
        ropePts = new Vector2[28];
        rope.material = new Material(Shader.Find("Sprites/Default"));
        rope.sortingOrder = 7;
        rope.numCapVertices = 6;
        rope.widthCurve = new AnimationCurve(new Keyframe(0f, 0.16f), new Keyframe(1f, 0.06f));
        rope.enabled = false;
    }

    Vector2 Origin() { return (Vector2)transform.position + facing * gapOffset; }

    void UpdateFacing()
    {
        float r = (baseAngle + aim) * Mathf.Deg2Rad;
        facing = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    void Update()
    {
        if (Time.timeScale == 0f) { fire = false; prevFire = false; return; }   // menu / duraklatma
        ReadInput();
        if (lockTimer > 0f) { lockTimer -= Time.deltaTime; fire = false; }
        if (fire && !prevFire) { SoundFX.Play("fire", 0.6f); FX.Burst(Origin(), playerId == 1 ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 0.75f, 1f), 8, 3.5f, 0.1f, 0.25f); }
        prevFire = fire;
        DrawRope();
        if (arrow != null)
        {
            if (lockTimer > 0f) arrow.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);
            else arrow.color = fire ? ropeColor : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        aimVel = Mathf.MoveTowards(aimVel, rotInput * rotateSpeed, rotateAccel * dt);
        aim += aimVel * dt;
        if (aim > maxAimAngle) { aim = maxAimAngle; aimVel = 0f; }
        if (aim < -maxAimAngle) { aim = -maxAimAngle; aimVel = 0f; }
        rb.MoveRotation(baseAngle + aim);
        UpdateFacing();

        pulling = false;
        if (fire && lockTimer <= 0f) FireBeam();
        
    }

    void FireBeam()
    {
        Vector2 origin = Origin();
        aimEnd = origin + facing * beamRange;
        ropeColor = playerId == 1 ? new Color(1f, 0.55f, 0.55f) : new Color(0.55f, 0.8f, 1f);

        var hits = Physics2D.CircleCastAll(origin, beamRadius, facing, beamRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
            var ball = h.collider.GetComponent<Ball>();
            if (ball == null)
            {
                aimEnd = origin + facing * h.distance;   // duvar ya da rakip miknatis ipi keser
                break;
            }
            Vector2 bp = ball.transform.position;
            aimEnd = bp;
            float d = Vector2.Distance(origin, bp);
            float falloff = Mathf.Lerp(1f, 0.5f, d / beamRange);
            float pol = PolarityEvent.Sign;              // ters kutupta cekme/itme yer degistirir
            bool myTarget = ball.team != playerId;       // rakip renkteki top: benim hedefim
            if (myTarget)
            {
                ropeColor = pol > 0f ? new Color(0.4f, 1f, 0.6f) : new Color(1f, 0.6f, 0.1f);
                pulling = pol > 0f;
                if (pol > 0f && d < collectRadius + 2f * (ball.transform.lossyScale.x * 0.5f - 0.25f))
                {
                    score += ball.points;
                    SoundFX.Play("collect");
                    CrowdBob.Cheer(playerId);
                    CrowdBob.Sulk(playerId == 1 ? 2 : 1);
                    ScoreLights.Pulse(playerId);
                    Cosmetics.CollectFx(playerId, bp, ball.GetComponent<SpriteRenderer>().color, ball.points >= 3);
                    ScorePopup.Show(bp, ball.GetComponent<SpriteRenderer>().color, ball.points);
                    CameraShake.Add(ball.points >= 3 ? 0.35f : 0.18f);
                    Debug.Log(name + " skor: " + score);
                    Destroy(ball.gameObject);
                }
                else ball.Body.AddForce((origin - bp).normalized * beamForce * falloff * pol);
            }
            else
            {
                ropeColor = pol > 0f ? new Color(1f, 0.6f, 0.1f) : new Color(0.4f, 1f, 0.6f);   // rakibin topu: it
                pulling = pol < 0f;
                ball.Body.AddForce((bp - origin).normalized * beamForce * falloff * pol);
            }
            break;
        }
        targetLen = (aimEnd - origin).magnitude;
        sparkTimer -= Time.fixedDeltaTime;
        if (sparkTimer <= 0f && targetLen > 0.5f && ropeLen >= targetLen - 0.2f) { sparkTimer = 0.07f; FX.Spark(aimEnd, ropeColor); }
    }

    // Aktif ipin dogru parcasi
    bool GetRope(out Vector2 a, out Vector2 b)
    {
        a = Origin(); b = a;
        if (lockTimer > 0f || ropeLen < 0.05f) return false;
        Vector2 toEnd = aimEnd - a;
        Vector2 dir = toEnd.sqrMagnitude > 0.0001f ? toEnd.normalized : facing;
        b = a + dir * (ropeLen + ropeTipExtension);
        return true;
    }

    // Top cekerken rakibin ipi degerse dil kopar
    void CheckRopeCut()
    {
        if (!pulling || lockTimer > 0f) return;
        Vector2 a, b;
        if (!GetRope(out a, out b)) return;
        foreach (var o in all)
        {
            if (o == null || o == this) continue;
            Vector2 c, d;
            if (!o.GetRope(out c, out d)) continue;
            if (SegDist(a, b, c, d) < ropeTouchRadius)
            {
                Vector2 hitPt = a; float best = float.MaxValue;
                for (int q = 0; q <= 20; q++) { Vector2 pt = Vector2.Lerp(a, b, q / 20f); float dd = PointSeg(pt, c, d); if (dd < best) { best = dd; hitPt = pt; } }
                FX.Burst(hitPt, new Color(1f, 0.75f, 0.3f), 36, 7f, 0.16f, 0.6f);
                FX.Burst(hitPt, Color.white, 14, 4f, 0.12f, 0.4f);
                LoseTongue();
                return;
            }
        }
    }

    // --- Adil dil kopma: iki oyuncu ayni anda, ayni durum uzerinden degerlendirilir ---
    bool cutPending;
    Vector2 cutPoint;

    // Ekranda cizilen ipin gercek noktalari (dalgali hali) + uca ekstra pay
    List<Vector2> RopePoints()
    {
        if (lockTimer > 0f || ropeLen < 0.05f || ropePts == null) return null;
        var list = new List<Vector2>(ropePts);
        Vector2 last = list[list.Count - 1], prev = list[list.Count - 2];
        Vector2 tan = (last - prev).normalized;
        list.Add(last + tan * ropeTipExtension);
        return list;
    }

    void EvaluateCut()
    {
        cutPending = false;
        if (!pulling || lockTimer > 0f) return;
        var mine = RopePoints();
        if (mine == null) return;
        foreach (var o in all)
        {
            if (o == null || o == this) continue;
            var theirs = o.RopePoints();
            if (theirs == null) continue;
            for (int i = 0; i < mine.Count - 1; i++)
                for (int j = 0; j < theirs.Count - 1; j++)
                    if (SegDist(mine[i], mine[i + 1], theirs[j], theirs[j + 1]) < ropeTouchRadius)
                    {
                        cutPoint = (mine[i] + mine[i + 1]) * 0.5f;
                        cutPending = true;
                        return;
                    }
        }
    }

    static void ResolveCuts()
    {
        foreach (var p in all) if (p != null) p.EvaluateCut();      // once herkes degerlendirilir
        foreach (var p in all)                                      // sonra hepsine birden uygulanir
        {
            if (p == null || !p.cutPending) continue;
            FX.Burst(p.cutPoint, new Color(1f, 0.75f, 0.3f), 36, 7f, 0.16f, 0.6f);
            FX.Burst(p.cutPoint, Color.white, 14, 4f, 0.12f, 0.4f);
            p.LoseTongue();
        }
    }

    void LateUpdate()
    {
        if (all.Count > 0 && all[0] == this) ResolveCuts();
    }

    void LoseTongue()
    {
        lockTimer = tongueLockTime;
        SoundFX.Play("cut");
        CameraShake.Add(0.55f);
        fire = false;
        pulling = false;
        ropeLen = 0f;
        Debug.Log(name + " dilini kaybetti (" + tongueLockTime + " sn)");
    }

    static float Cr(Vector2 p, Vector2 q, Vector2 r) { return (q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x); }

    static float PointSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    static float SegDist(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float d1 = Cr(c, d, a), d2 = Cr(c, d, b), d3 = Cr(a, b, c), d4 = Cr(a, b, d);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return 0f;
        return Mathf.Min(Mathf.Min(PointSeg(a, c, d), PointSeg(b, c, d)), Mathf.Min(PointSeg(c, a, b), PointSeg(d, a, b)));
    }

    void DrawRope()
    {
        ropeLen = Mathf.MoveTowards(ropeLen, fire ? targetLen : 0f, ropeSpeed * Time.deltaTime);
        if (ropeLen < 0.05f) { rope.enabled = false; return; }
        rope.enabled = true;
        Vector2 origin = Origin();
        Vector2 toEnd = aimEnd - origin;
        Vector2 dir = toEnd.sqrMagnitude > 0.0001f ? toEnd.normalized : facing;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        int n = rope.positionCount;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float wave = Mathf.Sin(t * 18f - Time.time * 14f) * 0.12f * Mathf.Sin(t * Mathf.PI);
            Vector2 p = origin + dir * (ropeLen * t) + perp * wave;
            rope.SetPosition(i, new Vector3(p.x, p.y, 0f));
            ropePts[i] = p;
        }
        rope.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 0.95f);
        rope.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 0.55f);
    }

    void ReadInput()
    {
        float rot;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) { rotInput = 0f; fire = false; return; }
        if (playerId == 1)
        {
            rot = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
            fire = k.dKey.isPressed;
        }
        else
        {
            rot = (k.downArrowKey.isPressed ? 1 : 0) - (k.upArrowKey.isPressed ? 1 : 0);
            fire = k.leftArrowKey.isPressed;
        }
#else
        if (playerId == 1)
        {
            rot = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            fire = Input.GetKey(KeyCode.D);
        }
        else
        {
            rot = (Input.GetKey(KeyCode.DownArrow) ? 1 : 0) - (Input.GetKey(KeyCode.UpArrow) ? 1 : 0);
            fire = Input.GetKey(KeyCode.LeftArrow);
        }
#endif
        rotInput = rot;
    }
}
