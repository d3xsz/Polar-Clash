using UnityEngine;

// Hareketli duvar cifti: biri (x, y), digeri orta noktaya gore (-x, -y) konumunda.
// Ikisi zit yonde dikey gidip gelir, boylece iki oyuncu da ayni sekilde etkilenir.
public class MovingWall : MonoBehaviour
{
    public float amplitude = 3f;   // gidis mesafesi (yukari/asagi)
    public float period = 7f;      // tam tur suresi (saniye)
    public float phase = 0f;

    Rigidbody2D rb;
    float x0, sign;

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
        var p = GameObject.Find("HareketliDuvarlar");
        if (p == null) return;
        foreach (Transform t in p.transform)
            if (t.GetComponent<MovingWall>() == null) t.gameObject.AddComponent<MovingWall>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sign = transform.GetSiblingIndex() % 2 == 0 ? 1f : -1f;
        x0 = Mathf.Abs(transform.position.x);
    }

    void FixedUpdate()
    {
        float y = amplitude * Mathf.Sin(6.2831853f * Time.fixedTime / period + phase);
        rb.MovePosition(new Vector2(sign * x0, sign * y));
    }
}
