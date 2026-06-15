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

    private string lastDescription = "";

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

    //Generate Poster Image
    public void GenerateFullPosterImage()
    {
        string prompt = promptInput.text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            statusText.text = "Please enter a poster prompt.";
            return;
        }
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

        statusText.text = "Generating poster image...";
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "API Error: " + request.error + "\n" + request.downloadHandler.text;
            yield break;
        }
        FullPosterImageResponse response
            = JsonUtility.FromJson<FullPosterImageResponse>(request.downloadHandler.text);

        if (!response.success || string.IsNullOrEmpty(response.imageUrl))
        {
            statusText.text = "No poster image returned.";
            yield break;
        }

        latestImageUrl = response.imageUrl;
        latestPromptUsed = response.promptUsed;
        latestStoragePath = response.storagePath;

        yield return StartCoroutine(DownloadImage(response.imageUrl));
    }

    private IEnumerator DownloadImage(string imageUrl)
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
        posterRawImage.texture = texture;
        posterRawImage.SetNativeSize();

        statusText.text = "Poster image generated successfully.";

        StartCoroutine(DescribeGeneratedImage());

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

        UAP_AccessibilityManager.Say(
            lastDescription,
            false,
            true
        );
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
            revisionPrompt = "",
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

        statusText.text =
            "Calculating score...";

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text =
                "Score API Error: " + request.error;

            Debug.LogError(
                request.downloadHandler.text);

            yield break;
        }

        Debug.Log(
            request.downloadHandler.text);

        ScoreResponse response =
            JsonUtility.FromJson<ScoreResponse>(
                request.downloadHandler.text);

        DisplayScore(response);
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
            "Total score "
            + response.score.total
            + " out of one hundred. "
            + "Prompt quality "
            + response.score.promptQuality
            + " out of twenty. "
            + "Poster message "
            + response.score.posterMessage
            + " out of twenty. "
            + "Design quality "
            + response.score.designQuality
            + " out of twenty. "
            + "Accessibility understanding "
            + response.score.accessibilityUnderstanding
            + " out of twenty. "
            + "Feedback. "
            + response.score.feedback;

              statusText.text =
              "Score calculated successfully.";

        ReadScore();
    }

    public void ReadScore()
    {
        if (string.IsNullOrEmpty(scoreSpeechText))
            return;

        UAP_AccessibilityManager.Say(
            scoreSpeechText,
            false,
            true
        );
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

