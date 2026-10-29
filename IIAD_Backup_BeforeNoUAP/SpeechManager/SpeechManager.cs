using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeechManager : MonoBehaviour
{
    public static SpeechManager Instance { get; private set; }


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Speech Settings")]

    [SerializeField]
    private bool speakAutomatically = true;


    [SerializeField]
    [Range(0f, 100f)]
    private float speechRate = 50f;


    [SerializeField]
    private bool stopPreviousSpeech = true;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsSpeaking
    {
        get;
        private set;
    }


    public bool IsAccessibilityEnabled
    {
        get
        {
            return AccessibilityToggle
                .AccessibilityEnabled;
        }
    }


    public string LastSpokenText
    {
        get;
        private set;
    }


    public float SpeechRate
    {
        get
        {
            return speechRate;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);


        LoadSpeechRate();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ApplySpeechRate();
    }


    // =========================================================
    // SPEAK
    // =========================================================

    public void Speak(
        string text)
    {
        if (!speakAutomatically)
        {
            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                text
            )
        )
        {
            return;
        }


        if (!IsAccessibilityEnabled)
        {
            return;
        }


        LastSpokenText =
            text;


        IsSpeaking =
            true;


        if (stopPreviousSpeech)
        {
            Stop();
        }


        try
        {
            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(
                    text
                );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "SpeechManager Speak Error: " +
                exception.Message
            );


            IsSpeaking =
                false;
        }
    }


    // =========================================================
    // FORCE SPEAK
    // =========================================================
    // Use this when the app needs to speak regardless of the
    // automatic speech preference.
    // =========================================================

    public void ForceSpeak(
        string text)
    {
        if (
            string.IsNullOrWhiteSpace(
                text
            )
        )
        {
            return;
        }


        if (!IsAccessibilityEnabled)
        {
            return;
        }


        LastSpokenText =
            text;


        IsSpeaking =
            true;


        try
        {
            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(
                    text
                );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "SpeechManager ForceSpeak Error: " +
                exception.Message
            );


            IsSpeaking =
                false;
        }
    }


    // =========================================================
    // REPLAY
    // =========================================================

    public void Replay()
    {
        if (
            string.IsNullOrWhiteSpace(
                LastSpokenText
            )
        )
        {
            return;
        }


        Speak(
            LastSpokenText
        );
    }


    // =========================================================
    // REPLAY SPECIFIC TEXT
    // =========================================================

    public void ReplayText(
        string text)
    {
        if (
            string.IsNullOrWhiteSpace(
                text
            )
        )
        {
            return;
        }


        LastSpokenText =
            text;


        Speak(
            text
        );
    }


    // =========================================================
    // SPEAK INPUT FIELD
    // =========================================================

    public void SpeakInputField(
        TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }


        string text =
            inputField.text;


        if (
            string.IsNullOrWhiteSpace(
                text
            )
        )
        {
            Speak(
                "The input field is empty."
            );

            return;
        }


        Speak(
            text
        );
    }


    // =========================================================
    // SPEAK TEXT COMPONENT
    // =========================================================

    public void SpeakText(
        TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            return;
        }


        Speak(
            textComponent.text
        );
    }


    // =========================================================
    // SPEAK BUTTON
    // =========================================================

    public void SpeakButton(
        Button button)
    {
        if (button == null)
        {
            return;
        }


        string buttonName =
            button.gameObject.name;


        Speak(
            buttonName
        );
    }


    // =========================================================
    // SPEAK STATUS
    // =========================================================

    public void SpeakStatus(
        string status)
    {
        Speak(
            status
        );
    }


    // =========================================================
    // SPEAK NAVIGATION
    // =========================================================

    public void SpeakNavigation(
        string message)
    {
        Speak(
            message
        );
    }


    // =========================================================
    // STOP
    // =========================================================

    public void Stop()
    {
        try
        {
            /*
             * The actual TTS implementation is handled by
             * AccessibilityToggle.
             *
             * If your AccessibilityToggle exposes a Stop()
             * method, it can be called here.
             *
             * We intentionally don't call an unknown method
             * so this manager remains compatible with the
             * current AccessibilityToggle script.
             */
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "SpeechManager Stop Error: " +
                exception.Message
            );
        }


        IsSpeaking =
            false;
    }


    // =========================================================
    // SET SPEECH RATE
    // =========================================================

    public void SetSpeechRate(
        float rate)
    {
        speechRate =
            Mathf.Clamp(
                rate,
                0f,
                100f
            );


        PlayerPrefs.SetFloat(
            "Accessibility_Speech_Rate",
            speechRate
        );


        PlayerPrefs.Save();


        ApplySpeechRate();
    }


    // =========================================================
    // GET SPEECH RATE
    // =========================================================

    public float GetSpeechRate()
    {
        return speechRate;
    }


    // =========================================================
    // LOAD SPEECH RATE
    // =========================================================

    private void LoadSpeechRate()
    {
        speechRate =
            PlayerPrefs.GetFloat(
                "Accessibility_Speech_Rate",
                50f
            );
    }


    // =========================================================
    // APPLY SPEECH RATE
    // =========================================================

    private void ApplySpeechRate()
    {
        /*
         * Your existing AccessibilityToggle /
         * AndroidTTS implementation controls the actual
         * Android Text-to-Speech speed.
         *
         * The value is stored here so all managers use
         * the same application speech-rate setting.
         */
    }


    public void ToggleAccessibility()
    {
        AccessibilityToggle.AccessibilityEnabled =
            !AccessibilityToggle.AccessibilityEnabled;

        if (!AccessibilityToggle.AccessibilityEnabled)
        {
            Stop();
        }
    }

    public void SetAccessibilityEnabled(bool enabled)
    {
        AccessibilityToggle.AccessibilityEnabled =
            enabled;

        if (!enabled)
        {
            Stop();
        }
    }



    // =========================================================
    // CHECK ACCESSIBILITY
    // =========================================================

    public bool IsEnabled()
    {
        return AccessibilityToggle
            .AccessibilityEnabled;
    }


    // =========================================================
    // SET AUTOMATIC SPEECH
    // =========================================================

    public void SetAutomaticSpeech(
        bool enabled)
    {
        speakAutomatically =
            enabled;


        if (!enabled)
        {
            Stop();
        }
    }


    // =========================================================
    // GET AUTOMATIC SPEECH
    // =========================================================

    public bool GetAutomaticSpeech()
    {
        return speakAutomatically;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetSpeech()
    {
        Stop();


        LastSpokenText =
            "";


        LoadSpeechRate();
    }
}