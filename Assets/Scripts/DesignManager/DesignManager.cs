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

    // =========================================================
    // IN-MEMORY POSTER URLs
    // =========================================================
    //
    // IMPORTANT:
    // Generated images are Base64 strings when Firebase Storage
    // is not being used.
    //
    // NEVER save these values into Firestore.
    // They only exist during the current Unity session.
    //

    private string currentOriginalPosterUrl = "";
    private string currentLatestPosterUrl = "";

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

        // Make sure Sample Prompt starts hidden
        // in Competition Mode.
        UpdateSamplePromptButton();

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
        _ = LoadParticipantDataAsync();
    }


    public async Task RestoreCurrentSubmissionPosterAsync()
    {
        if (ParticipantManager.Instance == null)
            return;

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
            return;

        await RestoreSavedPostersAsync(data);

        UpdateAllButtonStates();
    }


    private async Task LoadParticipantDataAsync()
    {
        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
            return;

        // -----------------------------------------------------
        // PROMPT
        // -----------------------------------------------------

        OriginalPrompt =
            data.prompt ?? "";

        if (promptInput != null)
            promptInput.text =
                OriginalPrompt;

        // -----------------------------------------------------
        // SUBMISSION STATE
        // -----------------------------------------------------

        scoreCalculated =
            data.HasScore();

        // IMPORTANT:
        // Do NOT use data.originalImageUrl to determine whether
        // a poster exists.
        //
        // Images are no longer stored in Firestore.

        originalPosterGenerated =
            !string.IsNullOrWhiteSpace(
                currentOriginalPosterUrl
            );

        submittedViewMode =
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
        // REVISION HISTORY
        // -----------------------------------------------------

        LoadRevisionHistory(
            data.revisionHistory
        );

        // -----------------------------------------------------
        // POSTER RESTORE
        // -----------------------------------------------------
        //
        // The main submission document contains metadata only.
        // Restore the actual poster from local/Firestore poster
        // storage, with backend storagePath as a legacy fallback.
        //

        if (!IsPracticeMode())
        {
            await RestoreSavedPostersAsync(data);
        }

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
            data.isSubmitted +
            " | Poster restore enabled = true"
        );

        UpdateAllButtonStates();
    }



    private async Task RestoreSavedPostersAsync(
        ParticipantData data)
    {
        if (data == null ||
            ParticipantManager.Instance == null)
        {
            return;
        }

        string submissionID =
            data.submissionID ?? "";

        if (string.IsNullOrWhiteSpace(submissionID))
            return;

        Texture2D originalTexture =
            await PosterStorage.LoadAsync(
                submissionID,
                "original"
            );

        Texture2D latestTexture =
            await PosterStorage.LoadAsync(
                submissionID,
                "latest"
            );

        // -----------------------------------------------------
        // BACKEND STORAGE FALLBACK
        // -----------------------------------------------------

        if (latestTexture == null &&
            !string.IsNullOrWhiteSpace(data.storagePath))
        {
            latestTexture =
                await DownloadStoredPosterTexture(
                    data.storagePath
                );
        }

        if (originalTexture == null &&
            latestTexture != null &&
            data.revisionCount <= 0)
        {
            originalTexture =
                latestTexture;
        }

        // -----------------------------------------------------
        // RESTORE ORIGINAL
        // -----------------------------------------------------

        if (originalTexture != null)
        {
            currentOriginalPosterUrl =
                TextureToDataUrl(originalTexture);

            if (data.revisionCount <= 0)
            {
                currentLatestPosterUrl =
                    currentOriginalPosterUrl;
            }

            if (originalFullPosterImage != null)
                originalFullPosterImage.texture =
                    originalTexture;

            if (data.revisionCount <= 0 &&
                outputPosterImage != null)
            {
                outputPosterImage.texture =
                    originalTexture;
            }

            originalPosterGenerated = true;
        }

        // -----------------------------------------------------
        // RESTORE LATEST
        // -----------------------------------------------------

        if (latestTexture != null)
        {
            currentLatestPosterUrl =
                TextureToDataUrl(latestTexture);

            CurrentPosterUrl =
                currentLatestPosterUrl;

            if (latestFullPosterImage != null)
                latestFullPosterImage.texture =
                    latestTexture;

            if (data.revisionCount > 0)
            {
                if (descriptionPosterImage != null)
                    descriptionPosterImage.texture =
                        latestTexture;

                if (revisionPosterImage != null)
                    revisionPosterImage.texture =
                        latestTexture;

                if (finalExplanationPosterImage != null)
                    finalExplanationPosterImage.texture =
                        latestTexture;
            }

            originalPosterGenerated = true;
        }

        if (!string.IsNullOrWhiteSpace(
            currentLatestPosterUrl))
        {
            CurrentPosterUrl =
                currentLatestPosterUrl;
        }

        Debug.Log(
            "DesignManager: Poster restore completed. " +
            "Original = " +
            (originalTexture != null) +
            " | Latest = " +
            (latestTexture != null) +
            " | Submitted = " +
            data.isSubmitted
        );
    }


    private string TextureToDataUrl(
        Texture2D texture)
    {
        if (texture == null)
            return "";

        try
        {
            byte[] pngBytes =
                texture.EncodeToPNG();

            if (pngBytes == null ||
                pngBytes.Length == 0)
            {
                return "";
            }

            return
                "data:image/png;base64," +
                Convert.ToBase64String(pngBytes);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DesignManager: Failed to convert restored poster to data URL: " +
                exception.Message
            );

            return "";
        }
    }


    private async Task<bool> SavePosterBackupAsync(
        string variant,
        string imageUrl,
        bool requireCloud)
    {
        if (IsPracticeMode())
            return true;

        if (ParticipantManager.Instance == null)
            return false;

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null ||
            string.IsNullOrWhiteSpace(data.submissionID) ||
            string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        Texture2D texture =
            await DownloadPosterTexture(imageUrl);

        if (texture == null)
            return false;

        PosterStorage.SaveResult result =
            await PosterStorage.SaveAsync(
                data.submissionID,
                variant,
                texture
            );

        if (requireCloud)
        {
            if (!result.cloudSaved)
            {
                SetStatus(
                    "Poster backup could not be saved to the cloud. Please check your connection and try again."
                );

                return false;
            }

            return true;
        }

        return result.localSaved || result.cloudSaved;
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
        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "Please wait until the current process is finished."
            );

            return;
        }


        // =====================================================
        // PRACTICE
        // =====================================================

        if (IsPracticeMode())
        {
            if (PracticeManager.Instance != null)
            {
                PracticeManager.Instance.ExitPractice();
            }
            else
            {
                SetDesignMode(DesignMode.Competition);
                ClearPracticeRuntimeState();

                if (UIManager.Instance != null)
                    UIManager.Instance.ShowMainMenu();
            }

            return;
        }


        // =====================================================
        // SUBMITTED CHALLENGE
        // =====================================================

        if (ParticipantManager.Instance != null &&
            ParticipantManager.Instance.IsSubmitted())
        {
            ReturnToMainDashboard();
            return;
        }


        // =====================================================
        // ACTIVE CHALLENGE
        // =====================================================

        ShowHomeConfirmationPopup();
    }

    private void ClearPracticeRuntimeState()
    {
        practiceData = null;

        submittedViewMode = false;

        scoreCalculated = false;

        originalPosterGenerated = false;

        CurrentRevisionCount = 0;

        OriginalPrompt = "";

        CurrentPosterUrl = "";

        currentOriginalPosterUrl = "";

        currentLatestPosterUrl = "";

        CurrentPosterDescription = "";

        LastRevisionPrompt = "";

        revisionHistory.Clear();

        latestFullPosterReturnPage =
            FullPosterReturnPage.None;

        if (promptInput != null)
        {
            promptInput.text = "";
            promptInput.interactable = true;
        }

        if (revisionPromptInput != null)
            revisionPromptInput.text = "";

        if (finalExplanationInput != null)
            finalExplanationInput.text = "";

        if (descriptionText != null)
            descriptionText.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (improvementSuggestionText != null)
            improvementSuggestionText.text = "";

        if (samplePromptPanel != null)
            samplePromptPanel.SetActive(false);

        ClearImage(outputPosterImage);
        ClearImage(descriptionPosterImage);
        ClearImage(revisionPosterImage);
        ClearImage(finalExplanationPosterImage);
        ClearImage(originalFullPosterImage);
        ClearImage(latestFullPosterImage);

        UpdateSamplePromptButton();
        UpdateRevisionCounter();
        UpdateAllButtonStates();
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

        // -------------------------------------------------
        // MAKE SURE WE ARE IN COMPETITION MODE
        // -------------------------------------------------

        SetDesignMode(
            DesignMode.Competition
        );


        // -------------------------------------------------
        // RESTORE SAVED CHALLENGE DATA
        // -------------------------------------------------

        LoadParticipantData();


        // -------------------------------------------------
        // OPEN IDEA PROMPT
        // -------------------------------------------------

        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OpenIdeaPrompt();
        }


        // -------------------------------------------------
        // REFRESH BUTTON STATES
        // -------------------------------------------------

        UpdateRevisionCounter();

        UpdateAllButtonStates();


        Debug.Log(
            "DesignManager: Continuing existing challenge. " +
            "Saved participant data restored."
        );
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

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            if (PracticeManager.Instance != null)
            {
                PracticeManager.Instance.ExitPractice();
            }
            else
            {
                SetDesignMode(
                    DesignMode.Competition
                );

                ClearPracticeRuntimeState();

                CloseAllWorkspacePanels();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMainMenu();
                }
            }

            return;
        }

        // =====================================================
        // COMPETITION MODE
        // =====================================================

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

        // ALWAYS enforce Sample Prompt visibility
        // based on the current design mode.
        bool isPractice =
            CurrentMode == DesignMode.Practice;

        if (samplePromptButton != null)
        {
            samplePromptButton.gameObject.SetActive(
                isPractice
            );
        }

        if (!isPractice &&
            samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }

        // Make sure Sample Prompt visibility
        // always matches the current design mode.
        UpdateSamplePromptButton();

        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            Debug.Log(
                "DesignManager: Opening Idea Prompt in Practice Mode."
            );

            submittedViewMode = false;

            if (practiceData == null)
            {
                CreatePracticeData();
            }

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

        Speak(
            "Poster prompt page. Enter your design idea."
        );
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

    public void StartCompetitionMode()
    {
        Debug.Log(
            "DesignManager: Competition Mode selected."
        );

        SetDesignMode(
            DesignMode.Competition
        );

        UpdateSamplePromptButton();

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
                GetPracticeOriginalPosterUrl();
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
            await LoadPosterToImage(
                originalUrl,
                outputPosterImage
            );

            Speak(
                "Generated poster page. This is your original poster."
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

        bool generationSucceeded = false;

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

            // -------------------------------------------------
            // GENERATE ORIGINAL POSTER
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
                    "Poster generation failed."
                );

                Speak(
                    "Poster generation failed."
                );

                return;
            }
            // -------------------------------------------------
            // UPDATE STATE
            // -------------------------------------------------

            originalPosterGenerated = true;

            CurrentRevisionCount = 0;

            CurrentPosterDescription = "";

            LastRevisionPrompt = "";

            revisionHistory.Clear();

            OriginalPrompt = prompt;

            // -------------------------------------------------
            // KEEP IMAGE ONLY IN MEMORY
            // -------------------------------------------------

            CurrentPosterUrl =
                result.imageUrl;

            currentOriginalPosterUrl =
                result.imageUrl;

            currentLatestPosterUrl =
                result.imageUrl;

            // -------------------------------------------------
            // FIRESTORE DATA
            // -------------------------------------------------
            //
            // IMPORTANT:
            // Never put result.imageUrl into ParticipantData.
            //
            // These fields MUST remain empty because result.imageUrl
            // may contain several MB of Base64 data.
            //

            data.prompt =
                prompt;

            data.promptUsed =
                result.promptUsed ?? "";

            data.originalImageUrl =
                "";

            data.posterImageUrl =
                "";

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

            // -------------------------------------------------
            // SAVE METADATA ONLY
            // -------------------------------------------------

            bool saved =
                await ParticipantManager.Instance.Save();

            if (!saved)
            {
                SetStatus(
                    "Poster generated, but failed to save submission data."
                );

                return;
            }

            // -------------------------------------------------
            // DOWNLOAD GENERATED IMAGE
            // -------------------------------------------------

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

            if (texture != null)
            {
                PosterStorage.SaveResult backupResult =
                    await PosterStorage.SaveAsync(
                        data.submissionID,
                        "original",
                        texture
                    );

                if (!backupResult.localSaved &&
                    !backupResult.cloudSaved)
                {
                    Debug.LogWarning(
                        "DesignManager: Original poster backup could not be saved."
                    );
                }
            }

            generationSucceeded = true;

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
            // IMPORTANT:
            // Wait until the loading popup has completely
            // finished before opening Output.
            await HideLoading();

            if (generationSucceeded)
            {
                CloseAllWorkspacePanels();

                if (outputPanel != null)
                    outputPanel.SetActive(true);

                Speak(
                    "Poster generated successfully. Opening output page."
                );
            }

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
        if (practiceData == null)
        {
            CreatePracticeData();
        }

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

        practiceData.prompt =
            prompt;

        OriginalPrompt =
            prompt;

        SetPromptButtonsInteractable(false);

        if (promptInput != null)
            promptInput.interactable = false;

        ShowLoading(
            "Generating your practice poster. Please wait."
        );

        IsProcessing = true;

        bool generationSucceeded = false;

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

            // -------------------------------------------------
            // UPDATE PRACTICE DATA
            // -------------------------------------------------

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

            // -------------------------------------------------
            // UPDATE DESIGN MANAGER
            // -------------------------------------------------

            CurrentPosterUrl =
                result.imageUrl;

            currentOriginalPosterUrl =
                result.imageUrl;

            currentLatestPosterUrl =
                result.imageUrl;

            OriginalPrompt =
                prompt;

            CurrentRevisionCount = 0;

            CurrentPosterDescription = "";

            LastRevisionPrompt = "";

            revisionHistory.Clear();

            originalPosterGenerated = true;

            // -------------------------------------------------
            // DOWNLOAD IMAGE
            // -------------------------------------------------

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

            generationSucceeded = true;
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

            // -------------------------------------------------
            // OPEN OUTPUT ONLY AFTER LOADING FINISHES
            // -------------------------------------------------

            if (generationSucceeded)
            {
                CloseAllWorkspacePanels();

                if (outputPanel != null)
                    outputPanel.SetActive(true);

                Speak(
                    "Practice poster generated successfully. Opening output page."
                );
            }

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
        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            await OpenPracticeDescriptionAsync();
            return;
        }


        // =====================================================
        // COMPETITION MODE
        // =====================================================

        if (IsProcessing)
        {
            ShowTemporaryLoading(
                "The poster description is still loading. Please wait."
            );

            return;
        }


        string latestUrl =
            GetLatestPosterUrl();


        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            SetStatus(
                "Poster is not available."
            );

            return;
        }


        // =====================================================
        // OPEN DESCRIPTION PANEL
        // =====================================================

        CloseAllWorkspacePanels();

        if (descriptionPanel != null)
            descriptionPanel.SetActive(true);


        // =====================================================
        // CHECK EXISTING DESCRIPTION FIRST
        // =====================================================
        //
        // If description already exists:
        // - Do not show loading popup
        // - Do not generate description again
        // - Just display the existing description
        //
        // =====================================================

        if (!string.IsNullOrWhiteSpace(
            CurrentPosterDescription))
        {
            if (descriptionText != null)
            {
                descriptionText.text =
                    CurrentPosterDescription;
            }

            SetDescriptionStatus("");

            try
            {
                await LoadPosterToImage(
                    latestUrl,
                    descriptionPosterImage
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            UpdateAllButtonStates();

            return;
        }


        // =====================================================
        // DESCRIPTION DOES NOT EXIST
        // =====================================================
        //
        // Only show loading when description actually needs
        // to be generated.
        //
        // =====================================================

        ShowLoading(
            "Analyzing your poster. Please wait."
        );

        SetDescriptionStatus(
            "Loading your latest poster..."
        );


        try
        {
            // -------------------------------------------------
            // LOAD LATEST IMAGE
            // -------------------------------------------------

            await LoadPosterToImage(
                latestUrl,
                descriptionPosterImage
            );


            // -------------------------------------------------
            // GENERATE DESCRIPTION
            // -------------------------------------------------

            if (descriptionText != null)
                descriptionText.text = "";

            SetDescriptionStatus(
                "Analyzing your poster. Please wait..."
            );

            await GenerateDescription();
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

            // Make sure Description panel is visible
            // after AI generation has completed.
            CloseAllWorkspacePanels();

            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);

            UpdateAllButtonStates();

            // Automatically read the complete AI description.
            if (!string.IsNullOrWhiteSpace(
                CurrentPosterDescription))
            {
                SpeakFullDescription();
            }
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


        // =====================================================
        // OPEN DESCRIPTION PANEL
        // =====================================================

        CloseAllWorkspacePanels();

        if (descriptionPanel != null)
            descriptionPanel.SetActive(true);


        // =====================================================
        // CHECK EXISTING DESCRIPTION FIRST
        // =====================================================
        //
        // If description already exists:
        // - Do not show loading popup
        // - Do not generate again
        // - Just display the existing description
        //
        // =====================================================

        if (!string.IsNullOrWhiteSpace(
            CurrentPosterDescription))
        {
            if (descriptionText != null)
            {
                descriptionText.text =
                    CurrentPosterDescription;
            }

            SetDescriptionStatus("");

            try
            {
                await LoadPosterToImage(
                    latestUrl,
                    descriptionPosterImage
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            UpdateAllButtonStates();

            return;
        }


        // =====================================================
        // DESCRIPTION DOES NOT EXIST
        // =====================================================
        //
        // Only show loading when description actually needs
        // to be generated.
        //
        // =====================================================

        ShowLoading(
            "Analyzing your practice poster. Please wait."
        );

        SetDescriptionStatus(
            "Loading your practice poster..."
        );


        try
        {
            // -------------------------------------------------
            // LOAD LATEST PRACTICE IMAGE
            // -------------------------------------------------

            await LoadPosterToImage(
                latestUrl,
                descriptionPosterImage
            );


            // -------------------------------------------------
            // GENERATE PRACTICE DESCRIPTION
            // -------------------------------------------------

            if (descriptionText != null)
                descriptionText.text = "";

            SetDescriptionStatus(
                "Analyzing your poster. Please wait..."
            );

            await GeneratePracticeDescription();
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

            CloseAllWorkspacePanels();

            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);

            UpdateAllButtonStates();

            if (!string.IsNullOrWhiteSpace(
                CurrentPosterDescription))
            {
                SpeakFullDescription();
            }
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

    private string GetPracticeOriginalPosterUrl()
    {
        if (!string.IsNullOrWhiteSpace(
            currentOriginalPosterUrl))
        {
            return currentOriginalPosterUrl;
        }

        return "";
    }


    private string GetPracticePosterUrl()
    {
        if (!string.IsNullOrWhiteSpace(
            currentLatestPosterUrl))
        {
            CurrentPosterUrl =
                currentLatestPosterUrl;

            return currentLatestPosterUrl;
        }

        if (!string.IsNullOrWhiteSpace(
            CurrentPosterUrl))
        {
            return CurrentPosterUrl;
        }

        return "";
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

            await RefreshLatestPosterImages();

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

    public void ReplayDescription()
    {
        if (!AccessibilityToggle.AccessibilityEnabled)
            return;

        if (string.IsNullOrWhiteSpace(CurrentPosterDescription))
        {
            Speak(
                "The poster description is not available yet."
            );

            return;
        }

        Speak(CurrentPosterDescription);
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


    public void BackFromLatestFullPoster()
    {
        if (IsProcessing)
            return;

        // Close fullscreen
        if (latestFullPosterPanel != null)
            latestFullPosterPanel.SetActive(false);

        switch (latestFullPosterReturnPage)
        {
            case FullPosterReturnPage.Description:

                CloseAllWorkspacePanels();

                if (descriptionPanel != null)
                    descriptionPanel.SetActive(true);

                UpdateAllButtonStates();

                Speak(
                    "Returning to poster description page."
                );

                break;


            case FullPosterReturnPage.Revision:

                CloseAllWorkspacePanels();

                if (revisionPanel != null)
                    revisionPanel.SetActive(true);

                UpdateAllButtonStates();

                Speak(
                    "Returning to poster revision page."
                );

                break;


            case FullPosterReturnPage.FinalExplanation:

                CloseAllWorkspacePanels();

                if (finalExplanationPanel != null)
                    finalExplanationPanel.SetActive(true);

                UpdateAllButtonStates();

                Speak(
                    "Returning to final explanation page."
                );

                break;


            case FullPosterReturnPage.Score:

                OpenScore();

                break;


            case FullPosterReturnPage.Feedback:

                OpenFeedback();

                break;


            default:

                CloseAllWorkspacePanels();

                if (descriptionPanel != null)
                    descriptionPanel.SetActive(true);

                UpdateAllButtonStates();

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

            Speak(
                "Poster revision page. You can enter changes you want to make to the poster."
            );

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

            // =====================================================
            // DEBUG
            // =====================================================

            Debug.Log(
                "========== REVISION DEBUG =========="
            );

            Debug.Log(
                "Revision Number: " +
                CurrentRevisionCount
            );

            Debug.Log(
                "NEW AI IMAGE URL: " +
                result.imageUrl
            );

            Debug.Log(
                "CurrentPosterUrl: " +
                CurrentPosterUrl
            );


            // -------------------------------------------------
            // SAVE REVISION HISTORY
            // -------------------------------------------------


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
            // UPDATE MEMORY-ONLY IMAGE STATE
            // -------------------------------------------------

            CurrentPosterUrl =
                result.imageUrl;

            currentLatestPosterUrl =
                result.imageUrl;

            // IMPORTANT:
            // We intentionally DO NOT save the image URL/Base64
            // into Firestore.

            // -------------------------------------------------
            // SAVE REVISION METADATA ONLY
            // -------------------------------------------------

            data.revisionPrompt =
                revisionPrompt;

            data.revisionCount =
                CurrentRevisionCount;

            data.revisedImageUrl =
                "";

            data.posterImageUrl =
                "";

            data.originalImageUrl =
                "";

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
                await ParticipantManager.Instance.Save();

            if (!saved)
            {
                SetStatus(
                    "Revision generated, but failed to save."
                );

                return;
            }

            // -------------------------------------------------
            // LOAD NEW REVISED IMAGE
            // -------------------------------------------------

            // Update all latest-poster UI images first.
            await RefreshLatestPosterImages();

            // Persist the latest poster outside the main
            // submission document. This supports same-device
            // restore and cross-device submitted-view restore.
            bool latestBackupSaved =
                await SavePosterBackupAsync(
                    "latest",
                    CurrentPosterUrl,
                    false
                );

            if (!latestBackupSaved)
            {
                Debug.LogWarning(
                    "DesignManager: Latest poster backup could not be saved."
                );
            }

            // Explicitly make sure Description uses
            // the NEW revised poster.
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

            // -------------------------------------------------
            // OPEN DESCRIPTION WITH NEW REVISED POSTER
            // -------------------------------------------------

            CloseAllWorkspacePanels();

            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);

            // Make absolutely sure the Description panel
            // displays the revised poster.
            await LoadPosterToImage(
                CurrentPosterUrl,
                descriptionPosterImage
            );

            SetDescriptionStatus(
                "Analyzing your revised poster. Please wait..."
            );

            // CurrentPosterDescription was cleared above,
            // so GenerateDescription() will generate a NEW
            // description based on the revised image.
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

            practiceData.originalImageUrl = "";
            practiceData.posterImageUrl = "";
            practiceData.revisedImageUrl = "";

            CurrentPosterUrl =
                result.imageUrl;

            currentLatestPosterUrl =
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

            // IMPORTANT:
            // Also update the Description panel image
            // to the newly revised poster.
            await LoadPosterToImage(
                CurrentPosterUrl,
                descriptionPosterImage
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

            SetDescriptionStatus(
                "Analyzing your revised poster. Please wait."
            );

            // Generate NEW description from revised image.
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
            // Use the in-memory original poster.
            CurrentPosterUrl =
                currentOriginalPosterUrl;

            currentLatestPosterUrl =
                currentOriginalPosterUrl;

            // Never save image data to Firestore.
            data.originalImageUrl = "";
            data.revisedImageUrl = "";
            data.posterImageUrl = "";

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
            if (practiceData == null)
            {
                SetStatus(
                    "Practice data is not available."
                );

                return;
            }

            // User MUST calculate score first.
            if (!scoreCalculated)
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

            Speak(
                "Final explanation page. Explain your poster concept, message, target audience, and accessibility considerations."
            );


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


            // Make sure the final poster has a cloud backup
            // before marking the challenge submitted.
            bool posterBackupSaved =
                await SavePosterBackupAsync(
                    "latest",
                    latestUrl,
                    true
                );

            if (!posterBackupSaved)
            {
                SetStatus(
                    "Your score is ready, but the poster backup could not be saved. Please try again."
                );

                return;
            }

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

            // Make sure loading popup is completely closed
            // before opening the Score panel.
            await HideLoading();

            IsProcessing = false;

            OpenScore();

            // Read the complete score automatically.
            ReadScore();
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
            if (loadingPopupVisible)
                await HideLoading();

            IsProcessing = false;

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

            await HideLoading();

            IsProcessing = false;

            CloseAllWorkspacePanels();

            if (scorePanel != null)
                scorePanel.SetActive(true);

            UpdatePracticeScoreUI();

            // Automatically read complete score.
            ReadPracticeScore();
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

        Speak(
            "Score page. Your final evaluation score is " +
            totalScoreText.text +
            " out of 100."
        );
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

            // Automatically read feedback + improvement.
            ReadFeedbackPage();

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

        Speak(
            "Feedback page. " +
            feedbackText.text +
            " Improvement suggestion. " +
            improvementSuggestionText.text
        );
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

            Speak(
                "Leaderboard page. Showing participant rankings."
            );
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
        {
            OpenPrompt();

            Speak(
                "Returning to poster prompt page."
            );
        }
    }


    public void BackToOutput()
    {
        if (!IsProcessing)
        {
            OpenOutput();

            Speak(
                "Returning to generated poster page."
            );
        }
    }


    public async void BackToDescription()
    {
        if (!IsProcessing)
        {
            await OpenDescriptionAsync();
        }
    }


    public async void BackToRevision()
    {
        if (!IsProcessing)
        {
            await OpenRevisionAsync();

            Speak(
                "Returning to poster revision page."
            );
        }
    }


    public async void BackToFinalExplanation()
    {
        if (IsProcessing)
            return;

        await OpenFinalExplanationAsync();

        Speak(
            "Returning to final explanation page."
        );
    }


    public void BackToScore()
    {
        if (!IsProcessing)
        {
            OpenScore();

            Speak(
                "Returning to score page."
            );
        }
    }


    // =========================================================
    // POSTER URL
    // =========================================================

    private string GetOriginalPosterUrl()
    {
        // Image is stored only in memory.
        if (!string.IsNullOrWhiteSpace(
            currentOriginalPosterUrl))
        {
            return currentOriginalPosterUrl;
        }

        return "";
    }


    public string GetLatestPosterUrl()
    {
        // Latest generated/revised poster is memory-only.
        if (!string.IsNullOrWhiteSpace(
            currentLatestPosterUrl))
        {
            CurrentPosterUrl =
                currentLatestPosterUrl;

            return currentLatestPosterUrl;
        }

        // Fallback to CurrentPosterUrl.
        if (!string.IsNullOrWhiteSpace(
            CurrentPosterUrl))
        {
            currentLatestPosterUrl =
                CurrentPosterUrl;

            return CurrentPosterUrl;
        }

        return "";
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
            // =====================================================
            // PRACTICE MODE
            // =====================================================

            if (IsPracticeMode())
            {
                finalExplanationNextButton.interactable =
                    !IsProcessing &&
                    scoreCalculated;
            }

            // =====================================================
            // COMPETITION MODE
            // =====================================================

            else
            {
                finalExplanationNextButton.interactable =
                    !IsProcessing &&
                    data != null &&
                    data.HasScore() &&
                    !submittedViewMode;
            }
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
        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            if (reviseButton != null)
            {
                reviseButton.interactable =
                    !IsProcessing &&
                    originalPosterGenerated &&
                    CurrentRevisionCount <
                    MAX_REVISION_COUNT;
            }

            if (revisionNextButton != null)
            {
                revisionNextButton.interactable =
                    !IsProcessing &&
                    originalPosterGenerated;
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


        if (reviseButton != null)
        {
            reviseButton.interactable =
                !IsProcessing &&
                !submitted &&
                !submittedViewMode &&
                originalPosterGenerated &&
                CurrentRevisionCount <
                MAX_REVISION_COUNT;
        }


        if (revisionNextButton != null)
        {
            revisionNextButton.interactable =
                !IsProcessing &&
                !submitted &&
                !submittedViewMode &&
                originalPosterGenerated;
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


        // =====================================================
        // PRACTICE MODE
        // =====================================================

        if (IsPracticeMode())
        {
            calculateScoreButton.interactable =
                !IsProcessing &&
                originalPosterGenerated;

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


        calculateScoreButton.interactable =
            !IsProcessing &&
            !submitted &&
            !submittedViewMode;
    }


    // =========================================================
    // LOAD POSTER
    // =========================================================

    private async Task<Texture2D> DownloadPosterTexture(
    string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        if (aiBackendManager == null)
            aiBackendManager =
                AIBackendManager.Instance;

        if (aiBackendManager == null)
            return null;

        try
        {
            Texture2D texture =
                await aiBackendManager.DownloadImage(
                    imageUrl
                );

            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DesignManager: Failed to download poster image: " +
                exception.Message
            );

            return null;
        }
    }

    private async Task<Texture2D> DownloadStoredPosterTexture(
    string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return null;

        if (aiBackendManager == null)
            aiBackendManager =
                AIBackendManager.Instance;

        if (aiBackendManager == null)
            return null;

        try
        {
            Texture2D texture =
                await aiBackendManager.DownloadStoredImage(
                    storagePath
                );

            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DesignManager: Failed to download stored poster: " +
                exception.Message
            );

            return null;
        }
    }


    private async Task LoadPosterToImage(
        string imageUrl,
        RawImage target)
    {
        if (target == null)
            return;

        Texture2D texture =
            await DownloadPosterTexture(imageUrl);

        if (texture != null)
        {
            target.texture = texture;

            Canvas.ForceUpdateCanvases();
        }
    }

    private async Task RefreshLatestPosterImages()
    {
        string latestUrl =
            GetLatestPosterUrl();

        if (string.IsNullOrWhiteSpace(latestUrl))
        {
            Debug.LogWarning(
                "DesignManager: Cannot refresh poster. Latest URL is empty."
            );

            return;
        }

        Texture2D texture =
            await DownloadPosterTexture(latestUrl);

        if (texture == null)
        {
            Debug.LogWarning(
                "DesignManager: Failed to refresh latest poster texture."
            );

            return;
        }

        // Update EVERY panel that should show the latest poster.

        if (descriptionPosterImage != null)
            descriptionPosterImage.texture = texture;

        if (revisionPosterImage != null)
            revisionPosterImage.texture = texture;

        if (finalExplanationPosterImage != null)
            finalExplanationPosterImage.texture = texture;

        if (latestFullPosterImage != null)
            latestFullPosterImage.texture = texture;

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "DesignManager: All latest poster images refreshed."
        );
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

        // IMPORTANT:
        // A new challenge always starts in Competition Mode.
        SetDesignMode(
            DesignMode.Competition
        );

        // New Challenge must never reuse Practice data.
        practiceData = null;

        scoreCalculated = false;

        originalPosterGenerated = false;

        CurrentRevisionCount = 0;

        OriginalPrompt = "";

        CurrentPosterUrl = "";

        currentOriginalPosterUrl = "";

        currentLatestPosterUrl = "";

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

        // =====================================================
        // ACCESSIBILITY
        // =====================================================
        //
        // Tell a blind user that an operation has started.
        //
        // =====================================================

        Speak(
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

        currentOriginalPosterUrl = "";

        currentLatestPosterUrl = "";

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
        bool showSamplePrompt =
            CurrentMode == DesignMode.Practice;

        if (samplePromptButton != null)
        {
            samplePromptButton.gameObject.SetActive(
                showSamplePrompt
            );
        }

        if (!showSamplePrompt &&
            samplePromptPanel != null)
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

        // Close Idea Prompt panel
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        // Open Sample Prompt panel
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

        // Reopen Idea Prompt panel
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        Speak(
            "Sample prompts closed. Enter your design idea."
        );
    }


    public void SelectSamplePrompt1()
    {
        SelectSamplePrompt(
            "Create a recycling awareness poster using green colours, recycling icons, and the slogan Recycle Today Save Tomorrow. Use large fonts and high colour contrast."
        );
    }


    public void SelectSamplePrompt2()
    {
        SelectSamplePrompt(
            "Create a road safety poster reminding students to use the pedestrian crossing. Use yellow warning colours and large readable text."
        );
    }


    public void SelectSamplePrompt3()
    {
        SelectSamplePrompt(
            "Create a healthy eating poster encouraging students to eat fruits and vegetables every day. Use colourful illustrations and large fonts."
        );
    }


    private void SelectSamplePrompt(string samplePrompt)
    {
        if (!IsPracticeMode())
            return;

        if (promptInput == null)
            return;

        promptInput.text = samplePrompt;

        promptInput.interactable = true;

        if (samplePromptPanel != null)
        {
            samplePromptPanel.SetActive(false);
        }

        // Reopen Idea Prompt
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
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
    // ACCESSIBILITY / TALKBACK
    // =========================================================

    private string lastSpokenMessage = "";

    private float lastSpeakTime = -10f;

    [SerializeField]
    private float speechCooldown = 0.15f;


    // =========================================================
    // SPEAK
    // =========================================================

    private void Speak(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            if (!AccessibilityToggle.AccessibilityEnabled)
                return;

            // Prevent accidental duplicate speech
            if (
                message == lastSpokenMessage &&
                Time.unscaledTime - lastSpeakTime <
                speechCooldown
            )
            {
                return;
            }

            lastSpokenMessage = message;
            lastSpeakTime = Time.unscaledTime;

            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(message);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DesignManager Accessibility Speech Error: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // READ CURRENT PAGE
    // =========================================================

    public void RepeatCurrentPage()
    {
        if (IsProcessing)
        {
            Speak(
                "Please wait. The current process is still loading."
            );

            return;
        }

        // Sample Prompt
        if (
            samplePromptPanel != null &&
            samplePromptPanel.activeSelf
        )
        {
            Speak(
                "Sample prompts. " +
                "Choose sample prompt one, sample prompt two, " +
                "or sample prompt three. " +
                "You can close this panel to return to the idea prompt."
            );

            return;
        }

        // Prompt
        if (
            promptPanel != null &&
            promptPanel.activeSelf
        )
        {
            ReadPromptPage();
            return;
        }

        // Output
        if (
            outputPanel != null &&
            outputPanel.activeSelf
        )
        {
            Speak(
                "Output page. " +
                "This page displays your generated poster. " +
                "You can open the poster in full screen, " +
                "go back to the idea prompt, " +
                "or continue to the poster description."
            );

            return;
        }

        // Description
        if (
            descriptionPanel != null &&
            descriptionPanel.activeSelf
        )
        {
            ReadDescriptionPage();
            return;
        }

        // Revision
        if (
            revisionPanel != null &&
            revisionPanel.activeSelf
        )
        {
            ReadRevisionPage();
            return;
        }

        // Final Explanation
        if (
            finalExplanationPanel != null &&
            finalExplanationPanel.activeSelf
        )
        {
            ReadFinalExplanationPage();
            return;
        }

        // Score
        if (
            scorePanel != null &&
            scorePanel.activeSelf
        )
        {
            ReadScore();
            return;
        }

        // Feedback
        if (
            feedbackPanel != null &&
            feedbackPanel.activeSelf
        )
        {
            ReadFeedbackPage();
            return;
        }

        Speak(
            "Design workspace."
        );
    }


    // =========================================================
    // READ PROMPT PAGE
    // =========================================================

    public void ReadPromptPage()
    {
        string prompt =
            promptInput != null
                ? promptInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Speak(
                "Idea prompt page. " +
                "Enter your design idea in the prompt field. " +
                "Then press Generate Poster."
            );

            return;
        }

        Speak(
            "Idea prompt page. " +
            "Your current design idea is: " +
            prompt +
            ". " +
            "Press Generate Poster to create your poster. " +
            "You can also open Sample Prompts in Practice Mode."
        );
    }


    // =========================================================
    // READ OUTPUT PAGE
    // =========================================================

    public void ReadOutputPage()
    {
        Speak(
            "Output page. " +
            "Your generated poster is displayed here. " +
            "You can open the poster in full screen. " +
            "Press Next to continue to the poster description. " +
            "Press Back to return to the idea prompt."
        );
    }
    private void SpeakFullDescription()
    {
        if (string.IsNullOrWhiteSpace(CurrentPosterDescription))
        {
            Speak(
                "Poster description is not available."
            );

            return;
        }

        Speak(
            "Poster description is ready. " +
            CurrentPosterDescription
        );
    }

    // =========================================================
    // READ DESCRIPTION PAGE
    // =========================================================

    public void ReadDescriptionPage()
    {
        string description =
            CurrentPosterDescription;

        if (string.IsNullOrWhiteSpace(description))
        {
            Speak(
                "Poster description page. " +
                "The poster description is not available yet. " +
                "Please wait while the poster is analyzed."
            );

            return;
        }

        Speak(
            "Poster description. " +
            description +
            ". " +
            "Press Next to continue to poster revision."
        );
    }


    // =========================================================
    // READ REVISION PAGE
    // =========================================================

    public void ReadRevisionPage()
    {
        string revisionStatus =
            CurrentRevisionCount +
            " of " +
            MAX_REVISION_COUNT +
            " revision attempts used.";

        string previousRevision =
            LastRevisionPrompt;

        if (string.IsNullOrWhiteSpace(previousRevision))
        {
            Speak(
                "Poster revision page. " +
                revisionStatus +
                " You can enter a request to change your poster. " +
                "Press Revise Poster to apply the change, " +
                "or continue without revision."
            );

            return;
        }

        Speak(
            "Poster revision page. " +
            revisionStatus +
            " Your latest revision request was: " +
            previousRevision +
            ". " +
            "You can enter another revision request, " +
            "or continue to the final explanation."
        );
    }


    // =========================================================
    // READ FINAL EXPLANATION PAGE
    // =========================================================

    public void ReadFinalExplanationPage()
    {
        string explanation =
            finalExplanationInput != null
                ? finalExplanationInput.text.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(explanation))
        {
            Speak(
                "Final explanation page. " +
                "Explain why you designed your poster this way. " +
                "Include the purpose of your design, " +
                "important design choices, " +
                "and how the design supports accessibility. " +
                "Then press Calculate Score."
            );

            return;
        }

        Speak(
            "Final explanation page. " +
            "Your current explanation is: " +
            explanation +
            ". " +
            "Press Calculate Score when you are ready."
        );
    }


    // =========================================================
    // READ SCORE
    // =========================================================

    public void ReadScore()
    {
        if (IsPracticeMode())
        {
            ReadPracticeScore();
            return;
        }

        ParticipantData data =
            ParticipantManager.Instance != null
                ? ParticipantManager.Instance.CurrentParticipant
                : null;

        if (data == null)
        {
            Speak(
                "Score information is not available."
            );

            return;
        }

        Speak(
            BuildScoreSpeech(
                data.promptQuality,
                data.posterMessage,
                data.designQuality,
                data.accessibilityUnderstanding,
                data.GetFinalDesignJustificationScore(),
                data.score
            )
        );
    }


    // =========================================================
    // READ PRACTICE SCORE
    // =========================================================

    private void ReadPracticeScore()
    {
        if (practiceData == null)
        {
            Speak(
                "Practice score information is not available."
            );

            return;
        }

        int finalJustification =
            practiceData.revisionProcessScore +
            practiceData.finalExplanationScore;

        Speak(
            BuildScoreSpeech(
                practiceData.promptQuality,
                practiceData.posterMessage,
                practiceData.designQuality,
                practiceData.accessibilityUnderstanding,
                finalJustification,
                practiceData.score
            )
        );
    }


    // =========================================================
    // BUILD SCORE SPEECH
    // =========================================================

    private string BuildScoreSpeech(
        int promptScore,
        int posterMessageScore,
        int designScore,
        int accessibilityScore,
        int finalJustificationScore,
        int totalScore)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.Append(
            "Score page. "
        );

        builder.Append(
            "Your total score is " +
            totalScore +
            " out of 100. "
        );

        builder.Append(
            "Prompt quality: " +
            promptScore +
            " out of 20. "
        );

        builder.Append(
            "Poster message: " +
            posterMessageScore +
            " out of 20. "
        );

        builder.Append(
            "Design quality: " +
            designScore +
            " out of 20. "
        );

        builder.Append(
            "Accessibility understanding: " +
            accessibilityScore +
            " out of 20. "
        );

        builder.Append(
            "Final design justification: " +
            finalJustificationScore +
            " out of 20. "
        );

        builder.Append(
            "Press Next to hear your feedback."
        );

        return builder.ToString();
    }


    // =========================================================
    // READ FEEDBACK
    // =========================================================

    public void ReadFeedback()
    {
        ReadFeedbackPage();
    }


    // =========================================================
    // READ FEEDBACK PAGE
    // =========================================================

    private void ReadFeedbackPage()
    {
        string feedback = "";
        string improvement = "";

        if (IsPracticeMode())
        {
            if (practiceData != null)
            {
                feedback =
                    practiceData.feedback ?? "";

                improvement =
                    practiceData.improvementSuggestion ?? "";
            }
        }
        else
        {
            ParticipantData data =
                ParticipantManager.Instance != null
                    ? ParticipantManager.Instance.CurrentParticipant
                    : null;

            if (data != null)
            {
                feedback =
                    data.feedback ?? "";

                improvement =
                    data.improvementSuggestion ?? "";
            }
        }

        if (string.IsNullOrWhiteSpace(feedback))
        {
            Speak(
                "Feedback page. " +
                "No feedback is available."
            );

            return;
        }

        string speech =
            "Feedback. " +
            feedback + ".";

        if (!string.IsNullOrWhiteSpace(improvement))
        {
            speech +=
                " Improvement suggestion. " +
                improvement + ".";
        }

        Speak(speech);
    }


    // =========================================================
    // READ ONLY FEEDBACK
    // =========================================================

    public void ReadFeedbackOnly()
    {
        string feedback = "";

        if (IsPracticeMode())
        {
            if (practiceData != null)
                feedback = practiceData.feedback ?? "";
        }
        else
        {
            ParticipantData data =
                ParticipantManager.Instance != null
                    ? ParticipantManager.Instance.CurrentParticipant
                    : null;

            if (data != null)
                feedback = data.feedback ?? "";
        }

        if (string.IsNullOrWhiteSpace(feedback))
        {
            Speak(
                "No feedback is available."
            );

            return;
        }

        Speak(
            "Feedback. " +
            feedback
        );
    }


    // =========================================================
    // READ IMPROVEMENT
    // =========================================================

    public void ReadImprovement()
    {
        string improvement = "";

        if (IsPracticeMode())
        {
            if (practiceData != null)
            {
                improvement =
                    practiceData.improvementSuggestion ?? "";
            }
        }
        else
        {
            ParticipantData data =
                ParticipantManager.Instance != null
                    ? ParticipantManager.Instance.CurrentParticipant
                    : null;

            if (data != null)
            {
                improvement =
                    data.improvementSuggestion ?? "";
            }
        }

        if (string.IsNullOrWhiteSpace(improvement))
        {
            Speak(
                "No improvement suggestions are available."
            );

            return;
        }

        Speak(
            "Suggested improvement. " +
            improvement
        );
    }


    // =========================================================
    // READ SCORE FEEDBACK + IMPROVEMENT
    // =========================================================

    public void ReadCompleteEvaluation()
    {
        ReadScore();

        // Give the score speech time before feedback.
        Invoke(
            nameof(ReadFeedback),
            3.0f
        );

        Invoke(
            nameof(ReadImprovement),
            7.0f
        );
    }


    // =========================================================
    // READ REVISION ATTEMPTS
    // =========================================================

    public void ReadRevisionAttempts()
    {
        int remaining =
            MAX_REVISION_COUNT -
            CurrentRevisionCount;

        Speak(
            "You have used " +
            CurrentRevisionCount +
            " of " +
            MAX_REVISION_COUNT +
            " revision attempts. " +
            remaining +
            " attempts remaining."
        );
    }

    private void ClearStoredImageData()
    {
        if (ParticipantManager.Instance == null)
            return;

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
            return;

        data.originalImageUrl = "";
        data.posterImageUrl = "";
        data.revisedImageUrl = "";

        Debug.Log(
            "DesignManager: Cleared Firestore image URL fields. Storage path preserved."
        );
    }
}