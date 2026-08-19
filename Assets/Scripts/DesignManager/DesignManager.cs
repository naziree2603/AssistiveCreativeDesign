using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DesignMode
{
    Competition,
    Practice
}

public class DesignManager : MonoBehaviour
{
    public static DesignManager Instance { get; private set; }

    // =========================================================
    // AI BACKEND
    // =========================================================

    [Header("AI Backend")]
    [SerializeField] private AIBackendManager aiBackendManager;


    // =========================================================
    // PANELS
    // =========================================================

    [Header("Design Workspace Panels")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private GameObject outputPanel;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private GameObject revisionPanel;
    [SerializeField] private GameObject finalExplanationPanel;
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private GameObject feedbackPanel;


    [Header("Original Full Poster Panel")]
    [SerializeField] private GameObject originalFullPosterPanel;
    [SerializeField] private RawImage originalFullPosterImage;


    [Header("Latest Full Poster Panel")]
    [SerializeField] private GameObject latestFullPosterPanel;
    [SerializeField] private RawImage latestFullPosterImage;


    // =========================================================
    // PROMPT UI
    // =========================================================

    [Header("Idea Prompt UI")]
    [SerializeField] private TMP_InputField promptInput;
    [SerializeField] private TMP_Text promptStatusText;
    [SerializeField] private TMP_Text promptMessageText;
    [SerializeField] private Button generatePosterButton;
    [SerializeField] private Button promptNextButton;
    [SerializeField] private Button promptBackButton;


    // =========================================================
    // OUTPUT UI
    // =========================================================

    [Header("Output UI")]
    [SerializeField] private RawImage outputPosterImage;
    [SerializeField] private Button outputBackButton;
    [SerializeField] private Button outputNextButton;


    // =========================================================
    // DESCRIPTION UI
    // =========================================================

    [Header("Description UI")]
    [SerializeField] private RawImage descriptionPosterImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text descriptionStatusText;
    [SerializeField] private Button descriptionBackButton;
    [SerializeField] private Button descriptionNextButton;


    // =========================================================
    // REVISION UI
    // =========================================================

    [Header("Revision UI")]
    [SerializeField] private RawImage revisionPosterImage;
    [SerializeField] private TMP_InputField revisionPromptInput;
    [SerializeField] private TMP_Text revisionAttemptText;
    [SerializeField] private Button revisionBackButton;
    [SerializeField] private Button reviseButton;
    [SerializeField] private Button revisionNextButton;


    // =========================================================
    // FINAL EXPLANATION UI
    // =========================================================

    [Header("Final Explanation UI")]
    [SerializeField] private RawImage finalExplanationPosterImage;
    [SerializeField] private TMP_InputField finalExplanationInput;
    [SerializeField] private TMP_Text finalExplanationStatusText;
    [SerializeField] private Button finalExplanationBackButton;
    [SerializeField] private Button calculateScoreButton;
    [SerializeField] private Button finalExplanationNextButton;


    // =========================================================
    // SCORE UI
    // =========================================================

    [Header("Score UI")]
    [SerializeField] private TMP_Text promptQualityText;
    [SerializeField] private TMP_Text posterMessageText;
    [SerializeField] private TMP_Text designQualityText;
    [SerializeField] private TMP_Text accessibilityText;
    [SerializeField] private TMP_Text finalDesignJustificationText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private Button scoreBackButton;
    [SerializeField] private Button scoreNextButton;


    // =========================================================
    // FEEDBACK UI
    // =========================================================

    [Header("Feedback UI")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text improvementSuggestionText;
    [SerializeField] private Button feedbackBackButton;
    [SerializeField] private Button feedbackNextButton;


    // =========================================================
    // FULLSCREEN BUTTONS
    // =========================================================

    [Header("Fullscreen Navigation")]
    [SerializeField] private Button originalFullPosterBackButton;
    [SerializeField] private Button latestFullPosterBackButton;


    // =========================================================
    // STATUS
    // =========================================================

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;


    // =========================================================
    // LOADING
    // =========================================================

    [Header("Loading Popup")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingMessage;


    // =========================================================
    // STATE
    // =========================================================

    public DesignMode CurrentMode
    {
        get;
        private set;
    }

    private const int MAX_REVISION_COUNT = 3;

    public int CurrentRevisionCount { get; private set; }

    public bool IsProcessing { get; private set; }

    // Compatibility property.
    // Internally the real value is ParticipantData.isSubmitted.
    

    private bool originalPosterGenerated;
    private bool scoreCalculated;

    // True when viewing an already submitted design.
    // In this mode the user can navigate,
    // but cannot edit, generate, revise, or calculate.
    private bool submittedViewMode;

    public string OriginalPrompt { get; private set; }

    public string CurrentPosterUrl { get; private set; }

    public string CurrentPosterDescription { get; private set; }

    public string LastRevisionPrompt { get; private set; }


    // =========================================================
    // PRACTICE DATA
    // =========================================================

    private PracticeData practiceData;



    public PracticeData CurrentPracticeData
    {
        get
        {
            return practiceData;
        }
    }

    private void CreatePracticeData()
    {
        practiceData = new PracticeData();

        Debug.Log(
            "DesignManager: New PracticeData created."
        );
    }


    // =========================================================
    // FULLSCREEN RETURN
    // =========================================================

    private enum FullPosterReturnPage
    {
        None,
        Output,
        Description,
        Revision,
        FinalExplanation,
        Score,
        Feedback
    }

    private FullPosterReturnPage latestFullPosterReturnPage =
        FullPosterReturnPage.None;


    // =========================================================
    // REVISION HISTORY
    // =========================================================

    [Serializable]
    private class RevisionEntry
    {
        public int revisionNumber;
        public string request;
    }

    private List<RevisionEntry> revisionHistory =
        new List<RevisionEntry>();



    // =========================================================
    // HOME CONFIRMATION POPUP
    // =========================================================

    [Header("Home Confirmation Popup")]

    [SerializeField]
    private GameObject homeConfirmationPopup;

    [SerializeField]
    private TMP_Text homeConfirmationTitleText;

    [SerializeField]
    private TMP_Text homeConfirmationMessageText;

    [SerializeField]
    private Button homeYesButton;

    [SerializeField]
    private Button homeNoButton;


    // =========================================================
    // CONTINUE CHALLENGE POPUP
    // =========================================================

    [Header("Continue Challenge Popup")]

    [SerializeField]
    private GameObject continueChallengePopup;

    [SerializeField]
    private TMP_Text continueChallengeTitleText;

    [SerializeField]
    private TMP_Text continueChallengeMessageText;

    [SerializeField]
    private Button continueYesButton;

    [SerializeField]
    private Button continueNoButton;

    // =========================================================
    // LOADING POPUP COOLDOWN
    // =========================================================

    [Header("Loading Popup Timing")]

    [SerializeField]
    private float minimumLoadingDuration = 1.5f;

    private float loadingStartTime = 0f;

    private bool loadingPopupVisible = false;

    private int loadingRequestCount = 0;

    // =========================================================
    // SAMPLE PROMPT UI
    // =========================================================

    [Header("Sample Prompt UI")]

    [SerializeField]
    private Button samplePromptButton;

    [SerializeField]
    private GameObject samplePromptPanel;

    [SerializeField]
    private Button samplePrompt1Button;

    [SerializeField]
    private Button samplePrompt2Button;

    [SerializeField]
    private Button samplePrompt3Button;

    [SerializeField]
    private Button samplePromptCloseButton;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentMode =
            DesignMode.Competition;
    }

    // =========================================================
    // DESIGN MODE
    // =========================================================

    public void SetDesignMode(
    DesignMode mode)
    {
        CurrentMode = mode;

        Debug.Log(
            "DesignManager: Design Mode = " +
            CurrentMode
        );

        UpdateSamplePromptButton();

        // Close sample popup whenever
        // the mode changes.
        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }
    }


    public bool IsPracticeMode()
    {
        return CurrentMode ==
               DesignMode.Practice;
    }


    public bool IsCompetitionMode()
    {
        return CurrentMode ==
               DesignMode.Competition;
    }


    private void Start()
    {
        ForceHideLoading();

        CloseAllWorkspacePanels();

        HideHomeConfirmationPopup();
        HideContinueChallengePopup();

        LoadParticipantState();

        UpdateAllButtonStates();
    }


    // =========================================================
    // LOAD PARTICIPANT STATE
    // =========================================================

    private void LoadParticipantState()
    {
        if (ParticipantManager.Instance == null)
            return;

        if (ParticipantManager.Instance.CurrentParticipant == null)
            return;

        LoadParticipantData();
    }


    private void LoadParticipantData()
    {
        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
            return;


        // -----------------------------------------------------
        // PROMPT
        // -----------------------------------------------------

        OriginalPrompt = data.prompt ?? "";

        if (promptInput != null)
            promptInput.text = OriginalPrompt;


        // -----------------------------------------------------
        // SUBMISSION STATE
        // -----------------------------------------------------

        scoreCalculated =
            data.HasScore();

        originalPosterGenerated =
            !string.IsNullOrWhiteSpace(data.originalImageUrl);


        // IMPORTANT:
        // New system uses isSubmitted.
        bool submitted =
            data.isSubmitted;


        // -----------------------------------------------------
        // REVISION
        // -----------------------------------------------------

        CurrentRevisionCount =
            Mathf.Clamp(
                data.revisionCount,
                0,
                MAX_REVISION_COUNT
            );


        LastRevisionPrompt =
            data.revisionPrompt ?? "";


        // -----------------------------------------------------
        // LOAD REVISION HISTORY
        // -----------------------------------------------------

        LoadRevisionHistory(
            data.revisionHistory
        );


        // -----------------------------------------------------
        // LATEST POSTER
        // -----------------------------------------------------

        CurrentPosterUrl =
            data.GetLatestPosterUrl();


        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        CurrentPosterDescription =
            data.posterDescription ?? "";

        if (descriptionText != null)
            descriptionText.text =
                CurrentPosterDescription;


        // -----------------------------------------------------
        // FINAL EXPLANATION
        // -----------------------------------------------------

        if (finalExplanationInput != null)
        {
            finalExplanationInput.text =
                data.finalExplanation ?? "";
        }


        // -----------------------------------------------------
        // SCORE
        // -----------------------------------------------------

        UpdateScoreUIFromData(data);


        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        if (feedbackText != null)
            feedbackText.text =
                data.feedback ?? "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text =
                data.improvementSuggestion ?? "";


        UpdateRevisionCounter();

        Debug.Log(
            "DesignManager: Participant state loaded. " +
            "Submitted = " +
            submitted
        );

        UpdateAllButtonStates();
    }


    // =========================================================
    // REVISION HISTORY LOAD
    // =========================================================

    private void LoadRevisionHistory(
        string savedHistory)
    {
        revisionHistory.Clear();

        if (string.IsNullOrWhiteSpace(savedHistory))
            return;

        string[] lines =
            savedHistory.Split(
                new[] { '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

        foreach (string line in lines)
        {
            string trimmed =
                line.Trim();

            if (!trimmed.StartsWith("Revision "))
                continue;

            int colonIndex =
                trimmed.IndexOf(':');

            if (colonIndex <= 9)
                continue;

            string numberText =
                trimmed.Substring(
                    9,
                    colonIndex - 9
                ).Trim();

            int number;

            if (!int.TryParse(
                numberText,
                out number))
            {
                continue;
            }

            string request =
                trimmed.Substring(
                    colonIndex + 1
                ).Trim();

            revisionHistory.Add(
                new RevisionEntry
                {
                    revisionNumber = number,
                    request = request
                }
            );
        }
    }


    private string BuildRevisionHistoryText()
    {
        if (
            revisionHistory == null ||
            revisionHistory.Count == 0
        )
        {
            if (CurrentRevisionCount == 0)
            {
                return
                    "No revision was made. Original design accepted.";
            }

            if (!string.IsNullOrWhiteSpace(
                LastRevisionPrompt))
            {
                return
                    "Revision " +
                    CurrentRevisionCount +
                    ": " +
                    LastRevisionPrompt;
            }

            return
                "Revision count: " +
                CurrentRevisionCount;
        }

        StringBuilder builder =
            new StringBuilder();

        foreach (RevisionEntry entry in revisionHistory)
        {
            builder.AppendLine(
                "Revision " +
                entry.revisionNumber +
                ": " +
                entry.request
            );
        }

        return builder.ToString().Trim();
    }

    // =========================================================
    // HOME BUTTON
    // =========================================================

    public void OnHomeButtonPressed()
    {
        // -----------------------------------------------------
        // DO NOT ALLOW HOME DURING PROCESSING
        // -----------------------------------------------------

        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Please wait until the current process is finished."
            );

            return;
        }


        // -----------------------------------------------------
        // PRACTICE MODE
        // -----------------------------------------------------
        //
        // Practice progress does NOT need to be saved.
        // Immediately return to Main Dashboard.
        //

        if (IsPracticeMode())
        {
            Debug.Log(
                "DesignManager: Leaving Practice Mode. " +
                "Practice progress will not be saved."
            );

            ReturnToMainDashboard();

            return;
        }


        // -----------------------------------------------------
        // SUBMITTED VIEW MODE
        // -----------------------------------------------------

        if (submittedViewMode)
        {
            ReturnToMainDashboard();

            return;
        }


        // -----------------------------------------------------
        // COMPETITION MODE
        // -----------------------------------------------------

        ShowHomeConfirmationPopup();
    }

    private void ShowHomeConfirmationPopup()
    {
        if (homeConfirmationPopup == null)
        {
            ReturnToMainDashboard();
            return;
        }


        if (homeConfirmationTitleText != null)
        {
            homeConfirmationTitleText.text =
                "Leave Challenge?";
        }


        if (homeConfirmationMessageText != null)
        {
            homeConfirmationMessageText.text =
                "Your current progress will be saved. " +
                "Do you want to return to the Main Dashboard?";
        }


        homeConfirmationPopup.SetActive(true);


        Speak(
            "Leave challenge? Your current progress will be saved."
        );
    }

    public void OnHomeConfirmationNo()
    {
        HideHomeConfirmationPopup();

        Speak(
            "Continuing the current challenge."
        );
    }

    public async void OnHomeConfirmationYes()
    {
        if (IsProcessing)
            return;


        HideHomeConfirmationPopup();


        ShowLoading(
            "Saving your challenge progress. Please wait."
        );


        bool saved = false;


        try
        {
            if (ParticipantManager.Instance != null)
            {
                saved =
                    await ParticipantManager.Instance
                        .SaveCurrentSubmission();
            }


            if (!saved)
            {
                SetStatus(
                    "Unable to save your current progress."
                );

                return;
            }


            await HideLoading();


            // ONLY return to dashboard.
            ReturnToMainDashboard();
        }
        catch (Exception exception)
        {
            SetStatus(
                "Unable to save your current progress: " +
                exception.Message
            );

            Debug.LogException(exception);


            await HideLoading();
        }
    }

    public void ShowContinueChallengePopup()
    {
        if (continueChallengePopup == null)
        {
            ReturnToMainDashboard();
            return;
        }

        if (continueChallengeTitleText != null)
        {
            continueChallengeTitleText.text =
                "Continue Challenge?";
        }

        if (continueChallengeMessageText != null)
        {
            continueChallengeMessageText.text =
                "Your previous challenge progress was found. " +
                "Would you like to continue?";
        }

        continueChallengePopup.SetActive(true);

        Speak(
            "Your previous challenge progress was found. Would you like to continue?"
        );
    }

    public void OnContinueChallengeYes()
    {
        HideContinueChallengePopup();

        Speak(
            "Continuing your previous challenge."
        );


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OpenIdeaPrompt();
        }
    }

    public void OnContinueChallengeNo()
    {
        HideContinueChallengePopup();

        Speak(
            "Starting the challenge again."
        );

        // Reset challenge progress but KEEP:
        // - Participant details
        // - Challenge
        // - Event code
        // - Submission ID
        if (ParticipantManager.Instance != null)
        {
            ParticipantManager.Instance
                .RestartCurrentChallenge();
        }

        // Reset UI/design workspace
        PrepareForNewChallenge();

        // Start from Idea Prompt
        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OpenIdeaPrompt();
        }

        Debug.Log(
            "DesignManager: User selected NO. " +
            "Challenge restarted from Idea Prompt."
        );
    }

    private void HideHomeConfirmationPopup()
    {
        if (homeConfirmationPopup != null)
        {
            homeConfirmationPopup.SetActive(false);
        }
    }


    private void HideContinueChallengePopup()
    {
        if (continueChallengePopup != null)
        {
            continueChallengePopup.SetActive(false);
        }
    }

    private void ReturnToMainDashboard()
    {
        HideHomeConfirmationPopup();
        HideContinueChallengePopup();

        CloseAllWorkspacePanels();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }






    // =========================================================
    // OPEN PROMPT
    // =========================================================

    public void OpenPrompt()
    {
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Please wait until the current process is finished."
            );

            return;
        }


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            Debug.Log(
                "DesignManager: Opening Idea Prompt in Practice Mode."
            );

            submittedViewMode = false;

            // Create completely separate practice data
            if (practiceData == null)
            {
                CreatePracticeData();
            }

            CloseAllWorkspacePanels();

            if (promptPanel != null)
                promptPanel.SetActive(true);


            // =====================================================
            // PRACTICE MODE
            // =====================================================

            if (IsPracticeMode())
            {
                Debug.Log(
                    "DesignManager: Opening Idea Prompt in Practice Mode."
                );

                submittedViewMode = false;

                CloseAllWorkspacePanels();

                if (promptPanel != null)
                    promptPanel.SetActive(true);

                UpdateSamplePromptButton();

                SetPromptStatus("");

                UpdateRevisionCounter();

                UpdateAllButtonStates();

                Speak(
                    "Practice mode. Enter your design idea."
                );

                return;
            }
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        if (ParticipantManager.Instance == null)
        {
            SetStatus(
                "Participant Manager is not available."
            );

            return;
        }


        ParticipantData participant =
            ParticipantManager.Instance.CurrentParticipant;


        if (participant == null)
        {
            SetStatus(
                "Please complete participant details first."
            );

            if (UIManager.Instance != null)
                UIManager.Instance.ShowParticipant();

            return;
        }


        if (!participant.HasParticipantDetails())
        {
            SetStatus(
                "Please complete all participant details first."
            );

            if (UIManager.Instance != null)
                UIManager.Instance.ShowParticipant();

            return;
        }


        if (!participant.HasChallenge())
        {
            SetStatus(
                "Please join a challenge first."
            );

            return;
        }


        CloseAllWorkspacePanels();

        if (promptPanel != null)
            promptPanel.SetActive(true);


        LoadParticipantData();


        // -----------------------------------------------------
        // READ-ONLY MODE
        // -----------------------------------------------------

        if (promptInput != null)
        {
            promptInput.interactable =
                !submittedViewMode;
        }


        SetPromptStatus("");

        UpdateAllButtonStates();
    }

    public void StartPracticeMode()
    {
        Debug.Log(
            "DesignManager: Practice Mode selected."
        );

        SetDesignMode(
            DesignMode.Practice
        );

        StartNewPracticeSession();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenIdeaPrompt();
        }
        else
        {
            OpenPrompt();
        }
    }


    // =========================================================
    // OUTPUT
    // =========================================================

    public async void OpenOutput()
    {
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Your poster is still being processed. Please wait."
            );

            return;
        }

        string originalUrl;

        if (IsPracticeMode())
        {
            originalUrl =
                GetPracticePosterUrl();
        }
        else
        {
            originalUrl =
                GetOriginalPosterUrl();
        }

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            ShowTemporaryLoading(
                "Original poster is not available."
            );

            return;
        }

        CloseAllWorkspacePanels();

        if (outputPanel != null)
            outputPanel.SetActive(true);

        IsProcessing = true;
        UpdateAllButtonStates();

        try
        {
            // OUTPUT ALWAYS SHOWS ORIGINAL POSTER
            await LoadPosterToImage(
                originalUrl,
                outputPosterImage
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }
    }


    // =========================================================
    // GENERATE ORIGINAL POSTER
    // =========================================================

    public async void GeneratePoster()
    {
        if (IsProcessing)
            return;


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            await GeneratePracticePoster();
            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
        {
            SetStatus(
                "Participant data is not available."
            );

            return;
        }

        if (data.isSubmitted)
        {
            SetPromptStatus(
                "This challenge has already been submitted."
            );

            return;
        }

        if (originalPosterGenerated)
        {
            SetPromptStatus(
                "The original poster has already been generated."
            );

            Speak(
                "The original poster has already been generated."
            );

            return;
        }

        if (!ValidateParticipant())
            return;


        string prompt =
            promptInput != null
                ? promptInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetPromptStatus(
                "Please enter your design idea before generating the poster."
            );

            Speak(
                "Please enter your design idea before generating the poster."
            );

            return;
        }


        SetPromptStatus("");

        OriginalPrompt = prompt;

        ParticipantManager.Instance.SetPrompt(prompt);

        await ParticipantManager.Instance.Save();


        SetPromptButtonsInteractable(false);

        if (promptInput != null)
            promptInput.interactable = false;


        ShowLoading(
            "Generating your poster. Please wait."
        );


        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }


            AIBackendManager.PosterResult result =
                await aiBackendManager.GeneratePoster(
                    prompt
                );


            if (
                result == null ||
                !result.success ||
                string.IsNullOrWhiteSpace(result.imageUrl)
            )
            {
                SetStatus(
                    "Poster generation failed."
                );

                Speak(
                    "Poster generation failed."
                );

                return;
            }


            CurrentPosterUrl =
                result.imageUrl;

            originalPosterGenerated = true;

            CurrentRevisionCount = 0;

            CurrentPosterDescription = "";

            LastRevisionPrompt = "";

            revisionHistory.Clear();


            data.prompt =
                prompt;

            data.promptUsed =
                result.promptUsed ?? "";

            data.originalImageUrl =
                result.imageUrl;

            data.posterImageUrl =
                result.imageUrl;

            data.revisedImageUrl =
                "";

            data.revisionCount =
                0;

            data.revisionPrompt =
                "";

            data.revisionHistory =
                "";

            data.posterDescription =
                "";

            data.storagePath =
                result.storagePath ?? "";

            data.lastPage =
                "Output";


            bool saved =
                await ParticipantManager.Instance
                    .Save();


            if (!saved)
            {
                SetStatus(
                    "Poster generated, but failed to save submission data."
                );

                return;
            }


            Texture2D texture =
                await aiBackendManager.DownloadImage(
                    result.imageUrl
                );


            if (
                texture != null &&
                outputPosterImage != null
            )
            {
                outputPosterImage.texture =
                    texture;
            }


            CloseAllWorkspacePanels();

            if (outputPanel != null)
                outputPanel.SetActive(true);


            Speak(
                "Poster generated successfully. Opening output page."
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "Poster generation failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            if (promptInput != null)
            {
                promptInput.interactable =
                    !originalPosterGenerated;
            }

            UpdateAllButtonStates();
        }
    }

    private async Task GeneratePracticePoster()
    {
        // =====================================================
        // MAKE SURE PRACTICE DATA EXISTS
        // =====================================================

        if (practiceData == null)
        {
            CreatePracticeData();
        }


        // =====================================================
        // PREVENT MULTIPLE GENERATIONS
        // =====================================================

        if (originalPosterGenerated)
        {
            SetPromptStatus(
                "A practice poster has already been generated."
            );

            Speak(
                "A practice poster has already been generated."
            );

            return;
        }


        // =====================================================
        // GET PROMPT
        // =====================================================

        string prompt =
            promptInput != null
                ? promptInput.text.Trim()
                : "";


        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetPromptStatus(
                "Please enter your design idea before generating the poster."
            );

            Speak(
                "Please enter your design idea before generating the poster."
            );

            return;
        }


        SetPromptStatus("");


        // =====================================================
        // SAVE TO PRACTICE DATA
        // =====================================================

        practiceData.prompt =
            prompt;

        OriginalPrompt =
            prompt;


        // =====================================================
        // UI
        // =====================================================

        SetPromptButtonsInteractable(false);

        if (promptInput != null)
            promptInput.interactable = false;


        ShowLoading(
            "Generating your practice poster. Please wait."
        );


        IsProcessing = true;


        try
        {
            // -------------------------------------------------
            // AI BACKEND
            // -------------------------------------------------

            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;


            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }


            // -------------------------------------------------
            // GENERATE
            // -------------------------------------------------

            AIBackendManager.PosterResult result =
                await aiBackendManager.GeneratePoster(
                    prompt
                );


            if (
                result == null ||
                !result.success ||
                string.IsNullOrWhiteSpace(result.imageUrl)
            )
            {
                SetStatus(
                    "Practice poster generation failed."
                );

                Speak(
                    "Practice poster generation failed."
                );

                return;
            }


            // =================================================
            // UPDATE PRACTICE DATA
            // =================================================

            practiceData.prompt =
                prompt;

            practiceData.originalImageUrl =
                result.imageUrl;

            practiceData.posterImageUrl =
                result.imageUrl;

            practiceData.revisedImageUrl =
                "";

            practiceData.revisionCount =
                0;

            practiceData.revisionPrompt =
                "";

            practiceData.revisionHistory =
                "";

            practiceData.posterDescription =
                "";

            practiceData.finalExplanation =
                "";


            // =================================================
            // UPDATE DESIGN MANAGER STATE
            // =================================================

            CurrentPosterUrl =
                result.imageUrl;

            OriginalPrompt =
                prompt;

            CurrentRevisionCount = 0;

            CurrentPosterDescription = "";

            LastRevisionPrompt = "";

            revisionHistory.Clear();

            originalPosterGenerated = true;


            // =================================================
            // DOWNLOAD IMAGE
            // =================================================

            Texture2D texture =
                await aiBackendManager.DownloadImage(
                    result.imageUrl
                );


            if (
                texture != null &&
                outputPosterImage != null
            )
            {
                outputPosterImage.texture =
                    texture;
            }


            // =================================================
            // OPEN OUTPUT
            // =================================================

            CloseAllWorkspacePanels();

            if (outputPanel != null)
                outputPanel.SetActive(true);


            Speak(
                "Practice poster generated successfully. Opening output page."
            );


            Debug.Log(
                "DesignManager: Practice poster generated successfully."
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "Practice poster generation failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            IsProcessing = false;

            if (promptInput != null)
            {
                promptInput.interactable =
                    !originalPosterGenerated;
            }

            UpdateAllButtonStates();
        }
    }


    // =========================================================
    // ORIGINAL FULLSCREEN
    // =========================================================

    public async void OpenOriginalFullPoster()
    {
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Please wait until the current process is finished."
            );

            return;
        }

        string originalUrl;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            originalUrl =
                GetPracticePosterUrl();
        }

        // =====================================================
        // COMPETITION MODE
        // =====================================================

        else
        {
            originalUrl =
                GetOriginalPosterUrl();
        }


        // =====================================================
        // CHECK POSTER
        // =====================================================

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            ShowTemporaryLoading(
                "Original poster is not available."
            );

            return;
        }


        // =====================================================
        // OPEN FULLSCREEN
        // =====================================================

        CloseAllWorkspacePanels();

        if (originalFullPosterPanel != null)
            originalFullPosterPanel.SetActive(true);

        IsProcessing = true;

        UpdateAllButtonStates();

        try
        {
            await LoadPosterToImage(
                originalUrl,
                originalFullPosterImage
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }
    }


    public void BackFromOriginalFullPoster()
    {
        if (IsProcessing)
            return;

        OpenOutput();
    }


    // =========================================================
    // DESCRIPTION
    // =========================================================

    public async void OpenDescription()
    {
        await OpenDescriptionAsync();
    }


    private async Task OpenDescriptionAsync()
    {
        if (IsPracticeMode())
        {
            await OpenPracticeDescriptionAsync();
            return;
        }


        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "The poster description is still loading. Please wait."
            );

            return;
        }

        // DESCRIPTION ALWAYS SHOWS LATEST POSTER
        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Poster is not available."
            );

            return;
        }

        CloseAllWorkspacePanels();

        if (descriptionPanel != null)
            descriptionPanel.SetActive(true);

        IsProcessing = true;

        SetDescriptionStatus(
            "Loading your latest poster..."
        );

        try
        {
            await LoadPosterToImage(
                latestUrl,
                descriptionPosterImage
            );

            if (!string.IsNullOrWhiteSpace(
                CurrentPosterDescription))
            {
                if (descriptionText != null)
                {
                    descriptionText.text =
                        CurrentPosterDescription;
                }

                SetDescriptionStatus("");

                return;
            }

            if (descriptionText != null)
                descriptionText.text = "";

            SetDescriptionStatus(
                "Analyzing your poster. Please wait..."
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }

        if (string.IsNullOrWhiteSpace(
            CurrentPosterDescription))
        {
            await GenerateDescription();
        }
    }

    private async Task OpenPracticeDescriptionAsync()
    {
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "The poster description is still loading. Please wait."
            );

            return;
        }

        string latestUrl =
            GetPracticePosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Practice poster is not available."
            );

            return;
        }

        CloseAllWorkspacePanels();

        if (descriptionPanel != null)
            descriptionPanel.SetActive(true);

        IsProcessing = true;

        SetDescriptionStatus(
            "Loading your practice poster..."
        );

        try
        {
            await LoadPosterToImage(
                latestUrl,
                descriptionPosterImage
            );

            if (!string.IsNullOrWhiteSpace(
                CurrentPosterDescription))
            {
                if (descriptionText != null)
                {
                    descriptionText.text =
                        CurrentPosterDescription;
                }

                SetDescriptionStatus("");

                return;
            }

            if (descriptionText != null)
                descriptionText.text = "";

            SetDescriptionStatus(
                "Analyzing your poster. Please wait..."
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }

        if (string.IsNullOrWhiteSpace(
            CurrentPosterDescription))
        {
            await GeneratePracticeDescription();
        }
    }

    private async Task GeneratePracticeDescription()
    {
        IsProcessing = true;

        ShowLoading(
            "Analyzing your practice poster. Please wait."
        );

        SetDescriptionStatus(
            "Analyzing your poster. Please wait..."
        );

        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetDescriptionStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }

            string latestUrl =
                GetPracticePosterUrl();

            if (string.IsNullOrWhiteSpace(latestUrl))
            {
                SetDescriptionStatus(
                    "Practice poster is not available."
                );

                return;
            }

            AIBackendManager.DescriptionResult result =
                await aiBackendManager.DescribePoster(
                    latestUrl
                );

            if (
                result == null ||
                !result.success
            )
            {
                SetDescriptionStatus(
                    "Unable to analyze the practice poster."
                );

                return;
            }

            CurrentPosterDescription =
                result.description ?? "";

            if (descriptionText != null)
            {
                descriptionText.text =
                    CurrentPosterDescription;
            }

            SetDescriptionStatus("");

            // Save ONLY to PracticeData
            if (practiceData != null)
            {
                practiceData.posterDescription =
                    CurrentPosterDescription;
            }

            Speak(
                "Practice poster description generated successfully."
            );
        }
        catch (Exception exception)
        {
            SetDescriptionStatus(
                "Description generation failed."
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            UpdateAllButtonStates();
        }
    }

    private string GetPracticePosterUrl()
    {
        if (practiceData == null)
            return "";

        string url =
            practiceData.GetLatestPosterUrl();

        if (!string.IsNullOrWhiteSpace(url))
        {
            CurrentPosterUrl = url;
            return url;
        }

        return CurrentPosterUrl ?? "";
    }


    private async Task GenerateDescription()
    {
        IsProcessing = true;

        ShowLoading(
            "Analyzing your poster. Please wait."
        );

        SetDescriptionStatus(
            "Analyzing your poster. Please wait..."
        );

        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetDescriptionStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }

            string latestUrl =
                GetLatestPosterUrl();

            if (string.IsNullOrWhiteSpace(latestUrl))
            {
                SetDescriptionStatus(
                    "Poster is not available."
                );

                return;
            }

            AIBackendManager.DescriptionResult result =
                await aiBackendManager.DescribePoster(
                    latestUrl
                );

            if (
                result == null ||
                !result.success
            )
            {
                SetDescriptionStatus(
                    "Unable to analyze the poster. Please try again."
                );

                return;
            }

            CurrentPosterDescription =
                result.description ?? "";

            if (descriptionText != null)
            {
                descriptionText.text =
                    CurrentPosterDescription;
            }

            SetDescriptionStatus("");

            ParticipantData data =
                ParticipantManager.Instance.CurrentParticipant;

            if (data != null)
            {
                data.posterDescription =
                    CurrentPosterDescription;

                data.lastPage =
                    "Description";

                await ParticipantManager.Instance
                    .Save();
            }

            Speak(
                "Poster description generated successfully."
            );
        }
        catch (Exception exception)
        {
            SetDescriptionStatus(
                "Description generation failed."
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            UpdateAllButtonStates();
        }
    }


    public async void DescriptionNextButton()
    {
        if (IsProcessing)
            return;

        if (string.IsNullOrWhiteSpace(
            CurrentPosterDescription))
        {
            SetDescriptionStatus(
                "Please wait until the poster description is ready."
            );

            return;
        }

        await OpenRevisionAsync();
    }


    // =========================================================
    // LATEST FULLSCREEN
    // =========================================================

    public async void OpenLatestFullPoster()
    {
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Please wait until the current process is finished."
            );

            return;
        }

        // ALWAYS GET THE LATEST POSTER
        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Latest poster is not available."
            );

            return;
        }

        // ---------------------------------------------------------
        // DETERMINE RETURN PAGE
        // ---------------------------------------------------------

        if (
            scorePanel != null &&
            scorePanel.activeSelf
        )
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.Score;
        }
        else if (
            feedbackPanel != null &&
            feedbackPanel.activeSelf
        )
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.Feedback;
        }
        else if (
            descriptionPanel != null &&
            descriptionPanel.activeSelf
        )
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.Description;
        }
        else if (
            revisionPanel != null &&
            revisionPanel.activeSelf
        )
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.Revision;
        }
        else if (
            finalExplanationPanel != null &&
            finalExplanationPanel.activeSelf
        )
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.FinalExplanation;
        }
        else
        {
            latestFullPosterReturnPage =
                FullPosterReturnPage.Description;
        }

        CloseAllWorkspacePanels();

        if (latestFullPosterPanel != null)
            latestFullPosterPanel.SetActive(true);

        IsProcessing = true;

        UpdateAllButtonStates();

        try
        {
            await LoadPosterToImage(
                latestUrl,
                latestFullPosterImage
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }
    }

    private async void OpenLatestFullPosterFrom(
    FullPosterReturnPage returnPage)
    {
        if (IsProcessing)
            return;

        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus("Latest poster is not available.");
            return;
        }

        latestFullPosterReturnPage =
            returnPage;

        CloseAllWorkspacePanels();

        if (latestFullPosterPanel != null)
            latestFullPosterPanel.SetActive(true);

        IsProcessing = true;

        try
        {
            await LoadPosterToImage(
                latestUrl,
                latestFullPosterImage
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }
    }

    // Description → Latest Full Screen
    public void OpenLatestFullPosterFromDescription()
    {
        OpenLatestFullPosterFrom(
            FullPosterReturnPage.Description
        );
    }


    // Revision → Latest Full Screen
    public void OpenLatestFullPosterFromRevision()
    {
        OpenLatestFullPosterFrom(
            FullPosterReturnPage.Revision
        );
    }


    // Final Explanation → Latest Full Screen
    public void OpenLatestFullPosterFromFinalExplanation()
    {
        OpenLatestFullPosterFrom(
            FullPosterReturnPage.FinalExplanation
        );
    }


    // Score → Latest Full Screen
    public void OpenLatestFullPosterFromScore()
    {
        OpenLatestFullPosterFrom(
            FullPosterReturnPage.Score
        );
    }


    public async void BackFromLatestFullPoster()
    {
        if (IsProcessing)
            return;

        switch (latestFullPosterReturnPage)
        {
            case FullPosterReturnPage.Description:
                await OpenDescriptionAsync();
                break;

            case FullPosterReturnPage.Revision:
                await OpenRevisionAsync();
                break;

            case FullPosterReturnPage.FinalExplanation:
                await OpenFinalExplanationAsync();
                break;

            case FullPosterReturnPage.Score:
                OpenScore();
                break;

            case FullPosterReturnPage.Feedback:
                OpenFeedback();
                break;
        }
    }


    // =========================================================
    // REVISION
    // =========================================================

    private async Task OpenRevisionAsync()
    {
        if (IsProcessing)
            return;


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            string practiceUrl =
                GetPracticePosterUrl();

            if (string.IsNullOrWhiteSpace(practiceUrl))
            {
                SetStatus(
                    "Practice poster is not available."
                );

                return;
            }

            CloseAllWorkspacePanels();

            if (revisionPanel != null)
                revisionPanel.SetActive(true);

            IsProcessing = true;

            UpdateRevisionCounter();

            try
            {
                await LoadPosterToImage(
                    practiceUrl,
                    revisionPosterImage
                );
            }
            finally
            {
                IsProcessing = false;
                UpdateAllButtonStates();
            }

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return;


        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Poster is not available."
            );

            return;
        }

        CloseAllWorkspacePanels();

        if (revisionPanel != null)
            revisionPanel.SetActive(true);

        IsProcessing = true;

        UpdateRevisionCounter();

        try
        {
            await LoadPosterToImage(
                latestUrl,
                revisionPosterImage
            );
        }
        finally
        {
            IsProcessing = false;
            UpdateAllButtonStates();
        }
    }


    public async void OpenRevisionFromButton()
    {
        await OpenRevisionAsync();
    }


    public async void RevisePoster()
    {
        if (IsProcessing)
            return;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            await RevisePracticePoster();
            return;
        }

        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return;

        if (data.isSubmitted)
        {
            SetStatus(
                "This challenge has already been submitted."
            );

            return;
        }

        if (CurrentRevisionCount >= MAX_REVISION_COUNT)
        {
            SetStatus(
                "You have used all 3 revision attempts."
            );

            return;
        }

        string revisionPrompt =
            revisionPromptInput != null
                ? revisionPromptInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(revisionPrompt))
        {
            SetStatus(
                "Please describe what you want to change."
            );

            return;
        }

        IsProcessing = true;

        SetRevisionButtonsInteractable(false);

        ShowLoading(
            "Applying your poster revision. Please wait."
        );

        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }

            int nextRevision =
                CurrentRevisionCount + 1;

            string cumulativePrompt =
                BuildCumulativeRevisionPrompt(
                    revisionPrompt,
                    nextRevision
                );

            AIBackendManager.PosterResult result =
                await aiBackendManager.GeneratePoster(
                    cumulativePrompt
                );

            if (
                result == null ||
                !result.success ||
                string.IsNullOrWhiteSpace(result.imageUrl)
            )
            {
                SetStatus(
                    "Poster revision failed."
                );

                return;
            }


            // -------------------------------------------------
            // UPDATE REVISION
            // -------------------------------------------------

            CurrentRevisionCount =
                nextRevision;

            LastRevisionPrompt =
                revisionPrompt;

            CurrentPosterUrl =
                result.imageUrl;


            revisionHistory.Add(
                new RevisionEntry
                {
                    revisionNumber =
                        CurrentRevisionCount,

                    request =
                        revisionPrompt
                }
            );


            // -------------------------------------------------
            // SAVE PARTICIPANT DATA
            // -------------------------------------------------

            data.revisionPrompt =
                revisionPrompt;

            data.revisionCount =
                CurrentRevisionCount;

            data.revisedImageUrl =
                CurrentPosterUrl;

            data.posterImageUrl =
                CurrentPosterUrl;

            data.promptUsed =
                result.promptUsed ?? "";

            data.storagePath =
                result.storagePath ?? "";

            data.posterDescription =
                "";

            data.revisionHistory =
                BuildRevisionHistoryText();

            data.lastPage =
                "Description";


            CurrentPosterDescription =
                "";

            if (descriptionText != null)
                descriptionText.text = "";


            bool saved =
                await ParticipantManager.Instance
                    .Save();

            if (!saved)
            {
                SetStatus(
                    "Revision generated, but failed to save."
                );

                return;
            }


            // -------------------------------------------------
            // LOAD IMAGE
            // -------------------------------------------------

            await LoadPosterToImage(
                CurrentPosterUrl,
                revisionPosterImage
            );

            await LoadPosterToImage(
                CurrentPosterUrl,
                descriptionPosterImage
            );


            if (revisionPromptInput != null)
                revisionPromptInput.text = "";


            UpdateRevisionCounter();


            int remaining =
                MAX_REVISION_COUNT -
                CurrentRevisionCount;

            SetStatus(
                "Revision " +
                CurrentRevisionCount +
                " of " +
                MAX_REVISION_COUNT +
                " completed. " +
                remaining +
                " remaining."
            );


            CloseAllWorkspacePanels();

            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);

            await GenerateDescription();
        }
        catch (Exception exception)
        {
            SetStatus(
                "Revision failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            UpdateAllButtonStates();
        }
    }

    private async Task RevisePracticePoster()
    {
        if (practiceData == null)
        {
            CreatePracticeData();
        }

        if (CurrentRevisionCount >= MAX_REVISION_COUNT)
        {
            SetStatus(
                "You have used all 3 revision attempts."
            );

            return;
        }

        string revisionPrompt =
            revisionPromptInput != null
                ? revisionPromptInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(revisionPrompt))
        {
            SetStatus(
                "Please describe what you want to change."
            );

            return;
        }

        IsProcessing = true;

        SetRevisionButtonsInteractable(false);

        ShowLoading(
            "Applying your practice poster revision. Please wait."
        );

        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }


            int nextRevision =
                CurrentRevisionCount + 1;


            string cumulativePrompt =
                BuildCumulativeRevisionPrompt(
                    revisionPrompt,
                    nextRevision
                );


            AIBackendManager.PosterResult result =
                await aiBackendManager.GeneratePoster(
                    cumulativePrompt
                );


            if (
                result == null ||
                !result.success ||
                string.IsNullOrWhiteSpace(result.imageUrl)
            )
            {
                SetStatus(
                    "Practice poster revision failed."
                );

                return;
            }


            // =================================================
            // UPDATE PRACTICE STATE
            // =================================================

            CurrentRevisionCount =
                nextRevision;

            LastRevisionPrompt =
                revisionPrompt;

            CurrentPosterUrl =
                result.imageUrl;


            revisionHistory.Add(
                new RevisionEntry
                {
                    revisionNumber =
                        CurrentRevisionCount,

                    request =
                        revisionPrompt
                }
            );


            // =================================================
            // SAVE TO PRACTICE DATA ONLY
            // =================================================

            practiceData.prompt =
                OriginalPrompt;

            practiceData.originalImageUrl =
                practiceData.originalImageUrl;

            practiceData.posterImageUrl =
                result.imageUrl;

            practiceData.revisedImageUrl =
                result.imageUrl;

            practiceData.revisionCount =
                CurrentRevisionCount;

            practiceData.revisionPrompt =
                revisionPrompt;

            practiceData.revisionHistory =
                BuildRevisionHistoryText();

            practiceData.posterDescription =
                "";

            practiceData.finalExplanation =
                "";


            CurrentPosterDescription =
                "";

            if (descriptionText != null)
                descriptionText.text = "";


            if (revisionPromptInput != null)
                revisionPromptInput.text = "";


            UpdateRevisionCounter();


            // =================================================
            // LOAD NEW POSTER
            // =================================================

            await LoadPosterToImage(
                CurrentPosterUrl,
                revisionPosterImage
            );


            int remaining =
                MAX_REVISION_COUNT -
                CurrentRevisionCount;


            SetStatus(
                "Revision " +
                CurrentRevisionCount +
                " of " +
                MAX_REVISION_COUNT +
                " completed. " +
                remaining +
                " remaining."
            );


            CloseAllWorkspacePanels();

            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);


            // =================================================
            // GENERATE NEW DESCRIPTION
            // =================================================

            await GeneratePracticeDescription();


            Debug.Log(
                "DesignManager: Practice revision " +
                CurrentRevisionCount +
                " completed."
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "Practice revision failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            IsProcessing = false;

            UpdateAllButtonStates();
        }
    }


    // =========================================================
    // CUMULATIVE REVISION PROMPT
    // =========================================================

    private string BuildCumulativeRevisionPrompt(
        string newestRevision,
        int revisionNumber)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            "You are revising an existing poster design."
        );

        builder.AppendLine();

        builder.AppendLine(
            "ORIGINAL DESIGN PROMPT:"
        );

        builder.AppendLine(
            OriginalPrompt
        );

        builder.AppendLine();

        builder.AppendLine(
            "Preserve the original design purpose."
        );

        builder.AppendLine(
            "Preserve all previously accepted elements."
        );

        builder.AppendLine(
            "Do not remove previously accepted changes."
        );

        builder.AppendLine();

        if (revisionHistory.Count > 0)
        {
            builder.AppendLine(
                "PREVIOUS ACCEPTED REVISIONS:"
            );

            foreach (RevisionEntry entry in revisionHistory)
            {
                builder.AppendLine(
                    "Revision " +
                    entry.revisionNumber +
                    ": " +
                    entry.request
                );
            }

            builder.AppendLine();
        }

        builder.AppendLine(
            "CURRENT REVISION " +
            revisionNumber +
            ":"
        );

        builder.AppendLine(
            newestRevision
        );

        builder.AppendLine();

        builder.AppendLine(
            "FINAL INSTRUCTION:"
        );

        builder.AppendLine(
            "Generate the updated poster containing the original design plus every accepted revision."
        );

        builder.AppendLine(
            "Only modify the parts requested by the newest revision."
        );

        return builder.ToString();
    }


    // =========================================================
    // SKIP REVISION
    // =========================================================

    public async void SkipRevision()
    {
        if (IsProcessing)
            return;

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return;

        if (data.isSubmitted)
        {
            SetStatus(
                "This challenge has already been submitted."
            );

            return;
        }

        string revisionInput =
            revisionPromptInput != null
                ? revisionPromptInput.text.Trim()
                : "";

        if (!string.IsNullOrWhiteSpace(revisionInput))
        {
            SetStatus(
                "Please apply the revision or clear the input before skipping."
            );

            return;
        }

        if (CurrentRevisionCount == 0)
        {
            CurrentPosterUrl =
                data.originalImageUrl;

            data.revisedImageUrl = "";

            data.posterImageUrl =
                data.originalImageUrl;

            data.revisionPrompt = "";
        }

        data.revisionCount =
            CurrentRevisionCount;

        data.revisionHistory =
            BuildRevisionHistoryText();

        data.lastPage =
            "Final Explanation";

        await ParticipantManager.Instance.Save();

        Speak(
            "Poster accepted. Please provide your final explanation."
        );

        await OpenFinalExplanationAsync();
    }


    // =========================================================
    // CONTINUE REVISION
    // =========================================================

    public async void ContinueFromRevision()
    {
        if (IsProcessing)
            return;

        string revisionInput =
            revisionPromptInput != null
                ? revisionPromptInput.text.Trim()
                : "";

        if (!string.IsNullOrWhiteSpace(revisionInput))
        {
            SetStatus(
                "Please apply the revision or clear the input before continuing."
            );

            return;
        }

        await OpenFinalExplanationAsync();
    }


    // =========================================================
    // FINAL EXPLANATION
    // =========================================================

    public async void OpenFinalExplanation()
    {
        await OpenFinalExplanationAsync();
    }

    public void FinalExplanationNextButton()
    {
        if (IsProcessing)
            return;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            string explanation =
                finalExplanationInput != null
                    ? finalExplanationInput.text.Trim()
                    : "";


            if (string.IsNullOrWhiteSpace(explanation))
            {
                SetStatus(
                    "Please enter your final explanation first."
                );

                return;
            }


            OpenScore();

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
        {
            SetStatus(
                "Participant data is not available."
            );

            return;
        }

        if (!data.HasScore())
        {
            if (finalExplanationStatusText != null)
            {
                finalExplanationStatusText.text =
                    "Please calculate the score first.";
            }

            SetStatus(
                "Please calculate the score first."
            );

            Speak(
                "Please calculate the score first."
            );

            return;
        }

        OpenScore();
    }


    private async Task OpenFinalExplanationAsync()
    {
        if (IsProcessing)
            return;


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            if (practiceData == null)
                CreatePracticeData();


            string latestUrl =
                GetPracticePosterUrl();

            if (string.IsNullOrWhiteSpace(latestUrl))
            {
                SetStatus(
                    "Practice poster is not available."
                );

                return;
            }


            CurrentPosterUrl =
                latestUrl;


            CloseAllWorkspacePanels();


            if (finalExplanationPanel != null)
                finalExplanationPanel.SetActive(true);


            IsProcessing = true;


            try
            {
                await LoadPosterToImage(
                    latestUrl,
                    finalExplanationPosterImage
                );


                if (finalExplanationInput != null)
                {
                    finalExplanationInput.text =
                        practiceData.finalExplanation ?? "";

                    finalExplanationInput.interactable =
                        true;
                }


                if (finalExplanationStatusText != null)
                    finalExplanationStatusText.text = "";
            }
            finally
            {
                IsProcessing = false;

                UpdateAllButtonStates();
            }

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return;


        string latestUrlCompetition =
            GetLatestPosterUrl();


        if (string.IsNullOrWhiteSpace(latestUrlCompetition))
        {
            SetStatus(
                "Final poster is not available."
            );

            return;
        }


        CurrentPosterUrl =
            latestUrlCompetition;


        CloseAllWorkspacePanels();


        if (finalExplanationPanel != null)
            finalExplanationPanel.SetActive(true);


        IsProcessing = true;


        try
        {
            await LoadPosterToImage(
                latestUrlCompetition,
                finalExplanationPosterImage
            );


            if (finalExplanationInput != null)
            {
                finalExplanationInput.text =
                    data.finalExplanation ?? "";

                finalExplanationInput.interactable =
                    !submittedViewMode;
            }


            if (finalExplanationStatusText != null)
                finalExplanationStatusText.text = "";
        }
        finally
        {
            IsProcessing = false;

            UpdateAllButtonStates();
        }
    }


    public async void OpenFinalExplanationFromButton()
    {
        await OpenFinalExplanationAsync();
    }




    // =========================================================
    // CALCULATE SCORE
    // =========================================================

    public async void CalculateScore()
    {
        if (IsProcessing)
            return;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            await CalculatePracticeScore();
            return;
        }


        // =====================================================
        // EXISTING COMPETITION CODE
        // =====================================================

        if (ParticipantManager.Instance == null)
        {
            SetStatus(
                "Participant Manager is not available."
            );

            return;
        }

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
        {
            SetStatus(
                "Participant data is not available."
            );

            return;
        }


        // -----------------------------------------------------
        // IMPORTANT:
        // NEW SYSTEM USES isSubmitted
        // -----------------------------------------------------

        if (data.isSubmitted)
        {
            scoreCalculated = true;

            SetStatus(
                "This challenge has already been submitted."
            );

            Speak(
                "This challenge has already been submitted."
            );

            UpdateAllButtonStates();

            return;
        }


        // -----------------------------------------------------
        // EXPLANATION
        // -----------------------------------------------------

        string explanation =
            finalExplanationInput != null
                ? finalExplanationInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(explanation))
        {
            SetStatus(
                "Please explain your final poster design."
            );

            if (finalExplanationStatusText != null)
            {
                finalExplanationStatusText.text =
                    "Please enter your final explanation before submitting.";
            }

            Speak(
                "Please enter your final explanation."
            );

            return;
        }


        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Final poster is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // SAVE FINAL EXPLANATION FIRST
        // -----------------------------------------------------

        ParticipantManager.Instance.SetFinalExplanation(
            explanation
        );

        data.lastPage =
            "Final Explanation";

        bool explanationSaved =
            await ParticipantManager.Instance.Save();

        if (!explanationSaved)
        {
            SetStatus(
                "Unable to save your final explanation."
            );

            return;
        }


        // -----------------------------------------------------
        // PROCESS
        // -----------------------------------------------------

        IsProcessing = true;

        UpdateAllButtonStates();

        ShowLoading(
            "Evaluating your submission. Please wait."
        );

        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;

            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }


            AIBackendManager.ScoreRequestData requestData =
                new AIBackendManager.ScoreRequestData
                {
                    userPrompt =
                        OriginalPrompt,

                    imageUrl =
                        latestUrl,

                    revisionPrompt =
                        LastRevisionPrompt,

                    revisionHistory =
                        BuildRevisionHistoryText(),

                    revisionCount =
                        CurrentRevisionCount,

                    finalExplanation =
                        explanation
                };


            AIBackendManager.ScoreResult result =
                await aiBackendManager.ScorePoster(
                    requestData
                );

            if (
                result == null ||
                !result.success ||
                result.score == null
            )
            {
                SetStatus(
                    "Score calculation failed. Please try again."
                );

                return;
            }


            // -------------------------------------------------
            // SAVE SCORE DATA
            // -------------------------------------------------

            DisplayScore(
                result.score
            );


            data =
                ParticipantManager.Instance.CurrentParticipant;

            if (data == null)
            {
                SetStatus(
                    "Participant data was lost during scoring."
                );

                return;
            }


            data.finalExplanation =
                explanation;

            data.lastPage =
                "Score";


            // IMPORTANT:
            // Do NOT directly set isSubmitted before Firebase
            // succeeds.
            //
            // SubmitCurrentChallenge() handles this safely.


            bool submitted =
                await ParticipantManager.Instance
                    .SubmitCurrentChallenge();

            if (!submitted)
            {
                SetStatus(
                    "Score was calculated, but submission could not be saved. Please try again."
                );

                return;
            }


            scoreCalculated = true;

            IsProcessing = false;

            OpenScore();

            Speak(
                "Evaluation completed. Your challenge has been submitted."
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "Score calculation failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            UpdateAllButtonStates();
        }
    }

    private async Task CalculatePracticeScore()
    {
        if (practiceData == null)
            CreatePracticeData();


        string explanation =
            finalExplanationInput != null
                ? finalExplanationInput.text.Trim()
                : "";


        if (string.IsNullOrWhiteSpace(explanation))
        {
            SetStatus(
                "Please explain your final poster design."
            );

            if (finalExplanationStatusText != null)
            {
                finalExplanationStatusText.text =
                    "Please enter your final explanation before calculating the score.";
            }

            Speak(
                "Please enter your final explanation."
            );

            return;
        }


        string latestUrl =
            GetPracticePosterUrl();


        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Practice poster is missing."
            );

            return;
        }


        practiceData.finalExplanation =
            explanation;


        IsProcessing = true;

        UpdateAllButtonStates();

        ShowLoading(
            "Evaluating your practice design. Please wait."
        );


        try
        {
            if (aiBackendManager == null)
                aiBackendManager =
                    AIBackendManager.Instance;


            if (aiBackendManager == null)
            {
                SetStatus(
                    "AI Backend Manager is not available."
                );

                return;
            }


            AIBackendManager.ScoreRequestData requestData =
                new AIBackendManager.ScoreRequestData
                {
                    userPrompt =
                        OriginalPrompt,

                    imageUrl =
                        latestUrl,

                    revisionPrompt =
                        LastRevisionPrompt,

                    revisionHistory =
                        BuildRevisionHistoryText(),

                    revisionCount =
                        CurrentRevisionCount,

                    finalExplanation =
                        explanation
                };


            AIBackendManager.ScoreResult result =
                await aiBackendManager.ScorePoster(
                    requestData
                );


            if (
                result == null ||
                !result.success ||
                result.score == null
            )
            {
                SetStatus(
                    "Practice score calculation failed."
                );

                return;
            }


            // =================================================
            // DISPLAY PRACTICE SCORE
            // =================================================

            DisplayPracticeScore(
                result.score
            );


            scoreCalculated = true;


            CloseAllWorkspacePanels();

            if (scorePanel != null)
                scorePanel.SetActive(true);


            Speak(
                "Practice evaluation completed."
            );
        }
        catch (Exception exception)
        {
            SetStatus(
                "Practice score calculation failed: " +
                exception.Message
            );

            Debug.LogException(exception);
        }
        finally
        {
            await HideLoading();

            IsProcessing = false;

            UpdateAllButtonStates();
        }
    }

    private void DisplayPracticeScore(
    AIBackendManager.ScoreBreakdown score)
    {
        if (score == null)
            return;


        int promptScore =
            Mathf.Clamp(
                score.promptQuality,
                0,
                20
            );

        int posterMessageScore =
            Mathf.Clamp(
                score.posterMessage,
                0,
                20
            );

        int designScore =
            Mathf.Clamp(
                score.designQuality,
                0,
                20
            );

        int accessibilityScore =
            Mathf.Clamp(
                score.accessibilityUnderstanding,
                0,
                20
            );


        int revisionScore;

        if (CurrentRevisionCount == 0)
        {
            revisionScore = 10;
        }
        else
        {
            revisionScore =
                Mathf.Clamp(
                    score.revisionProcess,
                    0,
                    10
                );
        }


        int explanationScore =
            Mathf.Clamp(
                score.finalExplanation,
                0,
                10
            );


        int finalJustification =
            Mathf.Clamp(
                revisionScore +
                explanationScore,
                0,
                20
            );


        int total =
            Mathf.Clamp(
                promptScore +
                posterMessageScore +
                designScore +
                accessibilityScore +
                finalJustification,
                0,
                100
            );


        // =====================================================
        // UI
        // =====================================================

        if (promptQualityText != null)
            promptQualityText.text =
                promptScore + "/20";

        if (posterMessageText != null)
            posterMessageText.text =
                posterMessageScore + "/20";

        if (designQualityText != null)
            designQualityText.text =
                designScore + "/20";

        if (accessibilityText != null)
            accessibilityText.text =
                accessibilityScore + "/20";

        if (finalDesignJustificationText != null)
            finalDesignJustificationText.text =
                finalJustification + "/20";

        if (totalScoreText != null)
            totalScoreText.text =
                total.ToString();


        if (feedbackText != null)
            feedbackText.text =
                score.feedback ?? "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text =
                score.improvementSuggestion ?? "";


        // =====================================================
        // SAVE PRACTICE DATA ONLY
        // =====================================================

        practiceData.score =
            total;

        practiceData.promptQuality =
            promptScore;

        practiceData.posterMessage =
            posterMessageScore;

        practiceData.designQuality =
            designScore;

        practiceData.accessibilityUnderstanding =
            accessibilityScore;

        practiceData.revisionProcessScore =
            revisionScore;

        practiceData.finalExplanationScore =
            explanationScore;

        practiceData.feedback =
            score.feedback ?? "";

        practiceData.improvementSuggestion =
            score.improvementSuggestion ?? "";


        Debug.Log(
            "PRACTICE FINAL SCORE: " +
            total +
            "/100"
        );
    }


    // =========================================================
    // DISPLAY SCORE
    // =========================================================

    private void DisplayScore(
        AIBackendManager.ScoreBreakdown score)
    {
        if (score == null)
            return;


        int promptScore =
            Mathf.Clamp(
                score.promptQuality,
                0,
                20
            );

        int posterMessageScore =
            Mathf.Clamp(
                score.posterMessage,
                0,
                20
            );

        int designScore =
            Mathf.Clamp(
                score.designQuality,
                0,
                20
            );

        int accessibilityScore =
            Mathf.Clamp(
                score.accessibilityUnderstanding,
                0,
                20
            );


        int revisionScore;

        if (CurrentRevisionCount == 0)
        {
            revisionScore = 10;
        }
        else
        {
            revisionScore =
                Mathf.Clamp(
                    score.revisionProcess,
                    0,
                    10
                );
        }


        int explanationScore =
            Mathf.Clamp(
                score.finalExplanation,
                0,
                10
            );


        int finalJustification =
            Mathf.Clamp(
                revisionScore +
                explanationScore,
                0,
                20
            );


        int total =
            Mathf.Clamp(
                promptScore +
                posterMessageScore +
                designScore +
                accessibilityScore +
                finalJustification,
                0,
                100
            );


        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (promptQualityText != null)
            promptQualityText.text =
                promptScore + "/20";

        if (posterMessageText != null)
            posterMessageText.text =
                posterMessageScore + "/20";

        if (designQualityText != null)
            designQualityText.text =
                designScore + "/20";

        if (accessibilityText != null)
            accessibilityText.text =
                accessibilityScore + "/20";

        if (finalDesignJustificationText != null)
            finalDesignJustificationText.text =
                finalJustification + "/20";

        if (totalScoreText != null)
            totalScoreText.text =
                total.ToString();


        if (feedbackText != null)
            feedbackText.text =
                score.feedback ?? "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text =
                score.improvementSuggestion ?? "";


        // -----------------------------------------------------
        // PARTICIPANT DATA
        // -----------------------------------------------------

        ParticipantManager.Instance.SetScore(
            total,
            promptScore,
            posterMessageScore,
            designScore,
            accessibilityScore,
            revisionScore,
            explanationScore
        );

        ParticipantManager.Instance.SetFeedback(
            score.feedback,
            score.improvementSuggestion
        );


        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data != null)
        {
            data.score =
                total;

            data.promptQuality =
                promptScore;

            data.posterMessage =
                posterMessageScore;

            data.designQuality =
                designScore;

            data.accessibilityUnderstanding =
                accessibilityScore;

            data.revisionProcessScore =
                revisionScore;

            data.finalExplanationScore =
                explanationScore;

            data.feedback =
                score.feedback ?? "";

            data.improvementSuggestion =
                score.improvementSuggestion ?? "";
        }


        Debug.Log(
            "FINAL SCORE: " +
            total +
            "/100"
        );
    }


    // =========================================================
    // SCORE PANEL
    // =========================================================

    public void OpenScore()
    {
        if (IsProcessing)
            return;

        CloseAllWorkspacePanels();

        if (scorePanel != null)
            scorePanel.SetActive(true);

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            if (practiceData != null)
            {
                UpdatePracticeScoreUI();
            }

            UpdateAllButtonStates();

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data != null)
            UpdateScoreUIFromData(data);

        UpdateAllButtonStates();
    }

    private void UpdatePracticeScoreUI()
    {
        if (practiceData == null)
            return;


        if (promptQualityText != null)
            promptQualityText.text =
                practiceData.promptQuality + "/20";

        if (posterMessageText != null)
            posterMessageText.text =
                practiceData.posterMessage + "/20";

        if (designQualityText != null)
            designQualityText.text =
                practiceData.designQuality + "/20";

        if (accessibilityText != null)
            accessibilityText.text =
                practiceData.accessibilityUnderstanding + "/20";


        int finalJustification =
            practiceData.revisionProcessScore +
            practiceData.finalExplanationScore;


        if (finalDesignJustificationText != null)
            finalDesignJustificationText.text =
                finalJustification + "/20";


        if (totalScoreText != null)
            totalScoreText.text =
                practiceData.score.ToString();
    }


    private void UpdateScoreUIFromData(
        ParticipantData data)
    {
        if (data == null)
            return;

        if (promptQualityText != null)
            promptQualityText.text =
                data.promptQuality + "/20";

        if (posterMessageText != null)
            posterMessageText.text =
                data.posterMessage + "/20";

        if (designQualityText != null)
            designQualityText.text =
                data.designQuality + "/20";

        if (accessibilityText != null)
            accessibilityText.text =
                data.accessibilityUnderstanding + "/20";

        if (finalDesignJustificationText != null)
        {
            finalDesignJustificationText.text =
                data.GetFinalDesignJustificationScore() +
                "/20";
        }

        if (totalScoreText != null)
            totalScoreText.text =
                data.score.ToString();
    }


    // =========================================================
    // FEEDBACK
    // =========================================================

    public void OpenFeedback()
    {
        if (IsProcessing)
            return;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            CloseAllWorkspacePanels();

            if (feedbackPanel != null)
                feedbackPanel.SetActive(true);


            if (practiceData != null)
            {
                if (feedbackText != null)
                    feedbackText.text =
                        practiceData.feedback ?? "";

                if (improvementSuggestionText != null)
                {
                    improvementSuggestionText.text =
                        practiceData.improvementSuggestion ?? "";
                }
            }


            UpdateAllButtonStates();

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        CloseAllWorkspacePanels();

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data != null)
        {
            if (feedbackText != null)
                feedbackText.text =
                    data.feedback ?? "";

            if (improvementSuggestionText != null)
            {
                improvementSuggestionText.text =
                    data.improvementSuggestion ?? "";
            }
        }

        UpdateAllButtonStates();
    }

    public void ScoreNextButton()
    {
        if (IsProcessing)
            return;

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            if (practiceData == null ||
                !scoreCalculated)
            {
                SetStatus(
                    "Please calculate your practice score first."
                );

                return;
            }

            OpenFeedback();

            return;
        }


        // =====================================================
        // EXISTING COMPETITION CODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
        {
            SetStatus(
                "Participant data is not available."
            );

            return;
        }

        if (!data.HasScore())
        {
            SetStatus(
                "Please calculate your score first."
            );

            Speak(
                "Please calculate your score first."
            );

            return;
        }

        OpenFeedback();
    }

    public void FeedbackNextButton()
    {
        if (IsProcessing)
            return;


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            Debug.Log(
                "DesignManager: Practice completed. " +
                "Returning to Main Dashboard."
            );

            ReturnToMainDashboard();

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        OpenLeaderboard();
    }
     

    public void OpenLeaderboard()
    {
        if (IsProcessing)
            return;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLeaderboard();
        }
        else
        {
            CloseAllWorkspacePanels();
        }
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    public void BackToPrompt()
    {
        if (!IsProcessing)
            OpenPrompt();
    }


    public void BackToOutput()
    {
        if (!IsProcessing)
            OpenOutput();
    }


    public async void BackToDescription()
    {
        if (!IsProcessing)
            await OpenDescriptionAsync();
    }


    public async void BackToRevision()
    {
        if (!IsProcessing)
            await OpenRevisionAsync();
    }


    public async void BackToFinalExplanation()
    {
        if (IsProcessing)
            return;

        await OpenFinalExplanationAsync();
    }


    public void BackToScore()
    {
        if (!IsProcessing)
            OpenScore();
    }


    // =========================================================
    // POSTER URL
    // =========================================================

    private string GetOriginalPosterUrl()
    {
        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return "";

        return data.originalImageUrl ?? "";
    }


    private string GetLatestPosterUrl()
    {
        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
            return "";

        string url =
            data.GetLatestPosterUrl();

        if (!string.IsNullOrWhiteSpace(url))
        {
            CurrentPosterUrl = url;
            return url;
        }

        return CurrentPosterUrl ?? "";
    }


    // =========================================================
    // VALIDATE
    // =========================================================

    private bool ValidateParticipant()
    {
        if (ParticipantManager.Instance == null)
        {
            SetStatus(
                "Participant Manager is not available."
            );

            return false;
        }

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
        {
            SetStatus(
                "Participant data is not available."
            );

            return false;
        }

        if (!data.HasParticipantDetails())
        {
            SetStatus(
                "Please complete all participant details."
            );

            return false;
        }

        if (!data.HasChallenge())
        {
            SetStatus(
                "Please join a challenge first."
            );

            return false;
        }

        if (data.isSubmitted)
        {
            SetStatus(
                "This challenge has already been submitted."
            );

            return false;
        }

        return true;
    }


    // =========================================================
    // REVISION COUNTER
    // =========================================================

    private void UpdateRevisionCounter()
    {
        if (revisionAttemptText == null)
            return;

        revisionAttemptText.text =
            CurrentRevisionCount +
            " / " +
            MAX_REVISION_COUNT;
    }


    // =========================================================
    // BUTTON STATES
    // =========================================================

    private void UpdateAllButtonStates()
    {
        UpdatePromptButtonStates();
        UpdateRevisionButtonStates();
        UpdateCalculateScoreButton();
        UpdateNavigationButtonStates();


        if (descriptionNextButton != null)
        {
            descriptionNextButton.interactable =
                !IsProcessing &&
                !string.IsNullOrWhiteSpace(
                    CurrentPosterDescription
                );
        }


        if (outputNextButton != null)
        {
            bool posterAvailable;

            if (IsPracticeMode())
            {
                posterAvailable =
                    !string.IsNullOrWhiteSpace(
                        GetPracticePosterUrl()
                    );
            }
            else
            {
                posterAvailable =
                    !string.IsNullOrWhiteSpace(
                        GetOriginalPosterUrl()
                    );
            }

            outputNextButton.interactable =
                !IsProcessing &&
                posterAvailable;
        }


        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;


        bool hasScore = 
            data != null && data.HasScore();

        if (scoreNextButton != null)
        {
            scoreNextButton.interactable =
                !IsProcessing &&
                hasScore;
        }


        if (feedbackNextButton != null)
        {
            feedbackNextButton.interactable =
                !IsProcessing &&
                submitted;
        }

        if (promptInput != null)
        {
            promptInput.interactable =
                !IsProcessing &&
                !submittedViewMode;
        }


        if (revisionPromptInput != null)
        {
            revisionPromptInput.interactable =
                !IsProcessing &&
                !submittedViewMode;
        }


        if (finalExplanationInput != null)
        {
            finalExplanationInput.interactable =
                !IsProcessing &&
                !submittedViewMode;
        }

        if (finalExplanationNextButton != null)
        {
            finalExplanationNextButton.interactable =
                !IsProcessing &&
                data != null &&
                data.HasScore();
        }
    }


    private void UpdateNavigationButtonStates()
    {
        bool enabled =
            !IsProcessing;


        if (promptBackButton != null)
            promptBackButton.interactable =
                enabled;


        if (outputBackButton != null)
            outputBackButton.interactable =
                enabled;


        if (descriptionBackButton != null)
            descriptionBackButton.interactable =
                enabled;


        if (revisionBackButton != null)
            revisionBackButton.interactable =
                enabled;


        if (finalExplanationBackButton != null)
            finalExplanationBackButton.interactable =
                enabled;


        if (scoreBackButton != null)
            scoreBackButton.interactable =
                enabled;


        if (feedbackBackButton != null)
            feedbackBackButton.interactable =
                enabled;


        if (originalFullPosterBackButton != null)
            originalFullPosterBackButton.interactable =
                enabled;


        if (latestFullPosterBackButton != null)
            latestFullPosterBackButton.interactable =
                enabled;
    }


    private void UpdatePromptButtonStates()
    {
        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            bool practicePosterExists =
                !string.IsNullOrWhiteSpace(
                    GetPracticePosterUrl()
                );

            if (generatePosterButton != null)
            {
                generatePosterButton.interactable =
                    !IsProcessing &&
                    !originalPosterGenerated;
            }

            if (promptNextButton != null)
            {
                promptNextButton.interactable =
                    !IsProcessing &&
                    practicePosterExists;
            }

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;

        bool competitionPosterExists =
            !string.IsNullOrWhiteSpace(
                GetOriginalPosterUrl()
            );


        if (generatePosterButton != null)
        {
            generatePosterButton.interactable =
                !IsProcessing &&
                !originalPosterGenerated &&
                !submitted &&
                !submittedViewMode;
        }


        if (promptNextButton != null)
        {
            promptNextButton.interactable =
                !IsProcessing &&
                competitionPosterExists;
        }
    }


    private void SetPromptButtonsInteractable(
    bool value)
    {
        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            bool posterExists =
                !string.IsNullOrWhiteSpace(
                    GetPracticePosterUrl()
                );

            if (generatePosterButton != null)
            {
                generatePosterButton.interactable =
                    value &&
                    !originalPosterGenerated;
            }

            if (promptNextButton != null)
            {
                promptNextButton.interactable =
                    value &&
                    posterExists;
            }

            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;

        if (generatePosterButton != null)
        {
            generatePosterButton.interactable =
                value &&
                !originalPosterGenerated &&
                !submitted;
        }

        if (promptNextButton != null)
        {
            promptNextButton.interactable =
                value &&
                !string.IsNullOrWhiteSpace(
                    GetOriginalPosterUrl()
                );
        }
    }


    private void UpdateRevisionButtonStates()
    {
        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;


        if (reviseButton != null)
        {
            reviseButton.interactable =
                !IsProcessing &&
                !submitted &&
                CurrentRevisionCount <
                MAX_REVISION_COUNT;
        }


        if (revisionNextButton != null)
        {
            revisionNextButton.interactable =
                !IsProcessing;
        }
    }


    private void SetRevisionButtonsInteractable(
        bool value)
    {
        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;


        if (reviseButton != null)
        {
            reviseButton.interactable =
                value &&
                !submitted &&
                CurrentRevisionCount <
                MAX_REVISION_COUNT;
        }

        if (revisionNextButton != null)
        {
            revisionNextButton.interactable =
                value &&
                !submitted;
        }
    }


    private void UpdateCalculateScoreButton()
    {
        if (calculateScoreButton == null)
            return;

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        bool submitted =
            data != null &&
            data.isSubmitted;

        calculateScoreButton.interactable =
            !IsProcessing &&
            !submitted;
    }


    // =========================================================
    // LOAD POSTER
    // =========================================================

    private async Task LoadPosterToImage(
        string imageUrl,
        RawImage target)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        if (target == null)
            return;

        if (aiBackendManager == null)
            aiBackendManager =
                AIBackendManager.Instance;

        if (aiBackendManager == null)
            return;

        try
        {
            Texture2D texture =
                await aiBackendManager.DownloadImage(
                    imageUrl
                );

            if (texture != null)
                target.texture = texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DesignManager: Failed to download image: " +
                exception.Message
            );
        }
    }

    // =========================================================
    // SUBMITTED VIEW MODE
    // =========================================================

    public void SetSubmittedViewMode(bool value)
    {
        submittedViewMode = value;

        Debug.Log(
            "DesignManager: Submitted View Mode = " +
            submittedViewMode
        );

        UpdateAllButtonStates();
    }

    public bool IsSubmittedViewMode()
    {
        return submittedViewMode;
    }

    // =========================================================
    // RESET NEW CHALLENGE
    // =========================================================

    public void PrepareForNewChallenge()
    {
        submittedViewMode = false;

        IsProcessing = false;

        scoreCalculated = false;

        originalPosterGenerated = false;

        CurrentRevisionCount = 0;

        OriginalPrompt = "";

        CurrentPosterUrl = "";

        CurrentPosterDescription = "";

        LastRevisionPrompt = "";

        latestFullPosterReturnPage =
            FullPosterReturnPage.None;

        revisionHistory.Clear();


        if (promptInput != null)
        {
            promptInput.text = "";
            promptInput.interactable = true;
        }

        if (revisionPromptInput != null)
            revisionPromptInput.text = "";

        if (finalExplanationInput != null)
            finalExplanationInput.text = "";


        SetPromptStatus("");
        SetDescriptionStatus("");

        if (descriptionText != null)
            descriptionText.text = "";

        if (finalExplanationStatusText != null)
            finalExplanationStatusText.text = "";

        if (promptQualityText != null)
            promptQualityText.text = "";

        if (posterMessageText != null)
            posterMessageText.text = "";

        if (designQualityText != null)
            designQualityText.text = "";

        if (accessibilityText != null)
            accessibilityText.text = "";

        if (finalDesignJustificationText != null)
            finalDesignJustificationText.text = "";

        if (totalScoreText != null)
            totalScoreText.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text = "";


        ClearImage(outputPosterImage);
        ClearImage(descriptionPosterImage);
        ClearImage(revisionPosterImage);
        ClearImage(finalExplanationPosterImage);
        ClearImage(originalFullPosterImage);
        ClearImage(latestFullPosterImage);


        ForceHideLoading();

        CloseAllWorkspacePanels();

        UpdateRevisionCounter();

        UpdateAllButtonStates();
    }


    public void ResetDesign()
    {
        PrepareForNewChallenge();

        if (ParticipantManager.Instance != null)
        {
            ParticipantManager.Instance.ClearDesignData();
        }
    }


    // =========================================================
    // CLOSE PANELS
    // =========================================================

    public void CloseAllWorkspacePanels()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (outputPanel != null)
            outputPanel.SetActive(false);

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        if (revisionPanel != null)
            revisionPanel.SetActive(false);

        if (finalExplanationPanel != null)
            finalExplanationPanel.SetActive(false);

        if (scorePanel != null)
            scorePanel.SetActive(false);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (originalFullPosterPanel != null)
            originalFullPosterPanel.SetActive(false);

        if (latestFullPosterPanel != null)
            latestFullPosterPanel.SetActive(false);
    }


    // =========================================================
    // IMAGE
    // =========================================================

    private void ClearImage(
        RawImage image)
    {
        if (image != null)
            image.texture = null;
    }


    // =========================================================
    // LOADING
    // =========================================================

    public void ShowLoading(string message)
    {
        loadingRequestCount++;

        IsProcessing = true;

        if (!loadingPopupVisible)
        {
            loadingStartTime =
                Time.unscaledTime;

            loadingPopupVisible = true;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // IMPORTANT:
        // The correct variable is loadingMessage,
        // not loadingText.
        if (loadingMessage != null)
        {
            loadingMessage.text =
                message;
        }

        UpdateAllButtonStates();

        Debug.Log(
            "DesignManager: Loading started. " +
            "Requests = " +
            loadingRequestCount +
            " | Message = " +
            message
        );
    }


    private void ForceHideLoading()
    {
        loadingRequestCount = 0;

        loadingPopupVisible = false;

        IsProcessing = false;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (loadingMessage != null)
        {
            loadingMessage.text = "";
        }
    }


    public async Task HideLoading()
    {
        if (!loadingPopupVisible)
            return;

        loadingRequestCount =
            Mathf.Max(
                0,
                loadingRequestCount - 1
            );

        // Another loading operation is still active.
        if (loadingRequestCount > 0)
            return;

        float elapsed =
            Time.unscaledTime -
            loadingStartTime;

        float remaining =
            minimumLoadingDuration -
            elapsed;

        if (remaining > 0f)
        {
            await Task.Delay(
                Mathf.RoundToInt(
                    remaining * 1000f
                )
            );
        }

        // A new loading operation may have started
        // while we were waiting.
        if (loadingRequestCount > 0)
            return;

        loadingPopupVisible = false;

        IsProcessing = false;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (loadingMessage != null)
        {
            loadingMessage.text = "";
        }

        UpdateAllButtonStates();

        Debug.Log(
            "DesignManager: Loading finished."
        );
    }


    private async void ShowTemporaryLoading(
        string message)
    {
        ShowLoading(message);

        await Task.Delay(2500);

        await HideLoading();
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void SetPromptStatus(
        string message)
    {
        if (promptStatusText != null)
            promptStatusText.text = message;

        if (promptMessageText != null)
            promptMessageText.text = message;
    }


    private void SetDescriptionStatus(
        string message)
    {
        if (descriptionStatusText != null)
            descriptionStatusText.text = message;
    }


    private void SetStatus(
        string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(
            "DesignManager: " +
            message
        );
    }

    private void StartNewPracticeSession()
    {
        Debug.Log(
            "DesignManager: Starting a NEW Practice Mode session."
        );

        // Create completely fresh practice data
        practiceData = new PracticeData();

        submittedViewMode = false;

        IsProcessing = false;

        scoreCalculated = false;

        originalPosterGenerated = false;

        CurrentRevisionCount = 0;

        OriginalPrompt = "";

        CurrentPosterUrl = "";

        CurrentPosterDescription = "";

        LastRevisionPrompt = "";

        revisionHistory.Clear();

        latestFullPosterReturnPage =
            FullPosterReturnPage.None;


        // -----------------------------------------------------
        // CLEAR INPUTS
        // -----------------------------------------------------

        if (promptInput != null)
        {
            promptInput.text = "";
            promptInput.interactable = true;
        }

        if (revisionPromptInput != null)
        {
            revisionPromptInput.text = "";
        }

        if (finalExplanationInput != null)
        {
            finalExplanationInput.text = "";
        }


        // -----------------------------------------------------
        // CLEAR TEXT
        // -----------------------------------------------------

        if (descriptionText != null)
            descriptionText.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text = "";

        if (promptQualityText != null)
            promptQualityText.text = "";

        if (posterMessageText != null)
            posterMessageText.text = "";

        if (designQualityText != null)
            designQualityText.text = "";

        if (accessibilityText != null)
            accessibilityText.text = "";

        if (finalDesignJustificationText != null)
            finalDesignJustificationText.text = "";

        if (totalScoreText != null)
            totalScoreText.text = "";

        if (descriptionStatusText != null)
            descriptionStatusText.text = "";

        if (finalExplanationStatusText != null)
            finalExplanationStatusText.text = "";


        // -----------------------------------------------------
        // CLEAR IMAGES
        // -----------------------------------------------------

        ClearImage(outputPosterImage);
        ClearImage(descriptionPosterImage);
        ClearImage(revisionPosterImage);
        ClearImage(finalExplanationPosterImage);
        ClearImage(originalFullPosterImage);
        ClearImage(latestFullPosterImage);


        // -----------------------------------------------------
        // CLOSE SAMPLE POPUP
        // -----------------------------------------------------

        if (samplePromptPanel != null)
            samplePromptPanel.SetActive(false);


        UpdateRevisionCounter();
        UpdateSamplePromptButton();
        UpdateAllButtonStates();
    }

    // =========================================================
    // SAMPLE PROMPT
    // =========================================================

    private void UpdateSamplePromptButton()
    {
        if (samplePromptButton != null)
        {
            samplePromptButton.gameObject.SetActive(
                IsPracticeMode()
            );
        }

        // Sample panel must always be hidden
        // until the user clicks the button.
        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }
    }


    public void OpenSamplePromptPanel()
    {
        if (IsProcessing)
            return;

        if (!IsPracticeMode())
            return;

        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(true);
        }

        Speak(
            "Sample prompts. Choose one of three sample prompts."
        );
    }


    public void CloseSamplePromptPanel()
    {
        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }

        Speak(
            "Sample prompts closed."
        );
    }


    public void SelectSamplePrompt1()
    {
        SelectSamplePrompt(
            "Create a poster promoting environmental awareness and encouraging people to protect the environment."
        );
    }


    public void SelectSamplePrompt2()
    {
        SelectSamplePrompt(
            "Design a poster promoting inclusive education and equal learning opportunities for everyone."
        );
    }


    public void SelectSamplePrompt3()
    {
        SelectSamplePrompt(
            "Create a poster encouraging healthy digital habits and responsible use of technology."
        );
    }


    private void SelectSamplePrompt(
        string samplePrompt)
    {
        if (!IsPracticeMode())
            return;

        if (promptInput == null)
            return;

        promptInput.text =
            samplePrompt;

        promptInput.interactable = true;

        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }

        promptInput.Select();
        promptInput.ActivateInputField();

        SetPromptStatus("");

        UpdateAllButtonStates();

        Speak(
            "Sample prompt selected."
        );
    }


    // =========================================================
    // ACCESSIBILITY
    // =========================================================

    private void Speak(
        string message)
    {
        try
        {
            if (
                AccessibilityToggle
                    .AccessibilityEnabled
            )
            {
                AccessibilityToggle
                    .AccessibilitySpeech
                    .SpeakNavigation(
                        message
                    );
            }
        }
        catch
        {
            // Accessibility must never
            // break the main workflow.
        }
    }
}