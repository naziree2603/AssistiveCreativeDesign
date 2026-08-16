using System;
using System.Collections;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static AccessibilityToggle;

public class FullPosterImageAPI : MonoBehaviour
{
    

    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://assistive-design-backend-506363853940.asia-southeast1.run.app";

    [Header("Participant Details")]
    [SerializeField] TMP_InputField participantNameInput;
    [SerializeField] TMP_InputField institutionInput;
    [SerializeField] TMP_Dropdown categoryTypeDropdown;
    [SerializeField] TMP_Dropdown subCategoryDropdown;


    [Header("UI")]
    [SerializeField] private TMP_InputField promptInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RawImage posterRawImage;
    public string latestImageUrl;
    public string latestPromptUsed;
    public string latestStoragePath;

    [Header("Description UI")]
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private Button replayButton;
    [SerializeField] private RawImage descriptionRawImage;

    private string lastDescription = "";

    private string revisionHistory = "";

    [Header("Revision UI")]
    [SerializeField] private TMP_InputField revisionPromptInput;
    [SerializeField] private RawImage revisionPosterRawImage;
    private bool isRevisionMode = false;



    [Header("Revision Settings")]
    [SerializeField] private int maxRevisionCount = 3;

    private int currentRevisionCount = 0;

    [Header("Score UI")]
    [SerializeField] private TMP_InputField finalExplanationInput;
    [SerializeField] private TMP_Text promptQualityText;
    [SerializeField] private TMP_Text posterMessageText;
    [SerializeField] private TMP_Text designOutputText;
    [SerializeField] private TMP_Text accessibilityText;
    [SerializeField] private TMP_Text finalExplanationScoreText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text suggestionText;
    private string scoreSpeechText = "";

    [Header("Loading Status")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingMessage;

    private Coroutine loadingVoiceCoroutine;

    private bool isProcessing = false;

    private bool isDescriptionReady = false;
    

    [Header("Panel Page")]
    public GameObject loginPanel;
    public GameObject registerPanel; 
    public GameObject mainMenuPanel;
    public GameObject instructionPanel;
    public GameObject challengePanel;
    public GameObject participantDetailsPanel;
    public GameObject promptPanel;
    public GameObject outputPanel;
    public GameObject descriptionPanel;
    public GameObject revisionPanel;
    public GameObject finalExplanationPanel;
    public GameObject scorePanel;
    public GameObject leaderboardPanel;
    public GameObject submittedPanel;


    [Header("Review Page")]
    [SerializeField] public GameObject scorePosterReviewPanel;
    [SerializeField] private RawImage scoreReviewRawImage;
    [SerializeField] public GameObject revisionPosterReviewPanel;
    [SerializeField] private RawImage revisionReviewRawImage;
    [SerializeField] private GameObject originalPreviewPanel;
    [SerializeField] private RawImage originalPreviewRawImage;

    [Header("Action Buttons")]
    [SerializeField] private Button outputNextButton;
    [SerializeField] private Button finalExplanationNextButton;
    [SerializeField] private GameObject generatePosterButton;
    [SerializeField] private GameObject generateRevisionButton;
    [SerializeField] private GameObject calculateScoreButton;
    [SerializeField] private GameObject sample1;
    [SerializeField] private GameObject sample2;
    [SerializeField] private GameObject sample3;

    public enum PosterReviewSource
    {
        Revision,
        Score
    }

    private PosterReviewSource reviewSource;

    //Generate Poster Image

    public async void StartParticipant()
    {


        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        AccountData account = FirestoreAccountManager.Instance.CurrentAccount;




        data.participantName = participantNameInput.text;
        data.institution = institutionInput.text;
        data.categoryType = categoryTypeDropdown.options[categoryTypeDropdown.value].text;
        data.subCategory = subCategoryDropdown.options[subCategoryDropdown.value].text;

        ParticipantData participant = ParticipantManager.Instance.CurrentParticipant;

        participant.challengeID = ChallengeManager.Instance.CurrentChallenge.challengeID;
        participant.challengeTitle = ChallengeManager.Instance.CurrentChallenge.title;

        if (ChallengeManager.Instance.CurrentChallenge == null)
        {
            statusText.text =
                "Please select a challenge.";

            return;
        }

        await ParticipantManager.Instance.Save();


    }

    public void GenerateFullPosterImage()
    {
        if (isProcessing)
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Please wait. Generation is still in progress.");
            return;
        }

        string prompt = promptInput.text.Trim();

        if (string.IsNullOrEmpty(prompt))
        {
            statusText.text = "Please enter a poster prompt.";
            return;
        }

        isProcessing = true;

        isRevisionMode = false;

        StartCoroutine(PostGeneratePosterImage(prompt));
    }

    private IEnumerator PostGeneratePosterImage(string userPrompt)
    {
        string url = backendUrl + "/generate-full-poster-image";
        PosterImageRequest requestData = new PosterImageRequest
        {
            userPrompt = userPrompt
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        ShowLoading(
            "Generating your poster. Please wait."
        );

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            HideLoading();

            isProcessing = false;

            statusText.text =
                "API Error: " + request.error;

            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "Poster generation failed."
            );

            yield break;
        }
        FullPosterImageResponse response
            = JsonUtility.FromJson<FullPosterImageResponse>(request.downloadHandler.text);

        if (!response.success || string.IsNullOrEmpty(response.imageUrl))
        {
            HideLoading();

            statusText.text =
                "No poster image returned.";

            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "No poster image returned."
            );

            yield break;
        }

        latestImageUrl = response.imageUrl;
        latestPromptUsed = response.promptUsed;
        latestStoragePath = response.storagePath;

        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        data.prompt = promptInput.text;
        data.promptUsed = latestPromptUsed;
        data.storagePath = latestStoragePath;
        data.lastPage = "Output";

        ParticipantManager.Instance.Save();


        yield return StartCoroutine(DownloadImage(response.imageUrl));
    }

    private Texture2D originalPosterTexture;
    private Texture2D revisedPosterTexture;



    private IEnumerator DownloadImage(string imageUrl, bool isRevision = false, bool isLoadingSavedData = false)
    {
        statusText.text = "Downloading poster image...";

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            isProcessing = false;

            statusText.text = "Image download error: " + request.error;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);

        // Upload only when generating a new poster
        if (!isLoadingSavedData)
        {
            var uploadTask =
                FirebaseStorageManager.Instance.UploadParticipantImage(
                    texture,
                    isRevision);

            yield return new WaitUntil(() => uploadTask.IsCompleted);
        }


        if (!isRevision)
        {
            // Original Poster

            posterRawImage.texture = texture;

            originalPosterTexture = texture;


            if (descriptionRawImage != null)
            {
                descriptionRawImage.texture = texture;
            }
        }
        else
        {
            if (revisionPosterRawImage != null)
            {
                revisionPosterRawImage.texture = texture;
                revisedPosterTexture = texture;
            }


        }

        posterRawImage.SetNativeSize();

        HideLoading();

        isProcessing = false;

        if (!isLoadingSavedData)
        {
            if (!isRevision)
            {
                AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                    "Poster generated successfully. Opening poster description page."
                );
            }
            else
            {
                AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                    "Revised poster generated successfully."
                );
            }
        }

        if (!isLoadingSavedData)
        {
            isDescriptionReady = false;

            outputNextButton.interactable = false;

            StartCoroutine(DescribeGeneratedImage());

            if (!isRevision)
            {
                promptPanel.SetActive(false);
                outputPanel.SetActive(true); 
            }
        }

    }

 

   

    private Texture2D LoadTexture(string path)
    {
        if (!System.IO.File.Exists(path))
            return null;

        byte[] bytes = System.IO.File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2);

        tex.LoadImage(bytes);

        return tex;
    }

    private void LoadOriginalPoster(string path)
    {
        Texture2D tex = LoadTexture(path);

        if (tex == null)
            return;

        originalPosterTexture = tex;

        posterRawImage.texture = tex;

        descriptionRawImage.texture = tex;


    }

    private void LoadRevisedPoster(string path)
    {
        Texture2D tex = LoadTexture(path);

        if (tex == null)
            return;

        revisedPosterTexture = tex;

        revisionPosterRawImage.texture = tex;

        scoreReviewRawImage.texture = tex;
    }

    private IEnumerator DescribeGeneratedImage()
    {

        string url =
            backendUrl + "/describe-generated-image";

        DescribeImageRequest requestData =
            new DescribeImageRequest();

        requestData.imageUrl =
            latestImageUrl;

        string jsonBody =
            JsonUtility.ToJson(requestData);

        using UnityWebRequest request =
            new UnityWebRequest(url, "POST");

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HideLoading();

            statusText.text = "Describe API Error";

            isDescriptionReady = true;

            outputPanel.SetActive(true);

            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "Unable to analyze the poster."
            );

            yield break;
        }

        DescribeImageResponse response =
            JsonUtility.FromJson<DescribeImageResponse>(
                request.downloadHandler.text);

        if (!response.success)
            yield break;

        lastDescription =
            response.description.detailedDescription;

        if (isRevisionMode)
        {
            revisionHistory +=
                "\n\nRevision " + currentRevisionCount +
                "\nRequest: " + revisionPromptInput.text +
                "\nResult: " + lastDescription;
        }

        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        data.posterDescription = lastDescription;
        data.lastPage = "Description";

        ParticipantManager.Instance.Save();

        detailsText.text =
            lastDescription;

       

        if (!isRevisionMode)
        {

        }

        // Only auto-read if Accessibility is ON
        if (UAP_AccessibilityManager.IsEnabled())
        {
            ReadDescription();
        }

        isDescriptionReady = true;

        HideLoading();

        outputNextButton.interactable = true;

        // Open Description automatically
        CloseAllPanels();

        if (isRevisionMode)
        {
            revisionPanel.SetActive(true);
        }
        else
        {
            descriptionPanel.SetActive(true);
        }

        // Revision flow is finished
        isRevisionMode = false;

        // Read only after panel is visible
        if (UAP_AccessibilityManager.IsEnabled())
        {
            ReadDescription();
        }
    }

    public void OpenDescription()
    {
        // History mode
        if (ParticipantManager.Instance.CurrentParticipant != null &&
            ParticipantManager.Instance.CurrentParticipant.isCompleted)
        {
            CloseAllPanels();
            descriptionPanel.SetActive(true);
            return;
        }

        if (!isDescriptionReady)
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Please wait. Description is still loading.");
            return;
        }

        CloseAllPanels();
        descriptionPanel.SetActive(true);
    }

    public void ReplayDescription()
    {
        if (!AccessibilityToggle.AccessibilityEnabled)
            return;

        ReadDescription();
    }

    private void ReadDescription()
    {
        if (string.IsNullOrEmpty(lastDescription))
            return;

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
        lastDescription
        );
    }




    public void OpenOriginalPoster()
    {
        revisionPanel.SetActive(false);

        originalPreviewPanel.SetActive(true);

        originalPreviewRawImage.texture =
            originalPosterTexture;

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Opening original poster preview."
        );
    }

    public void BackToRevision()
    {
        originalPreviewPanel.SetActive(false);

        revisionPanel.SetActive(true);

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Returning to revision page."
        );
    }

    public void GenerateRevisionPoster()
    {
        if (string.IsNullOrWhiteSpace(revisionPromptInput.text))
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "Please enter a revision prompt."
            );

            return;
        }


        if (currentRevisionCount >= maxRevisionCount)
        {
            ShowLoading(
                "Maximum revision limit reached. Opening final explanation page."
            );

            StartCoroutine(OpenFinalExplanationAfterDelay());

            return;
        }

        if (string.IsNullOrEmpty(revisionPromptInput.text))
        {
            statusText.text =
                "Please enter revision prompt.";

            return;
        }

         revisionHistory += "\nRevision " + (currentRevisionCount + 1) + ": " + revisionPromptInput.text;

        string finalRevisionPrompt =
            BuildRevisionPrompt();

        isRevisionMode = true;

        StartCoroutine(
            GenerateRevisionImage(
                finalRevisionPrompt
            ));
    }

    private IEnumerator OpenFinalExplanationAfterDelay()
    {
        yield return new WaitForSeconds(6f);

        HideLoading();

        revisionPanel.SetActive(false);

        finalExplanationPanel.SetActive(true);

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Maximum revisions reached. Please provide your final explanation."
        );
    }
    private string BuildRevisionPrompt()
    {
        return
            "Original Prompt:\n"
            + promptInput.text

            + "\n\nCurrent Poster Description:\n"
            + lastDescription

            + "\n\nAccepted Revision History:\n"
            + revisionHistory

            + "\n\nLatest Revision Request:\n"
            + revisionPromptInput.text

            + "\n\nIMPORTANT RULES:\n"
            + "- Preserve all previously accepted changes.\n"
            + "- Do not remove existing objects.\n"
            + "- Only apply the newest requested modification.\n"
            + "- Keep the same poster purpose and accessibility design.\n"
            + "- Generate a new improved version.";
    }


    private IEnumerator GenerateRevisionImage(  string revisionPrompt)
    {
        string url =
            backendUrl +
            "/generate-full-poster-image";

        PosterImageRequest requestData =
            new PosterImageRequest
            {
                userPrompt =
                    revisionPrompt
            };

        string jsonBody =
            JsonUtility.ToJson(
                requestData);

        using UnityWebRequest request =
            new UnityWebRequest(
                url,
                "POST");

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(
                jsonBody);

        request.uploadHandler =
            new UploadHandlerRaw(
                bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        ShowLoading(
            "Applying your poster revisions. Please wait."
        );

        yield return request.SendWebRequest();

        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            HideLoading(); // Hide popup if error

            statusText.text =
                request.error;

            yield break;
        }

        // SUCCESS
        HideLoading();

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Revision "
            + currentRevisionCount
            + " completed. "
            + (maxRevisionCount - currentRevisionCount)
            + " revisions remaining."
        );

        FullPosterImageResponse response =
            JsonUtility.FromJson<FullPosterImageResponse>(
                request.downloadHandler.text);

        latestImageUrl =
            response.imageUrl;

        currentRevisionCount++;

        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        data.revisionPrompt = revisionPromptInput.text;
        data.revisionCount = currentRevisionCount;
        data.revisedImageUrl = latestImageUrl;
        data.lastPage = "Revision";

        ParticipantManager.Instance.Save();

        statusText.text =
            "Revision "
            + currentRevisionCount
            + " of "
            + maxRevisionCount;

        yield return StartCoroutine(DownloadImage(response.imageUrl,true));



    }

    private IEnumerator DownloadRevisionImage(
    string imageUrl)
    {
        using UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(
                imageUrl);

        yield return request.SendWebRequest();

        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            yield break;
        }

        Texture2D texture =
            DownloadHandlerTexture.GetContent(
                request);

        revisionPosterRawImage.texture =
            texture;

        statusText.text =
            "Revision generated.";
    }


    //AI Scoring
    public void CalculateAIScore()
    {
        if (string.IsNullOrWhiteSpace(revisionPromptInput.text))
        {
            revisionPromptInput.text =
                "No changes required. The participant accepted the original poster because it already met the design objectives.";
        }

        if (string.IsNullOrWhiteSpace(finalExplanationInput.text))
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "Please enter your final explanation."
            );

            return;
        }


        if (string.IsNullOrEmpty(latestImageUrl))
        {
            statusText.text = "Generate poster first.";
            return;
        }

        StartCoroutine(PostScoreRequest());
    }

    private IEnumerator PostScoreRequest()
    {
        string url = backendUrl + "/score-full-poster";

        ScoreRequest requestData = new ScoreRequest
        {
            userPrompt = promptInput.text,
            imageUrl = latestImageUrl,
            revisionPrompt =
                revisionPromptInput.text,
            finalExplanation = finalExplanationInput.text
        };

        string jsonBody =
            JsonUtility.ToJson(requestData);

        using UnityWebRequest request =
            new UnityWebRequest(url, "POST");

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        ShowLoading(
            "Evaluating your submission. Please wait."
        );
        
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HideLoading();

            isProcessing = false;

            statusText.text =
                "Score API Error: " + request.error;

            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
                "Evaluation failed. Please try again."
            );

            Debug.LogError(
                request.downloadHandler.text);

            yield break;
        }

        HideLoading();

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Evaluation completed successfully. Opening score page."
        );



        yield return new WaitForSeconds(3f);

        ScoreResponse response =
            JsonUtility.FromJson<ScoreResponse>(
                request.downloadHandler.text);

        DisplayScore(response);

        CloseAllPanels();

        scorePanel.SetActive(true);

        yield return new WaitForSeconds(1f);

        if (UAP_AccessibilityManager.IsEnabled())
        {
            ReadScore();
        }


    }
    private void DisplayScore(ScoreResponse response)
    {
        promptQualityText.text = response.score.promptQuality + "/20";

        posterMessageText.text = response.score.posterMessage + "/20";

        designOutputText.text = response.score.designQuality + "/20";

        accessibilityText.text = response.score.accessibilityUnderstanding + "/20";

        int finalSubmissionScore = response.score.revisionProcess + response.score.finalExplanation;

        finalExplanationScoreText.text = finalSubmissionScore + "/20";

        totalScoreText.text = response.score.total + "/100";

        feedbackText.text = response.score.feedback;

        suggestionText.text = response.score.improvementSuggestion;

        ParticipantManager.Instance.CurrentParticipant.isCompleted = true;

        ParticipantManager.Instance.CurrentParticipant.completedDate =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");



        ParticipantManager.Instance.CurrentParticipant.finalExplanation = finalExplanationInput.text;

        ParticipantManager.Instance.CurrentParticipant.score = response.score.total;

        ParticipantManager.Instance.CurrentParticipant.promptQuality = response.score.promptQuality;

        ParticipantManager.Instance.CurrentParticipant.posterMessage = response.score.posterMessage;

        ParticipantManager.Instance.CurrentParticipant.designQuality = response.score.designQuality;

        ParticipantManager.Instance.CurrentParticipant.accessibilityUnderstanding = response.score.accessibilityUnderstanding;

        ParticipantManager.Instance.CurrentParticipant.revisionProcessScore = response.score.revisionProcess;

        ParticipantManager.Instance.CurrentParticipant.finalExplanationScore = response.score.revisionProcess + response.score.finalExplanation;

        ParticipantManager.Instance.CurrentParticipant.feedback = response.score.feedback;

        ParticipantManager.Instance.CurrentParticipant.improvementSuggestion = response.score.improvementSuggestion;


        ParticipantManager.Instance.CurrentParticipant.lastPage = "Score";


        

        scoreSpeechText =
         "Evaluation completed. "

         + "Total score: "
         + response.score.total
         + " out of one hundred. "

         + "Prompt quality: "
         + response.score.promptQuality
         + " out of twenty. "

         + "Poster message and content: "
         + response.score.posterMessage
         + " out of twenty. "

         + "Design output quality: " 
         + response.score.designQuality
         + " out of twenty. "

         + "Accessibility understanding: "
         + response.score.accessibilityUnderstanding
         + " out of twenty. "

         + "Final design justification: "
         + finalSubmissionScore
         + " out of twenty. "

         + "Feedback: "
         + response.score.feedback

         + ". Improvement suggestion: "
         + response.score.improvementSuggestion;


        StartCoroutine(SaveScoreAndRefreshLeaderboard());


    }

    private IEnumerator SaveScoreAndRefreshLeaderboard()
    {
        Debug.Log("===== BEFORE SAVE =====");

        Debug.Log("EntryID = " +
            ParticipantManager.Instance.CurrentParticipant.entryID);

        Debug.Log("Completed = " +
            ParticipantManager.Instance.CurrentParticipant.isCompleted);

        Debug.Log("CompletedDate = " +
            ParticipantManager.Instance.CurrentParticipant.completedDate);

        Debug.Log("Score = " +
            ParticipantManager.Instance.CurrentParticipant.score);

        var saveTask =
            ParticipantManager.Instance.Save();

        yield return new WaitUntil(() => saveTask.IsCompleted);

        MainMenuManager menu = FindFirstObjectByType<MainMenuManager>();

        if (menu != null)
        {
            var refreshTask = menu.RefreshButtons();

            yield return new WaitUntil(() => refreshTask.IsCompleted);
        }

        LeaderboardManager leaderboard = FindFirstObjectByType<LeaderboardManager>();


        if (leaderboard != null)
        {
            leaderboard.LoadLeaderboard();
        }

        Debug.Log("Submission completed.");
    }

    public void ReadScore()
    {
        if (!AccessibilityToggle.AccessibilityEnabled)
            return;

        if (string.IsNullOrEmpty(scoreSpeechText))
            return;

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(scoreSpeechText);
    }

    public void ShowLoading(string message)
    {
        loadingPanel.SetActive(true);

        loadingMessage.text = message;

        if (loadingVoiceCoroutine != null)
            StopCoroutine(loadingVoiceCoroutine);

        loadingVoiceCoroutine =
            StartCoroutine(
                RepeatLoadingVoice(message)
            );
    }

    private IEnumerator RepeatLoadingVoice(string message)
    {
        while (loadingPanel.activeSelf)
        {
            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(message);

            yield return new WaitForSeconds(6f);
        }
    }

    public void HideLoading()
    {
        loadingPanel.SetActive(false);

        if (loadingVoiceCoroutine != null)
        {
            StopCoroutine(loadingVoiceCoroutine);
            loadingVoiceCoroutine = null;
        }
    }

    public void OpenRevisePosterReview()
    {
        revisionPanel.SetActive(false);

        revisionPosterReviewPanel.SetActive(true);

        if (revisedPosterTexture != null)
        {
            revisionReviewRawImage.texture =
                revisedPosterTexture;
        }
        else
        {
            revisionReviewRawImage.texture =
                originalPosterTexture;
        }

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Opening revised poster."
        );
    }

    public void OpenFinalPosterReviewInScore()
    {
        scorePanel.SetActive(false);

        scorePosterReviewPanel.SetActive(true);

        if (revisedPosterTexture != null)
        {
            scoreReviewRawImage.texture =
                revisedPosterTexture;
        }
        else
        {
            scoreReviewRawImage.texture =
                originalPosterTexture;
        }

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Opening final revised poster."
        );
    }

    public void CloseScorePosterReview()
    {
        scorePosterReviewPanel.SetActive(false);

        scorePanel.SetActive(true);

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Returning to score page."
        );
    }

    public void CloseRevisionPosterReview()
    {
        revisionPosterReviewPanel.SetActive(false);

        revisionPanel.SetActive(true);

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
            "Returning to revision page."
        );
    }

    public void LoadParticipant()
    {
        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        if (data == null)
        {
            Debug.Log("No participant data loaded.");
            return;
        }

        //---------------------------------------
        // Participant
        //---------------------------------------

        participantNameInput.text = data.participantName;
        institutionInput.text = data.institution;


        // Restore Category Type
        for (int i = 0; i < categoryTypeDropdown.options.Count; i++)
        {
            if (categoryTypeDropdown.options[i].text == data.categoryType)
            {
                categoryTypeDropdown.value = i;
                break;
            }
        }

        // Restore Sub Category
        for (int i = 0; i < subCategoryDropdown.options.Count; i++)
        {
            if (subCategoryDropdown.options[i].text == data.subCategory)
            {
                subCategoryDropdown.value = i;
                break;
            }
        }

        //---------------------------------------
        // Prompt
        //---------------------------------------

        promptInput.text = data.prompt;

        //---------------------------------------
        // Revision
        //---------------------------------------

        revisionPromptInput.text = data.revisionPrompt;

     

        //---------------------------------------
        // Final Explanation
        //---------------------------------------

        finalExplanationInput.text = data.finalExplanation;

        if (data.isCompleted)
        {
            finalExplanationNextButton.interactable = true;
        }


        //---------------------------------------
        // Description
        //---------------------------------------

        detailsText.text = data.posterDescription;

        lastDescription = data.posterDescription;

        isDescriptionReady =
            !string.IsNullOrEmpty(data.posterDescription);

        outputNextButton.interactable =
            isDescriptionReady;

        //---------------------------------------
        // Score
        //---------------------------------------

        promptQualityText.text = data.promptQuality + "/20";
        posterMessageText.text = data.posterMessage + "/20";
        designOutputText.text = data.designQuality + "/20";
        accessibilityText.text = data.accessibilityUnderstanding + "/20";
        finalExplanationScoreText.text = data.finalExplanationScore + "/20";
        totalScoreText.text = data.score + "/100";

        feedbackText.text = data.feedback;
        suggestionText.text = data.improvementSuggestion;

        //---------------------------------------
        // Internal Variables
        //---------------------------------------

        if (!string.IsNullOrEmpty(data.revisedImageUrl))
        {
            latestImageUrl = data.revisedImageUrl;
        }
        else
        {
            latestImageUrl = data.originalImageUrl;
        }

        currentRevisionCount = data.revisionCount;

        //---------------------------------------
        // Load Local Images
        //---------------------------------------
        if (!string.IsNullOrEmpty(data.originalImageUrl))
        {
            StartCoroutine(DownloadImage(data.originalImageUrl, false, true));
        }

        if (!string.IsNullOrEmpty(data.revisedImageUrl))
        {
            StartCoroutine(DownloadImage(data.revisedImageUrl, true, true));
        }


        if (ParticipantManager.Instance.CurrentParticipant.isCompleted)
        {
            SetSubmissionReadOnly(true);
        }
        else
        {
            SetSubmissionReadOnly(false);
        }

    }



    public void PrepareForNewChallenge()
    {
        // State
        isProcessing = false;
        isRevisionMode = false;
        isDescriptionReady = false;


        currentRevisionCount = 0;

        latestImageUrl = "";
        latestPromptUsed = "";
        latestStoragePath = "";

        lastDescription = "";
        revisionHistory = "";
        scoreSpeechText = "";

        // Text
        promptInput.text = "";
        revisionPromptInput.text = "";
        finalExplanationInput.text = "";
        detailsText.text = "";

        promptQualityText.text = "";
        posterMessageText.text = "";
        designOutputText.text = "";
        accessibilityText.text = "";
        finalExplanationScoreText.text = "";
        totalScoreText.text = "";
        feedbackText.text = "";
        suggestionText.text = "";

        // Images
        posterRawImage.texture = null;
        descriptionRawImage.texture = null;
        revisionPosterRawImage.texture = null;
        scoreReviewRawImage.texture = null;

        originalPosterTexture = null;
        revisedPosterTexture = null;

        // Restore UI
        HideLoading();

        promptInput.interactable = true;
        revisionPromptInput.interactable = true;
        finalExplanationInput.interactable = true;

        outputNextButton.interactable = true;

        generatePosterButton.SetActive(true);
        generateRevisionButton.SetActive(true);
        calculateScoreButton.SetActive(true);

        sample1.SetActive(true);
        sample2.SetActive(true);
        sample3.SetActive(true);

        outputNextButton.interactable = true;



        finalExplanationNextButton.interactable = false;

        Debug.Log("PrepareForNewChallenge()");
    }


    public void ResetSystem()
    {


        participantNameInput.text = "";
        institutionInput.text = "";
        categoryTypeDropdown.value = 0;
        subCategoryDropdown.value = 0;


        currentRevisionCount = 0;

        latestImageUrl = "";
        latestPromptUsed = "";
        latestStoragePath = "";
        lastDescription = "";
        scoreSpeechText = "";

        promptInput.text = "";
        revisionPromptInput.text = "";
        finalExplanationInput.text = "";

        detailsText.text = "";

        promptQualityText.text = "";
        posterMessageText.text = "";
        designOutputText.text = "";
        accessibilityText.text = "";
        finalExplanationScoreText.text = "";
        totalScoreText.text = "";

        feedbackText.text = "";
        suggestionText.text = "";

        posterRawImage.texture = null;
        descriptionRawImage.texture = null;
        revisionPosterRawImage.texture = null;
        scoreReviewRawImage.texture = null;
        originalPosterTexture = null;
        revisedPosterTexture = null;

        isDescriptionReady = false;
        isRevisionMode = false;
        isProcessing = false;

        revisionHistory = "";

        outputNextButton.interactable = true;


        finalExplanationNextButton.interactable = false;



        SetSubmissionReadOnly(false);

        HideLoading();


    }



    public void CloseAllPanels()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        instructionPanel.SetActive(false);
        challengePanel.SetActive(false);
        participantDetailsPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        scorePosterReviewPanel.SetActive(false);
        originalPreviewPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        submittedPanel.SetActive(false);
    }

    public void SetSubmissionReadOnly(bool isReadOnly)
    {
        // Input Fields
        promptInput.interactable = !isReadOnly;
        revisionPromptInput.interactable = !isReadOnly;
        finalExplanationInput.interactable = !isReadOnly;

        // Hide buttons
        generatePosterButton.SetActive(!isReadOnly);
        generateRevisionButton.SetActive(!isReadOnly);
        calculateScoreButton.SetActive(!isReadOnly);
        sample1.SetActive(!isReadOnly);
        sample2.SetActive(!isReadOnly);
        sample3.SetActive(!isReadOnly);
    }




}



[Serializable]
public class PosterImageRequest
{
    public string userPrompt;
}

[Serializable] 
public class FullPosterImageResponse 
{ 
    public bool success; 
    public string imageUrl; 
    public string storagePath; 
    public string mimeType; 
    public string promptUsed; 
}


[Serializable]
public class DescribeImageRequest
{
    public string imageUrl;
}
    
[Serializable]
public class DescribeImageResponse
{
    public bool success;
    public ImageDescription description;
}

[Serializable]
public class ImageDescription
{
    public string shortDescription;
    public string detailedDescription;
    public string detectedText;
    public string mainObjects;
    public string colors;
    public string layout;
    public string message;
}

[Serializable]
public class RevisionPosterRequest
{
    public string imageUrl;
    public string revisionPrompt;
}

[Serializable]
public class ScoreRequest
{
    public string userPrompt;
    public string imageUrl;
    public string revisionPrompt;
    public string finalExplanation;
}

[Serializable]
public class ScoreResponse
{
    public bool success;

    public ScoreBreakdown score;
}

[Serializable]
public class ScoreBreakdown
{
    public int promptQuality;
    public int posterMessage;
    public int designQuality;
    public int accessibilityUnderstanding;
    public int revisionProcess;
    public int finalExplanation;
    public int total;
    public string feedback;
    public string improvementSuggestion;
}

