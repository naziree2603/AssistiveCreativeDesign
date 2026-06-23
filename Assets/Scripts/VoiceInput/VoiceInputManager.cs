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

    private TMP_InputField currentInput;
    private SpeechRecognizerListener listener;

    private bool isRecording = false;

    private bool canPressMic = true;
    [SerializeField] private float micCooldown = 1.5f;

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
        if (!canPressMic)
            return;

        StartCoroutine(MicCooldown());

        StartRecording();
    }

    private System.Collections.IEnumerator MicCooldown()
    {
        canPressMic = false;

        yield return new WaitForSeconds(micCooldown);

        canPressMic = true;
    }

    private void StartRecording()
    {
        Handheld.Vibrate();

        AndroidTTS.StopSpeaking();

        if (audioSource != null && startBeep != null)
            audioSource.PlayOneShot(startBeep);

        recordingStatusText.text =
            "Listening...";

        SpeechRecognizer.StartRecording(false);

        isRecording = true;
    }



    private void OnFinalResult(string result)
    {
        isRecording = false;

        if (string.IsNullOrEmpty(result))
        {
            recordingStatusText.text =
                "No speech detected";

            AndroidTTS.Speak(
                "No speech detected. Please try again."
            );

            return;
        }

        if (currentInput != null)
        {
            currentInput.text = result;
        }

        recordingStatusText.text =
            "Voice input completed";

        StartCoroutine(ReadResult(result));
    }

    private System.Collections.IEnumerator ReadResult(string result)
    {
        yield return new WaitForSeconds(0.5f);

        AndroidTTS.Speak(
        "Voice input completed. You entered. " + result);
    }

    public void ReplayInput()
    {
        if (currentInput != null)
        {
            AndroidTTS.Speak(currentInput.text);
        }
    }

    private void OnDestroy()
    {
        if (listener != null)
        {
            listener.onFinalResults.RemoveListener(OnFinalResult);
        }
    }
}