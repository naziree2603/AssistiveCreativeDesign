using KKSpeech;
using TMPro;
using UnityEngine;

public class VoiceInputManager : MonoBehaviour
{
    public TMP_InputField currentInput;

    private SpeechRecognizerListener listener;

    private void Start()
    {
        listener = FindFirstObjectByType<SpeechRecognizerListener>();

        listener.onFinalResults.AddListener(OnFinalResult);

        SpeechRecognizer.RequestAccess();

        SpeechRecognizer.SetDetectionLanguage("ms-MY");
    }

    public void SetTargetInput(TMP_InputField input)
    {
        currentInput = input;
    }

    public void StartMic()
    {
        SpeechRecognizer.StartRecording(false);
    }

    private void OnFinalResult(string result)
    {
        Debug.Log("Speech Result: " + result);

        if (currentInput != null)
        {
            currentInput.text = result;
        }
    }
}