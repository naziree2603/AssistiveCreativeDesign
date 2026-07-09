using System;
using System.Threading.Tasks;
using Firebase.Storage;
using UnityEngine;

public class FirebaseStorageManager : MonoBehaviour
{
    public static FirebaseStorageManager Instance;

    FirebaseStorage storage;

    StorageReference storageReference;

    void Awake()
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

        storage = FirebaseStorage.DefaultInstance;

        storageReference = storage.RootReference;
    }


    public async System.Threading.Tasks.Task<string> UploadImage(Texture2D texture, bool isRevision)
    {
        try
        {
            byte[] bytes = texture.EncodeToPNG();

            string documentID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

            string fileName;

            if (isRevision)
            {
                fileName =
                    "revision_" +
                    System.DateTime.Now.Ticks +
                    ".png";
            }
            else
            {
                fileName = "original.png";
            }

            StorageReference imageRef =
                storageReference
                .Child("users")
                .Child(documentID)
                .Child(fileName);

            await imageRef.PutBytesAsync(bytes);

            string downloadUrl =
                (await imageRef.GetDownloadUrlAsync()).ToString();

            Debug.Log(downloadUrl);

            return downloadUrl;
        }
        catch (Exception e)
        {
            Debug.LogError(e);

            return "";
        }
    }

    public async System.Threading.Tasks.Task<string> UploadParticipantImage(Texture2D texture, bool isRevision)
    {
        string url = await UploadImage(texture, isRevision);

        if (string.IsNullOrEmpty(url))
            return "";

        if (!isRevision)
        {
            ParticipantManager.Instance.CurrentParticipant.originalImageUrl = url;
        }
        else
        {
            ParticipantManager.Instance.CurrentParticipant.revisedImageUrl = url;
        }

        ParticipantManager.Instance.Save();

        return url;
    }
}