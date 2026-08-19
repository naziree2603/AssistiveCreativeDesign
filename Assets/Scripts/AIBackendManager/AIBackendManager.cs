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

    public bool IsProcessing
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

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // GENERATE POSTER
    // =========================================================
    //
    // POST:
    //
    // /generate-full-poster-image
    //
    // Used for:
    //
    // 1. Original poster
    // 2. Revision 1
    // 3. Revision 2
    // 4. Revision 3
    //
    // =========================================================

    public async Task<PosterResult>
        GeneratePoster(
            string userPrompt)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                userPrompt
            )
        )
        {
            SetError(
                "Poster prompt is empty."
            );

            return null;
        }


        string url =
            backendUrl +
            "/generate-full-poster-image";


        PosterImageRequest requestData =
            new PosterImageRequest
            {
                userPrompt =
                    userPrompt.Trim()
            };


        string json =
            JsonUtility.ToJson(
                requestData
            );


        try
        {
            IsProcessing = true;


            UnityWebRequest request =
                CreatePostRequest(
                    url,
                    json
                );


            await SendRequestAsync(
                request
            );


            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                SetError(
                    "Poster generation failed: " +
                    request.error
                );


                Debug.LogError(
                    "AIBackendManager GeneratePoster: " +
                    request.error
                );


                return null;
            }


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


            FullPosterImageResponse response =
                JsonUtility.FromJson<
                    FullPosterImageResponse
                >(
                    responseText
                );


            if (
                response == null
            )
            {
                SetError(
                    "Unable to read poster API response."
                );


                return null;
            }


            if (
                !response.success
            )
            {
                SetError(
                    "Poster generation was unsuccessful."
                );


                return null;
            }


            if (
                string.IsNullOrWhiteSpace(
                    response.imageUrl
                )
            )
            {
                SetError(
                    "Poster API did not return an image URL."
                );


                return null;
            }


            PosterResult result =
                new PosterResult
                {
                    success =
                        true,

                    imageUrl =
                        response.imageUrl,

                    storagePath =
                        response.storagePath,

                    mimeType =
                        response.mimeType,

                    promptUsed =
                        response.promptUsed
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
    //
    // POST:
    //
    // /describe-generated-image
    //
    // =========================================================

    public async Task<DescriptionResult>
        DescribePoster(
            string imageUrl)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                imageUrl
            )
        )
        {
            SetError(
                "Image URL is empty."
            );


            return null;
        }


        string url =
            backendUrl +
            "/describe-generated-image";


        DescribeImageRequest requestData =
            new DescribeImageRequest
            {
                imageUrl =
                    imageUrl
            };


        string json =
            JsonUtility.ToJson(
                requestData
            );


        try
        {
            IsProcessing = true;


            UnityWebRequest request =
                CreatePostRequest(
                    url,
                    json
                );


            await SendRequestAsync(
                request
            );


            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                SetError(
                    "Poster description failed: " +
                    request.error
                );


                Debug.LogError(
                    "AIBackendManager DescribePoster: " +
                    request.error
                );


                return null;
            }


            string responseText =
                request.downloadHandler.text;


            if (
                string.IsNullOrWhiteSpace(
                    responseText
                )
            )
            {
                SetError(
                    "Description API returned an empty response."
                );


                return null;
            }


            DescribeImageResponse response =
                JsonUtility.FromJson<
                    DescribeImageResponse
                >(
                    responseText
                );


            if (
                response == null
            )
            {
                SetError(
                    "Unable to read description API response."
                );


                return null;
            }


            if (
                !response.success
            )
            {
                SetError(
                    "Poster description was unsuccessful."
                );


                return null;
            }


            string description =
                "";


            if (
                response.description != null
            )
            {
                description =
                    response.description
                        .detailedDescription;


                if (
                    string.IsNullOrWhiteSpace(
                        description
                    )
                )
                {
                    description =
                        response.description
                            .shortDescription;
                }
            }


            if (
                string.IsNullOrWhiteSpace(
                    description
                )
            )
            {
                SetError(
                    "No description was returned."
                );


                return null;
            }


            DescriptionResult result =
                new DescriptionResult
                {
                    success =
                        true,

                    description =
                        description,

                    shortDescription =
                        response.description != null
                            ? response.description
                                .shortDescription
                            : "",

                    detailedDescription =
                        response.description != null
                            ? response.description
                                .detailedDescription
                            : "",

                    detectedText =
                        response.description != null
                            ? response.description
                                .detectedText
                            : "",

                    mainObjects =
                        response.description != null
                            ? response.description
                                .mainObjects
                            : "",

                    colors =
                        response.description != null
                            ? response.description
                                .colors
                            : "",

                    layout =
                        response.description != null
                            ? response.description
                                .layout
                            : "",

                    message =
                        response.description != null
                            ? response.description
                                .message
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
    // DOWNLOAD IMAGE
    // =========================================================
    //
    // Downloads generated poster from the URL returned
    // by the backend.
    //
    // =========================================================

    public async Task<Texture2D>
        DownloadImage(
            string imageUrl)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                imageUrl
            )
        )
        {
            SetError(
                "Image URL is empty."
            );


            return null;
        }


        try
        {
            IsProcessing = true;


            using UnityWebRequest request =
                UnityWebRequestTexture
                    .GetTexture(
                        imageUrl
                    );


            await SendRequestAsync(
                request
            );


            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                SetError(
                    "Image download failed: " +
                    request.error
                );


                Debug.LogError(
                    "AIBackendManager DownloadImage: " +
                    request.error
                );


                return null;
            }


            Texture2D texture =
                DownloadHandlerTexture
                    .GetContent(
                        request
                    );


            if (
                texture == null
            )
            {
                SetError(
                    "Downloaded image is empty."
                );


                return null;
            }


            Debug.Log(
                "AIBackendManager: Image downloaded successfully."
            );


            return texture;
        }
        catch (Exception exception)
        {
            SetError(
                "Image download exception: " +
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
    // SCORE POSTER
    // =========================================================
    //
    // POST:
    //
    // /score-full-poster
    //
    // Scoring:
    //
    // Prompt Quality              20
    // Poster Message & Content    20
    // Design Quality              20
    // Accessibility Understanding 20
    // Final Design Justification  20
    //
    // Final Design Justification:
    //
    // Revision Process + Final Explanation
    //
    // =========================================================

    public async Task<ScoreResult>
        ScorePoster(
            ScoreRequestData requestData)
    {
        LastError = "";


        if (
            requestData == null
        )
        {
            SetError(
                "Score request is null."
            );


            return null;
        }


        // -----------------------------------------------------
        // VALIDATE PROMPT
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                requestData.userPrompt
            )
        )
        {
            SetError(
                "Original design prompt is empty."
            );


            return null;
        }


        // -----------------------------------------------------
        // VALIDATE IMAGE
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                requestData.imageUrl
            )
        )
        {
            SetError(
                "Final poster image is missing."
            );


            return null;
        }


        // -----------------------------------------------------
        // VALIDATE FINAL EXPLANATION
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                requestData.finalExplanation
            )
        )
        {
            SetError(
                "Final explanation is empty."
            );


            return null;
        }


        // -----------------------------------------------------
        // URL
        // -----------------------------------------------------

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
            JsonUtility.ToJson(
                apiRequest
            );


        try
        {
            IsProcessing = true;


            UnityWebRequest request =
                CreatePostRequest(
                    url,
                    json
                );


            await SendRequestAsync(
                request
            );


            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                SetError(
                    "Score API failed: " +
                    request.error
                );


                Debug.LogError(
                    "AIBackendManager ScorePoster: " +
                    request.error
                );


                Debug.LogError(
                    request.downloadHandler != null
                        ? request.downloadHandler.text
                        : ""
                );


                return null;
            }


            string responseText =
                request.downloadHandler.text;


            if (
                string.IsNullOrWhiteSpace(
                    responseText
                )
            )
            {
                SetError(
                    "Score API returned an empty response."
                );


                return null;
            }


            ScoreApiResponse response =
                JsonUtility.FromJson<
                    ScoreApiResponse
                >(
                    responseText
                );


            if (
                response == null
            )
            {
                SetError(
                    "Unable to read score API response."
                );


                return null;
            }


            if (
                !response.success
            )
            {
                SetError(
                    "AI evaluation was unsuccessful."
                );


                return null;
            }


            if (
                response.score == null
            )
            {
                SetError(
                    "Score breakdown was not returned."
                );


                return null;
            }


            // -------------------------------------------------
            // NORMALIZE SCORE
            // -------------------------------------------------

            NormalizeScore(
                response.score
            );


            ScoreResult result =
                new ScoreResult
                {
                    success =
                        true,

                    score =
                        response.score
                };


            Debug.Log(
                "AIBackendManager: Evaluation completed."
            );


            Debug.Log(
                "Total Score = " +
                response.score.total +
                "/100"
            );


            return result;
        }
        catch (Exception exception)
        {
            SetError(
                "Score exception: " +
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
    // NORMALIZE SCORE
    // =========================================================
    //
    // The backend may return:
    //
    // revisionProcess = 12
    // finalExplanation = 8
    //
    // We combine them:
    //
    // 12 + 8 = 20
    //
    // But the backend response still keeps the two values
    // internally.
    //
    // DesignManager displays only:
    //
    // Final Design Justification
    // 20 / 20
    //
    // =========================================================

    private void NormalizeScore(
        ScoreBreakdown score)
    {
        if (score == null)
        {
            return;
        }


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


        // -----------------------------------------------------
        // FINAL DESIGN JUSTIFICATION
        // -----------------------------------------------------

        int finalDesignJustification =
            score.revisionProcess +
            score.finalExplanation;


        finalDesignJustification =
            Mathf.Clamp(
                finalDesignJustification,
                0,
                20
            );


        // -----------------------------------------------------
        // TOTAL
        // -----------------------------------------------------

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
    // CREATE POST REQUEST
    // =========================================================

    private UnityWebRequest
        CreatePostRequest(
            string url,
            string json)
    {
        UnityWebRequest request =
            new UnityWebRequest(
                url,
                UnityWebRequest.kHttpVerbPOST
            );


        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(
                json
            );


        request.uploadHandler =
            new UploadHandlerRaw(
                bodyRaw
            );


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


        return request;
    }


    // =========================================================
    // ASYNC UNITY WEB REQUEST
    // =========================================================
    //
    // UnityWebRequest does not directly behave like a normal
    // Task in every Unity version.
    //
    // This method bridges Unity coroutine execution into
    // async/await.
    //
    // =========================================================

    private Task
        SendRequestAsync(
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


    private IEnumerator
        SendRequestCoroutine(
            UnityWebRequest request,
            TaskCompletionSource<bool>
                completionSource)
    {
        yield return request.SendWebRequest();


        completionSource.TrySetResult(
            true
        );


        request.Dispose();
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(
        string message)
    {
        LastError =
            message;


        Debug.LogError(
            "AIBackendManager: " +
            message
        );
    }


    // =========================================================
    // GET BACKEND URL
    // =========================================================

    public string GetBackendUrl()
    {
        return backendUrl;
    }


    // =========================================================
    // SET BACKEND URL
    // =========================================================

    public void SetBackendUrl(
        string url)
    {
        if (
            string.IsNullOrWhiteSpace(
                url
            )
        )
        {
            return;
        }


        backendUrl =
            url.Trim().TrimEnd('/');
    }


    // =========================================================
    // DATA - POSTER REQUEST
    // =========================================================

    [Serializable]
    public class PosterImageRequest
    {
        public string userPrompt;
    }


    // =========================================================
    // DATA - POSTER RESPONSE
    // =========================================================

    [Serializable]
    public class FullPosterImageResponse
    {
        public bool success;

        public string imageUrl;

        public string storagePath;

        public string mimeType;

        public string promptUsed;
    }


    // =========================================================
    // DATA - POSTER RESULT
    // =========================================================

    [Serializable]
    public class PosterResult
    {
        public bool success;

        public string imageUrl;

        public string storagePath;

        public string mimeType;

        public string promptUsed;
    }


    // =========================================================
    // DATA - DESCRIPTION REQUEST
    // =========================================================

    [Serializable]
    public class DescribeImageRequest
    {
        public string imageUrl;
    }


    // =========================================================
    // DATA - DESCRIPTION RESPONSE
    // =========================================================

    [Serializable]
    public class DescribeImageResponse
    {
        public bool success;

        public ImageDescription description;
    }


    // =========================================================
    // DATA - IMAGE DESCRIPTION
    // =========================================================

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


    // =========================================================
    // DATA - DESCRIPTION RESULT
    // =========================================================

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


    // =========================================================
    // DATA - SCORE REQUEST FROM DESIGN MANAGER
    // =========================================================

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


    // =========================================================
    // DATA - SCORE API REQUEST
    // =========================================================

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


    // =========================================================
    // DATA - SCORE API RESPONSE
    // =========================================================

    [Serializable]
    private class ScoreApiResponse
    {
        public bool success;

        public ScoreBreakdown score;
    }


    // =========================================================
    // DATA - SCORE RESULT
    // =========================================================

    [Serializable]
    public class ScoreResult
    {
        public bool success;

        public ScoreBreakdown score;
    }


    // =========================================================
    // DATA - SCORE BREAKDOWN
    // =========================================================

    [Serializable]
    public class ScoreBreakdown
    {
        // -----------------------------------------------------
        // 20 MARKS
        // -----------------------------------------------------

        public int promptQuality;

        // -----------------------------------------------------
        // 20 MARKS
        // -----------------------------------------------------

        public int posterMessage;

        // -----------------------------------------------------
        // 20 MARKS
        // -----------------------------------------------------

        public int designQuality;

        // -----------------------------------------------------
        // 20 MARKS
        // -----------------------------------------------------

        public int accessibilityUnderstanding;

        // -----------------------------------------------------
        // FINAL 20 MARKS
        //
        // Internally:
        //
        // revisionProcess = 0 - 10
        // finalExplanation = 0 - 10
        //
        // Combined:
        //
        // 0 - 20
        // -----------------------------------------------------

        public int revisionProcess;

        public int finalExplanation;

        // -----------------------------------------------------
        // TOTAL
        // -----------------------------------------------------

        public int total;

        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        public string feedback;

        public string improvementSuggestion;
    }
}