using KKSpeech;
using TMPro;
using UnityEngine;

public class VoiceInputManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text recordingStatusText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip startBeep;
    public AudioClip stopBeep;

    private TMP_InputField currentInput;
    private SpeechRecognizerListener listener;

    private bool isRecording = false;

    private void Start()
    {
        listener = FindFirstObjectByType<SpeechRecognizerListener>();

        if (listener != null)
        {
            listener.onFinalResults.AddListener(OnFinalResult);
        }
        else
        {
            Debug.LogError("SpeechRecognizerListener not found in scene!");
        }

        SpeechRecognizer.RequestAccess();

        // Choose ONE language
        //SpeechRecognizer.SetDetectionLanguage("en-US");
        SpeechRecognizer.SetDetectionLanguage("ms-MY");

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Ready";
        }
    }

    public void SetTargetInput(TMP_InputField input)
    {
        currentInput = input;
    }

    public void ToggleMic()
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

    private void StartRecording()
    {
        Debug.Log("Recording Started");

        Handheld.Vibrate();

        if (audioSource != null && startBeep != null)
        {
            audioSource.PlayOneShot(startBeep);
        }

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Listening...";
        }

        SpeechRecognizer.StartRecording(false);

        isRecording = true;
    }

    private void StopRecording()
    {
        Debug.Log("Recording Stopped");

        SpeechRecognizer.StopIfRecording();

        Handheld.Vibrate();

        if (audioSource != null && stopBeep != null)
        {
            audioSource.PlayOneShot(stopBeep);
        }

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Processing...";
        }

        isRecording = false;
    }

    private void OnFinalResult(string result)
    {
        Debug.Log("Speech Result: " + result);

        if (currentInput != null)
        {
            currentInput.text = result;
        }

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Voice input completed";
        }

        AndroidTTS.Speak(
            "Voice input completed."
        );

        isRecording = false;
    }

    private void OnDestroy()
    {
        if (listener != null)
        {
            listener.onFinalResults.RemoveListener(OnFinalResult);
        }
    }
}