using UnityEngine;
using TMPro;

/// <summary>
/// Header accessibility button. It now switches Voice Guide on and off (the app's own
/// screen reading for people who do not use TalkBack). TalkBack is detected automatically.
///
/// The old static API is kept so the rest of the project does not need changes:
///   AccessibilityToggle.AccessibilityEnabled
///   AccessibilityToggle.AccessibilitySpeech.SpeakNavigation / SpeakContent
/// No UAP code is used any more.
/// </summary>
public class AccessibilityToggle : MonoBehaviour
{
    [Tooltip("Not used any more (was the UAP Accessibility Manager). Safe to leave empty.")]
    public GameObject accessibilityManager;

    public TMP_Text buttonText;

    /// <summary>
    /// True when anything will be heard: TalkBack is on, or Voice Guide is on.
    /// Setting it switches Voice Guide.
    /// </summary>
    public static bool AccessibilityEnabled
    {
        get => IIADSpeech.IsAnyOutputOn;
        set => IIADSpeech.VoiceGuideEnabled = value;
    }

    void OnEnable()
    {
        IIADSpeech.VoiceGuideChanged += OnChanged;
        Refresh();
    }

    void OnDisable() => IIADSpeech.VoiceGuideChanged -= OnChanged;

    void OnChanged(bool _) => Refresh();

    public void ToggleAccessibility()
    {
        IIADVoiceGuide.Toggle();
        Refresh();
    }

    void Refresh()
    {
        if (buttonText == null) return;
        buttonText.text = IIADSpeech.IsScreenReaderOn ? "TalkBack ON"
                        : IIADSpeech.VoiceGuideEnabled ? "Voice Guide ON"
                        : "Voice Guide OFF";
    }

    public static class AccessibilitySpeech
    {
        /// <summary>Short navigation messages: page names, "Generating poster", errors.</summary>
        public static void SpeakNavigation(string text) => IIADSpeech.Say(text);

        /// <summary>Longer content: descriptions, feedback, scores.</summary>
        public static void SpeakContent(string text) => IIADSpeech.Say(text);

        public static void Stop() => IIADSpeech.Stop();
    }
}
