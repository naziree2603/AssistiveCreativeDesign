using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


public class AIBackendManager : MonoBehaviour
{
    public static AIBackendManager Instance { get; private set; }

    // =========================================================
    // BACKEND
    // =========================================================

    [Header("Backend Settings")]
    [SerializeField]
    private string backendUrl =
        "https://assistive-design-backend-506363853940.asia-southeast1.run.app";

    // =========================================================
    // STATE
    // =========================================================

    public bool IsProcessing { get; private set; }

    public string LastError { get; private set; }

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
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // GENERATE POSTER
    // =========================================================

    public async Task<PosterResult> GeneratePoster(
    string userPrompt)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            SetError("Poster prompt is empty.");
            return null;
        }

        string url =
            backendUrl +
            "/generate-full-poster-image";

        PosterImageRequest requestData =
            new PosterImageRequest
            {
                userPrompt = userPrompt.Trim()
            };

        string json =
            JsonUtility.ToJson(requestData);

        try
        {
            IsProcessing = true;

            using UnityWebRequest request =
                CreatePostRequest(
                    url,
                    json
                );

            await SendRequestAsync(request);

            // =====================================================
            // CHECK BACKEND RESPONSE
            // =====================================================

            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                string errorDetails =
                    request.error;

                if (
                    request.downloadHandler != null &&
                    !string.IsNullOrWhiteSpace(
                        request.downloadHandler.text
                    )
                )
                {
                    errorDetails +=
                        " | Response: " +
                        request.downloadHandler.text;
                }

                SetError(
                    "Poster generation failed: " +
                    errorDetails
                );

                Debug.LogError(
                    "AIBackendManager GeneratePoster: " +
                    errorDetails
                );

                return null;
            }

            // =====================================================
            // READ RESPONSE
            // =====================================================

            string responseText =
                request.downloadHandler.text;

            if (
                string.IsNullOrWhiteSpace(
                    responseText
                )
            )
            {
                SetError(
                    "Poster API returned an empty response."
                );

                return null;
            }

            Debug.Log(
                "AIBackendManager: Poster API response received."
            );

            FullPosterImageResponse response =
                JsonUtility.FromJson<
                    FullPosterImageResponse
                >(
                    responseText
                );

            if (response == null)
            {
                SetError(
                    "Unable to read poster API response."
                );

                return null;
            }

            // =====================================================
            // SUCCESS
            // =====================================================

            if (!response.success)
            {
                SetError(
                    "Poster generation was unsuccessful."
                );

                return null;
            }

            // =====================================================
            // IMAGE CHECK
            // =====================================================

            if (
                string.IsNullOrWhiteSpace(
                    response.imageUrl
                )
            )
            {
                SetError(
                    "Poster API did not return an image."
                );

                return null;
            }

            // =====================================================
            // IMPORTANT
            //
            // DO NOT UPLOAD TO FIREBASE STORAGE.
            //
            // We return the image exactly as received
            // from the backend.
            // =====================================================

            Debug.Log(
                "AIBackendManager: Image received directly from backend."
            );

            Debug.Log(
                "AIBackendManager: Image format = " +
                (
                    response.imageUrl.StartsWith(
                        "data:image/",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "BASE64"
                        : "URL"
                )
            );

            // =====================================================
            // RETURN RESULT
            // =====================================================

            PosterResult result =
                new PosterResult
                {
                    success = true,

                    imageUrl =
                        response.imageUrl,

                    storagePath =
                        response.storagePath ?? "",

                    mimeType =
                        response.mimeType ?? "",

                    promptUsed =
                        response.promptUsed ?? ""
                };

            Debug.Log(
                "AIBackendManager: Poster generated successfully."
            );

            return result;
        }
        catch (Exception exception)
        {
            SetError(
                "Poster generation exception: " +
                exception.Message
            );

            Debug.LogException(
                exception
            );

            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // =========================================================
    // DESCRIBE POSTER
    // =========================================================

    public async Task<DescriptionResult> DescribePoster(
        string imageUrl)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            SetError("Image URL is empty.");
            return null;
        }

        string url =
            backendUrl +
            "/describe-generated-image";

        DescribeImageRequest requestData =
            new DescribeImageRequest
            {
                imageUrl = imageUrl
            };

        string json =
            JsonUtility.ToJson(requestData);

        try
        {
            IsProcessing = true;

            using UnityWebRequest request =
                CreatePostRequest(url, json);

            await SendRequestAsync(request);

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                SetError(
                    "Poster description failed: " +
                    request.error
                );

                if (request.downloadHandler != null)
                {
                    Debug.LogError(
                        request.downloadHandler.text
                    );
                }

                return null;
            }

            string responseText =
                request.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                SetError(
                    "Description API returned an empty response."
                );

                return null;
            }

            DescribeImageResponse response =
                JsonUtility.FromJson<DescribeImageResponse>(
                    responseText
                );

            if (response == null)
            {
                SetError(
                    "Unable to read description API response."
                );

                return null;
            }

            if (!response.success)
            {
                SetError(
                    "Poster description was unsuccessful."
                );

                return null;
            }

            string description = "";

            if (response.description != null)
            {
                description =
                    response.description.detailedDescription;

                if (string.IsNullOrWhiteSpace(description))
                {
                    description =
                        response.description.shortDescription;
                }
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                SetError(
                    "No description was returned."
                );

                return null;
            }

            DescriptionResult result =
                new DescriptionResult
                {
                    success = true,

                    description = description,

                    shortDescription =
                        response.description != null
                            ? response.description.shortDescription
                            : "",

                    detailedDescription =
                        response.description != null
                            ? response.description.detailedDescription
                            : "",

                    detectedText =
                        response.description != null
                            ? response.description.detectedText
                            : "",

                    mainObjects =
                        response.description != null
                            ? response.description.mainObjects
                            : "",

                    colors =
                        response.description != null
                            ? response.description.colors
                            : "",

                    layout =
                        response.description != null
                            ? response.description.layout
                            : "",

                    message =
                        response.description != null
                            ? response.description.message
                            : ""
                };

            Debug.Log(
                "AIBackendManager: Poster description generated."
            );

            return result;
        }
        catch (Exception exception)
        {
            SetError(
                "Description exception: " +
                exception.Message
            );

            Debug.LogException(exception);

            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // =========================================================
    // ENSURE IMAGE IS STORED
    // =========================================================



    // =========================================================
    // DOWNLOAD IMAGE
    // =========================================================

    public async Task<Texture2D> DownloadImage(
        string imageUrl)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            SetError("Image URL is empty.");
            return null;
        }

        try
        {
            IsProcessing = true;

            // -------------------------------------------------
            // BASE64
            // -------------------------------------------------

            if (imageUrl.StartsWith(
                    "data:image/",
                    StringComparison.OrdinalIgnoreCase))
            {
                int commaIndex =
                    imageUrl.IndexOf(',');

                if (commaIndex < 0)
                {
                    SetError(
                        "Invalid Base64 image data."
                    );

                    return null;
                }

                string base64Data =
                    imageUrl.Substring(
                        commaIndex + 1
                    );

                if (string.IsNullOrWhiteSpace(base64Data))
                {
                    SetError(
                        "Base64 image data is empty."
                    );

                    return null;
                }

                byte[] imageBytes =
                    Convert.FromBase64String(
                        base64Data
                    );

                Texture2D texture =
                    new Texture2D(
                        2,
                        2,
                        TextureFormat.RGBA32,
                        false
                    );

                bool loaded =
                    texture.LoadImage(imageBytes);

                if (!loaded)
                {
                    Destroy(texture);

                    SetError(
                        "Unable to decode generated poster image."
                    );

                    return null;
                }

                return texture;
            }

            // -------------------------------------------------
            // NORMAL URL
            // -------------------------------------------------

            using UnityWebRequest request =
                UnityWebRequestTexture.GetTexture(
                    imageUrl
                );

            await SendRequestAsync(request);

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                SetError(
                    "Image download failed: " +
                    request.error
                );

                return null;
            }

            Texture2D downloadedTexture =
                DownloadHandlerTexture.GetContent(
                    request
                );

            if (downloadedTexture == null)
            {
                SetError(
                    "Downloaded image is empty."
                );

                return null;
            }

            return downloadedTexture;
        }
        catch (FormatException exception)
        {
            SetError(
                "Invalid Base64 image data: " +
                exception.Message
            );

            Debug.LogException(exception);

            return null;
        }
        catch (Exception exception)
        {
            SetError(
                "Image download exception: " +
                exception.Message
            );

            Debug.LogException(exception);

            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // =========================================================
    // DOWNLOAD STORED IMAGE
    // =========================================================
    //
    // Downloads a previously generated image using the
    // storagePath returned by the backend.
    //
    // IMPORTANT:
    // This does NOT use Firebase Storage.
    // The image remains hosted by the AI backend.
    //

    public async Task<Texture2D> DownloadStoredImage(
        string storagePath)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            SetError(
                "Storage path is empty."
            );

            return null;
        }

        string path =
            storagePath.Trim();

        try
        {
            IsProcessing = true;

            // -------------------------------------------------
            // If backend already returned a full URL
            // -------------------------------------------------

            string imageUrl;

            if (
                path.StartsWith(
                    "http://",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                path.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                imageUrl = path;
            }
            else
            {
                // -------------------------------------------------
                // Convert storage path into backend image URL
                // -------------------------------------------------

                imageUrl =
                    backendUrl.TrimEnd('/') +
                    "/generated-images/" +
                    path.TrimStart('/');
            }

            Debug.Log(
                "AIBackendManager: Downloading stored image: " +
                imageUrl
            );

            using UnityWebRequest request =
                UnityWebRequestTexture.GetTexture(
                    imageUrl
                );

            await SendRequestAsync(request);

            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                SetError(
                    "Stored image download failed: " +
                    request.error
                );

                Debug.LogError(
                    "AIBackendManager: Stored image download failed. " +
                    "HTTP = " +
                    request.responseCode +
                    " | URL = " +
                    imageUrl
                );

                return null;
            }

            Texture2D texture =
                DownloadHandlerTexture.GetContent(
                    request
                );

            if (texture == null)
            {
                SetError(
                    "Stored image is empty."
                );

                return null;
            }

            return texture;
        }
        catch (Exception exception)
        {
            SetError(
                "Stored image download exception: " +
                exception.Message
            );

            Debug.LogException(
                exception
            );

            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }


    private bool IsRateLimitResponse(
    UnityWebRequest request)
    {
        if (request == null)
            return false;

        // Direct HTTP 429
        if (request.responseCode == 429)
            return true;

        // Your backend currently appears
        // to convert Vertex 429 into HTTP 500.
        if (request.responseCode == 500 &&
            request.downloadHandler != null)
        {
            string body =
                request.downloadHandler.text ?? "";

            if (body.Contains("\"code\":429") ||
                body.Contains("\"code\": 429") ||
                body.Contains("Too Many Requests") ||
                body.Contains("Resource exhausted"))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // SCORE POSTER
    // =========================================================

    public async Task<ScoreResult> ScorePoster(
        ScoreRequestData requestData)
    {
        LastError = "";

        if (requestData == null)
        {
            SetError(
                "Score request is null."
            );

            return null;
        }

        if (string.IsNullOrWhiteSpace(
                requestData.userPrompt))
        {
            SetError(
                "Original design prompt is empty."
            );

            return null;
        }

        if (string.IsNullOrWhiteSpace(
                requestData.imageUrl))
        {
            SetError(
                "Final poster image is missing."
            );

            return null;
        }

        if (string.IsNullOrWhiteSpace(
                requestData.finalExplanation))
        {
            SetError(
                "Final explanation is empty."
            );

            return null;
        }

        string url =
            backendUrl +
            "/score-full-poster";

        ScoreApiRequest apiRequest =
            new ScoreApiRequest
            {
                userPrompt =
                    requestData.userPrompt,

                imageUrl =
                    requestData.imageUrl,

                revisionPrompt =
                    requestData.revisionPrompt,

                revisionHistory =
                    requestData.revisionHistory,

                revisionCount =
                    requestData.revisionCount,

                finalExplanation =
                    requestData.finalExplanation
            };

        string json =
            JsonUtility.ToJson(apiRequest);

        try
        {
            IsProcessing = true;

            const int maxAttempts = 3;

            for (int attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                using UnityWebRequest request =
                    CreatePostRequest(
                        url,
                        json
                    );

                await SendRequestAsync(request);

                // =================================================
                // SUCCESS
                // =================================================

                if (request.result ==
                    UnityWebRequest.Result.Success)
                {
                    string responseText =
                        request.downloadHandler != null
                            ? request.downloadHandler.text
                            : "";

                    if (string.IsNullOrWhiteSpace(
                        responseText))
                    {
                        SetError(
                            "Score API returned an empty response."
                        );

                        return null;
                    }

                    ScoreApiResponse response =
                        JsonUtility.FromJson<ScoreApiResponse>(
                            responseText
                        );

                    if (response == null)
                    {
                        SetError(
                            "Unable to read score API response."
                        );

                        return null;
                    }

                    if (!response.success)
                    {
                        SetError(
                            "AI evaluation was unsuccessful."
                        );

                        return null;
                    }

                    if (response.score == null)
                    {
                        SetError(
                            "Score breakdown was not returned."
                        );

                        return null;
                    }

                    NormalizeScore(response.score);

                    return new ScoreResult
                    {
                        success = true,
                        score = response.score
                    };
                }

                // =================================================
                // ERROR
                // =================================================

                bool rateLimited =
                    IsRateLimitResponse(request);

                string responseBody =
                    request.downloadHandler != null
                        ? request.downloadHandler.text
                        : "";

                Debug.LogWarning(
                    "AIBackendManager: Score attempt " +
                    attempt +
                    "/" +
                    maxAttempts +
                    " failed. HTTP " +
                    request.responseCode +
                    " | " +
                    request.error
                );

                if (!string.IsNullOrWhiteSpace(
                    responseBody))
                {
                    Debug.LogWarning(
                        "Score API response: " +
                        responseBody
                    );
                }

                // =================================================
                // RETRY ONLY RATE LIMITS
                // =================================================

                if (rateLimited &&
                    attempt < maxAttempts)
                {
                    int delaySeconds =
                        attempt == 1 ? 3 : 6;

                    Debug.LogWarning(
                        "Vertex AI is temporarily busy. " +
                        "Retrying score request in " +
                        delaySeconds +
                        " seconds."
                    );

                    await Task.Delay(
                        delaySeconds * 1000
                    );

                    continue;
                }

                // =================================================
                // FINAL FAILURE
                // =================================================

                SetError(
                    "Score API failed: " +
                    request.error
                );

                return null;
            }

            SetError(
                "Score API failed after all retry attempts."
            );

            return null;
        }
        catch (Exception exception)
        {
            SetError(
                "Score exception: " +
                exception.Message
            );

            Debug.LogException(exception);

            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // =========================================================
    // NORMALIZE SCORE
    // =========================================================

    private void NormalizeScore(
        ScoreBreakdown score)
    {
        if (score == null)
            return;

        score.promptQuality =
            Mathf.Clamp(
                score.promptQuality,
                0,
                20
            );

        score.posterMessage =
            Mathf.Clamp(
                score.posterMessage,
                0,
                20
            );

        score.designQuality =
            Mathf.Clamp(
                score.designQuality,
                0,
                20
            );

        score.accessibilityUnderstanding =
            Mathf.Clamp(
                score.accessibilityUnderstanding,
                0,
                20
            );

        score.revisionProcess =
            Mathf.Clamp(
                score.revisionProcess,
                0,
                10
            );

        score.finalExplanation =
            Mathf.Clamp(
                score.finalExplanation,
                0,
                10
            );

        int finalDesignJustification =
            score.revisionProcess +
            score.finalExplanation;

        finalDesignJustification =
            Mathf.Clamp(
                finalDesignJustification,
                0,
                20
            );

        score.total =
            score.promptQuality +
            score.posterMessage +
            score.designQuality +
            score.accessibilityUnderstanding +
            finalDesignJustification;

        score.total =
            Mathf.Clamp(
                score.total,
                0,
                100
            );
    }

    // =========================================================
    // HTTP
    // =========================================================

    private UnityWebRequest CreatePostRequest(
        string url,
        string json)
    {
        UnityWebRequest request =
            new UnityWebRequest(
                url,
                UnityWebRequest.kHttpVerbPOST
            );

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(json);

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.SetRequestHeader(
            "Accept",
            "application/json"
        );

        // Prevent the Unity request from hanging forever.
        request.timeout = 120;

        return request;
    }

    private Task SendRequestAsync(
        UnityWebRequest request)
    {
        TaskCompletionSource<bool>
            completionSource =
            new TaskCompletionSource<bool>();

        StartCoroutine(
            SendRequestCoroutine(
                request,
                completionSource
            )
        );

        return completionSource.Task;
    }

    private IEnumerator SendRequestCoroutine(
        UnityWebRequest request,
        TaskCompletionSource<bool>
            completionSource)
    {
        yield return request.SendWebRequest();

        completionSource.TrySetResult(true);
    }

    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(string message)
    {
        LastError = message;

        Debug.LogError(
            "AIBackendManager: " +
            message
        );
    }

    // =========================================================
    // BACKEND URL
    // =========================================================

    public string GetBackendUrl()
    {
        return backendUrl;
    }

    public void SetBackendUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        backendUrl =
            url.Trim().TrimEnd('/');
    }

    // =========================================================
    // DATA
    // =========================================================

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
    public class PosterResult
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
    public class DescriptionResult
    {
        public bool success;
        public string description;
        public string shortDescription;
        public string detailedDescription;
        public string detectedText;
        public string mainObjects;
        public string colors;
        public string layout;
        public string message;
    }

    [Serializable]
    public class ScoreRequestData
    {
        public string userPrompt;
        public string imageUrl;
        public string revisionPrompt;
        public string revisionHistory;
        public int revisionCount;
        public string finalExplanation;
    }

    [Serializable]
    private class ScoreApiRequest
    {
        public string userPrompt;
        public string imageUrl;
        public string revisionPrompt;
        public string revisionHistory;
        public int revisionCount;
        public string finalExplanation;
    }


    [Serializable]
    private class ScoreApiResponse
    {
        public bool success;
        public ScoreBreakdown score;
    }

    [Serializable]
    public class ScoreResult
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