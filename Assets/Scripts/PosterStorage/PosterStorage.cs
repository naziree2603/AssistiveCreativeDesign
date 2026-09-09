using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public static class PosterStorage
{
    // =========================================================
    // FIRESTORE SETTINGS
    // =========================================================

    private const string POSTER_COLLECTION =
        "posterImages";

    private const string CHUNKS_COLLECTION =
        "chunks";


    // =========================================================
    // CHUNK SIZE
    // =========================================================
    //
    // 400,000 raw bytes ≈ 533 KB Base64.
    //
    // This keeps every Firestore chunk safely below
    // the 1 MiB document limit.
    //
    // =========================================================

    private const int CHUNK_SIZE =
        400000;


    // =========================================================
    // LOCAL STORAGE
    // =========================================================

    private const string LOCAL_FOLDER_NAME =
        "IIAD_Posters";


    // =========================================================
    // SAVE RESULT
    // =========================================================

    public sealed class SaveResult
    {
        public bool localSaved;

        public bool cloudSaved;
    }


    // =========================================================
    // SAVE POSTER
    // =========================================================

    public static async Task<SaveResult> SaveAsync(
        string submissionID,
        string variant,
        Texture2D texture)
    {
        SaveResult result =
            new SaveResult
            {
                localSaved = false,
                cloudSaved = false
            };


        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            submissionID))
        {
            Debug.LogWarning(
                "PosterStorage: Submission ID is empty."
            );

            return result;
        }


        if (string.IsNullOrWhiteSpace(
            variant))
        {
            Debug.LogWarning(
                "PosterStorage: Variant is empty."
            );

            return result;
        }


        if (texture == null)
        {
            Debug.LogWarning(
                "PosterStorage: Texture is NULL."
            );

            return result;
        }


        // -----------------------------------------------------
        // CONVERT TEXTURE TO PNG
        // -----------------------------------------------------

        byte[] pngBytes;

        try
        {
            pngBytes =
                texture.EncodeToPNG();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Failed to encode poster: " +
                exception.Message
            );

            return result;
        }


        if (pngBytes == null ||
            pngBytes.Length == 0)
        {
            Debug.LogWarning(
                "PosterStorage: PNG data is empty."
            );

            return result;
        }


        Debug.Log(
            "PosterStorage: Saving " +
            variant +
            " poster. Size = " +
            pngBytes.Length +
            " bytes."
        );


        // -----------------------------------------------------
        // SAVE LOCAL
        // -----------------------------------------------------

        result.localSaved =
            SaveLocal(
                submissionID,
                variant,
                pngBytes
            );


        // -----------------------------------------------------
        // SAVE CLOUD
        // -----------------------------------------------------

        try
        {
            result.cloudSaved =
                await SaveToFirestore(
                    submissionID,
                    variant,
                    pngBytes,
                    texture.width,
                    texture.height
                );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Firestore save failed: " +
                exception.Message
            );

            result.cloudSaved = false;
        }


        Debug.Log(
            "PosterStorage: Save completed. " +
            "Local = " +
            result.localSaved +
            " | Cloud = " +
            result.cloudSaved
        );


        return result;
    }


    // =========================================================
    // SAVE LOCAL
    // =========================================================

    private static bool SaveLocal(
        string submissionID,
        string variant,
        byte[] pngBytes)
    {
        try
        {
            string folder =
                Path.Combine(
                    Application.persistentDataPath,
                    LOCAL_FOLDER_NAME
                );


            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(
                    folder
                );
            }


            string safeSubmissionID =
                SanitizeFileName(
                    submissionID
                );


            string safeVariant =
                SanitizeFileName(
                    variant
                );


            string filePath =
                Path.Combine(
                    folder,
                    safeSubmissionID +
                    "_" +
                    safeVariant +
                    ".png"
                );


            File.WriteAllBytes(
                filePath,
                pngBytes
            );


            bool exists =
                File.Exists(
                    filePath
                );


            if (exists)
            {
                Debug.Log(
                    "PosterStorage: Local poster saved: " +
                    filePath
                );
            }


            return exists;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Local save failed: " +
                exception.Message
            );

            return false;
        }
    }


    // =========================================================
    // SAVE FIRESTORE
    // =========================================================

    private static async Task<bool> SaveToFirestore(
        string submissionID,
        string variant,
        byte[] pngBytes,
        int width,
        int height)
    {
        // -----------------------------------------------------
        // FIREBASE MANAGER
        // -----------------------------------------------------

        if (FirebaseManager.Instance == null)
        {
            Debug.LogWarning(
                "PosterStorage: FirebaseManager is NULL."
            );

            return false;
        }


        // -----------------------------------------------------
        // WAIT FOR FIREBASE
        // -----------------------------------------------------

        if (!await FirebaseManager.Instance
            .WaitUntilReady())
        {
            Debug.LogWarning(
                "PosterStorage: Firebase is not ready."
            );

            return false;
        }


        // -----------------------------------------------------
        // ACCOUNT ID
        // -----------------------------------------------------

        string accountID =
            GetCurrentAccountID();


        if (string.IsNullOrWhiteSpace(
            accountID))
        {
            Debug.LogWarning(
                "PosterStorage: Account ID is missing."
            );

            return false;
        }


        // -----------------------------------------------------
        // CHALLENGE ID
        // -----------------------------------------------------

        string challengeID =
            GetCurrentChallengeID();


        // -----------------------------------------------------
        // CHUNK COUNT
        // -----------------------------------------------------

        int chunkCount =
            Mathf.CeilToInt(
                (float)pngBytes.Length /
                CHUNK_SIZE
            );


        if (chunkCount <= 0)
        {
            return false;
        }


        // -----------------------------------------------------
        // FIRESTORE
        // -----------------------------------------------------

        FirebaseFirestore firestore =
            FirebaseFirestore.DefaultInstance;


        // -----------------------------------------------------
        // CREATE PARENT DOCUMENT ID
        // -----------------------------------------------------

        string parentDocumentID =
            BuildParentDocumentID(
                submissionID,
                variant
            );


        DocumentReference parentDocument =
            firestore
                .Collection(
                    POSTER_COLLECTION
                )
                .Document(
                    parentDocumentID
                );


        // -----------------------------------------------------
        // DELETE OLD CHUNKS
        // -----------------------------------------------------
        //
        // Important when replacing a poster.
        //
        // Example:
        //
        // Old poster = 5 chunks
        // New poster = 3 chunks
        //
        // Without deleting the old chunks:
        //
        // chunk 0003
        // chunk 0004
        //
        // would remain.
        //
        // -----------------------------------------------------

        await DeleteExistingChunks(
            parentDocument
        );


        // -----------------------------------------------------
        // PARENT DOCUMENT
        // -----------------------------------------------------

        Dictionary<string, object>
            parentData =
            new Dictionary<string, object>
            {
                {
                    "posterImage",
                    true
                },

                {
                    "accountID",
                    accountID
                },

                {
                    "submissionID",
                    submissionID
                },

                {
                    "challengeID",
                    challengeID
                },

                {
                    "variant",
                    variant
                },

                {
                    "chunkCount",
                    chunkCount
                },

                {
                    "totalByteCount",
                    pngBytes.Length
                },

                {
                    "mimeType",
                    "image/png"
                },

                {
                    "width",
                    width
                },

                {
                    "height",
                    height
                },

                {
                    "updatedAt",
                    Timestamp.GetCurrentTimestamp()
                }
            };


        await parentDocument.SetAsync(
            parentData
        );


        Debug.Log(
            "PosterStorage: Parent document created: " +
            POSTER_COLLECTION +
            "/" +
            parentDocumentID
        );


        // -----------------------------------------------------
        // SAVE CHUNKS
        // -----------------------------------------------------

        for (
            int index = 0;
            index < chunkCount;
            index++)
        {
            int offset =
                index *
                CHUNK_SIZE;


            int remaining =
                pngBytes.Length -
                offset;


            int currentSize =
                Mathf.Min(
                    CHUNK_SIZE,
                    remaining
                );


            byte[] chunk =
                new byte[currentSize];


            Buffer.BlockCopy(
                pngBytes,
                offset,
                chunk,
                0,
                currentSize
            );


            string base64 =
                Convert.ToBase64String(
                    chunk
                );


            string chunkDocumentID =
                index.ToString(
                    "D4"
                );


            DocumentReference chunkDocument =
                parentDocument
                    .Collection(
                        CHUNKS_COLLECTION
                    )
                    .Document(
                        chunkDocumentID
                    );


            Dictionary<string, object>
                chunkData =
                new Dictionary<string, object>
                {
                    {
                        "index",
                        index
                    },

                    {
                        "byteCount",
                        currentSize
                    },

                    {
                        "chunkCount",
                        chunkCount
                    },

                    {
                        "data",
                        base64
                    }
                };


            await chunkDocument.SetAsync(
                chunkData
            );


            Debug.Log(
                "PosterStorage: Saved " +
                variant +
                " chunk " +
                (index + 1) +
                "/" +
                chunkCount
            );
        }


        Debug.Log(
            "PosterStorage: Firestore poster saved successfully."
        );


        return true;
    }


    // =========================================================
    // DELETE EXISTING CHUNKS
    // =========================================================

    private static async Task DeleteExistingChunks(
        DocumentReference parentDocument)
    {
        try
        {
            QuerySnapshot snapshot =
                await parentDocument
                    .Collection(
                        CHUNKS_COLLECTION
                    )
                    .GetSnapshotAsync();


            if (snapshot == null)
            {
                return;
            }


            foreach (
                DocumentSnapshot document
                in snapshot.Documents)
            {
                if (document == null ||
                    !document.Exists)
                {
                    continue;
                }


                await document.Reference
                    .DeleteAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Failed to delete old chunks: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // LOAD POSTER
    // =========================================================

    public static async Task<Texture2D> LoadAsync(
        string submissionID,
        string variant)
    {
        if (string.IsNullOrWhiteSpace(
            submissionID))
        {
            Debug.LogWarning(
                "PosterStorage: Submission ID is empty."
            );

            return null;
        }


        if (string.IsNullOrWhiteSpace(
            variant))
        {
            Debug.LogWarning(
                "PosterStorage: Variant is empty."
            );

            return null;
        }


        // -----------------------------------------------------
        // LOCAL FIRST
        // -----------------------------------------------------

        Texture2D localTexture =
            LoadLocal(
                submissionID,
                variant
            );


        if (localTexture != null)
        {
            Debug.Log(
                "PosterStorage: Poster loaded from local storage. " +
                "Variant = " +
                variant
            );

            return localTexture;
        }


        // -----------------------------------------------------
        // FIRESTORE
        // -----------------------------------------------------

        try
        {
            Texture2D cloudTexture =
                await LoadFromFirestore(
                    submissionID,
                    variant
                );


            if (cloudTexture != null)
            {
                Debug.Log(
                    "PosterStorage: Poster loaded from Firestore. " +
                    "Variant = " +
                    variant
                );


                // -------------------------------------------------
                // CACHE CLOUD POSTER LOCALLY
                // -------------------------------------------------

                try
                {
                    byte[] pngBytes =
                        cloudTexture.EncodeToPNG();


                    if (pngBytes != null &&
                        pngBytes.Length > 0)
                    {
                        SaveLocal(
                            submissionID,
                            variant,
                            pngBytes
                        );
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "PosterStorage: Failed to cache poster locally: " +
                        exception.Message
                    );
                }


                return cloudTexture;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Firestore load failed: " +
                exception.Message
            );
        }


        Debug.LogWarning(
            "PosterStorage: Poster not found. " +
            "Submission = " +
            submissionID +
            " | Variant = " +
            variant
        );


        return null;
    }


    // =========================================================
    // LOAD LOCAL
    // =========================================================

    private static Texture2D LoadLocal(
        string submissionID,
        string variant)
    {
        try
        {
            string folder =
                Path.Combine(
                    Application.persistentDataPath,
                    LOCAL_FOLDER_NAME
                );


            string safeSubmissionID =
                SanitizeFileName(
                    submissionID
                );


            string safeVariant =
                SanitizeFileName(
                    variant
                );


            string filePath =
                Path.Combine(
                    folder,
                    safeSubmissionID +
                    "_" +
                    safeVariant +
                    ".png"
                );


            if (!File.Exists(
                filePath))
            {
                return null;
            }


            byte[] pngBytes =
                File.ReadAllBytes(
                    filePath
                );


            if (pngBytes == null ||
                pngBytes.Length == 0)
            {
                return null;
            }


            Texture2D texture =
                new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );


            bool loaded =
                texture.LoadImage(
                    pngBytes
                );


            if (!loaded)
            {
                UnityEngine.Object.Destroy(
                    texture
                );

                return null;
            }


            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Local load failed: " +
                exception.Message
            );

            return null;
        }
    }


    // =========================================================
    // LOAD FIRESTORE
    // =========================================================

    private static async Task<Texture2D>
        LoadFromFirestore(
            string submissionID,
            string variant)
    {
        if (FirebaseManager.Instance == null)
        {
            Debug.LogWarning(
                "PosterStorage: FirebaseManager is NULL."
            );

            return null;
        }


        if (!await FirebaseManager.Instance
            .WaitUntilReady())
        {
            Debug.LogWarning(
                "PosterStorage: Firebase is not ready."
            );

            return null;
        }


        FirebaseFirestore firestore =
            FirebaseFirestore.DefaultInstance;


        // -----------------------------------------------------
        // PARENT DOCUMENT
        // -----------------------------------------------------

        string parentDocumentID =
            BuildParentDocumentID(
                submissionID,
                variant
            );


        DocumentReference parentDocument =
            firestore
                .Collection(
                    POSTER_COLLECTION
                )
                .Document(
                    parentDocumentID
                );


        DocumentSnapshot parentSnapshot =
            await parentDocument
                .GetSnapshotAsync();


        if (parentSnapshot == null ||
            !parentSnapshot.Exists)
        {
            Debug.LogWarning(
                "PosterStorage: Poster parent document not found: " +
                parentDocumentID
            );

            return null;
        }


        // -----------------------------------------------------
        // CHUNK COUNT
        // -----------------------------------------------------

        int chunkCount =
            GetIntField(
                parentSnapshot,
                "chunkCount"
            );


        if (chunkCount <= 0)
        {
            Debug.LogWarning(
                "PosterStorage: Invalid chunk count."
            );

            return null;
        }


        // -----------------------------------------------------
        // LOAD CHUNKS
        // -----------------------------------------------------

        List<byte[]> chunks =
            new List<byte[]>();


        int totalBytes = 0;


        for (
            int index = 0;
            index < chunkCount;
            index++)
        {
            string chunkDocumentID =
                index.ToString(
                    "D4"
                );


            DocumentReference chunkDocument =
                parentDocument
                    .Collection(
                        CHUNKS_COLLECTION
                    )
                    .Document(
                        chunkDocumentID
                    );


            DocumentSnapshot chunkSnapshot =
                await chunkDocument
                    .GetSnapshotAsync();


            if (
                chunkSnapshot == null ||
                !chunkSnapshot.Exists
            )
            {
                Debug.LogWarning(
                    "PosterStorage: Missing chunk " +
                    index +
                    "/" +
                    chunkCount
                );

                return null;
            }


            // -------------------------------------------------
            // GET DATA
            // -------------------------------------------------

            string base64 =
                GetStringField(
                    chunkSnapshot,
                    "data"
                );


            if (string.IsNullOrWhiteSpace(
                base64))
            {
                Debug.LogWarning(
                    "PosterStorage: Chunk " +
                    index +
                    " has no data."
                );

                return null;
            }


            byte[] chunkBytes;


            try
            {
                chunkBytes =
                    Convert.FromBase64String(
                        base64
                    );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "PosterStorage: Invalid Base64 in chunk " +
                    index +
                    ": " +
                    exception.Message
                );

                return null;
            }


            chunks.Add(
                chunkBytes
            );


            totalBytes +=
                chunkBytes.Length;


            Debug.Log(
                "PosterStorage: Loaded " +
                variant +
                " chunk " +
                (index + 1) +
                "/" +
                chunkCount
            );
        }


        // -----------------------------------------------------
        // COMBINE CHUNKS
        // -----------------------------------------------------

        byte[] completeBytes =
            new byte[totalBytes];


        int position = 0;


        foreach (
            byte[] chunk
            in chunks)
        {
            Buffer.BlockCopy(
                chunk,
                0,
                completeBytes,
                position,
                chunk.Length
            );


            position +=
                chunk.Length;
        }


        // -----------------------------------------------------
        // CREATE TEXTURE
        // -----------------------------------------------------

        Texture2D texture =
            new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );


        bool loaded =
            texture.LoadImage(
                completeBytes
            );


        if (!loaded)
        {
            UnityEngine.Object.Destroy(
                texture
            );


            Debug.LogWarning(
                "PosterStorage: Failed to decode poster."
            );


            return null;
        }


        return texture;
    }


    // =========================================================
    // BUILD PARENT DOCUMENT ID
    // =========================================================

    private static string BuildParentDocumentID(
        string submissionID,
        string variant)
    {
        return
            SanitizeID(
                submissionID
            ) +
            "_" +
            SanitizeID(
                variant
            );
    }


    // =========================================================
    // GET CURRENT ACCOUNT ID
    // =========================================================

    private static string GetCurrentAccountID()
    {
        try
        {
            if (AccountManager.Instance == null)
            {
                Debug.LogWarning(
                    "PosterStorage: AccountManager is NULL."
                );

                return "";
            }


            if (
                AccountManager.Instance.CurrentAccount == null
            )
            {
                Debug.LogWarning(
                    "PosterStorage: CurrentAccount is NULL."
                );

                return "";
            }


            string accountID =
                AccountManager.Instance
                    .CurrentAccount
                    .accountId;


            if (string.IsNullOrWhiteSpace(
                accountID))
            {
                Debug.LogWarning(
                    "PosterStorage: Account accountId is empty."
                );

                return "";
            }


            return accountID.Trim();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Failed to get account ID: " +
                exception.Message
            );

            return "";
        }
    }


    // =========================================================
    // GET CURRENT CHALLENGE ID
    // =========================================================

    private static string GetCurrentChallengeID()
    {
        try
        {
            if (ParticipantManager.Instance == null)
            {
                return "";
            }


            if (
                ParticipantManager.Instance
                    .CurrentParticipant == null
            )
            {
                return "";
            }


            string challengeID =
                ParticipantManager.Instance
                    .CurrentParticipant
                    .challengeID;


            if (string.IsNullOrWhiteSpace(
                challengeID))
            {
                return "";
            }


            return challengeID.Trim();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PosterStorage: Failed to get challenge ID: " +
                exception.Message
            );

            return "";
        }
    }


    // =========================================================
    // GET INT FIELD
    // =========================================================

    private static int GetIntField(
        DocumentSnapshot document,
        string fieldName)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(
                fieldName
            )
        )
        {
            return 0;
        }


        try
        {
            return document.GetValue<int>(
                fieldName
            );
        }
        catch
        {
            try
            {
                long value =
                    document.GetValue<long>(
                        fieldName
                    );


                return (int)value;
            }
            catch
            {
                return 0;
            }
        }
    }


    // =========================================================
    // GET STRING FIELD
    // =========================================================

    private static string GetStringField(
        DocumentSnapshot document,
        string fieldName)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(
                fieldName
            )
        )
        {
            return "";
        }


        try
        {
            object value =
                document.GetValue<object>(
                    fieldName
                );


            if (value == null)
            {
                return "";
            }


            return value.ToString();
        }
        catch
        {
            return "";
        }
    }


    // =========================================================
    // SANITIZE FILE NAME
    // =========================================================

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "poster";
        }


        char[] invalidCharacters =
            Path.GetInvalidFileNameChars();


        foreach (
            char invalidCharacter
            in invalidCharacters)
        {
            value =
                value.Replace(
                    invalidCharacter,
                    '_'
                );
        }


        return value;
    }


    // =========================================================
    // SANITIZE FIRESTORE ID
    // =========================================================

    private static string SanitizeID(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "unknown";
        }


        return
            value
                .Trim()
                .Replace(
                    "/",
                    "_"
                )
                .Replace(
                    "\\",
                    "_"
                );
    }
}