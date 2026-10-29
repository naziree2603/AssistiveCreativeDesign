using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The 4-step bar at the top of the poster studio: Idea, Poster, Explain, Score.
/// Shows the step visually and gives the screen reader "Step 2 of 4, Poster".
/// Put it on the bar; add one Image per step as children (in order) and an optional label text.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("IIAD/Step Indicator")]
public class IIADStepIndicator : MonoBehaviour
{
    public string[] stepNames = { "Idea", "Poster", "Explain", "Score" };

    [Range(1, 8)] public int currentStep = 1;

    [Tooltip("e.g. 'Practice' or 'Competition'. Shown after the step name.")]
    public string modeName = "Practice";

    public Image[] segments;
    public TMP_Text label;

    [Tooltip("If set, the screen's 'Step' text is kept in sync so it is spoken when the screen opens.")]
    public IIADScreen screen;

    void OnEnable() => Refresh();
#if UNITY_EDITOR
    void OnValidate() => UnityEditor.EditorApplication.delayCall += () => { if (this != null) Refresh(); };
#endif

    public void SetStep(int step)
    {
        currentStep = Mathf.Clamp(step, 1, stepNames.Length);
        Refresh();
    }

    public void SetMode(string mode) { modeName = mode; Refresh(); }

    public string SpokenText => $"Step {currentStep} of {stepNames.Length}, {stepNames[Mathf.Clamp(currentStep - 1, 0, stepNames.Length - 1)]}";

    void Refresh()
    {
        var t = IIADTheme.Active;
        Color on = t != null ? t.text : Color.black;
        Color off = t != null ? t.line : new Color(0.85f, 0.86f, 0.83f);
        if (segments != null)
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].color = i < currentStep ? on : off;

        if (label != null)
            label.text = $"STEP {currentStep} OF {stepNames.Length} · {stepNames[Mathf.Clamp(currentStep - 1, 0, stepNames.Length - 1)].ToUpperInvariant()}"
                       + (string.IsNullOrEmpty(modeName) ? "" : " · " + modeName.ToUpperInvariant());

        if (screen != null) screen.step = SpokenText;
    }
}
