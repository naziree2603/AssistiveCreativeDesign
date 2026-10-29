using UnityEngine;

/// <summary>
/// Put one on every panel (Login, Dashboard, Prompt, Output, Score, popups...).
/// When the panel becomes active the screen reader is told the screen changed,
/// focus moves to the first item, and the title / step is spoken.
/// Replaces UAP's AccessibleUIGroupRoot.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("IIAD/Accessible Screen")]
public class IIADScreen : MonoBehaviour
{
    [Tooltip("Spoken name of the screen, e.g. 'Idea prompt'.")]
    public string title = "";

    [Tooltip("Optional progress, e.g. 'Step 1 of 4'.")]
    public string step = "";

    [Tooltip("Optional one-line instruction read after the title, e.g. 'Describe the poster you want.'")]
    [TextArea(1, 3)] public string instruction = "";

    [Tooltip("Popups and bottom sheets: while open, only this screen's items can be reached.")]
    public bool isModal = false;

    [Tooltip("Speak the title when the screen opens. Turn off if an existing script already announces this page.")]
    public bool announceOnOpen = true;

    [Tooltip("Optional: the item focused first. Otherwise the first item marked 'Focus First', or the top item.")]
    public IIADAccessibleElement firstFocus;

    internal float openedAt;

    void OnEnable()
    {
        openedAt = Time.unscaledTime;
        IIADAccessibilityManager.ScreenOpened(this);
    }

    void OnDisable() => IIADAccessibilityManager.ScreenClosed(this);

    /// <summary>"Idea prompt. Step 1 of 4. Describe the poster you want."</summary>
    public string GetAnnouncement()
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(title)) sb.Append(title.Trim()).Append(". ");
        if (!string.IsNullOrWhiteSpace(step)) sb.Append(step.Trim()).Append(". ");
        if (!string.IsNullOrWhiteSpace(instruction)) sb.Append(instruction.Trim());
        return sb.ToString().Trim();
    }

    /// <summary>Speak the screen again (two-finger double-tap in Voice Guide, or a "Repeat" button).</summary>
    public void Repeat() => IIADSpeech.Say(GetAnnouncement());
}
