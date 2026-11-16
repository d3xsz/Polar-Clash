using UnityEngine;

// Toplarin arkasinda hiza bagli neon iz birakir (hizli = uzun ve kalin, yavas = kisa ve ince).
// Ball.Awake tarafindan otomatik eklenir; sahnede ayri bir ayar gerekmez.
// Iz, topun cocugu olan ayri bir objede durur (ayni objede baska renderer ile cakismasin diye).
public class BallTrail : MonoBehaviour
{
    const float MinSpeed = 0.3f;
    const float FullSpeed = 6f;

    static Material sharedMat;

    TrailRenderer trail;
    float baseWidth;
    Vector3 lastPos;

    static Material GetMaterial()
    {
        if (sharedMat != null) return sharedMat;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) return null;
        sharedMat = new Material(sh);
        return sharedMat;
    }

    void Awake()
    {
        var mat = GetMaterial();
        if (mat == null) { enabled = false; return; }

        var sr = GetComponent<SpriteRenderer>();
        Color c = sr != null ? sr.color : Color.white;
        c = Color.Lerp(c, Color.white, 0.15f);

        baseWidth = transform.lossyScale.x * 0.8f;
        lastPos = transform.position;

        var go = new GameObject("Iz");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        if (trail == null) { enabled = false; return; }

        trail.sharedMaterial = mat;
        trail.time = 0.3f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 4;
        trail.alignment = LineAlignment.View;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        if (sr != null) { trail.sortingLayerID = sr.sortingLayerID; trail.sortingOrder = sr.sortingOrder - 1; }

        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.widthMultiplier = 0f;
        trail.emitting = false;
    }

    void Update()
    {
        if (trail == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;

        float k = Mathf.Clamp01(speed / FullSpeed);
        trail.emitting = speed > MinSpeed;
        trail.time = Mathf.Lerp(0.15f, 0.5f, k);
        float target = baseWidth * Mathf.Lerp(0.35f, 1f, k);
        trail.widthMultiplier = Mathf.Lerp(trail.widthMultiplier, target, dt * 10f);
    }
}
