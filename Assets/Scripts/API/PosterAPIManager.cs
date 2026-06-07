using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class PosterAPIManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField promptInput;

    public TMP_Text statusText;

    public RawImage posterImage;

    private string backendUrl =
    "https://assistive-design-backend-506363853940.asia-southeast1.run.app";


    public void GeneratePoster()
    {
        StartCoroutine(
            GeneratePosterCoroutine(promptInput.text)
        );
    }

    IEnumerator GeneratePosterCoroutine(string prompt)
    {
        statusText.text = "Generating...";

        PosterImageRequest requestData =
            new PosterImageRequest();

        requestData.userPrompt = prompt;

        string json =
            JsonUtility.ToJson(requestData);

        UnityWebRequest request =
            new UnityWebRequest(
                backendUrl +
                "/generate-full-poster-image",
                "POST"
            );

        byte[] body =
            Encoding.UTF8.GetBytes(json);

        request.uploadHandler =
            new UploadHandlerRaw(body);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text =
                request.error;

            yield break;
        }

        PosterImageResponse response =
            JsonUtility.FromJson<PosterImageResponse>(
                request.downloadHandler.text
            );

        StartCoroutine(
            DownloadImage(response.imageUrl)
        );
    }

    IEnumerator DownloadImage(string imageUrl)
    {
        UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(imageUrl);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text =
                request.error;

            yield break;
        }

        Texture2D texture =
            DownloadHandlerTexture.GetContent(request);

        posterImage.texture = texture;

        statusText.text =
            "Poster Generated!";
    }


}



[Serializable]
public class PosterImageRequest
{
    public string userPrompt;
}

[Serializable]
public class PosterImageResponse
{
    public bool success;
    public string imageUrl;
    public string storagePath;
    public string mimeType;
    public string promptUsed;
}

[Serializable]
public class DescribeRequest
{
    public string imageUrl;
}

[Serializable]
public class ImageDescription
{
    public string shortDescription;
    public string detailedDescription;
}

[Serializable]
public class DescribeResponse
{
    public bool success;
    public ImageDescription description;
}