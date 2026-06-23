using System;
using System.Collections;
using System.Security.Policy;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FullPosterImageAPI : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://assistive-design-backend-506363853940.asia-southeast1.run.app";

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

    [Header("Panel Page")]
    public GameObject promptPanel;
    public GameObject outputPanel;
    public GameObject descriptionPanel;
    public GameObject revisionPanel;
    public GameObject finalExplanationPanel;
    public GameObject scorePanel;
    

    //Generate Poster Image
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

        yield return StartCoroutine(DownloadImage(response.imageUrl));
    }

    private IEnumerator DownloadImage(string imageUrl,bool isRevision = false)
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

        if (!isRevision)
        {
            // Original Poster
            posterRawImage.texture = texture;

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
            }
        }

        posterRawImage.SetNativeSize();

        HideLoading();

        AndroidTTS.Speak(
            "Poster generated successfully. Opening poster description page."
        );

        if (!isRevision)
        {
            promptPanel.SetActive(false);
            outputPanel.SetActive(true);
        }



        if (!isRevision)
        {
            StartCoroutine(DescribeGeneratedImage());
        }

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

    public void GenerateRevisionPoster()
    {
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
            "Revision completed successfully."
        );

        FullPosterImageResponse response =
            JsonUtility.FromJson<FullPosterImageResponse>(
                request.downloadHandler.text);

        latestImageUrl =
            response.imageUrl;

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
    private void DisplayScore(
    ScoreResponse response)
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


    }

    public void ReadScore()
    {
        if (string.IsNullOrEmpty(scoreSpeechText))
            return;

        AndroidTTS.Speak(
            scoreSpeechText
        );
    }

    private void ShowLoading(string message)
    {
        loadingPanel.SetActive(true);

        loadingMessage.text = message;

        AndroidTTS.Speak(message);
    }

    private void HideLoading()
    {
        loadingPanel.SetActive(false);
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




