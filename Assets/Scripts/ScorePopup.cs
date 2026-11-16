using UnityEngine;

// Puan aninda: toplanan noktada halka dalgasi + yukari suzulen '+N' yazisi
public class ScorePopup : MonoBehaviour
{
    TextMesh tm;
    float t;
    Vector3 start;
    Color col;
    const float Life = 0.9f;

    public static void Show(Vector2 pos, Color c, int points)
    {
        Color bright = Color.Lerp(c, Color.white, 0.35f);
        FX.Ring(pos, bright, 30, 6.5f, 0.11f, 0.55f);

        var go = new GameObject("ScorePopup");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var p = go.AddComponent<ScorePopup>();
        p.Init(points, bright);
    }

    void Init(int points, Color c)
    {
        col = c;
        start = transform.position;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm = gameObject.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = "+" + points;
        tm.fontSize = 64;
        tm.characterSize = 0.12f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = c;
        var mr = GetComponent<MeshRenderer>();
        mr.material = font.material;
        mr.sortingOrder = 30;
        transform.localScale = Vector3.one * 0.6f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / Life);
        float ease = 1f - (1f - k) * (1f - k);
        transform.position = start + Vector3.up * (1.6f * ease);
        float pop = k < 0.2f ? Mathf.Lerp(0.6f, 1.25f, k / 0.2f) : Mathf.Lerp(1.25f, 1f, (k - 0.2f) / 0.8f);
        transform.localScale = Vector3.one * pop;
        tm.color = new Color(col.r, col.g, col.b, 1f - Mathf.Clamp01((k - 0.5f) / 0.5f));
        if (k >= 1f) Destroy(gameObject);
    }
}
