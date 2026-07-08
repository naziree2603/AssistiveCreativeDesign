using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudinaryManager : MonoBehaviour
{
    public static CloudinaryManager Instance;

    [Header("Cloudinary")]

    [SerializeField]
    private string cloudName = "noajv4ek";

    [SerializeField]
    private string uploadPreset = "unity_posters";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UploadImage(Texture2D texture, Action<string> onComplete)
    {
        StartCoroutine(UploadCoroutine(texture, onComplete));
    }

    private IEnumerator UploadCoroutine(Texture2D texture, Action<string> onComplete)
    {
        byte[] imageBytes =
            texture.EncodeToPNG();

        WWWForm form =
            new WWWForm();

        form.AddField(
            "upload_preset",
            uploadPreset);

        string fileName =
            FirebaseAuthManager.Instance.GetUID()
            + "_"
            + DateTime.Now.Ticks
            + ".png";

        form.AddBinaryData(
            "file",
            imageBytes,
            fileName,
            "image/png");

        string url =
            $"https://api.cloudinary.com/v1_1/{cloudName}/image/upload";

        UnityWebRequest request =
            UnityWebRequest.Post(url, form);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);

            onComplete?.Invoke("");

            yield break;
        }

        Debug.Log(request.downloadHandler.text);

        CloudinaryResponse response =
            JsonUtility.FromJson<CloudinaryResponse>(
                request.downloadHandler.text);

        onComplete?.Invoke(response.secure_url);
    }
}
