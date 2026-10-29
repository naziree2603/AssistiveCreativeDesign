using TMPro;
using UnityEngine;
using KKSpeech;

/// <summary>
/// Microphone button for the Idea Prompt field.
/// Spoken states: Recording (vibration only, so the voice is not recorded), Recording stopped,
/// Got it, No speech heard.
/// </summary>
public class PromptVoiceInput : MonoBehaviour
{
    public TMP_InputField promptInput;

    private bool isRecording = false;

    public void ToggleRecording()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    void StartRecording()
    {
        if (!Application.isEditor && SpeechRecognizer.GetAuthorizationStatus() != AuthorizationStatus.Authorized)
        {
            SpeechRecognizer.RequestAccess();
            IIADSpeech.Say("Microphone permission is needed. Allow it, then tap the microphone again. You can also type your idea.");
            return;
        }

        IIADSpeech.Stop();          // never record the app's own voice
        Handheld.Vibrate();         // start signal that blind users can feel

        isRecording = true;
        SpeechRecognizer.StartRecording(true);

        Debug.Log("Recording Started");
    }

    void StopRecording()
    {
        isRecording = false;

        SpeechRecognizer.StopIfRecording();

        IIADSpeech.Say("Recording stopped.");

        Debug.Log("Recording Stopped");
    }

    public void OnFinalResult(string result)
    {
        isRecording = false;

        if (string.IsNullOrWhiteSpace(result))
        {
            IIADSpeech.Say("No speech heard. Tap the microphone and try again.");
            return;
        }

        promptInput.text = result;
        promptInput.caretPosition = promptInput.text.Length;

        IIADSpeech.Say("Got it. Your idea is: " + result);
    }
}
