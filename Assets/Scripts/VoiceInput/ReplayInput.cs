using TMPro;
using UnityEngine;
using static AccessibilityToggle;

public class ReplayInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void Replay()
    {
        if (inputField == null)
            return;

        string text = inputField.text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("The input field is empty.");
            return;
        }

        AndroidTTS.StopSpeaking();
        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(text);
    }
}