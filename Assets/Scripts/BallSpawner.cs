using UnityEngine;
using System.Collections.Generic;

// Toplar nokta simetrik ciftler halinde dogar: biri p noktasinda, karsi renkte olani -p noktasinda.
// Boylece iki oyuncunun hedef topu da ayni uzaklik/dizilimde olur.
public class BallSpawner : MonoBehaviour
{
    public int normalPerColor = 8;
    public int bigPerColor = 1;
    public Vector2 areaHalfSize = new Vector2(7.8f, 4.0f);
    public float gap = 0.6f;
    public float minPlayerDistance = 2.5f;

    Transform parent;
    readonly List<Vector3> placed = new List<Vector3>();   // x, y, yaricap
    readonly List<Vector2> players = new List<Vector2>();

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
        if (Object.FindFirstObjectByType<BallSpawner>() == null)
            new GameObject("BallSpawner").AddComponent<BallSpawner>();
    }

    void Start()
    {
        var parentGo = GameObject.Find("Toplar");
        if (parentGo == null) return;
        parent = parentGo.transform;

        GameObject nr = null, nb = null, br = null, bb = null;
        var originals = new List<GameObject>();
        foreach (Transform t in parent)
        {
            originals.Add(t.gameObject);
            if (nr == null && t.name == "Top_Kirmizi") nr = t.gameObject;
            else if (nb == null && t.name == "Top_Mavi") nb = t.gameObject;
            else if (br == null && t.name == "BuyukTop_Kirmizi") br = t.gameObject;
            else if (bb == null && t.name == "BuyukTop_Mavi") bb = t.gameObject;
        }
        if (nr == null || nb == null) return;

        var p1 = GameObject.Find("P1"); var p2 = GameObject.Find("P2");
        if (p1 != null) players.Add(p1.transform.position);
        if (p2 != null) players.Add(p2.transform.position);

        for (int i = 0; i < bigPerColor; i++) PlacePair(br, bb);
        for (int i = 0; i < normalPerColor; i++) PlacePair(nr, nb);

        foreach (var o in originals) Destroy(o);
    }

    void PlacePair(GameObject a, GameObject b)
    {
        if (a == null || b == null) return;
        float r = a.transform.lossyScale.x * 0.5f;
        for (int k = 0; k < 120; k++)
        {
            Vector2 pos = new Vector2(Random.Range(-areaHalfSize.x, areaHalfSize.x), Random.Range(-areaHalfSize.y, areaHalfSize.y));
            if (pos.magnitude < r + 0.5f * gap + r) continue;              // cift birbirine cok yakin olmasin
            bool ok = true;
            foreach (var pp in players)
                if (Vector2.Distance(pos, pp) < minPlayerDistance) { ok = false; break; }
            if (!ok) continue;
            foreach (var q in placed)
                if (Vector2.Distance(pos, new Vector2(q.x, q.y)) < r + q.z + gap) { ok = false; break; }
            if (!ok) continue;

            bool swap = Random.value < 0.5f;
            Spawn(swap ? b : a, pos, r);
            Spawn(swap ? a : b, -pos, r);
            return;
        }
    }

    void Spawn(GameObject src, Vector2 pos, float r)
    {
        placed.Add(new Vector3(pos.x, pos.y, r));
        var go = Instantiate(src, parent);
        go.transform.position = new Vector3(pos.x, pos.y, src.transform.position.z);
        go.SetActive(true);
    }
}
