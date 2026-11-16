using UnityEngine;

// Kamera sarsintisi: "travma" degeri eklenir, zamanla soner. Sarsinti = travma^2 (kucuk darbeler hafif, buyukler belirgin).
public class CameraShake : MonoBehaviour
{
    static CameraShake inst;
    public float maxOffset = 0.35f;
    public float maxAngle = 1.2f;
    public float decay = 1.8f;

    float trauma;
    Vector3 basePos;
    Quaternion baseRot;

    public static void Add(float amount)
    {
        if (inst == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            inst = cam.GetComponent<CameraShake>();
            if (inst == null) inst = cam.gameObject.AddComponent<CameraShake>();
        }
        inst.trauma = Mathf.Clamp01(inst.trauma + amount);
    }

    void Awake()
    {
        inst = this;
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
    }

    void OnDestroy() { if (inst == this) inst = null; }

    void LateUpdate()
    {
        if (trauma <= 0f) { transform.localPosition = basePos; transform.localRotation = baseRot; return; }
        trauma = Mathf.Max(0f, trauma - decay * Time.deltaTime);   // duraklatmada (timeScale 0) donar
        float s = trauma * trauma, t = Time.time * 30f;
        float x = (Mathf.PerlinNoise(t, 0.1f) * 2f - 1f) * maxOffset * s;
        float y = (Mathf.PerlinNoise(0.7f, t) * 2f - 1f) * maxOffset * s;
        float a = (Mathf.PerlinNoise(t, 3.3f) * 2f - 1f) * maxAngle * s;
        transform.localPosition = basePos + new Vector3(x, y, 0f);
        transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, a);
    }
}
