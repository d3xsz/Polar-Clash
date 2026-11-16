using UnityEngine;

// Tek paylasimli parcacik sistemi: patlama ve kivilcim efektleri
public static class FX
{
    static ParticleSystem sys;

    static void Ensure()
    {
        if (sys != null) return;
        var go = new GameObject("FX");
        Object.DontDestroyOnLoad(go);
        sys = go.AddComponent<ParticleSystem>();

        var main = sys.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 3000;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;
        var em = sys.emission; em.enabled = false;
        var sh = sys.shape; sh.enabled = false;

        var col = sys.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = sys.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.2f - d)));
        }
        tex.Apply();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = tex;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        r.sortingOrder = 20;
        sys.Play();
    }

    public static void Burst(Vector2 pos, Color c, int count, float speed, float size, float life)
    {
        Ensure();
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            ep.position = new Vector3(pos.x, pos.y, 0f);
            ep.velocity = dir * (speed * Random.Range(0.35f, 1f));
            ep.startLifetime = life * Random.Range(0.6f, 1f);
            ep.startSize = size * Random.Range(0.6f, 1f);
            ep.startColor = c;
            sys.Emit(ep, 1);
        }
    }

    public static void Ring(Vector2 pos, Color c, int count, float speed, float size, float life)
    {
        Ensure();
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float a = i * 6.2831853f / count;
            ep.position = new Vector3(pos.x, pos.y, 0f);
            ep.velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * speed;
            ep.startLifetime = life; ep.startSize = size; ep.startColor = c;
            sys.Emit(ep, 1);
        }
    }

    public static void Confetti(Vector2 pos, int count)
    {
        Ensure();
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            ep.position = new Vector3(pos.x, pos.y, 0f);
            ep.velocity = dir * Random.Range(3f, 8f);
            ep.startLifetime = Random.Range(0.7f, 1.1f);
            ep.startSize = Random.Range(0.12f, 0.2f);
            ep.startColor = Color.HSVToRGB(Random.value, 0.7f, 1f);
            sys.Emit(ep, 1);
        }
    }

    // Bes kollu yildiz: her kolda siralanan parcaciklar
    public static void Stars(Vector2 pos, Color c, int perSpoke)
    {
        Ensure();
        var ep = new ParticleSystem.EmitParams();
        for (int k = 0; k < 5; k++)
        {
            float a = (90f + k * 72f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            for (int m = 0; m < perSpoke; m++)
            {
                ep.position = new Vector3(pos.x, pos.y, 0f);
                ep.velocity = dir * (2f + m * 0.9f);
                ep.startLifetime = 0.55f;
                ep.startSize = Mathf.Max(0.07f, 0.17f - m * 0.008f);
                ep.startColor = m % 2 == 0 ? Color.white : c;
                sys.Emit(ep, 1);
            }
        }
    }

    // Iki ic ice halka: takim rengi ve altin
    public static void DoubleRing(Vector2 pos, Color c, float scale)
    {
        Ring(pos, c, (int)(24 * scale), 5f, 0.13f, 0.55f);
        Ring(pos, new Color(1f, 0.85f, 0.4f), (int)(32 * scale), 9f, 0.12f, 0.6f);
    }

    public static void Spark(Vector2 pos, Color c) { Burst(pos, c, 3, 4f, 0.08f, 0.3f); }
}
