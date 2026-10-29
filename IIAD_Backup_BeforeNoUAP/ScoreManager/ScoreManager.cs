using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }


    // =========================================================
    // SCORE UI
    // =========================================================

    [Header("Score UI")]

    [SerializeField]
    private TMP_Text promptQualityText;

    [SerializeField]
    private TMP_Text posterMessageText;

    [SerializeField]
    private TMP_Text designQualityText;

    [SerializeField]
    private TMP_Text accessibilityText;

    [SerializeField]
    private TMP_Text finalDesignJustificationText;

    [SerializeField]
    private TMP_Text totalScoreText;


    // =========================================================
    // FEEDBACK UI
    // =========================================================

    [Header("Feedback UI")]

    [SerializeField]
    private TMP_Text feedbackText;

    [SerializeField]
    private TMP_Text suggestionText;


    // =========================================================
    // FINAL EXPLANATION
    // =========================================================

    [Header("Final Explanation")]

    [SerializeField]
    private TMP_InputField finalExplanationInput;


    // =========================================================
    // PANELS
    // =========================================================

    [Header("Panels")]

    [SerializeField]
    private GameObject finalExplanationPanel;

    [SerializeField]
    private GameObject scorePanel;

    [SerializeField]
    private GameObject feedbackPanel;


    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]

    [SerializeField]
    private Button calculateScoreButton;

    [SerializeField]
    private Button feedbackNextButton;


    // =========================================================
    // LOADING
    // =========================================================

    [Header("Loading Popup")]

    [SerializeField]
    private GameObject loadingPanel;

    [SerializeField]
    private TMP_Text loadingMessage;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsCalculating
    {
        get;
        private set;
    }


    public bool HasCalculatedScore
    {
        get;
        private set;
    }


    public string LastError
    {
        get;
        private set;
    }


    // =========================================================
    // CURRENT SCORE
    // =========================================================

    public int CurrentTotalScore
    {
        get;
        private set;
    }


    public int CurrentPromptQuality
    {
        get;
        private set;
    }


    public int CurrentPosterMessage
    {
        get;
        private set;
    }


    public int CurrentDesignQuality
    {
        get;
        private set;
    }


    public int CurrentAccessibilityUnderstanding
    {
        get;
        private set;
    }


    public int CurrentRevisionProcess
    {
        get;
        private set;
    }


    public int CurrentFinalExplanation
    {
        get;
        private set;
    }


    public int CurrentFinalDesignJustification
    {
        get;
        private set;
    }


    // =========================================================
    // FEEDBACK DATA
    // =========================================================

    public string CurrentFeedback
    {
        get;
        private set;
    }


    public string CurrentSuggestion
    {
        get;
        private set;
    }


    // =========================================================
    // SCORE SPEECH
    // =========================================================

    private string scoreSpeechText = "";


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        HideLoading();

        ClearScoreUI();
    }


    // =========================================================
    // CALCULATE SCORE
    // =========================================================
    //
    // Called by:
    //
    // Final Explanation
    //       ↓
    // Calculate Score
    //
    // =========================================================

    public async void CalculateScore()
    {
        if (IsCalculating)
        {
            Speak(
                "Please wait. Your submission is still being evaluated."
            );

            return;
        }


        LastError = "";


        // -----------------------------------------------------
        // CHECK PARTICIPANT MANAGER
        // -----------------------------------------------------

        if (
            ParticipantManager.Instance == null
        )
        {
            ShowError(
                "Participant Manager is not available."
            );

            return;
        }


        ParticipantData participant =
            ParticipantManager.Instance
                .CurrentParticipant;


        if (participant == null)
        {
            ShowError(
                "Participant data is not available."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK PROMPT
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                participant.prompt
            )
        )
        {
            ShowError(
                "Please enter a design prompt first."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK FINAL POSTER
        // -----------------------------------------------------

        string finalPoster =
            participant.GetLatestPosterUrl();


        if (
            string.IsNullOrWhiteSpace(
                finalPoster
            )
        )
        {
            ShowError(
                "Please generate your poster first."
            );

            return;
        }


        // -----------------------------------------------------
        // GET FINAL EXPLANATION
        // -----------------------------------------------------

        string finalExplanation =
            finalExplanationInput != null
                ? finalExplanationInput.text.Trim()
                : participant.finalExplanation;


        // -----------------------------------------------------
        // REQUIRED FINAL EXPLANATION
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                finalExplanation
            )
        )
        {
            ShowError(
                "Please enter your final explanation."
            );


            Speak(
                "Please enter your final explanation before calculating your score."
            );

            return;
        }


        // -----------------------------------------------------
        // SAVE FINAL EXPLANATION LOCALLY
        // -----------------------------------------------------

        participant.finalExplanation =
            finalExplanation;


        // -----------------------------------------------------
        // DETERMINE FINAL POSTER
        // -----------------------------------------------------
        //
        // If no revision:
        //
        // original poster
        //
        // If revision exists:
        //
        // latest revised poster
        //
        // -----------------------------------------------------

        participant.posterImageUrl =
            finalPoster;


        // -----------------------------------------------------
        // BUILD SCORE REQUEST
        // -----------------------------------------------------

        AIBackendManager.ScoreRequestData
            requestData =
                new AIBackendManager.ScoreRequestData
                {
                    userPrompt =
                        participant.prompt,

                    imageUrl =
                        finalPoster,

                    revisionPrompt =
                        participant.revisionPrompt,

                    revisionHistory =
                        participant.revisionHistory,

                    revisionCount =
                        participant.revisionCount,

                    finalExplanation =
                        finalExplanation
                };


        // -----------------------------------------------------
        // START
        // -----------------------------------------------------

        IsCalculating = true;


        SetCalculateButton(
            false
        );


        ShowLoading(
            "Evaluating your submission. Please wait."
        );


        Speak(
            "Evaluating your submission. Please wait."
        );


        try
        {
            // -------------------------------------------------
            // CHECK AI BACKEND
            // -------------------------------------------------

            if (
                AIBackendManager.Instance == null
            )
            {
                ShowError(
                    "AI Backend Manager is not available."
                );

                return;
            }


            // -------------------------------------------------
            // SAVE FINAL EXPLANATION FIRST
            // -------------------------------------------------

            bool explanationSaved =
                await ParticipantManager.Instance
                    .Save();


            if (!explanationSaved)
            {
                Debug.LogWarning(
                    "ScoreManager: Final explanation could not be saved before scoring."
                );
            }


            // -------------------------------------------------
            // REQUEST AI SCORE
            // -------------------------------------------------

            AIBackendManager.ScoreResult result =
                await AIBackendManager.Instance
                    .ScorePoster(
                        requestData
                    );


            // -------------------------------------------------
            // CHECK RESULT
            // -------------------------------------------------

            if (
                result == null ||
                !result.success ||
                result.score == null
            )
            {
                string error =
                    AIBackendManager.Instance
                        .LastError;


                if (
                    string.IsNullOrWhiteSpace(
                        error
                    )
                )
                {
                    error =
                        "Unable to calculate score. Please try again.";
                }


                ShowError(
                    error
                );


                Speak(
                    "Evaluation failed. Please try again."
                );


                return;
            }


            // -------------------------------------------------
            // DISPLAY SCORE
            // -------------------------------------------------

            DisplayScore(
                result.score
            );


            // -------------------------------------------------
            // SAVE SCORE
            // -------------------------------------------------

            await SaveScore();


            // -------------------------------------------------
            // OPEN SCORE PAGE
            // -------------------------------------------------

            OpenScorePanel();


            Speak(
                "Evaluation completed successfully. Opening your score."
            );
        }
        catch (Exception exception)
        {
            LastError =
                exception.Message;


            Debug.LogException(
                exception
            );


            ShowError(
                "Score calculation failed: " +
                exception.Message
            );


            Speak(
                "Evaluation failed. Please try again."
            );
        }
        finally
        {
            IsCalculating = false;


            SetCalculateButton(
                true
            );


            HideLoading();
        }
    }


    // =========================================================
    // DISPLAY SCORE
    // =========================================================
    //
    // IMPORTANT:
    //
    // Only FIVE visible categories.
    //
    // Revision Process and Final Explanation
    // are combined into Final Design Justification.
    //
    // =========================================================

    public void DisplayScore(
        AIBackendManager.ScoreBreakdown score)
    {
        if (score == null)
        {
            return;
        }


        // -----------------------------------------------------
        // INDIVIDUAL SCORES
        // -----------------------------------------------------

        CurrentPromptQuality =
            Mathf.Clamp(
                score.promptQuality,
                0,
                20
            );


        CurrentPosterMessage =
            Mathf.Clamp(
                score.posterMessage,
                0,
                20
            );


        CurrentDesignQuality =
            Mathf.Clamp(
                score.designQuality,
                0,
                20
            );


        CurrentAccessibilityUnderstanding =
            Mathf.Clamp(
                score.accessibilityUnderstanding,
                0,
                20
            );


        CurrentRevisionProcess =
            Mathf.Clamp(
                score.revisionProcess,
                0,
                10
            );


        CurrentFinalExplanation =
            Mathf.Clamp(
                score.finalExplanation,
                0,
                10
            );


        // -----------------------------------------------------
        // COMBINE FINAL 20
        // -----------------------------------------------------

        CurrentFinalDesignJustification =
            Mathf.Clamp(
                CurrentRevisionProcess +
                CurrentFinalExplanation,
                0,
                20
            );


        // -----------------------------------------------------
        // TOTAL
        // -----------------------------------------------------

        CurrentTotalScore =
            Mathf.Clamp(
                CurrentPromptQuality +

                CurrentPosterMessage +

                CurrentDesignQuality +

                CurrentAccessibilityUnderstanding +

                CurrentFinalDesignJustification,

                0,
                100
            );


        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        CurrentFeedback =
            score.feedback ?? "";


        CurrentSuggestion =
            score.improvementSuggestion ?? "";


        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (promptQualityText != null)
        {
            promptQualityText.text =
                CurrentPromptQuality +
                " / 20";
        }


        if (posterMessageText != null)
        {
            posterMessageText.text =
                CurrentPosterMessage +
                " / 20";
        }


        if (designQualityText != null)
        {
            designQualityText.text =
                CurrentDesignQuality +
                " / 20";
        }


        if (accessibilityText != null)
        {
            accessibilityText.text =
                CurrentAccessibilityUnderstanding +
                " / 20";
        }


        if (finalDesignJustificationText != null)
        {
            finalDesignJustificationText.text =
                CurrentFinalDesignJustification +
                " / 20";
        }


        if (totalScoreText != null)
        {
            totalScoreText.text =
                CurrentTotalScore +
                " / 100";
        }


        if (feedbackText != null)
        {
            feedbackText.text =
                CurrentFeedback;
        }


        if (suggestionText != null)
        {
            suggestionText.text =
                CurrentSuggestion;
        }


        // -----------------------------------------------------
        // SPEECH
        // -----------------------------------------------------

        BuildScoreSpeech();


        HasCalculatedScore =
            true;


        Debug.Log(
            "ScoreManager: Score displayed."
        );


        Debug.Log(
            "Prompt Quality = " +
            CurrentPromptQuality +
            "/20"
        );


        Debug.Log(
            "Poster Message = " +
            CurrentPosterMessage +
            "/20"
        );


        Debug.Log(
            "Design Quality = " +
            CurrentDesignQuality +
            "/20"
        );


        Debug.Log(
            "Accessibility Understanding = " +
            CurrentAccessibilityUnderstanding +
            "/20"
        );


        Debug.Log(
            "Final Design Justification = " +
            CurrentFinalDesignJustification +
            "/20"
        );


        Debug.Log(
            "TOTAL = " +
            CurrentTotalScore +
            "/100"
        );
    }


    // =========================================================
    // SAVE SCORE
    // =========================================================

    private async Task<bool> SaveScore()
    {
        if (ParticipantManager.Instance == null)
        {
            return false;
        }

        ParticipantData participant =
            ParticipantManager.Instance.CurrentParticipant;

        if (participant == null)
        {
            return false;
        }

        // SCORE
        participant.promptQuality =
            CurrentPromptQuality;

        participant.posterMessage =
            CurrentPosterMessage;

        participant.designQuality =
            CurrentDesignQuality;

        participant.accessibilityUnderstanding =
            CurrentAccessibilityUnderstanding;

        // FINAL DESIGN JUSTIFICATION
        participant.revisionProcessScore =
            CurrentRevisionProcess;

        participant.finalExplanationScore =
            CurrentFinalExplanation;

        // TOTAL
        participant.score =
            CurrentTotalScore;

        // FEEDBACK
        participant.feedback =
            CurrentFeedback ?? "";

        participant.improvementSuggestion =
            CurrentSuggestion ?? "";

        // FINAL EXPLANATION
        if (finalExplanationInput != null)
        {
            participant.finalExplanation =
                finalExplanationInput.text.Trim();
        }

        // FINAL POSTER
        participant.posterImageUrl =
            participant.GetLatestPosterUrl();

        // ---------------------------------------------------------
        // DO NOT SET isSubmitted HERE
        // ---------------------------------------------------------
        //
        // Score calculation != final submission.
        //
        // isSubmitted should only become true when the user
        // presses the final Submit button.
        //
        // ---------------------------------------------------------

        participant.lastPage =
            "Score";

        // SAVE SCORE / PROGRESS
        bool saved =
            await ParticipantManager.Instance
                .Save();

        if (!saved)
        {
            Debug.LogError(
                "ScoreManager: Failed to save score."
            );

            return false;
        }

        Debug.Log(
            "ScoreManager: Score saved successfully."
        );

        return true;
    }


    // =========================================================
    // OPEN SCORE PANEL
    // =========================================================

    public void OpenScorePanel()
    {
        if (
            finalExplanationPanel != null
        )
        {
            finalExplanationPanel.SetActive(
                false
            );
        }


        if (
            scorePanel != null
        )
        {
            scorePanel.SetActive(
                true
            );
        }


        if (
            feedbackPanel != null
        )
        {
            feedbackPanel.SetActive(
                false
            );
        }


        Debug.Log(
            "ScoreManager: Score panel opened."
        );


        // -----------------------------------------------------
        // READ SCORE AFTER PANEL IS VISIBLE
        // -----------------------------------------------------

        if (
            UAP_AccessibilityManager.IsEnabled()
        )
        {
            SpeakScore();
        }
    }


    // =========================================================
    // OPEN FEEDBACK
    // =========================================================
    //
    // Score Page
    //      ↓
    // NEXT
    //      ↓
    // Feedback Page
    //
    // =========================================================

    public void OpenFeedback()
    {
        if (
            scorePanel != null
        )
        {
            scorePanel.SetActive(
                false
            );
        }


        if (
            feedbackPanel != null
        )
        {
            feedbackPanel.SetActive(
                true
            );
        }


        Debug.Log(
            "ScoreManager: Feedback panel opened."
        );


        if (
            UAP_AccessibilityManager.IsEnabled()
        )
        {
            SpeakFeedback();
        }
    }


    // =========================================================
    // NEXT BUTTON
    // =========================================================

    public void OnFeedbackNext()
    {
        OpenFeedback();
    }


    // =========================================================
    // SCORE NEXT
    // =========================================================

    public void OnScoreNext()
    {
        OpenFeedback();
    }


    // =========================================================
    // READ SCORE
    // =========================================================

    public void SpeakScore()
    {
        if (
            !AccessibilityToggle
                .AccessibilityEnabled
        )
        {
            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                scoreSpeechText
            )
        )
        {
            BuildScoreSpeech();
        }


        Speak(
            scoreSpeechText
        );
    }


    // =========================================================
    // BUILD SCORE SPEECH
    // =========================================================

    private void BuildScoreSpeech()
    {
        scoreSpeechText =
            "Evaluation completed. "

            + "Total score: "
            + CurrentTotalScore
            + " out of one hundred. "

            + "Prompt quality: "
            + CurrentPromptQuality
            + " out of twenty. "

            + "Poster message and content: "
            + CurrentPosterMessage
            + " out of twenty. "

            + "Design output quality: "
            + CurrentDesignQuality
            + " out of twenty. "

            + "Accessibility understanding: "
            + CurrentAccessibilityUnderstanding
            + " out of twenty. "

            + "Final design justification: "
            + CurrentFinalDesignJustification
            + " out of twenty. ";
    }


    // =========================================================
    // READ FEEDBACK
    // =========================================================

    public void SpeakFeedback()
    {
        if (
            !AccessibilityToggle
                .AccessibilityEnabled
        )
        {
            return;
        }


        string speech =
            "Feedback. ";


        if (
            !string.IsNullOrWhiteSpace(
                CurrentFeedback
            )
        )
        {
            speech +=
                CurrentFeedback +
                ". ";
        }


        if (
            !string.IsNullOrWhiteSpace(
                CurrentSuggestion
            )
        )
        {
            speech +=
                "Improvement suggestion. " +
                CurrentSuggestion;
        }


        Speak(
            speech
        );
    }


    // =========================================================
    // LOADING
    // =========================================================

    public void ShowLoading(
        string message)
    {
        if (
            loadingPanel != null
        )
        {
            loadingPanel.SetActive(
                true
            );
        }


        if (
            loadingMessage != null
        )
        {
            loadingMessage.text =
                message;
        }
    }


    // =========================================================
    // HIDE LOADING
    // =========================================================

    public void HideLoading()
    {
        if (
            loadingPanel != null
        )
        {
            loadingPanel.SetActive(
                false
            );
        }
    }


    // =========================================================
    // VALIDATE FINAL EXPLANATION
    // =========================================================

    public bool ValidateFinalExplanation()
    {
        string explanation =
            finalExplanationInput != null
                ? finalExplanationInput.text.Trim()
                : "";


        if (
            string.IsNullOrWhiteSpace(
                explanation
            )
        )
        {
            ShowError(
                "Please explain your final poster before submitting."
            );


            return false;
        }


        return true;
    }


    // =========================================================
    // CALCULATE BUTTON STATE
    // =========================================================

    private void SetCalculateButton(
        bool interactable)
    {
        if (
            calculateScoreButton != null
        )
        {
            calculateScoreButton.interactable =
                interactable;
        }
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void ShowError(
        string message)
    {
        LastError =
            message;


        Debug.LogError(
            "ScoreManager: " +
            message
        );


        if (
            loadingMessage != null &&
            loadingPanel != null &&
            loadingPanel.activeSelf
        )
        {
            loadingMessage.text =
                message;
        }


        Speak(
            message
        );
    }


    // =========================================================
    // SPEECH
    // =========================================================

    private void Speak(
        string message)
    {
        if (
            string.IsNullOrWhiteSpace(
                message
            )
        )
        {
            return;
        }


        if (
            !AccessibilityToggle
                .AccessibilityEnabled
        )
        {
            return;
        }


        AccessibilityToggle
            .AccessibilitySpeech
            .SpeakNavigation(
                message
            );
    }


    // =========================================================
    // CLEAR SCORE UI
    // =========================================================

    public void ClearScoreUI()
    {
        CurrentTotalScore = 0;

        CurrentPromptQuality = 0;

        CurrentPosterMessage = 0;

        CurrentDesignQuality = 0;

        CurrentAccessibilityUnderstanding = 0;

        CurrentRevisionProcess = 0;

        CurrentFinalExplanation = 0;

        CurrentFinalDesignJustification = 0;

        CurrentFeedback = "";

        CurrentSuggestion = "";

        scoreSpeechText = "";

        HasCalculatedScore = false;


        if (promptQualityText != null)
        {
            promptQualityText.text =
                "0 / 20";
        }


        if (posterMessageText != null)
        {
            posterMessageText.text =
                "0 / 20";
        }


        if (designQualityText != null)
        {
            designQualityText.text =
                "0 / 20";
        }


        if (accessibilityText != null)
        {
            accessibilityText.text =
                "0 / 20";
        }


        if (finalDesignJustificationText != null)
        {
            finalDesignJustificationText.text =
                "0 / 20";
        }


        if (totalScoreText != null)
        {
            totalScoreText.text =
                "0 / 100";
        }


        if (feedbackText != null)
        {
            feedbackText.text =
                "No feedback available yet.";
        }


        if (suggestionText != null)
        {
            suggestionText.text =
                "No improvement suggestion yet.";
        }
    }


    // =========================================================
    // LOAD SAVED SCORE
    // =========================================================
    //
    // Used when an already-completed submission is opened.
    //
    // =========================================================

    public void LoadSavedScore()
    {
        if (
            ParticipantManager.Instance == null
        )
        {
            return;
        }


        ParticipantData participant =
            ParticipantManager.Instance
                .CurrentParticipant;


        if (
            participant == null
        )
        {
            return;
        }


        CurrentPromptQuality =
            participant.promptQuality;


        CurrentPosterMessage =
            participant.posterMessage;


        CurrentDesignQuality =
            participant.designQuality;


        CurrentAccessibilityUnderstanding =
            participant.accessibilityUnderstanding;


        CurrentRevisionProcess =
            participant.revisionProcessScore;


        CurrentFinalExplanation =
            participant.finalExplanationScore;


        CurrentFinalDesignJustification =
            Mathf.Clamp(
                CurrentRevisionProcess +
                CurrentFinalExplanation,
                0,
                20
            );


        CurrentTotalScore =
            participant.score;


        CurrentFeedback =
            participant.feedback;


        CurrentSuggestion =
            participant.improvementSuggestion;


        if (
            finalExplanationInput != null
        )
        {
            finalExplanationInput.text =
                participant.finalExplanation;
        }


        DisplaySavedScoreUI();
    }


    // =========================================================
    // DISPLAY SAVED SCORE
    // =========================================================

    private void DisplaySavedScoreUI()
    {
        if (promptQualityText != null)
        {
            promptQualityText.text =
                CurrentPromptQuality +
                " / 20";
        }


        if (posterMessageText != null)
        {
            posterMessageText.text =
                CurrentPosterMessage +
                " / 20";
        }


        if (designQualityText != null)
        {
            designQualityText.text =
                CurrentDesignQuality +
                " / 20";
        }


        if (accessibilityText != null)
        {
            accessibilityText.text =
                CurrentAccessibilityUnderstanding +
                " / 20";
        }


        if (finalDesignJustificationText != null)
        {
            finalDesignJustificationText.text =
                CurrentFinalDesignJustification +
                " / 20";
        }


        if (totalScoreText != null)
        {
            totalScoreText.text =
                CurrentTotalScore +
                " / 100";
        }


        if (feedbackText != null)
        {
            feedbackText.text =
                CurrentFeedback;
        }


        if (suggestionText != null)
        {
            suggestionText.text =
                CurrentSuggestion;
        }


        BuildScoreSpeech();


        HasCalculatedScore = participantIsSubmitted() || CurrentTotalScore > 0;
    }


    // =========================================================
    // CHECK COMPLETION
    // =========================================================

    private bool participantIsSubmitted()
    {
        if (ParticipantManager.Instance == null)
        {
            return false;
        }

        ParticipantData participant =
            ParticipantManager.Instance.CurrentParticipant;

        return
            participant != null &&
            participant.isSubmitted;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetScore()
    {
        IsCalculating = false;

        LastError = "";

        ClearScoreUI();

        HideLoading();

        SetCalculateButton(
            true
        );
    }
}