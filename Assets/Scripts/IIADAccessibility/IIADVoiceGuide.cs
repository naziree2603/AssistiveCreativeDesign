using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Voice Guide: the app's own screen reading for people who do not use TalkBack.
/// It only runs when Voice Guide is on AND TalkBack is off (TalkBack has its own gestures).
///
/// Gestures while on:
///   swipe right / left        next / previous item (read aloud)
///   tap                       focus the item under your finger and read it
///   double-tap anywhere       activate the focused item
///   swipe up / down           change a slider or drop-down, otherwise scroll
///   two-finger tap            stop speaking
///   two-finger double-tap     repeat the screen title and step
/// Works on or off:
///   two-finger hold (1.5 s)   turn Voice Guide on or off
///
/// In the Editor: Tab / arrows to move, Enter to activate, Esc to stop, R to repeat, V to toggle.
/// </summary>
[AddComponentMenu("IIAD/Voice Guide")]
public class IIADVoiceGuide : MonoBehaviour
{
    [Header("Focus frame")]
    public Color frameColor = new Color(1f, 0.76f, 0.2f, 1f);   // IIAD signal yellow
    public float frameThickness = 6f;
    public float framePadding = 6f;

    [Header("Editor")]
    [Tooltip("In the Unity Editor, mouse clicks keep working normally while Voice Guide is on (use Tab / arrows / Enter to test Voice Guide). On a phone, taps are always handled by Voice Guide.")]
    public bool letMouseClickThroughInEditor = true;

    [Header("Gestures")]
    [Tooltip("Minimum swipe length in density-independent pixels.")]
    public float swipeDistanceDp = 50f;
    public float doubleTapSeconds = 0.35f;
    public float twoFingerHoldSeconds = 1.5f;

    static IIADVoiceGuide instance;

    IIADAccessibleElement focused;
    Canvas overlay;
    GameObject blocker;
    GameObject banner;
    RectTransform frame;
    readonly Image[] edges = new Image[4];

    // gesture state
    Vector2 downPos; float downTime; bool tracking;
    float lastTapTime = -10f; Vector2 lastTapPos; bool pendingSingleTap; Vector2 pendingTapPos;
    float twoDownTime; bool twoTracking; bool twoMoved; Vector2 twoStartCenter; bool holdFired;
    float lastTwoTapTime = -10f; bool pendingTwoTap;

    public static IIADAccessibleElement Focused => instance != null ? instance.focused : null;

    // ---------------------------------------------------------------------

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        BuildOverlay();
        IIADSpeech.VoiceGuideChanged += OnVoiceGuideChanged;
        IIADAccessibilityManager.ScreenChanged += OnScreenChanged;
        IIADAccessibilityManager.OrderChanged += OnOrderChanged;
    }

    void OnDestroy()
    {
        IIADSpeech.VoiceGuideChanged -= OnVoiceGuideChanged;
        IIADAccessibilityManager.ScreenChanged -= OnScreenChanged;
        IIADAccessibilityManager.OrderChanged -= OnOrderChanged;
        if (instance == this) instance = null;
        if (overlay != null) Destroy(overlay.gameObject);
    }

    // ---------------------------------------------------------------------
    // Public helpers
    // ---------------------------------------------------------------------

    public static void SpeakFirstRunHint()
    {
        IIADSpeech.SayDirect("Welcome to IIAD. If you are blind or have low vision, hold two fingers on the screen to turn on Voice Guide. You can also use TalkBack.");
    }

    public static void FocusElement(IIADAccessibleElement e, bool speak)
    {
        if (instance == null || e == null || !IIADSpeech.IsVoiceGuideActive) return;
        instance.SetFocus(e, speak);
    }

    /// <summary>For a header button: turns Voice Guide on/off and says so.</summary>
    public static void Toggle()
    {
        if (IIADSpeech.IsScreenReaderOn)
        {
            IIADSpeech.Say("TalkBack is on, so Voice Guide is not needed.");
            return;
        }
        IIADSpeech.VoiceGuideEnabled = !IIADSpeech.VoiceGuideEnabled;
    }

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------

    void OnVoiceGuideChanged(bool on)
    {
        if (on)
        {
            IIADSpeech.Say("Voice Guide on. Swipe right or left to move. Double-tap to select. Tap with two fingers to stop speech.");
            var first = IIADAccessibilityManager.FirstFocusFor(IIADAccessibilityManager.CurrentScreen);
            if (first != null) { SetFocus(first, false); IIADSpeech.Say(first.GetSpokenText(), false); }
        }
        else
        {
            focused = null;
            IIADSpeech.SayDirect("Voice Guide off.");
        }
    }

    void OnScreenChanged(IIADScreen screen, IIADAccessibleElement first)
    {
        if (!IIADSpeech.IsVoiceGuideActive) return;
        pendingSingleTap = false;
        SetFocus(first, false);
        bool spokeTitle = false;
        if (screen != null && screen.announceOnOpen)
        {
            string a = screen.GetAnnouncement();
            if (!string.IsNullOrEmpty(a)) { IIADSpeech.Say(a, true); spokeTitle = true; }
        }
        if (first != null) IIADSpeech.Say(first.GetSpokenText(), !spokeTitle ? true : false);
    }

    void OnOrderChanged()
    {
        if (focused != null && !Contains(focused)) focused = null;
    }

    static bool Contains(IIADAccessibleElement e)
    {
        var list = IIADAccessibilityManager.Ordered;
        for (int i = 0; i < list.Count; i++) if (list[i] == e) return true;
        return false;
    }

    static int IndexOf(IIADAccessibleElement e)
    {
        var list = IIADAccessibilityManager.Ordered;
        for (int i = 0; i < list.Count; i++) if (list[i] == e) return i;
        return -1;
    }

    // ---------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------

    void SetFocus(IIADAccessibleElement e, bool speak)
    {
        focused = e;
        if (e == null) return;
        EnsureVisible(e);
        if (speak) IIADSpeech.Say(e.GetSpokenText());
    }

    void Move(int dir)
    {
        var list = IIADAccessibilityManager.Ordered;
        if (list.Count == 0) { IIADSpeech.Say("Nothing to read on this screen."); return; }
        int i = focused != null ? IndexOf(focused) : -1;
        int next = i < 0 ? (dir > 0 ? 0 : list.Count - 1) : i + dir;
        if (next >= list.Count) { IIADSpeech.Say("End of screen."); return; }
        if (next < 0) { IIADSpeech.Say("Top of screen."); return; }
        SetFocus(list[next], true);
    }

    void ActivateFocused()
    {
        if (focused == null) { Move(1); return; }
        var target = focused;
        if (!target.Activate()) return;
        // Activation often changes the screen; the manager will announce it.
        IIADAccessibilityManager.MarkDirty();
    }

    void FocusAt(Vector2 screenPos)
    {
        var list = IIADAccessibilityManager.Ordered;
        IIADAccessibleElement best = null; float bestArea = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            Rect r = list[i].GetScreenRect(false);
            if (!r.Contains(screenPos)) continue;
            float area = r.width * r.height;              // smallest = most specific
            if (area < bestArea) { bestArea = area; best = list[i]; }
        }
        if (best != null) SetFocus(best, true);
        else IIADSpeech.Say("Nothing here. Swipe right to move to the next item.");
    }

    void VerticalSwipe(int dir) // +1 up, -1 down
    {
        if (focused != null && focused.IsAdjustable) { focused.Adjust(dir); return; }
        ScrollRect sr = focused != null ? focused.GetComponentInParent<ScrollRect>() : FindFirstObjectByType<ScrollRect>();
        if (sr == null || !sr.vertical) { IIADSpeech.Say("Nothing to scroll."); return; }
        float before = sr.verticalNormalizedPosition;
        sr.verticalNormalizedPosition = Mathf.Clamp01(before - dir * 0.3f); // swipe up shows content further down
        IIADSpeech.Say(Mathf.Approximately(before, sr.verticalNormalizedPosition) ? (dir > 0 ? "Bottom of list." : "Top of list.") : "Scrolled.");
        IIADAccessibilityManager.MarkDirty();
    }

    static void RepeatScreen()
    {
        var s = IIADAccessibilityManager.CurrentScreen;
        string a = s != null ? s.GetAnnouncement() : "";
        if (string.IsNullOrEmpty(a)) IIADSpeech.Repeat(); else IIADSpeech.Say(a);
    }

    /// <summary>Scrolls a ScrollRect so the focused item is on screen.</summary>
    static void EnsureVisible(IIADAccessibleElement e)
    {
        var sr = e.GetComponentInParent<ScrollRect>();
        if (sr == null || sr.content == null) return;
        var viewport = sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
        var canvas = sr.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector3[] c = new Vector3[4];
        viewport.GetWorldCorners(c);
        Vector2 vMin = RectTransformUtility.WorldToScreenPoint(cam, c[0]), vMax = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
        Rect r = e.GetScreenRect(false);
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        float shift = 0f;
        if (r.yMax > vMax.y) shift = r.yMax - vMax.y;          // above the viewport
        else if (r.yMin < vMin.y) shift = r.yMin - vMin.y;     // below the viewport
        if (Mathf.Abs(shift) > 0.5f)
        {
            sr.StopMovement();
            sr.content.anchoredPosition -= new Vector2(0f, shift / Mathf.Max(0.01f, scale));
        }
    }

    // ---------------------------------------------------------------------
    // Input
    // ---------------------------------------------------------------------

    void Update()
    {
        bool active = IIADSpeech.IsVoiceGuideActive;
        bool editorPassThrough = Application.isEditor && letMouseClickThroughInEditor;
        bool block = active && !editorPassThrough;
        if (blocker != null && blocker.activeSelf != block) blocker.SetActive(block);
        if (banner != null && banner.activeSelf != active) banner.SetActive(active);
        UpdateFrame(active);

#if ENABLE_LEGACY_INPUT_MANAGER
        HandleTwoFinger(active);
        if (Application.isEditor) HandleKeys(active);
        if (!active) { tracking = false; pendingSingleTap = false; return; }
        HandleOneFinger();
#endif
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    float Dp(float dp) => dp * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f);

    void HandleOneFinger()
    {
        bool down, held, up; Vector2 pos;
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            pos = t.position; down = t.phase == TouchPhase.Began; up = t.phase == TouchPhase.Ended; held = !up && t.phase != TouchPhase.Canceled;
        }
        else if (Input.touchCount == 0 && Application.isEditor && !letMouseClickThroughInEditor)
        {
            pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0); held = Input.GetMouseButton(0);
        }
        else { tracking = false; return; }

        if (down) { tracking = true; downPos = pos; downTime = Time.unscaledTime; }
        if (up && tracking)
        {
            tracking = false;
            Vector2 d = pos - downPos;
            float dt = Time.unscaledTime - downTime;
            if (d.magnitude >= Dp(swipeDistanceDp) && dt < 0.8f)
            {
                pendingSingleTap = false;
                if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) Move(d.x > 0 ? 1 : -1);
                else VerticalSwipe(d.y > 0 ? 1 : -1);
            }
            else if (dt < 0.5f)
            {
                if (Time.unscaledTime - lastTapTime <= doubleTapSeconds && (pos - lastTapPos).magnitude < Dp(60))
                {
                    pendingSingleTap = false; lastTapTime = -10f;
                    ActivateFocused();
                }
                else
                {
                    lastTapTime = Time.unscaledTime; lastTapPos = pos;
                    pendingSingleTap = true; pendingTapPos = pos;
                }
            }
        }

        if (pendingSingleTap && Time.unscaledTime - lastTapTime > doubleTapSeconds)
        {
            pendingSingleTap = false;
            FocusAt(pendingTapPos);
        }
    }

    void HandleTwoFinger(bool active)
    {
        if (IIADSpeech.IsScreenReaderOn) { twoTracking = false; return; }

        if (Input.touchCount == 2)
        {
            Vector2 center = (Input.GetTouch(0).position + Input.GetTouch(1).position) * 0.5f;
            if (!twoTracking) { twoTracking = true; twoMoved = false; holdFired = false; twoDownTime = Time.unscaledTime; twoStartCenter = center; }
            if ((center - twoStartCenter).magnitude > Dp(30)) twoMoved = true;
            if (!holdFired && !twoMoved && Time.unscaledTime - twoDownTime >= twoFingerHoldSeconds)
            {
                holdFired = true;
                IIADSpeech.VoiceGuideEnabled = !IIADSpeech.VoiceGuideEnabled;
            }
            tracking = false; pendingSingleTap = false;
        }
        else if (twoTracking && Input.touchCount == 0)
        {
            twoTracking = false;
            if (!active || holdFired || twoMoved || Time.unscaledTime - twoDownTime > 0.4f) return;
            if (Time.unscaledTime - lastTwoTapTime <= doubleTapSeconds + 0.1f) { pendingTwoTap = false; lastTwoTapTime = -10f; RepeatScreen(); }
            else { lastTwoTapTime = Time.unscaledTime; pendingTwoTap = true; }
        }

        if (pendingTwoTap && Time.unscaledTime - lastTwoTapTime > doubleTapSeconds + 0.1f)
        {
            pendingTwoTap = false;
            IIADSpeech.Stop();
        }
    }

    void HandleKeys(bool active)
    {
        if (Input.GetKeyDown(KeyCode.V) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)))
            IIADSpeech.VoiceGuideEnabled = !IIADSpeech.VoiceGuideEnabled;
        if (!active) return;
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.RightArrow) || (Input.GetKeyDown(KeyCode.Tab) && !shift)) Move(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || (Input.GetKeyDown(KeyCode.Tab) && shift)) Move(-1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ActivateFocused();
        if (Input.GetKeyDown(KeyCode.UpArrow)) VerticalSwipe(1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) VerticalSwipe(-1);
        if (Input.GetKeyDown(KeyCode.Escape)) IIADSpeech.Stop();
        if (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))) RepeatScreen();
    }
#endif

    // ---------------------------------------------------------------------
    // Overlay: blocks normal taps while Voice Guide is on, and draws the focus frame
    // ---------------------------------------------------------------------

    void BuildOverlay()
    {
        var go = new GameObject("IIAD Voice Guide Overlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        overlay = go.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32000;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        blocker = new GameObject("Tap Blocker", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(go.transform, false);
        Stretch((RectTransform)blocker.transform);
        var img = blocker.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;
        blocker.SetActive(false);

        // Always-visible strip so nobody thinks the app froze: tap it to turn Voice Guide off
        banner = new GameObject("Voice Guide Banner", typeof(RectTransform), typeof(Image), typeof(Button));
        banner.transform.SetParent(go.transform, false);
        var brt = (RectTransform)banner.transform;
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0);
        brt.anchoredPosition = Vector2.zero; brt.sizeDelta = new Vector2(0, 112);
        banner.GetComponent<Image>().color = new Color32(0xFF, 0xC2, 0x33, 0xF2);
        banner.GetComponent<Button>().onClick.AddListener(() => IIADSpeech.VoiceGuideEnabled = false);
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(banner.transform, false);
        Stretch((RectTransform)label.transform);
        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "<b>Voice Guide is on</b>  ·  double-tap to select  ·  tap here to turn off";
        tmp.fontSize = 34; tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color32(0x0B, 0x16, 0x30, 0xFF);
        tmp.raycastTarget = false;
        banner.SetActive(false);

        var f = new GameObject("Focus Frame", typeof(RectTransform));
        f.transform.SetParent(go.transform, false);
        frame = (RectTransform)f.transform;
        frame.anchorMin = frame.anchorMax = Vector2.zero;
        frame.pivot = Vector2.zero;
        for (int i = 0; i < 4; i++)
        {
            var e = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            e.transform.SetParent(frame, false);
            edges[i] = e.GetComponent<Image>();
            edges[i].raycastTarget = false;
            edges[i].color = frameColor;
        }
        f.SetActive(false);
    }

    void UpdateFrame(bool active)
    {
        if (frame == null) return;
        bool show = active && focused != null && focused.isActiveAndEnabled;
        if (frame.gameObject.activeSelf != show) frame.gameObject.SetActive(show);
        if (!show) return;

        float s = overlay.scaleFactor <= 0 ? 1f : overlay.scaleFactor;
        Rect r = focused.GetScreenRect(false);
        float p = framePadding, t = frameThickness;
        frame.anchoredPosition = new Vector2((r.xMin / s) - p, (r.yMin / s) - p);
        Vector2 size = new Vector2(r.width / s + p * 2, r.height / s + p * 2);
        frame.sizeDelta = size;
        Place(edges[0].rectTransform, 0, size.y - t, size.x, t);   // top
        Place(edges[1].rectTransform, 0, 0, size.x, t);            // bottom
        Place(edges[2].rectTransform, 0, 0, t, size.y);            // left
        Place(edges[3].rectTransform, size.x - t, 0, t, size.y);   // right
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
