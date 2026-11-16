using UnityEngine;
using System.Collections.Generic;

public static class SoundFX
{
    const int Rate = 44100;
    const float TwoPi = 6.2831853f;
    static AudioSource src;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        clips.Clear();
        var go = new GameObject("SoundFX");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;

        // Ip atma: kisa yukselen vizilti
        clips["fire"] = Make(0.15f, t => Mathf.Sin(TwoPi * (300f * t + 1000f * t * t)) * Mathf.Exp(-t * 14f) * 0.25f);

        // Top toplama: iki notalik tik
        clips["collect"] = Make(0.2f, t =>
        {
            float ft = t < 0.09f ? 660f : 990f;
            float tt = t < 0.09f ? t : t - 0.09f;
            return Mathf.Sin(TwoPi * ft * t) * Mathf.Exp(-tt * 18f) * 0.3f;
        });

        // Duvar uyarisi: yumusak bip
        clips["wallwarn"] = Make(0.08f, t => Mathf.Sin(TwoPi * 880f * t) * Mathf.Exp(-t * 25f) * 0.2f);

        // Duvar yukselmesi: alcak gumbuz
        clips["wallrise"] = Make(0.35f, t =>
        {
            float body = Mathf.Sin(TwoPi * (120f * t - 150f * t * t)) * 0.6f;
            float noise = (Random.value * 2f - 1f) * 0.4f * Mathf.Exp(-t * 20f);
            return (body + noise) * Mathf.Exp(-t * 9f) * 0.5f;
        });

        // Dil kopmasi: pesli vizilti
        clips["cut"] = Make(0.4f, t =>
        {
            float sq = Mathf.Sin(TwoPi * 140f * t) >= 0f ? 1f : -1f;
            return (sq * 0.15f + (Random.value * 2f - 1f) * 0.05f) * Mathf.Exp(-t * 5f);
        });

        // Mac sonu: yukselen dort nota
        clips["end"] = Make(0.9f, t =>
        {
            float[] notes = { 523f, 659f, 784f, 1047f };
            int idx = Mathf.Min(3, (int)(t / 0.2f));
            float tt = t - idx * 0.2f;
            return Mathf.Sin(TwoPi * notes[idx] * t) * Mathf.Exp(-tt * 6f) * 0.3f;
        });

        // UI: tiklama ve uzerine gelme
        clips["click"] = Make(0.07f, t => Mathf.Sin(TwoPi * (520f * t + 2600f * t * t)) * Mathf.Exp(-t * 45f) * 0.28f);
        clips["hover"] = Make(0.04f, t => Mathf.Sin(TwoPi * 1250f * t) * Mathf.Exp(-t * 90f) * 0.08f);
    }

    static AudioClip Make(float dur, System.Func<float, float> f)
    {
        int n = (int)(dur * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = f(i / (float)Rate);
        var c = AudioClip.Create("fx", n, 1, Rate, false);
        c.SetData(data, 0);
        return c;
    }

    public static void Play(string name, float vol = 1f)
    {
        AudioClip c;
        if (src != null && clips.TryGetValue(name, out c)) src.PlayOneShot(c, vol);
    }
}
