using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AssistiveDesignAPI : MonoBehaviour
{
    [SerializeField] private TextToSpeechManager ttsManager;

    [Header("Backend")]
    [SerializeField]
    private string backendUrl = "https://assistive-design-backend-506363853940.asia-southeast1.run.app";
    [Header("UI Input")]
    [SerializeField] private TMP_InputField promptInput;
    [Header("Status UI")]
    [SerializeField] private TMP_Text resultText;
    [Header("Poster UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text callToActionText;
    [SerializeField] private TMP_Text audioDescriptionText;
    [SerializeField] private TMP_Text themeText;
    [SerializeField] private TMP_Text mainVisualText;
    [SerializeField] private TMP_Text layoutText;
    private PosterData currentPoster;

    public void GeneratePoster()
    {
        string prompt = promptInput.text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            resultText.text = "Please enter a poster prompt.";
            return;
        }
        StartCoroutine(PostGeneratePoster(prompt));
    }

    private IEnumerator PostGeneratePoster(string userPrompt)
    {
        string url = backendUrl + "/generate-poster-json";
        GeneratePosterRequest requestData = new GeneratePosterRequest
        {
            userPrompt = userPrompt
        };
        string jsonBody = JsonUtility.ToJson(requestData);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        resultText.text = "Generating poster...";

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            resultText.text = "API Error: " + request.error + "\n" + request.downloadHandler.text;
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }
        string responseText = request.downloadHandler.text;
        Debug.Log("Backend Response: " + responseText);

        BackendResponse backendResponse = JsonUtility.FromJson<BackendResponse>(responseText);

        if (backendResponse == null || !backendResponse.success)
        {
            resultText.text = "Backend returned error.";
            yield break;
        }

        currentPoster = JsonUtility.FromJson<PosterData>(backendResponse.rawResult);

        if (currentPoster == null)
        {
            resultText.text = "Failed to parse poster JSON.";
            Debug.LogError("rawResult: " + backendResponse.rawResult);
            yield break;
        }

        ApplyPosterToUI(currentPoster);
        resultText.text = "Poster generated successfully.";
    }
    private void ApplyPosterToUI(PosterData poster)
    {
        titleText.text = poster.title;
        subtitleText.text = poster.subtitle;
        callToActionText.text = poster.callToAction;
        audioDescriptionText.text = poster.audioDescription;
        if (themeText != null) themeText.text = "Theme: " + poster.theme;
        if (mainVisualText != null) mainVisualText.text = "Visual: " + poster.mainVisual;
        if (layoutText != null) layoutText.text = "Layout: " + poster.layout;
    }
    public string GetAudioDescription()
    {
        if (currentPoster == null) return "No poster generated yet.";
        return currentPoster.audioDescription;
    }
}
[Serializable]
public class GeneratePosterRequest
{
    public string userPrompt;
}
[Serializable]
public class BackendResponse
{
    public bool success;
    public string rawResult;
}
[Serializable]
public class PosterData
{
    public string title;
    public string subtitle;
    public string theme;
    public string backgroundColor;
    public string mainVisual;
    public string layout;
    public string callToAction;
    public string audioDescription;
}







