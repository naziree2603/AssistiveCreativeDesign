using TMPro;
using UnityEngine;

public class IIADVoiceInput : MonoBehaviour
{
    [Header("Input Field")]
    [SerializeField]
    private TMP_InputField targetInput;


    [Header("Microphone Button")]
    [SerializeField]
    private GameObject microphoneButton;


    // =========================================================
    // START LISTENING
    // =========================================================

    public void StartVoiceInput()
    {
        if (targetInput == null)
            return;


        if (AccessibilityToggle.AccessibilityEnabled)
        {
            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(
                    "Listening. Please speak."
                );
        }


        // =====================================================
        // YOUR EXISTING SPEECH RECOGNITION SYSTEM
        // =====================================================
        //
        // Connect your existing KKSpeech /
        // VoiceInputManager here.
        //
    }


    // =========================================================
    // RECEIVE SPEECH RESULT
    // =========================================================

    public void SetSpeechResult(
        string result)
    {
        if (targetInput == null)
            return;


        if (string.IsNullOrWhiteSpace(result))
            return;


        targetInput.text =
            result;


        targetInput.caretPosition =
            targetInput.text.Length;


        if (AccessibilityToggle.AccessibilityEnabled)
        {
            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(
                    "Your speech has been entered."
                );
        }
    }
}