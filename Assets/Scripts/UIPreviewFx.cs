using UnityEngine;
using UnityEngine.UI;

// Kozmetik ekraninda iz ve efekt onizlemesi: mıknatis sallanir, uclarindan iz birakir, efekt periyodik oynar
public class UIPreviewFx : MonoBehaviour
{
    const int N = 110;
    RectTransform mik;
    float baseAngle, spawnTimer, demoTimer;
    Color col;
    int trailMode, fxMode;
    readonly Image[] img = new Image[N];
    readonly Vector2[] pos = new Vector2[N];
    readonly Vector2[] vel = new Vector2[N];
    readonly float[] life = new float[N], max = new float[N], size = new float[N];
    readonly Color[] pc = new Color[N];
    readonly bool[] drag = new bool[N];

    public void Setup(RectTransform magnet, RectTransform clip, float baseAng, Color c)
    {
        mik = magnet; baseAngle = baseAng; col = c;
        for (int i = 0; i < N; i++)
        {
            img[i] = UIKit.Dot(clip, "P", Vector2.zero, 8f, Color.white);
            img[i].enabled = false;
        }
    }

    public void SetModes(int trail, int fx)
    {
        trailMode = trail;
        if (fx != fxMode) { fxMode = fx; demoTimer = 0f; }
    }

    void Emit(Vector2 p, Vector2 v, float l, float s, Color c, bool d)
    {
        for (int i = 0; i < N; i++)
            if (life[i] <= 0f)
            {
                pos[i] = p; vel[i] = v; life[i] = max[i] = l; size[i] = s; pc[i] = c; drag[i] = d;
                img[i].enabled = true;
                return;
            }
    }

    void Demo()
    {
        Vector2 o = new Vector2(0f, -70f);
        Color light = Color.Lerp(col, Color.white, 0.3f);
        if (fxMode == 3)
        {
            for (int k = 0; k < 5; k++)
            {
                float a = (90f + k * 72f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (int m = 0; m < 6; m++) Emit(o, dir * (30f + m * 14f), 0.7f, Mathf.Max(5f, 11f - m), m % 2 == 0 ? Color.white : light, true);
            }
            return;
        }
        if (fxMode == 4)
        {
            for (int i = 0; i < 24; i++) { float a = i * 6.2831853f / 24f; Emit(o, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 80f, 0.7f, 9f, light, true); }
            for (int i = 0; i < 30; i++) { float a = i * 6.2831853f / 30f; Emit(o, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 140f, 0.7f, 8f, new Color(1f, 0.85f, 0.4f), true); }
            return;
        }
        if (fxMode == 0)
        {
            for (int i = 0; i < 18; i++)
                Emit(o, Random.insideUnitCircle.normalized * Random.Range(70f, 120f), Random.Range(0.6f, 0.9f), 9f, light, true);
        }
        else if (fxMode == 1)
        {
            for (int i = 0; i < 26; i++)
            {
                float a = i * 6.2831853f / 26f;
                Emit(o, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 110f, 0.7f, 9f, light, true);
            }
        }
        else
        {
            for (int i = 0; i < 34; i++)
                Emit(o, Random.insideUnitCircle.normalized * Random.Range(40f, 130f), Random.Range(0.7f, 1.1f), Random.Range(8f, 12f), Color.HSVToRGB(Random.value, 0.7f, 1f), true);
        }
    }

    void Update()
    {
        if (mik == null) return;
        float dt = Time.unscaledDeltaTime;
        float ang = baseAngle + Mathf.Sin(Time.unscaledTime * 1.6f) * 38f;
        mik.localRotation = Quaternion.Euler(0f, 0f, ang);

        if (trailMode > 0)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = 0.02f;
                float l = trailMode == 1 ? 0.35f : (trailMode == 3 ? 0.7f : (trailMode == 4 ? 0.5f : 0.9f));
                float s = trailMode == 1 ? 6f : (trailMode == 4 ? 5f : 8f);
                Color c = trailMode == 3 ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.6f, 1f), 0.65f, 1f) : (trailMode == 4 ? new Color(1f, 0.88f, 0.5f) : Color.Lerp(col, Color.white, 0.4f));
                Quaternion q = Quaternion.Euler(0f, 0f, ang);
                Emit((Vector2)(q * new Vector3(44f, 57f, 0f)), Vector2.zero, l, s, c, false);
                Emit((Vector2)(q * new Vector3(44f, -57f, 0f)), Vector2.zero, l, s, c, false);
            }
        }

        demoTimer -= dt;
        if (demoTimer <= 0f) { demoTimer = 1.8f; Demo(); }

        for (int i = 0; i < N; i++)
        {
            if (life[i] <= 0f) continue;
            life[i] -= dt;
            if (life[i] <= 0f) { img[i].enabled = false; continue; }
            pos[i] += vel[i] * dt;
            if (drag[i]) vel[i] *= Mathf.Max(0f, 1f - 2f * dt);
            float k = life[i] / max[i];
            var c2 = pc[i]; c2.a = k;
            img[i].color = c2;
            float sz = size[i] * (0.4f + 0.6f * k);
            img[i].rectTransform.sizeDelta = new Vector2(sz, sz);
            img[i].rectTransform.anchoredPosition = pos[i];
        }
    }
}
