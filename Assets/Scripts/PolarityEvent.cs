using UnityEngine;
using System.Collections;

// Ters Kutup: belirli araliklarla cekme/itme yer degistirir (iki oyuncu icin de ayni anda).
public class PolarityEvent : MonoBehaviour
{
    public static int Sign = 1;      // 1 = normal, -1 = ters
    public static bool Warning;      // ters kutup birazdan basliyor
    public float minWait = 15f, maxWait = 25f;
    public float warnTime = 1.5f;
    public float duration = 6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneReloaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneReloaded;
        Attach();
    }

    static void OnSceneReloaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) { Attach(); }

    static void Attach()
    {
        Sign = 1;
        Warning = false;
        if (Object.FindFirstObjectByType<PolarityEvent>() == null)
            new GameObject("PolarityEvent").AddComponent<PolarityEvent>();
    }

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            SoundFX.Play("wallwarn", 0.6f);
            Warning = true;
            yield return new WaitForSeconds(warnTime);
            Warning = false;
            Sign = -1;
            SoundFX.Play("wallrise");
            Debug.Log("TERS KUTUP basladi");
            yield return new WaitForSeconds(duration);
            Sign = 1;
            SoundFX.Play("wallrise", 0.5f);
            Debug.Log("TERS KUTUP bitti");
        }
    }
}
