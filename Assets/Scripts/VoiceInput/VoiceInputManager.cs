using KKSpeech;
using TMPro;
using UnityEngine;
using static AccessibilityToggle;

public class VoiceInputManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text recordingStatusText;
    public GameObject voiceLoadingPanel;

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

        SpeechRecognizer.SetDetectionLanguage("ms-MY");

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Ready";
        }

        if (voiceLoadingPanel != null)
        {
            voiceLoadingPanel.SetActive(false);
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

        if (voiceLoadingPanel != null)
        {
            voiceLoadingPanel.SetActive(true);
        }

        if (audioSource != null && startBeep != null)
            audioSource.PlayOneShot(startBeep);

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Listening...";
        }

        SpeechRecognizer.StartRecording(false);

        isRecording = true;
    }



    private void OnFinalResult(string result)
    {
        isRecording = false;

        if (voiceLoadingPanel != null)
        {
            voiceLoadingPanel.SetActive(false);
        }

        if (string.IsNullOrEmpty(result))
        {
            if (recordingStatusText != null)
            {
                recordingStatusText.text = "No speech detected";
            }

            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "No speech detected. Please try again."
            );

            return;
        }

        if (currentInput != null)
        {
            currentInput.text = result;
        }

        if (recordingStatusText != null)
        {
            recordingStatusText.text = "Voice input completed";
        }

        StartCoroutine(ReadResult(result));
    }

    private System.Collections.IEnumerator ReadResult(string result)
    {
        yield return new WaitForSeconds(0.5f);

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
        "Voice input completed. You entered. " + result);
    }


    private void OnDestroy()
    {
        if (listener != null)
        {
            listener.onFinalResults.RemoveListener(OnFinalResult);
        }
    }
}