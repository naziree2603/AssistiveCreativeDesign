using TMPro;
using UnityEngine;
using KKSpeech;

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
        isRecording = true;

        SpeechRecognizer.StartRecording(true);

        UAP_AccessibilityManager.Say("Recording started. Please speak your poster prompt.", false,true);

        Debug.Log("Recording Started");
    }

    void StopRecording()
    {
        isRecording = false;

        SpeechRecognizer.StopIfRecording();

        UAP_AccessibilityManager.Say("Recording stopped.", false, true);

        Debug.Log("Recording Stopped");
    }

    public void OnFinalResult(string result)
    {
        promptInput.text = result;
    }
}