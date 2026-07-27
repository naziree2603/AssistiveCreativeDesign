using UnityEngine;
using TMPro;

public class AccessibilityToggle : MonoBehaviour
{
    public GameObject accessibilityManager;
    public TMP_Text buttonText;

    public static bool AccessibilityEnabled = true;

    public void ToggleAccessibility()
    {
        AccessibilityEnabled = !AccessibilityEnabled; 

        Debug.Log("Toggle pressed");
        Debug.Log("AccessibilityEnabled = " + AccessibilityEnabled);

        UAP_AccessibilityManager.EnableAccessibility(AccessibilityEnabled);

        buttonText.text = AccessibilityEnabled
            ? "Accessibility ON"
            : "Accessibility OFF";
    }

    public static class AccessibilitySpeech
    {
        public static void SpeakNavigation(string text)
        {
            if (!UAP_AccessibilityManager.IsEnabled())
                return;

            AndroidTTS.Speak(text);
        }

        public static void SpeakContent(string text)
        {
            if (!UAP_AccessibilityManager.IsEnabled())
                return;

            AndroidTTS.Speak(text);
        }
    }
}