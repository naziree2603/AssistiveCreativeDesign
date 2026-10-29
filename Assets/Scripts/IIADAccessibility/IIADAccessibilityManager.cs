using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;

/// <summary>
/// Builds the native accessibility tree that TalkBack / VoiceOver read, using Unity 6's
/// built-in Screen Reader API (UnityEngine.Accessibility). No UAP.
///
/// - Every active IIADAccessibleElement becomes an AccessibilityNode, in reading order.
/// - When an IIADScreen opens, the screen reader is told and focus moves to its first item.
/// - Popups marked "Is Modal" hide everything behind them.
///
/// One instance is created automatically if the scene does not have one.
/// </summary>
[DefaultExecutionOrder(-500)]
[AddComponentMenu("IIAD/Accessibility Manager")]
public class IIADAccessibilityManager : MonoBehaviour
{
    [Tooltip("Language for Voice Guide speech, e.g. en-US or ms-MY. TalkBack uses the phone's language.")]
    public string speechLanguage = "en-US";

    [Tooltip("How often positions, labels and values are refreshed (seconds).")]
    public float refreshInterval = 0.25f;

    [Tooltip("On first launch without TalkBack, explain how to turn on Voice Guide.")]
    public bool speakFirstRunHint = true;

    [Tooltip("Keep this object when scenes change.")]
    public bool persistAcrossScenes = true;

    [Tooltip("Node rectangles use a top-left origin (Unity's screen-reader samples do this). Turn off only if TalkBack's focus boxes appear upside down on a device.")]
    public bool frameTopLeftOrigin = true;

    [Tooltip("Write the reading order to the Console each time it changes.")]
    public bool debugLog = false;

    // ---------------------------------------------------------------------
    static IIADAccessibilityManager instance;
    static readonly List<IIADAccessibleElement> registry = new List<IIADAccessibleElement>();
    static readonly List<IIADScreen> openScreens = new List<IIADScreen>();
    static bool dirty = true;
    static IIADScreen pendingAnnounce;
    static int registrationCounter;

    readonly List<IIADAccessibleElement> ordered = new List<IIADAccessibleElement>();
    readonly Dictionary<int, IIADAccessibleElement> byNodeId = new Dictionary<int, IIADAccessibleElement>();
    AccessibilityHierarchy hierarchy;
    float nextRefresh;
    int visibleSignature;
    IIADAccessibleElement readerFocus;

    /// <summary>Items a user can reach right now, in reading order.</summary>
    public static IReadOnlyList<IIADAccessibleElement> Ordered => instance != null ? instance.ordered : (IReadOnlyList<IIADAccessibleElement>)Array.Empty<IIADAccessibleElement>();

    /// <summary>The screen on top (most recently opened and still active).</summary>
    public static IIADScreen CurrentScreen
    {
        get
        {
            for (int i = openScreens.Count - 1; i >= 0; i--)
                if (openScreens[i] != null && openScreens[i].isActiveAndEnabled) return openScreens[i];
            return null;
        }
    }

    /// <summary>The item TalkBack is on (null if TalkBack is off).</summary>
    public static IIADAccessibleElement ScreenReaderFocus => instance != null ? instance.readerFocus : null;

    /// <summary>Raised after a screen opens, with the item that should get focus. Voice Guide listens to this.</summary>
    public static event Action<IIADScreen, IIADAccessibleElement> ScreenChanged;

    /// <summary>Raised whenever the reading order is rebuilt.</summary>
    public static event Action OrderChanged;

    // ---------------------------------------------------------------------
    // Registration (called by elements and screens)
    // ---------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        registry.Clear(); openScreens.Clear(); dirty = true; pendingAnnounce = null; instance = null;
        ScreenChanged = null; OrderChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance == null && FindFirstObjectByType<IIADAccessibilityManager>() == null)
            new GameObject("IIAD Accessibility").AddComponent<IIADAccessibilityManager>();
    }

    public static void Register(IIADAccessibleElement e)
    {
        if (!registry.Contains(e)) { e.registrationIndex = registrationCounter++; registry.Add(e); }
        dirty = true;
    }

    public static void Unregister(IIADAccessibleElement e) { registry.Remove(e); dirty = true; }

    public static void ScreenOpened(IIADScreen s)
    {
        openScreens.Remove(s);
        openScreens.Add(s);
        pendingAnnounce = s;
        dirty = true;
    }

    public static void ScreenClosed(IIADScreen s)
    {
        bool wasTop = CurrentScreen == s || (openScreens.Count > 0 && openScreens[openScreens.Count - 1] == s);
        openScreens.Remove(s);
        dirty = true;
        // Closing a popup: put focus back on the screen underneath, without re-reading its title.
        if (wasTop && s.isModal && pendingAnnounce == null) returnFocusAfterPopup = true;
    }
    static bool returnFocusAfterPopup;

    public static void MarkDirty() => dirty = true;

    /// <summary>Move screen-reader / Voice Guide focus to an item, e.g. to an error message.</summary>
    public static void Focus(IIADAccessibleElement e)
    {
        if (instance == null || e == null) return;
        instance.RebuildIfNeeded();
        if (IIADSpeech.IsScreenReaderOn && e.node != null)
            Safe(() => AssistiveSupport.notificationDispatcher.SendLayoutChanged(e.node));
        IIADVoiceGuide.FocusElement(e, true);
    }

    // ---------------------------------------------------------------------
    // Unity
    // ---------------------------------------------------------------------

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        if (persistAcrossScenes && transform.parent == null) DontDestroyOnLoad(gameObject);

        IIADSpeech.Initialize(speechLanguage);
        if (GetComponent<IIADVoiceGuide>() == null) gameObject.AddComponent<IIADVoiceGuide>();

        Safe(() =>
        {
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
        });
    }

    void Start()
    {
        if (speakFirstRunHint && PlayerPrefs.GetInt("IIAD_FirstRunHint", 0) == 0)
        {
            PlayerPrefs.SetInt("IIAD_FirstRunHint", 1);
            if (!IIADSpeech.IsScreenReaderOn && !IIADSpeech.VoiceGuideEnabled)
                IIADVoiceGuide.SpeakFirstRunHint();
        }
    }

    void OnDestroy()
    {
        if (instance != this) return;
        Safe(() =>
        {
            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
            if (AssistiveSupport.activeHierarchy == hierarchy) AssistiveSupport.activeHierarchy = null;
        });
        IIADSpeech.Shutdown();
        instance = null;
    }

    void OnApplicationPause(bool paused) { if (paused) IIADSpeech.Stop(); else dirty = true; }

    void OnScreenReaderStatusChanged(bool on)
    {
        dirty = true;
        if (on) IIADSpeech.Stop(); // TalkBack takes over from Voice Guide
        pendingAnnounce = CurrentScreen;
    }

    void OnNodeFocusChanged(AccessibilityNode n)
    {
        readerFocus = n != null && byNodeId.TryGetValue(n.id, out var e) ? e : null;
    }

    void LateUpdate()
    {
        IIADSpeech.DrainMainThreadQueue();
        RebuildIfNeeded();

        if (pendingAnnounce != null)
        {
            var s = pendingAnnounce; pendingAnnounce = null; returnFocusAfterPopup = false;
            if (s != null && s.isActiveAndEnabled) AnnounceScreen(s);
        }
        else if (returnFocusAfterPopup)
        {
            returnFocusAfterPopup = false;
            var s = CurrentScreen;
            var first = FirstFocusFor(s);
            if (IIADSpeech.IsScreenReaderOn && first != null && first.node != null)
                Safe(() => AssistiveSupport.notificationDispatcher.SendLayoutChanged(first.node));
            ScreenChanged?.Invoke(s, first);
        }

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + refreshInterval;
            RefreshNodes();
        }
    }

    // ---------------------------------------------------------------------
    // Building the tree
    // ---------------------------------------------------------------------

    void RebuildIfNeeded()
    {
        if (!dirty) return;
        dirty = false;
        Canvas.ForceUpdateCanvases(); // positions must be current before sorting
        Rebuild();
    }

    void Rebuild()
    {
        var previousFocus = readerFocus;
        CollectVisible(ordered);
        visibleSignature = Signature(ordered);

        hierarchy = new AccessibilityHierarchy();
        byNodeId.Clear();
        foreach (var e in ordered)
        {
            var el = e;
            var node = hierarchy.AddNode(el.GetLabel());
            node.role = el.NativeRole;
            node.value = el.GetValue();
            node.hint = el.hint;
            node.state = el.NativeState;
            bool topLeft = frameTopLeftOrigin;
            node.frameGetter = () => el.GetScreenRect(topLeft);
            node.invoked += el.Activate;
            if (el.IsAdjustable)
            {
                node.incremented += () => el.Adjust(1);
                node.decremented += () => el.Adjust(-1);
            }
            node.focusChanged += (n, focused) => { if (focused) readerFocus = el; };
            el.node = node;
            byNodeId[node.id] = el;
        }
        foreach (var e in registry) if (!ordered.Contains(e)) e.node = null;

        Safe(() =>
        {
            AssistiveSupport.activeHierarchy = hierarchy;
            if (IIADSpeech.IsScreenReaderOn && previousFocus != null && previousFocus.node != null && pendingAnnounce == null)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(previousFocus.node);
        });

        if (debugLog)
        {
            var sb = new System.Text.StringBuilder("IIAD reading order (" + ordered.Count + "):\n");
            for (int i = 0; i < ordered.Count; i++) sb.Append(i + 1).Append(". ").Append(ordered[i].GetSpokenText(false)).Append('\n');
            Debug.Log(sb.ToString());
        }
        OrderChanged?.Invoke();
    }

    /// <summary>Picks the items that can be reached now and sorts them into reading order.</summary>
    void CollectVisible(List<IIADAccessibleElement> result)
    {
        result.Clear();
        registry.RemoveAll(x => x == null);

        // Which screens are hidden behind a newer screen?
        IIADScreen modal = null;
        for (int i = openScreens.Count - 1; i >= 0; i--)
            if (openScreens[i] != null && openScreens[i].isActiveAndEnabled && openScreens[i].isModal) { modal = openScreens[i]; break; }

        foreach (var e in registry)
        {
            if (!e.IsVisibleToReader) continue;
            var s = e.Screen;
            if (modal != null)
            {
                if (s == null || !(s == modal || s.transform.IsChildOf(modal.transform))) continue;
            }
            else if (s != null && IsCoveredByNewerScreen(s)) continue;
            result.Add(e);
        }

        result.Sort(Compare);
    }

    /// <summary>A full screen opened later (and not nested inside s) hides s.</summary>
    static bool IsCoveredByNewerScreen(IIADScreen s)
    {
        int idx = openScreens.IndexOf(s);
        if (idx < 0) return false;
        for (int i = idx + 1; i < openScreens.Count; i++)
        {
            var newer = openScreens[i];
            if (newer == null || !newer.isActiveAndEnabled) continue;
            if (newer.transform.IsChildOf(s.transform)) continue; // nested sub-panel
            if (s.transform.IsChildOf(newer.transform)) continue;
            return true;
        }
        return false;
    }

    /// <summary>Header items (no screen) first; then explicit Focus Order; then top-to-bottom, left-to-right.</summary>
    static int Compare(IIADAccessibleElement a, IIADAccessibleElement b)
    {
        int ga = a.Screen == null ? 0 : 1, gb = b.Screen == null ? 0 : 1;
        if (ga != gb) return ga.CompareTo(gb);

        bool ea = a.focusOrder >= 0, eb = b.focusOrder >= 0;
        if (ea && eb && a.focusOrder != b.focusOrder) return a.focusOrder.CompareTo(b.focusOrder);
        if (ea != eb) return ea ? -1 : 1;

        Rect ra = a.GetScreenRect(true), rb = b.GetScreenRect(true);
        float rowTolerance = Mathf.Max(12f, Mathf.Min(ra.height, rb.height) * 0.5f);
        if (Mathf.Abs(ra.center.y - rb.center.y) > rowTolerance) return ra.center.y.CompareTo(rb.center.y);
        if (Mathf.Abs(ra.xMin - rb.xMin) > 1f) return ra.xMin.CompareTo(rb.xMin);
        return a.registrationIndex.CompareTo(b.registrationIndex);
    }

    static int Signature(List<IIADAccessibleElement> list)
    {
        unchecked
        {
            int h = 17;
            foreach (var e in list) h = h * 31 + e.GetInstanceID();
            return h;
        }
    }

    /// <summary>Keeps labels, values, enabled state and positions current between rebuilds.</summary>
    void RefreshNodes()
    {
        // Something appeared or disappeared without OnEnable/OnDisable (CanvasGroup fade, scrolling)?
        var tmp = ListPool.Get();
        CollectVisible(tmp);
        bool changed = Signature(tmp) != visibleSignature;
        ListPool.Release(tmp);
        if (changed) { dirty = true; return; }

        foreach (var e in ordered)
        {
            if (e == null || e.node == null) continue;
            var n = e.node;
            string label = e.GetLabel();
            if (n.label != label) n.label = label;
            string value = e.GetValue();
            if (n.value != value) n.value = value;
            var st = e.NativeState;
            if (n.state != st) n.state = st;
        }
        Safe(() => hierarchy?.RefreshNodeFrames());
    }

    void AnnounceScreen(IIADScreen s)
    {
        var first = FirstFocusFor(s);
        if (IIADSpeech.IsScreenReaderOn)
        {
            Safe(() =>
            {
                AssistiveSupport.notificationDispatcher.SendScreenChanged(first != null ? first.node : null);
                if (s.announceOnOpen)
                {
                    string a = s.GetAnnouncement();
                    if (!string.IsNullOrEmpty(a)) AssistiveSupport.notificationDispatcher.SendAnnouncement(a);
                }
            });
        }
        ScreenChanged?.Invoke(s, first);
    }

    internal static IIADAccessibleElement FirstFocusFor(IIADScreen s)
    {
        if (instance == null) return null;
        var list = instance.ordered;
        if (s != null)
        {
            if (s.firstFocus != null && list.Contains(s.firstFocus)) return s.firstFocus;
            foreach (var e in list) if (e.focusFirst && InScreen(e, s)) return e;
            foreach (var e in list) if (InScreen(e, s)) return e;
        }
        return list.Count > 0 ? list[0] : null;
    }

    static bool InScreen(IIADAccessibleElement e, IIADScreen s) =>
        e.Screen != null && (e.Screen == s || e.Screen.transform.IsChildOf(s.transform));

    static void Safe(Action a)
    {
        try { a(); }
        catch (Exception ex) { if (Application.isMobilePlatform) Debug.LogWarning("IIAD accessibility: " + ex.Message); }
    }

    static class ListPool
    {
        static readonly Stack<List<IIADAccessibleElement>> pool = new Stack<List<IIADAccessibleElement>>();
        public static List<IIADAccessibleElement> Get() => pool.Count > 0 ? pool.Pop() : new List<IIADAccessibleElement>();
        public static void Release(List<IIADAccessibleElement> l) { l.Clear(); pool.Push(l); }
    }
}
