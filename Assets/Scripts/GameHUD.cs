using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameHUD : MonoBehaviour
{
    public float matchTime = 300f;

    static readonly Color Red = new Color(1.00f, 0.42f, 0.45f);
    static readonly Color Blue = new Color(0.38f, 0.64f, 1.00f);
    static readonly Color Amber = new Color(1.00f, 0.78f, 0.32f);
    static readonly Color Lav = new Color(0.74f, 0.70f, 0.96f);
    static readonly Color Ink = new Color(0.16f, 0.09f, 0.02f);
    static readonly Color CardFill = new Color(0.085f, 0.075f, 0.185f, 0.94f);
    static readonly Color Dark = new Color(0.05f, 0.04f, 0.12f, 0.95f);

    float t, checkTimer;
    bool ended, inMenu = true, p1Done, p2Done;
    PlayerController p1, p2;
    Text p1Text, p2Text, timerText;
    RectTransform p1Pill, p2Pill, timerPill;
    int lastP1, lastP2;
    float punch1, punch2;

    GameObject menuPanel;
    CanvasGroup startBtn;
    Image[] menuStars; float[] menuStarA;
    readonly Text[][] values = new Text[3][];
    readonly Image[][][] dots = new Image[3][][];
    readonly Image[] previewBody = new Image[3];
    readonly Image[] previewTips = new Image[3];
    readonly Image[] previewRimL = new Image[3];
    readonly Image[] previewRimD = new Image[3];
    readonly UIPreviewFx[] previewFx = new UIPreviewFx[3];

    GameObject pausePanel;
    bool paused;
    CanvasGroup polarityBanner;
    Text polarityText;

    GameObject endPanel;
    Image[] endStars; float[] endStarA;
    Text endTitle, endScoreL, endScoreR;
    Image endGlow, endLine;

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
        var hud = GameObject.Find("HUD");
        if (hud != null && hud.GetComponent<GameHUD>() == null) hud.AddComponent<GameHUD>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        t = matchTime;
        p1 = GameObject.Find("P1").GetComponent<PlayerController>();
        p2 = GameObject.Find("P2").GetComponent<PlayerController>();

        // Eski altigen skor kutulari artik kullanilmiyor
        foreach (var n in new[] { "Hud_P1", "Hud_Timer", "Hud_P2" })
        {
            var tr = transform.Find(n);
            if (tr != null) tr.gameObject.SetActive(false);
        }

        EnsureEventSystem();
        Cosmetics.Load();
        Cosmetics.Apply(p1.gameObject, 1);      // klasik sprite'in alinmasi icin once uygula
        Cosmetics.Apply(p2.gameObject, 2);
        if (p1.GetComponent<MagnetFx>() == null) p1.gameObject.AddComponent<MagnetFx>();
        if (p2.GetComponent<MagnetFx>() == null) p2.gameObject.AddComponent<MagnetFx>();
        BuildTopBar();
        BuildMenu();
        BuildEndPanel();
        BuildPausePanel();
    }

    // Fare ile UI kullanimi icin EventSystem + raycaster
    void EnsureEventSystem()
    {
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---------- Ust cubuk ----------
    void BuildTopBar()
    {
        var root = UIKit.Stretch("UstCubuk", transform).transform;
        p1Pill = ScorePill(root, "KUZEY", Red, new Vector2(-300f, 468f), out p1Text);
        p2Pill = ScorePill(root, "GÜNEY", Blue, new Vector2(300f, 468f), out p2Text);
        var tp = UIKit.Pill(root, "Sure", new Vector2(0f, 468f), new Vector2(240f, 76f), CardFill, new Color(1f, 1f, 1f, 0.35f));
        timerPill = tp;

        // Fare icin duraklat dugmesi
        var pz = UIKit.Pill(root, "DuraklatDugme", new Vector2(880f, 468f), new Vector2(76f, 76f), CardFill, new Color(1f, 1f, 1f, 0.35f));
        UIKit.Block(pz, "Cubuk1", new Vector2(-9f, 0f), new Vector2(9f, 30f), Color.white);
        UIKit.Block(pz, "Cubuk2", new Vector2(9f, 0f), new Vector2(9f, 30f), Color.white);
        UIKit.Clickable(pz, () => { if (!inMenu && !ended && !paused) SetPaused(true); }, null, 1.1f);
        timerText = UIKit.Label(tp, "Yazi", "05:00", 48, Color.white, Vector2.zero, new Vector2(220f, 70f), TextAnchor.MiddleCenter, true);

        // Ters Kutup uyarisi
        var pb = UIKit.Pill(root, "TersKutup", new Vector2(0f, 392f), new Vector2(640f, 56f), Dark, Amber);
        polarityText = UIKit.Label(pb, "Yazi", "", 28, Amber, Vector2.zero, new Vector2(620f, 50f), TextAnchor.MiddleCenter, true);
        polarityBanner = pb.gameObject.AddComponent<CanvasGroup>();
        polarityBanner.alpha = 0f;
    }

    void UpdatePolarityBanner()
    {
        bool active = PolarityEvent.Sign < 0, warn = PolarityEvent.Warning;
        if (!active && !warn) { polarityBanner.alpha = 0f; return; }
        polarityText.text = UIKit.T(active ? "TERS KUTUP!  ÇEKME / İTME TERS" : "TERS KUTUP GELİYOR");
        float speed = active ? 3f : 7f;
        polarityBanner.alpha = 0.55f + 0.45f * Mathf.PingPong(Time.time * speed, 1f);
    }

    RectTransform ScorePill(Transform parent, string name, Color col, Vector2 pos, out Text score)
    {
        var p = UIKit.Pill(parent, "Skor" + name, pos, new Vector2(340f, 76f), CardFill, new Color(col.r, col.g, col.b, 0.85f));
        UIKit.Dot(p, "Nokta", new Vector2(-135f, 0f), 16f, col);
        UIKit.Label(p, "Isim", name, 30, Color.white, new Vector2(-42f, 0f), new Vector2(150f, 50f), TextAnchor.MiddleLeft, true);
        score = UIKit.Label(p, "Puan", "0", 48, col, new Vector2(118f, 0f), new Vector2(120f, 60f), TextAnchor.MiddleCenter, true);
        return p;
    }

    // Skor degisince kutu ziplar, son 10 saniyede sure kirmizi nabiz atar
    void AnimateTopBar()
    {
        if (p1.score != lastP1) { lastP1 = p1.score; punch1 = 1f; }
        if (p2.score != lastP2) { lastP2 = p2.score; punch2 = 1f; }
        punch1 = Mathf.MoveTowards(punch1, 0f, Time.deltaTime * 3.5f);
        punch2 = Mathf.MoveTowards(punch2, 0f, Time.deltaTime * 3.5f);
        p1Pill.localScale = Vector3.one * (1f + 0.22f * Punch(punch1));
        p2Pill.localScale = Vector3.one * (1f + 0.22f * Punch(punch2));

        if (t <= 10.5f && t > 0f)
        {
            float beat = 1f - (t % 1f);                        // her saniye basinda vurus
            float k = beat * beat;
            timerText.color = Color.Lerp(Color.white, Red, 0.6f + 0.4f * k);
            timerPill.localScale = Vector3.one * (1f + 0.12f * k);
        }
        else
        {
            timerText.color = Color.white;
            timerPill.localScale = Vector3.one;
        }
    }

    static float Punch(float x)
    {
        // 1 -> 0 arasi: hizli buyume, yaylanarak oturma
        return Mathf.Sin(x * Mathf.PI) * x;
    }

    // ---------- Kozmetik secim ekrani ----------
    void BuildMenu()
    {
        Time.timeScale = 0f;
        menuPanel = UIKit.Stretch("KozmetikMenu", transform);
        var m = menuPanel.transform;
        menuStars = UIKit.Background(m);
        menuStarA = AlphaOf(menuStars);

        // Oyun adi: iki kutbun renkleriyle
        UIKit.Label(m, "BaslikPolar", "POLAR", 96, Red, new Vector2(-312f, 412f), new Vector2(600f, 120f), TextAnchor.MiddleRight, true);
        UIKit.Label(m, "BaslikClash", "CLASH", 96, Blue, new Vector2(312f, 412f), new Vector2(600f, 120f), TextAnchor.MiddleLeft, true);
        var tag = UIKit.Pill(m, "Etiket", new Vector2(0f, 338f), new Vector2(720f, 44f), Dark, new Color(1f, 1f, 1f, 0.18f));
        UIKit.Dot(tag, "Nokta", new Vector2(-325f, 0f), 12f, Amber);
        UIKit.Label(tag, "Yazi", "Kozmetik  ·  yalnızca görsel  ·  tıkla ya da tuşa bas", 24, Lav, new Vector2(10f, 0f), new Vector2(680f, 40f), TextAnchor.MiddleCenter, false);

        BuildCard(1, "KUZEY", Red, new Vector2(-430f, -30f), new[] { "Z", "X", "C" });
        BuildCard(2, "GÜNEY", Blue, new Vector2(430f, -30f), new[] { "J", "K", "L" });

        var rules = UIKit.Pill(m, "Kurallar", new Vector2(0f, -318f), new Vector2(1240f, 50f), Dark, new Color(1f, 1f, 1f, 0.18f));
        UIKit.Label(rules, "Yazi", "Rakip renkteki topları kendine çek  ·  kendi renklerini rakibe it  ·  ipler değerse dil kopar", 24, Color.white, Vector2.zero, new Vector2(1220f, 44f), TextAnchor.MiddleCenter, false);
#if !UNITY_WEBGL
        var quit = UIKit.Pill(m, "Cikis", new Vector2(0f, -482f), new Vector2(260f, 46f), Dark, new Color(1f, 1f, 1f, 0.2f));
        UIKit.Label(quit, "Yazi", "ESC  ·  OYUNDAN ÇIK", 20, Lav, Vector2.zero, new Vector2(250f, 40f), TextAnchor.MiddleCenter, false);
        UIKit.Clickable(quit, Quit);
#endif

        var btn = UIKit.Pill(m, "Baslat", new Vector2(0f, -400f), new Vector2(420f, 78f), Amber, new Color(1f, 1f, 1f, 0.6f));
        startBtn = btn.gameObject.AddComponent<CanvasGroup>();
        var key = UIKit.Pill(btn, "Tus", new Vector2(-135f, 0f), new Vector2(112f, 46f), new Color(0.2f, 0.12f, 0.02f, 0.28f), new Color(0f, 0f, 0f, 0f));
        UIKit.Label(key, "Yazi", "SPACE", 24, Ink, Vector2.zero, new Vector2(110f, 44f), TextAnchor.MiddleCenter, true);
        UIKit.Label(btn, "Yazi", "MAÇI BAŞLAT", 34, Ink, new Vector2(55f, 0f), new Vector2(260f, 60f), TextAnchor.MiddleCenter, true);
        UIKit.Clickable(btn, StartMatch);
        RefreshMenu();
    }

    float[] AlphaOf(Image[] imgs)
    {
        var a = new float[imgs.Length];
        for (int i = 0; i < imgs.Length; i++) a[i] = imgs[i].color.a;
        return a;
    }

    void BuildCard(int id, string name, Color col, Vector2 center, string[] keys)
    {
        var card = UIKit.Panel(menuPanel.transform, "Kart" + id, center, new Vector2(780f, 500f), CardFill, new Color(col.r, col.g, col.b, 0.75f), new Color(col.r, col.g, col.b, 0.32f));

        // Isim etiketi (oyun ici isim etiketi gibi)
        var tag = UIKit.Pill(card, "Ad", new Vector2(-255f, 205f), new Vector2(240f, 60f), Dark, new Color(col.r, col.g, col.b, 0.7f));
        UIKit.Dot(tag, "Nokta", new Vector2(-88f, 0f), 18f, col);
        UIKit.Label(tag, "Isim", name, 34, Color.white, new Vector2(18f, 0f), new Vector2(170f, 56f), TextAnchor.MiddleCenter, true);

        string[] labels = { "TASARIM", "İZ", "EFEKT" };
        values[id] = new Text[3];
        dots[id] = new Image[3][];
        for (int r = 0; r < 3; r++)
        {
            float y = 100f - r * 104f;
            var row = UIKit.Panel(card, "Satir" + r, new Vector2(-120f, y), new Vector2(480f, 88f), new Color(0.05f, 0.04f, 0.13f, 0.7f), new Color(1f, 1f, 1f, 0.10f), new Color(0f, 0f, 0f, 0f));
            UIKit.KeyCap(row, keys[r], new Vector2(-190f, 0f), 56f);
            UIKit.Label(row, "Etiket", labels[r], 20, Lav, new Vector2(10f, 20f), new Vector2(300f, 26f), TextAnchor.MiddleLeft, false);
            values[id][r] = UIKit.Label(row, "Deger", "", 40, Color.white, new Vector2(10f, -12f), new Vector2(300f, 50f), TextAnchor.MiddleLeft, true);
            int pid = id, prow = r;
            UIKit.Clickable(row, () => Cycle(pid, prow, 1), () => Cycle(pid, prow, -1), 1.03f);
            int count = OptionCount(r);
            float step = count > 6 ? 17f : 20f;
            dots[id][r] = new Image[count];
            for (int d = 0; d < count; d++)
            {
                var dp = new Vector2((count > 6 ? 165f : 175f) + (d - (count - 1) / 2f) * step, 0f);
                int pidx = d;
                var hit = UIKit.HitArea(row, "NoktaAlan", dp, new Vector2(step, 40f));
                UIKit.Clickable(hit, () => Select(pid, prow, pidx), null, 1.25f);
                dots[id][r][d] = UIKit.Dot(hit, "Nokta", Vector2.zero, 12f, Lav);
            }
        }

        // Onizleme: arkasinda yumusak renkli isik
        var pv = UIKit.Panel(card, "Onizleme", new Vector2(270f, -5f), new Vector2(190f, 300f), new Color(0.04f, 0.03f, 0.10f, 0.9f), new Color(col.r, col.g, col.b, 0.35f), new Color(0f, 0f, 0f, 0f));
        UIKit.Soft(pv, "Isik", Vector2.zero, new Vector2(240f, 300f), new Color(col.r, col.g, col.b, 0.40f), 0f);
        UIKit.Label(card, "OnizlemeYazi", "ÖNİZLEME", 18, Lav, new Vector2(270f, -170f), new Vector2(190f, 24f), TextAnchor.MiddleCenter, false);
        var clip = UIKit.Clip(pv, "Kirpma", new Vector2(186f, 296f));
        var mik = UIKit.Pivot(clip, "Mik");
        previewBody[id] = MakePreview(mik, "Govde", 1);
        previewRimD[id] = MakePreview(mik, "Koyu", 1);
        previewRimL[id] = MakePreview(mik, "Parlama", 1);
        previewTips[id] = MakePreview(mik, "Uclar", 1);
        previewRimD[id].sprite = Cosmetics.RimShadeSprite(); previewRimD[id].color = new Color(0f, 0f, 0.05f, 0.30f);
        previewRimL[id].sprite = Cosmetics.RimLightSprite(); previewRimL[id].color = new Color(1f, 1f, 1f, 0.42f);
        var fxp = clip.gameObject.AddComponent<UIPreviewFx>();
        fxp.Setup(mik, clip, id == 2 ? 180f : 0f, col);
        previewFx[id] = fxp;
        

        // Kontroller
        float cy = -212f;
        if (id == 1)
        {
            UIKit.KeyCap(card, "W", new Vector2(-335f, cy), 42f);
            UIKit.KeyCap(card, "S", new Vector2(-287f, cy), 42f);
            UIKit.KeyCap(card, "D", new Vector2(-95f, cy), 42f);
        }
        else
        {
            ArrowCap(card, new Vector2(-335f, cy), 42f, 90f);
            ArrowCap(card, new Vector2(-287f, cy), 42f, -90f);
            ArrowCap(card, new Vector2(-95f, cy), 42f, 180f);
        }
        UIKit.Label(card, "NisanYazi", "NİŞAN AL", 22, Lav, new Vector2(-180f, cy), new Vector2(130f, 30f), TextAnchor.MiddleLeft, true);
        UIKit.Label(card, "AtesYazi", "MIKNATIS", 22, Lav, new Vector2(25f, cy), new Vector2(150f, 30f), TextAnchor.MiddleLeft, true);
    }

    // Ok tusu kapagi (font ok isareti icermedigi icin dondurulmus '>' kullanilir)
    void ArrowCap(Transform parent, Vector2 pos, float size, float rot)
    {
        var k = UIKit.KeyCap(parent, "", pos, size);
        var l = UIKit.Label(k, "Ok", ">", (int)(size * 0.55f), Color.white, Vector2.zero, new Vector2(size, size), TextAnchor.MiddleCenter, true);
        l.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
    }

    Image MakePreview(Transform parent, string name, int id)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(125f, 167f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0f, 0f, id == 2 ? 180f : 0f);
        var im = go.AddComponent<Image>();
        im.preserveAspect = true; im.raycastTarget = false;
        return im;
    }

    void RefreshMenu()
    {
        string[][] names = { Cosmetics.DesignNames, Cosmetics.TrailNames, Cosmetics.FxNames };
        for (int id = 1; id <= 2; id++)
        {
            int[] sel = { Cosmetics.design[id], Cosmetics.trail[id], Cosmetics.fx[id] };
            Color col = id == 1 ? Red : Blue;
            for (int r = 0; r < 3; r++)
            {
                values[id][r].text = UIKit.T(names[r][sel[r]]);
                for (int d = 0; d < dots[id][r].Length; d++)
                {
                    bool on = d == sel[r];
                    dots[id][r][d].color = on ? col : new Color(1f, 1f, 1f, 0.2f);
                    dots[id][r][d].rectTransform.sizeDelta = on ? new Vector2(16f, 16f) : new Vector2(11f, 11f);
                }
            }
            previewBody[id].sprite = Cosmetics.DesignSprite(sel[0]);
            previewBody[id].color = col;
            previewTips[id].sprite = Cosmetics.TipsSprite();
            previewTips[id].color = new Color(0.9f, 0.92f, 0.97f);
            previewTips[id].enabled = sel[0] == 1;
            previewFx[id].SetModes(sel[1], sel[2]);
        }
    }

    static int OptionCount(int row)
    {
        return row == 0 ? Cosmetics.DesignNames.Length : (row == 1 ? Cosmetics.TrailNames.Length : Cosmetics.FxNames.Length);
    }

    static int[] RowArray(int row)
    {
        return row == 0 ? Cosmetics.design : (row == 1 ? Cosmetics.trail : Cosmetics.fx);
    }

    void Cycle(int id, int row, int dir)
    {
        int n = OptionCount(row);
        var a = RowArray(row);
        Select(id, row, ((a[id] + dir) % n + n) % n);
    }

    void Select(int id, int row, int idx)
    {
        if (!inMenu) return;
        RowArray(row)[id] = idx;
        RefreshMenu();
        Cosmetics.Apply(id == 1 ? p1.gameObject : p2.gameObject, id);
        Cosmetics.Save();
    }

    void StartMatch()
    {
        if (!inMenu) return;
        inMenu = false;
        menuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void MenuUpdate()
    {
        for (int k = 0; k < 6; k++)
            if (Pressed(k)) { SoundFX.Play("click"); Cycle(k < 3 ? 1 : 2, k % 3, 1); }
        UIKit.Twinkle(menuStars, menuStarA);
        startBtn.alpha = 0.82f + 0.18f * Mathf.PingPong(Time.unscaledTime * 1.2f, 1f);
#if !UNITY_WEBGL
        if (KeyDown("escape")) { Quit(); return; }
#endif
        if (Pressed(6)) StartMatch();
    }

    // 0-2: Z X C (kuzey), 3-5: J K L (guney), 6: Space
    bool Pressed(int idx)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return false;
        switch (idx)
        {
            case 0: return k.zKey.wasPressedThisFrame;
            case 1: return k.xKey.wasPressedThisFrame;
            case 2: return k.cKey.wasPressedThisFrame;
            case 3: return k.jKey.wasPressedThisFrame;
            case 4: return k.kKey.wasPressedThisFrame;
            case 5: return k.lKey.wasPressedThisFrame;
            default: return k.spaceKey.wasPressedThisFrame;
        }
#else
        KeyCode[] codes = { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Space };
        return Input.GetKeyDown(codes[idx]);
#endif
    }

    // ---------- Mac sonu ekrani ----------
    void BuildEndPanel()
    {
        endPanel = UIKit.Stretch("MacSonu", transform);
        var m = endPanel.transform;
        endStars = UIKit.Background(m);
        endStarA = AlphaOf(endStars);

        var card = UIKit.Panel(m, "Kart", Vector2.zero, new Vector2(900f, 520f), CardFill, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 1f, 1f, 0.25f));
        endGlow = card.Find("Isik").GetComponent<Image>();

        var tag = UIKit.Pill(card, "Etiket", new Vector2(0f, 205f), new Vector2(220f, 46f), Dark, new Color(1f, 1f, 1f, 0.2f));
        UIKit.Label(tag, "Yazi", "MAÇ BİTTİ", 26, Lav, Vector2.zero, new Vector2(210f, 40f), TextAnchor.MiddleCenter, true);
        endTitle = UIKit.Label(card, "Baslik", "", 100, Color.white, new Vector2(0f, 110f), new Vector2(860f, 130f), TextAnchor.MiddleCenter, true);
        endLine = UIKit.Block(card, "Cizgi", new Vector2(0f, 42f), new Vector2(220f, 5f), Amber);

        ScoreBox(card, "KUZEY", Red, new Vector2(-215f, -62f), out endScoreL);
        ScoreBox(card, "GÜNEY", Blue, new Vector2(215f, -62f), out endScoreR);

#if UNITY_WEBGL
        float bx = 0f;
#else
        float bx = -120f;
#endif
        var btn = UIKit.Pill(card, "Tekrar", new Vector2(bx, -195f), new Vector2(400f, 72f), Amber, new Color(1f, 1f, 1f, 0.6f));
        var key = UIKit.Pill(btn, "Tus", new Vector2(-135f, 0f), new Vector2(64f, 46f), new Color(0.2f, 0.12f, 0.02f, 0.28f), new Color(0f, 0f, 0f, 0f));
        UIKit.Label(key, "Yazi", "R", 26, Ink, Vector2.zero, new Vector2(60f, 44f), TextAnchor.MiddleCenter, true);
        UIKit.Label(btn, "Yazi", "YENİDEN OYNA", 32, Ink, new Vector2(35f, 0f), new Vector2(280f, 60f), TextAnchor.MiddleCenter, true);
        UIKit.Clickable(btn, Restart);
#if !UNITY_WEBGL
        var qb = UIKit.Pill(card, "Cik", new Vector2(215f, -195f), new Vector2(220f, 72f), new Color(0.24f, 0.21f, 0.46f, 1f), new Color(1f, 1f, 1f, 0.45f));
        var qk = UIKit.Pill(qb, "Tus", new Vector2(-62f, 0f), new Vector2(56f, 46f), new Color(0f, 0f, 0f, 0.28f), new Color(0f, 0f, 0f, 0f));
        UIKit.Label(qk, "Yazi", "Q", 26, Color.white, Vector2.zero, new Vector2(52f, 44f), TextAnchor.MiddleCenter, true);
        UIKit.Label(qb, "Yazi", "ÇIK", 32, Color.white, new Vector2(32f, 0f), new Vector2(120f, 60f), TextAnchor.MiddleCenter, true);
        UIKit.Clickable(qb, Quit);
#endif
        endPanel.SetActive(false);
    }

    void ScoreBox(Transform card, string name, Color col, Vector2 pos, out Text score)
    {
        var box = UIKit.Panel(card, "Skor" + name, pos, new Vector2(380f, 150f), new Color(0.05f, 0.04f, 0.13f, 0.85f), new Color(col.r, col.g, col.b, 0.7f), new Color(col.r, col.g, col.b, 0.22f));
        UIKit.Dot(box, "Nokta", new Vector2(-150f, 48f), 18f, col);
        UIKit.Label(box, "Isim", name, 30, Color.white, new Vector2(-30f, 48f), new Vector2(200f, 44f), TextAnchor.MiddleLeft, true);
        score = UIKit.Label(box, "Puan", "0", 90, col, new Vector2(0f, -22f), new Vector2(340f, 100f), TextAnchor.MiddleCenter, true);
    }

    // ---------- Duraklatma ----------
    void BuildPausePanel()
    {
        pausePanel = UIKit.Stretch("Duraklat", transform);
        var m = pausePanel.transform;
        var shade = pausePanel.AddComponent<Image>();
        shade.color = new Color(0.02f, 0.015f, 0.06f, 0.72f); shade.raycastTarget = false;

#if UNITY_WEBGL
        float h = 330f;
#else
        float h = 420f;
#endif
        var card = UIKit.Panel(m, "Kart", Vector2.zero, new Vector2(620f, h), CardFill, new Color(1f, 1f, 1f, 0.3f), new Color(Amber.r, Amber.g, Amber.b, 0.18f));
        UIKit.Label(card, "Baslik", "DURAKLATILDI", 64, Color.white, new Vector2(0f, h * 0.5f - 72f), new Vector2(600f, 90f), TextAnchor.MiddleCenter, true);
        float y = h * 0.5f - 160f;
        var alt = new Color(0.24f, 0.21f, 0.46f, 1f);
        PauseButton(card, "Devam", "ESC", "DEVAM ET", new Vector2(0f, y), Amber, Ink, () => SetPaused(false)); y -= 92f;
        PauseButton(card, "Tekrar", "R", "YENİDEN BAŞLAT", new Vector2(0f, y), alt, Color.white, Restart); y -= 92f;
#if !UNITY_WEBGL
        PauseButton(card, "Cik", "Q", "OYUNDAN ÇIK", new Vector2(0f, y), alt, Color.white, Quit);
#endif
        pausePanel.SetActive(false);
    }

    void PauseButton(Transform parent, string name, string key, string text, Vector2 pos, Color fill, Color ink, System.Action click)
    {
        var btn = UIKit.Pill(parent, name, pos, new Vector2(420f, 72f), fill, new Color(1f, 1f, 1f, 0.45f));
        var kp = UIKit.Pill(btn, "Tus", new Vector2(-145f, 0f), new Vector2(84f, 46f), new Color(0f, 0f, 0f, 0.28f), new Color(0f, 0f, 0f, 0f));
        UIKit.Label(kp, "Yazi", key, 24, ink, Vector2.zero, new Vector2(80f, 44f), TextAnchor.MiddleCenter, true);
        UIKit.Label(btn, "Yazi", text, 30, ink, new Vector2(45f, 0f), new Vector2(300f, 60f), TextAnchor.MiddleCenter, true);
        UIKit.Clickable(btn, click);
    }

    void SetPaused(bool on)
    {
        paused = on;
        pausePanel.SetActive(on);
        Time.timeScale = on ? 0f : 1f;
        AudioListener.pause = on;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
        Application.Quit();
#endif
    }

    static bool KeyDown(string key)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return false;
        switch (key)
        {
            case "escape": return k.escapeKey.wasPressedThisFrame;
            case "q": return k.qKey.wasPressedThisFrame;
            default: return k.rKey.wasPressedThisFrame;
        }
#else
        switch (key)
        {
            case "escape": return Input.GetKeyDown(KeyCode.Escape);
            case "q": return Input.GetKeyDown(KeyCode.Q);
            default: return Input.GetKeyDown(KeyCode.R);
        }
#endif
    }

    bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    // ---------- Mac akisi ----------
    void Update()
    {
        if (inMenu) { MenuUpdate(); return; }

        if (ended)
        {
            UIKit.Twinkle(endStars, endStarA);
            if (RestartPressed()) Restart();
#if !UNITY_WEBGL
            else if (KeyDown("q")) Quit();
#endif
            return;
        }

        if (paused)
        {
            if (KeyDown("escape")) SetPaused(false);
            else if (KeyDown("r")) Restart();
#if !UNITY_WEBGL
            else if (KeyDown("q")) Quit();
#endif
            return;
        }
        if (KeyDown("escape")) { SetPaused(true); return; }

        UpdatePolarityBanner();

        t -= Time.deltaTime;
        // Bir taraf kendi hedef toplarinin hepsini toplayinca mac biter
        checkTimer += Time.deltaTime;
        if (checkTimer > 0.5f)
        {
            checkTimer = 0f;
            if (t < matchTime - 2f)
            {
                int red = 0, blue = 0;
                foreach (var b in Object.FindObjectsByType<Ball>(FindObjectsSortMode.None))
                {
                    if (b.team == 1) red++; else blue++;
                }
                p1Done = blue == 0;   // kuzey mavi topları toplar
                p2Done = red == 0;    // güney kırmızı topları toplar
                if (p1Done || p2Done) t = 0f;
            }
        }
        if (t < 0f) t = 0f;

        int s = Mathf.CeilToInt(t);
        timerText.text = (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        p1Text.text = p1.score.ToString();
        p2Text.text = p2.score.ToString();
        AnimateTopBar();

        if (t <= 0f) EndMatch();
    }

    void EndMatch()
    {
        ended = true;
        Time.timeScale = 0f;
        polarityBanner.alpha = 0f;
        SoundFX.Play("end");
        endPanel.SetActive(true);

        bool north = p1.score > p2.score || (p1.score == p2.score && p1Done && !p2Done);
        bool south = p2.score > p1.score || (p2.score == p1.score && p2Done && !p1Done);
        Color c = Color.white;
        if (north) { endTitle.text = "KUZEY KAZANDI"; c = Red; }
        else if (south) { endTitle.text = UIKit.T("GÜNEY KAZANDI"); c = Blue; }
        else endTitle.text = UIKit.T("BERABERLİK");
        endTitle.color = c;
        endLine.color = (north || south) ? c : Amber;
        endGlow.color = new Color(c.r, c.g, c.b, 0.3f);
        endScoreL.text = p1.score.ToString();
        endScoreR.text = p2.score.ToString();
        CrowdBob.Finale(north ? 1 : (south ? 2 : 0));
    }
}
