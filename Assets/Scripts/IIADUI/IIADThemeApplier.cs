using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives a UI object the new IIAD style. Pick a role (Primary Button, Card, Title...) and it sets
/// colours, font, size and minimum touch size from the active IIADTheme. Runs in the Editor too,
/// so you see the result without pressing Play.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("IIAD/Theme Applier")]
public class IIADThemeApplier : MonoBehaviour
{
    public enum Role
    {
        None,
        ScreenBackground,
        Surface,          // cards, sheets
        SoftSurface,      // input boxes, list rows
        PrimaryButton,    // yellow: Generate, Use this version, Sign in
        SecondaryButton,  // outlined: Cancel, Read aloud
        DarkButton,       // ink: microphone
        DangerButton,     // Delete account
        Title,
        Heading,
        Body,
        Muted,
        Caption,
        Divider,
        SuccessText,
        ErrorText
    }

    public Role role = Role.None;

    [Tooltip("Optional: use a different theme than the active one.")]
    public IIADTheme themeOverride;

    [Tooltip("Make sure this object is at least the theme's minimum touch size.")]
    public bool enforceTouchSize = true;

    IIADTheme Theme => themeOverride != null ? themeOverride : IIADTheme.Active;

    void OnEnable() { IIADTheme.ThemeChanged += OnThemeChanged; Apply(); }
    void OnDisable() => IIADTheme.ThemeChanged -= OnThemeChanged;
#if UNITY_EDITOR
    void OnValidate() => UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
#endif
    void OnThemeChanged(IIADTheme _) => Apply();

    [ContextMenu("Apply Theme")]
    public void Apply()
    {
        var t = Theme;
        if (t == null || role == Role.None) return;

        var img = GetComponent<Image>();
        var txt = GetComponent<TMP_Text>();
        var childTxt = txt == null ? GetComponentInChildren<TMP_Text>(true) : null;
        var sel = GetComponent<Selectable>();

        switch (role)
        {
            case Role.ScreenBackground: Fill(img, t.background); break;
            case Role.Surface: Fill(img, t.surface); break;
            case Role.SoftSurface: Fill(img, t.softSurface); break;
            case Role.Divider: Fill(img, t.line); break;

            case Role.PrimaryButton: Button(img, childTxt, sel, t, t.signal, t.onSignal); break;
            case Role.SecondaryButton: Button(img, childTxt, sel, t, t.surface, t.text); break;
            case Role.DarkButton: Button(img, childTxt, sel, t, t.ink, t.onInk); break;
            case Role.DangerButton: Button(img, childTxt, sel, t, t.surface, t.danger); break;

            case Role.Title: Text(txt, Display(t), t.titleSize, t.text, FontStyles.Bold); break;
            case Role.Heading: Text(txt, Display(t), t.headingSize, t.text, FontStyles.Bold); break;
            case Role.Body: Text(txt, t.bodyFont, t.bodySize, t.text, FontStyles.Normal); break;
            case Role.Muted: Text(txt, t.bodyFont, t.bodySize, t.mutedText, FontStyles.Normal); break;
            case Role.Caption: Text(txt, t.bodyFont, t.captionSize, t.mutedText, FontStyles.Bold); break;
            case Role.SuccessText: Text(txt, t.bodyFont, t.bodySize, t.success, FontStyles.Bold); break;
            case Role.ErrorText: Text(txt, t.bodyFont, t.bodySize, t.danger, FontStyles.Bold); break;
        }

        if (enforceTouchSize && sel != null && transform is RectTransform rt)
        {
            var le = GetComponent<LayoutElement>();
            if (le != null) le.minHeight = Mathf.Max(le.minHeight, t.minTouchSize);
            else if (rt.rect.height > 0 && rt.rect.height < t.minTouchSize && rt.anchorMin.y == rt.anchorMax.y)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, t.minTouchSize);
        }
    }

    static TMP_FontAsset Display(IIADTheme t) => t.displayFont != null ? t.displayFont : t.bodyFont;

    static void Fill(Image img, Color c) { if (img != null) img.color = c; }

    static void Text(TMP_Text txt, TMP_FontAsset font, float size, Color c, FontStyles style)
    {
        if (txt == null) return;
        if (font != null) txt.font = font;
        txt.fontSize = size;
        txt.color = c;
        txt.fontStyle = style;
        txt.enableAutoSizing = false; // fixed sizes read more consistently; wrap instead of shrinking
    }

    static void Button(Image img, TMP_Text label, Selectable sel, IIADTheme t, Color fill, Color fg)
    {
        Fill(img, fill);
        if (label != null) Text(label, t.bodyFont, t.buttonSize, fg, FontStyles.Bold);
        if (sel != null)
        {
            var cb = sel.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.94f, 0.94f, 0.94f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            sel.colors = cb;
        }
    }
}
