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
    [SerializeField] private TMP_Text revisionText;
    [SerializeField] private TMP_Text finalExplanationScoreText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text suggestionText;
    private string scoreSpeechText = "";

    [Header("Loading Status")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingMessage;

    private Coroutine loadingVoiceCoroutine;

    [Header("Panel Page")]
    public GameObject mainMenuPanel;
    public GameObject promptPanel;
    public GameObject outputPanel;
    public GameObject descriptionPanel;
    public GameObject revisionPanel;
    public GameObject finalExplanationPanel;
    public GameObject scorePanel;
    public GameObject leaderboardPanel;


    [Header("Review Page")]
    [SerializeField] public GameObject scorePosterReviewPanel;
    [SerializeField] private RawImage scoreReviewRawImage;
    [SerializeField] public GameObject revisionPosterReviewPanel;
    [SerializeField] private RawImage revisionReviewRawImage;
    [SerializeField] private GameObject originalPreviewPanel;
    [SerializeField] private RawImage originalPreviewRawImage;

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
        string prompt = promptInput.text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            statusText.text = "Please enter a poster prompt.";
            return;
        }

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

            statusText.text =
                "API Error: " + request.error;

            AndroidTTS.Speak(
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

            AndroidTTS.Speak(
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
            // Revised Poster
            if (revisionPosterRawImage != null)
            {
                revisionPosterRawImage.texture = texture;
                revisedPosterTexture = texture;
            }
        }

        posterRawImage.SetNativeSize();

        HideLoading();

        if (!isRevision)
        {
            AndroidTTS.Speak(
                "Poster generated successfully. Opening poster description page."
            );
        }
        else
        {
            AndroidTTS.Speak( 
                "Revised poster generated successfully."
            );
        }

        if (!isRevision && !isLoadingSavedData)
        {
            promptPanel.SetActive(false);
            outputPanel.SetActive(true);

            StartCoroutine(DescribeGeneratedImage());
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
            statusText.text =
                "Describe API Error";

            yield break;
        }

        DescribeImageResponse response =
            JsonUtility.FromJson<DescribeImageResponse>(
                request.downloadHandler.text);

        if (!response.success)
            yield break;

        lastDescription =
            response.description.detailedDescription;

        ParticipantData data = ParticipantManager.Instance.CurrentParticipant;

        data.posterDescription = lastDescription;
        data.lastPage = "Description";

        ParticipantManager.Instance.Save();

        detailsText.text =
            lastDescription;

       

        if (!isRevisionMode)
        {
            outputPanel.SetActive(false);
            descriptionPanel.SetActive(true);
        }

        ReadDescription();
    }

    public void ReplayDescription()
    {
        ReadDescription();
    }

    private void ReadDescription()
    {
        if (string.IsNullOrEmpty(lastDescription))
            return;

        AndroidTTS.Speak(
        lastDescription
        );
    }

    public void OpenOriginalPoster()
    {
        revisionPanel.SetActive(false);

        originalPreviewPanel.SetActive(true);

        originalPreviewRawImage.texture =
            originalPosterTexture;

        AndroidTTS.Speak(
            "Opening original poster preview."
        );
    }

    public void BackToRevision()
    {
        originalPreviewPanel.SetActive(false);

        revisionPanel.SetActive(true);

        AndroidTTS.Speak(
            "Returning to revision page."
        );
    }

    public void GenerateRevisionPoster()
    {
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

        AndroidTTS.Speak(
            "Maximum revisions reached. Please provide your final explanation."
        );
    }
    private string BuildRevisionPrompt()
    {
        return
            "Original Prompt: " +
            promptInput.text +

            ". Current Poster Description: " +
            lastDescription +

            ". User Revision Request: " +
            revisionPromptInput.text +

            ". Improve the existing poster while keeping the same theme, purpose, accessibility requirements, and overall message. Apply only the requested changes.";
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

        AndroidTTS.Speak(
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

            statusText.text =
                "Score API Error: " + request.error;

            AndroidTTS.Speak(
                "Evaluation failed. Please try again."
            );

            Debug.LogError(
                request.downloadHandler.text);

            yield break;
        }

        HideLoading();

        AndroidTTS.Speak(
            "Evaluation completed successfully. Opening score page."
        );



        yield return new WaitForSeconds(3f);

        ScoreResponse response =
            JsonUtility.FromJson<ScoreResponse>(
                request.downloadHandler.text);

        DisplayScore(response);

        finalExplanationPanel.SetActive(false);

        scorePanel.SetActive(true);

        yield return new WaitForSeconds(1f);

        ReadScore();


    }
    private void DisplayScore(ScoreResponse response)
    {
        promptQualityText.text =
            response.score.promptQuality + "/20";

        posterMessageText.text =
            response.score.posterMessage + "/20";

        designOutputText.text =
            response.score.designQuality + "/20";

        accessibilityText.text =
            response.score.accessibilityUnderstanding + "/20";

        revisionText.text =
            response.score.revisionProcess + "/10";

        finalExplanationScoreText.text =
            response.score.finalExplanation + "/10";

        totalScoreText.text =
            response.score.total + "/100";

        feedbackText.text =
            response.score.feedback;

        suggestionText.text =
            response.score.improvementSuggestion;

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

        ParticipantManager.Instance.CurrentParticipant.finalExplanationScore = response.score.finalExplanation;

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

         + "Revision process: "
         + response.score.revisionProcess
         + " out of ten. "

         + "Final explanation: "
         + response.score.finalExplanation
         + " out of ten. "

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
        if (string.IsNullOrEmpty(scoreSpeechText))
            return;

        AndroidTTS.Speak(
            scoreSpeechText
        );
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
            AndroidTTS.Speak(message);

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

        AndroidTTS.Speak(
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

        AndroidTTS.Speak(
            "Opening final revised poster."
        );
    }

    public void CloseScorePosterReview()
    {
        scorePosterReviewPanel.SetActive(false);

        scorePanel.SetActive(true);

        AndroidTTS.Speak(
            "Returning to score page."
        );
    }

    public void CloseRevisionPosterReview()
    {
        revisionPosterReviewPanel.SetActive(false);

        revisionPanel.SetActive(true);

        AndroidTTS.Speak(
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

        //---------------------------------------
        // Description
        //---------------------------------------

        detailsText.text = data.posterDescription;

        //---------------------------------------
        // Score
        //---------------------------------------

        promptQualityText.text = data.promptQuality + "/20";
        posterMessageText.text = data.posterMessage + "/20";
        designOutputText.text = data.designQuality + "/20";
        accessibilityText.text = data.accessibilityUnderstanding + "/20";
        revisionText.text = data.revisionProcessScore + "/10";
        finalExplanationScoreText.text = data.finalExplanationScore + "/10";
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


        Debug.Log("Participant restored successfully.");
    }

    public void PrepareForNewChallenge()
    {
        currentRevisionCount = 0;

        latestImageUrl = "";
        latestPromptUsed = "";
        latestStoragePath = "";

        lastDescription = "";
        scoreSpeechText = "";

        originalPosterTexture = null;
        revisedPosterTexture = null;

        promptInput.text = "";
        revisionPromptInput.text = "";
        finalExplanationInput.text = "";

        detailsText.text = "";

        posterRawImage.texture = null;
        descriptionRawImage.texture = null;
        revisionPosterRawImage.texture = null;
        scoreReviewRawImage.texture = null;

        Debug.Log("New Challenge Ready");
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
        revisionText.text = "";
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


    }

    public void CloseAllPanels()
    {
        mainMenuPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        scorePosterReviewPanel.SetActive(false);
        originalPreviewPanel.SetActive(false);
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

