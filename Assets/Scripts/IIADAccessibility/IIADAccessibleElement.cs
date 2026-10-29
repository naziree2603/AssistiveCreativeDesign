using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

public enum IIADRole
{
    Auto,        // detected from Button / Toggle / TMP_InputField / Slider / TMP_Dropdown / Text
    Button,
    Text,
    Header,      // screen titles: TalkBack users can jump between headings
    Image,       // e.g. the generated poster
    TextField,
    Toggle,
    Slider,
    Dropdown,
    Tab
}

/// <summary>
/// Put this on anything a blind user must reach: buttons, fields, titles, the poster, scores.
/// It becomes a native accessibility node that TalkBack / VoiceOver can focus and activate,
/// and an item that Voice Guide can swipe to. Replaces UAP's AccessibleButton, AccessibleLabel
/// and AccessibleTextEdit.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("IIAD/Accessible Element")]
public class IIADAccessibleElement : MonoBehaviour
{
    [Tooltip("What the screen reader says. Leave empty to read the text component below, or this object's own child text.")]
    [TextArea(1, 3)] public string label = "";

    [Tooltip("Optional text to read instead of a fixed label (for labels that change, like a score).")]
    public TMP_Text labelText;

    [Tooltip("If the label is empty and Label Text is not set, read the first child TMP text.")]
    public bool readChildText = true;

    [Tooltip("Extra help read after a pause, e.g. 'Double-tap to record your idea'.")]
    [TextArea(1, 2)] public string hint = "";

    public IIADRole role = IIADRole.Auto;

    [Tooltip("Lower numbers are read first. -1 = automatic (top to bottom, left to right).")]
    public int focusOrder = -1;

    [Tooltip("Screen reader focus starts here when this screen opens.")]
    public bool focusFirst = false;

    [Tooltip("Hide from TalkBack and Voice Guide (decorative images, duplicate text).")]
    public bool hidden = false;

    // cached components
    Selectable selectable;
    Button button;
    Toggle toggle;
    TMP_InputField input;
    Slider slider;
    TMP_Dropdown dropdown;
    RectTransform rect;
    Canvas canvas;
    IIADScreen screen;

    internal AccessibilityNode node;   // set by the manager
    internal int registrationIndex;

    public IIADScreen Screen => screen;
    public RectTransform Rect => rect;

    void Awake() => Cache();

    void Cache()
    {
        rect = (RectTransform)transform;
        selectable = GetComponent<Selectable>();
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        input = GetComponent<TMP_InputField>();
        slider = GetComponent<Slider>();
        dropdown = GetComponent<TMP_Dropdown>();
        canvas = GetComponentInParent<Canvas>(true);
        screen = GetComponentInParent<IIADScreen>(true);
        if (labelText == null && readChildText && string.IsNullOrEmpty(label) && input == null)
            labelText = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable() { if (rect == null) Cache(); IIADAccessibilityManager.Register(this); }
    void OnDisable() => IIADAccessibilityManager.Unregister(this);
    void OnTransformParentChanged() { Cache(); IIADAccessibilityManager.MarkDirty(); }

    // ---------------------------------------------------------------------
    // What gets read
    // ---------------------------------------------------------------------

    public IIADRole ResolvedRole
    {
        get
        {
            if (role != IIADRole.Auto) return role;
            if (input != null) return IIADRole.TextField;
            if (toggle != null) return IIADRole.Toggle;
            if (slider != null) return IIADRole.Slider;
            if (dropdown != null) return IIADRole.Dropdown;
            if (button != null || selectable != null) return IIADRole.Button;
            if (GetComponent<RawImage>() != null) return IIADRole.Image;
            return IIADRole.Text;
        }
    }

    public string GetLabel()
    {
        if (!string.IsNullOrWhiteSpace(label)) return label.Trim();
        if (labelText != null) return IIADSpeech.Clean(labelText.text);
        if (input != null && input.placeholder is TMP_Text ph) return IIADSpeech.Clean(ph.text);
        var own = GetComponent<TMP_Text>();
        if (own != null) return IIADSpeech.Clean(own.text);
        return gameObject.name;
    }

    /// <summary>Current value: field contents, on/off, slider %, selected option.</summary>
    public string GetValue()
    {
        if (input != null)
            return string.IsNullOrEmpty(input.text) ? "empty"
                 : input.contentType == TMP_InputField.ContentType.Password ? input.text.Length + " characters" : input.text;
        if (toggle != null) return toggle.isOn ? "on" : "off";
        if (slider != null) return Mathf.RoundToInt(Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 100f) + " percent";
        if (dropdown != null && dropdown.options.Count > dropdown.value && dropdown.value >= 0) return dropdown.options[dropdown.value].text;
        return "";
    }

    public bool IsInteractable => selectable == null || selectable.IsInteractable();

    /// <summary>Full sentence for Voice Guide, e.g. "Generate poster, button" / "Your idea, edit box, empty".</summary>
    public string GetSpokenText(bool withHint = true)
    {
        string text = GetLabel();
        switch (ResolvedRole)
        {
            case IIADRole.Button: text += ", button"; break;
            case IIADRole.Tab: text += ", tab"; break;
            case IIADRole.TextField: text += ", edit box"; break;
            case IIADRole.Toggle: text += ", switch"; break;
            case IIADRole.Slider: text += ", slider"; break;
            case IIADRole.Dropdown: text += ", drop-down"; break;
            case IIADRole.Header: text += ", heading"; break;
            case IIADRole.Image: text += ", image"; break;
        }
        string v = GetValue();
        if (!string.IsNullOrEmpty(v)) text += ", " + v;
        if (!IsInteractable) text += ", disabled";
        if (withHint && !string.IsNullOrWhiteSpace(hint)) text += ". " + hint.Trim();
        return text;
    }

    public AccessibilityRole NativeRole
    {
        get
        {
            switch (ResolvedRole)
            {
                case IIADRole.Button: return AccessibilityRole.Button;
                case IIADRole.Header: return AccessibilityRole.Header;
                case IIADRole.Image: return AccessibilityRole.Image;
                case IIADRole.TextField: return AccessibilityRole.TextField;
                case IIADRole.Toggle: return AccessibilityRole.Toggle;
                case IIADRole.Slider: return AccessibilityRole.Slider;
                case IIADRole.Dropdown: return AccessibilityRole.Dropdown;
                case IIADRole.Tab: return AccessibilityRole.TabButton;
                default: return AccessibilityRole.StaticText;
            }
        }
    }

    public AccessibilityState NativeState
    {
        get
        {
            var s = AccessibilityState.None;
            if (!IsInteractable) s |= AccessibilityState.Disabled;
            if (toggle != null && toggle.isOn) s |= AccessibilityState.Selected;
            return s;
        }
    }

    // ---------------------------------------------------------------------
    // Visibility and position
    // ---------------------------------------------------------------------

    /// <summary>Active, not hidden by a transparent CanvasGroup, and at least partly on screen.</summary>
    public bool IsVisibleToReader
    {
        get
        {
            if (hidden || !isActiveAndEnabled) return false;
            var groups = GetComponentsInParent<CanvasGroup>();
            foreach (var g in groups)
            {
                if (g.alpha <= 0.01f) return false;
                if (g.ignoreParentGroups) break;
            }
            Rect r = GetScreenRect(false);
            return r.width > 1 && r.height > 1 && r.xMax > 0 && r.yMax > 0 && r.xMin < UnityEngine.Screen.width && r.yMin < UnityEngine.Screen.height;
        }
    }

    static readonly Vector3[] corners = new Vector3[4];

    /// <summary>Screen rectangle. topLeftOrigin=true gives the coordinates the native screen reader expects.</summary>
    public Rect GetScreenRect(bool topLeftOrigin)
    {
        if (rect == null) return default;
        rect.GetWorldCorners(corners);
        Camera cam = null;
        if (canvas != null)
        {
            var root = canvas.rootCanvas;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay) cam = root.worldCamera;
        }
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        var r = UnityEngine.Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        if (topLeftOrigin) r.y = UnityEngine.Screen.height - r.yMax;
        return r;
    }

    // ---------------------------------------------------------------------
    // Actions (called by TalkBack double-tap or Voice Guide double-tap)
    // ---------------------------------------------------------------------

    public bool Activate()
    {
        if (!IsInteractable) { IIADSpeech.Say(GetLabel() + " is not available right now."); return false; }

        if (button != null) { button.onClick.Invoke(); return true; }
        if (toggle != null)
        {
            toggle.isOn = !toggle.isOn;
            IIADSpeech.Say(GetLabel() + (toggle.isOn ? ", on" : ", off"));
            return true;
        }
        if (input != null)
        {
            input.Select();
            input.ActivateInputField();       // opens the phone keyboard
            IIADSpeech.Say("Editing " + GetLabel() + ". Keyboard open.");
            return true;
        }
        if (dropdown != null) { dropdown.Show(); return true; }

        // Any other clickable thing (EventTrigger, IPointerClickHandler)
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null)
        {
            var data = new UnityEngine.EventSystems.PointerEventData(es);
            return UnityEngine.EventSystems.ExecuteEvents.Execute(gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }
        return false;
    }

    /// <summary>Swipe up/down on a slider or dropdown.</summary>
    public void Adjust(int direction)
    {
        if (slider != null)
        {
            float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) / 10f;
            slider.value = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);
            IIADSpeech.Say(GetValue());
        }
        else if (dropdown != null && dropdown.options.Count > 0)
        {
            dropdown.value = Mathf.Clamp(dropdown.value + direction, 0, dropdown.options.Count - 1);
            IIADSpeech.Say(GetValue());
        }
    }

    public bool IsAdjustable => slider != null || dropdown != null;

#if UNITY_EDITOR
    void OnValidate() { if (Application.isPlaying) IIADAccessibilityManager.MarkDirty(); }
    void Reset()
    {
        Cache();
        if (GetComponent<TMP_Text>() != null && GetComponent<Selectable>() == null) role = IIADRole.Text;
    }
#endif
}
