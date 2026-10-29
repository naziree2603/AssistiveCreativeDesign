using TMPro;
using UnityEngine;

/// <summary>
/// The new IIAD look in one asset: colours, fonts, sizes. Every IIADThemeApplier reads from
/// the active theme, so changing a colour here updates the whole app.
/// Create with Tools > IIAD > Create Default Themes.
///
/// Sizes are in canvas units for the project's 1080 x 1920 reference resolution
/// (about 3 units = 1 dp on a phone).
/// </summary>
[CreateAssetMenu(menuName = "IIAD/UI Theme", fileName = "IIAD Theme")]
public class IIADTheme : ScriptableObject
{
    [Header("Colours")]
    public Color background = Hex("#F2F3EF");
    public Color surface = Hex("#FFFFFF");
    public Color softSurface = Hex("#F7F7F3");     // input fields, list rows
    public Color text = Hex("#14181F");
    public Color mutedText = Hex("#59606C");
    public Color line = Hex("#D9DCD4");
    [Tooltip("Main action colour (Generate, Use this version). Yellow on dark text: contrast 11:1.")]
    public Color signal = Hex("#FFC233");
    public Color onSignal = Hex("#14181F");
    [Tooltip("Strong dark buttons (microphone, Competition card).")]
    public Color ink = Hex("#14181F");
    public Color onInk = Hex("#FFFFFF");
    public Color success = Hex("#0D7A5F");
    public Color danger = Hex("#B8361F");
    public Color focus = Hex("#1F5EFF");

    [Header("Fonts (TextMesh Pro)")]
    [Tooltip("Body and buttons. Recommended: Atkinson Hyperlegible (free, made for low-vision readers). Lexend, already in Arts/Fonts, also works.")]
    public TMP_FontAsset bodyFont;
    [Tooltip("Screen titles. Recommended: Bricolage Grotesque ExtraBold, or the body font in Bold.")]
    public TMP_FontAsset displayFont;

    [Header("Text sizes")]
    public float titleSize = 72f;      // ~24 dp
    public float headingSize = 56f;    // ~19 dp
    public float bodySize = 50f;       // ~17 dp
    public float buttonSize = 50f;
    public float captionSize = 40f;    // ~13 dp, never smaller

    [Header("Shapes")]
    [Tooltip("Smallest height for anything tappable: 48 dp.")]
    public float minTouchSize = 144f;
    public float buttonHeight = 156f;
    public float cornerRadius = 42f;
    public float screenPadding = 48f;
    public float gap = 36f;

    // ---------------------------------------------------------------------

    static IIADTheme active;

    /// <summary>The theme in use. Loaded from Resources/IIAD Theme if none was set.</summary>
    public static IIADTheme Active
    {
        get
        {
            if (active == null) active = Resources.Load<IIADTheme>("IIAD Theme");
            return active;
        }
        set
        {
            active = value;
            ThemeChanged?.Invoke(value);
        }
    }

    public static event System.Action<IIADTheme> ThemeChanged;

    void OnValidate() { if (active == this) ThemeChanged?.Invoke(this); }

    public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}
