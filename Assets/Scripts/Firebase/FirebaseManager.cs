using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }


    // =========================================================
    // FIRESTORE COLLECTIONS
    // =========================================================

    public const string ACCOUNTS_COLLECTION =
        "accounts";


    public const string SUBMISSIONS_COLLECTION =
        "submissions";


    // =========================================================
    // FIREBASE
    // =========================================================

    private FirebaseApp firebaseApp;

    private FirebaseFirestore db;

    private Task<bool> initializationTask;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsInitialized
    {
        get;
        private set;
    }


    public bool IsInitializing
    {
        get;
        private set;
    }


    public bool IsReady
    {
        get
        {
            return
                IsInitialized &&
                firebaseApp != null &&
                db != null;
        }
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


    private async void Start()
    {
        bool success =
            await InitializeFirebase();


        if (success)
        {
            Debug.Log(
                "FirebaseManager: Firebase is ready."
            );
        }
        else
        {
            Debug.LogError(
                "FirebaseManager: Firebase initialization failed."
            );
        }
    }


    // =========================================================
    // INITIALIZE FIREBASE
    // =========================================================

    public Task<bool> InitializeFirebase()
    {
        if (IsReady)
        {
            return Task.FromResult(true);
        }


        if (
            initializationTask != null &&
            !initializationTask.IsCompleted
        )
        {
            return initializationTask;
        }


        initializationTask =
            InitializeFirebaseInternal();


        return initializationTask;
    }


    // =========================================================
    // INTERNAL INITIALIZATION
    // =========================================================

    private async Task<bool>
        InitializeFirebaseInternal()
    {
        if (IsInitializing)
        {
            return IsReady;
        }


        IsInitializing = true;

        IsInitialized = false;

        LastError = "";


        try
        {
            Debug.Log(
                "FirebaseManager: Checking Firebase dependencies..."
            );


            DependencyStatus
                dependencyStatus =
                await FirebaseApp
                    .CheckAndFixDependenciesAsync();


            if (
                dependencyStatus !=
                DependencyStatus.Available
            )
            {
                SetError(
                    "Firebase dependencies unavailable: " +
                    dependencyStatus
                );


                return false;
            }


            Debug.Log(
                "FirebaseManager: Dependencies are available."
            );


            // -------------------------------------------------
            // FIREBASE APP
            // -------------------------------------------------

            firebaseApp =
                FirebaseApp.DefaultInstance;


            if (
                firebaseApp == null
            )
            {
                SetError(
                    "Firebase App could not be created."
                );


                return false;
            }


            // -------------------------------------------------
            // FIRESTORE
            // -------------------------------------------------

            db =
                FirebaseFirestore.DefaultInstance;


            if (
                db == null
            )
            {
                SetError(
                    "Firestore instance could not be created."
                );


                return false;
            }


            // -------------------------------------------------
            // SUCCESS
            // -------------------------------------------------

            IsInitialized = true;


            Debug.Log(
                "FirebaseManager: Firebase initialized successfully."
            );


            return true;
        }
        catch (
            Exception exception)
        {
            IsInitialized = false;


            firebaseApp = null;

            db = null;


            SetError(
                "Firebase initialization failed: " +
                exception.Message
            );


            return false;
        }
        finally
        {
            IsInitializing = false;
        }
    }


    // =========================================================
    // WAIT UNTIL READY
    // =========================================================

    public async Task<bool>
        WaitUntilReady()
    {
        if (IsReady)
        {
            return true;
        }


        Task<bool> task =
            InitializeFirebase();


        bool success =
            await task;


        if (
            success &&
            IsReady
        )
        {
            return true;
        }


        if (
            string.IsNullOrWhiteSpace(
                LastError
            )
        )
        {
            SetError(
                "Firebase is not ready."
            );
        }


        return false;
    }


    // =========================================================
    // GET FIRESTORE
    // =========================================================

    public FirebaseFirestore
        GetFirestore()
    {
        return db;
    }


    // =========================================================
    // =========================================================
    // ACCOUNT / PROFILE
    // =========================================================
    // =========================================================


    // =========================================================
    // SAVE ACCOUNT PROFILE
    // =========================================================
    //
    // accounts
    // └── accountID
    //     └── participant
    //
    // =========================================================

    public async Task<bool>
        SaveAccountProfile(
            string accountID,
            Dictionary<string, object>
                participantData)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            SetError(
                "Account ID is empty."
            );


            return false;
        }


        if (
            participantData == null
        )
        {
            SetError(
                "Participant data is null."
            );


            return false;
        }


        Dictionary<string, object>
            accountData =
            new Dictionary<string, object>
            {
                {
                    "participant",
                    participantData
                }
            };


        return
            await UpdateDocument(
                ACCOUNTS_COLLECTION,
                accountID,
                accountData
            );
    }


    // =========================================================
    // GET ACCOUNT PROFILE
    // =========================================================

    public async Task<
        Dictionary<string, object>>
        GetAccountProfile(
            string accountID)
    {
        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            return null;
        }


        DocumentSnapshot document =
            await GetDocument(
                ACCOUNTS_COLLECTION,
                accountID
            );


        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        if (
            !document.ContainsField(
                "participant"
            )
        )
        {
            return null;
        }


        try
        {
            return
                document.GetValue<
                    Dictionary<string, object>
                >(
                    "participant"
                );
        }
        catch (
            Exception exception)
        {
            Debug.LogWarning(
                "FirebaseManager: Failed to read account profile: " +
                exception.Message
            );


            return null;
        }
    }


    // =========================================================
    // SAVE USER ACCOUNT DETAILS
    // =========================================================
    //
    // Email and username can be stored here for convenience.
    //
    // Password MUST NOT be stored in Firestore.
    //
    // Firebase Authentication handles password securely.
    //
    // =========================================================

    public async Task<bool>
        SaveAccountDetails(
            string accountID,
            string email,
            string username)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            SetError(
                "Account ID is empty."
            );


            return false;
        }


        Dictionary<string, object>
            data =
            new Dictionary<string, object>
            {
                {
                    "email",
                    email ?? ""
                },

                {
                    "username",
                    username ?? ""
                }
            };


        return
            await UpdateDocument(
                ACCOUNTS_COLLECTION,
                accountID,
                data
            );
    }


    // =========================================================
    // =========================================================
    // SUBMISSION
    // =========================================================
    // =========================================================


    // =========================================================
    // BUILD SUBMISSION ID
    // =========================================================
    //
    // One user + one challenge = one Firestore document.
    //
    // Example:
    //
    // user:
    // abc123
    //
    // challenge:
    // challenge01
    //
    // result:
    //
    // abc123_challenge01
    //
    // =========================================================

    public string
        BuildSubmissionID(
            string accountID,
            string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(
                accountID
            ) ||
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return "";
        }


        return
            accountID.Trim()
            +
            "_"
            +
            challengeID.Trim();
    }


    // =========================================================
    // CHECK SUBMISSION EXISTS
    // =========================================================

    public async Task<bool>
        SubmissionExists(
            string accountID,
            string challengeID)
    {
        string submissionID =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        if (
            string.IsNullOrWhiteSpace(
                submissionID
            )
        )
        {
            return false;
        }


        return
            await DocumentExists(
                SUBMISSIONS_COLLECTION,
                submissionID
            );
    }


    // =========================================================
    // CHECK SUBMISSION COMPLETED
    // =========================================================

    public async Task<bool>
        HasSubmittedChallenge(
            string accountID,
            string challengeID)
    {
        string submissionID =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        if (
            string.IsNullOrWhiteSpace(
                submissionID
            )
        )
        {
            return false;
        }


        DocumentSnapshot document =
            await GetDocument(
                SUBMISSIONS_COLLECTION,
                submissionID
            );


        if (
            document == null ||
            !document.Exists
        )
        {
            return false;
        }


        if (
            !document.ContainsField(
                "isSubmitted"
            )
        )
        {
            return false;
        }


        try
        {
            return
                document.GetValue<bool>(
                    "isSubmitted"
                );
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // CHECK WHETHER USER CAN JOIN
    // =========================================================
    //
    // TRUE:
    //     no submission exists
    //
    // TRUE:
    //     submission exists but unfinished
    //
    // FALSE:
    //     submission already submitted
    //
    // =========================================================

    public async Task<bool>
        CanJoinChallenge(
            string accountID,
            string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(
                accountID
            ) ||
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return false;
        }


        bool submitted =
            await HasSubmittedChallenge(
                accountID,
                challengeID
            );


        return !submitted;
    }


    // =========================================================
    // SAVE SUBMISSION
    // =========================================================
    //
    // Uses MergeAll.
    //
    // This means:
    //
    // Output save
    //     ↓
    // Description save
    //     ↓
    // Revision save
    //     ↓
    // Final Explanation save
    //     ↓
    // Score save
    //
    // will update only the supplied fields.
    //
    // Previous fields are preserved.
    //
    // =========================================================

    public async Task<bool>
        SaveSubmission(
            string accountID,
            string challengeID,
            Dictionary<string, object>
                submissionData)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            SetError(
                "Account ID is empty."
            );


            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            SetError(
                "Challenge ID is empty."
            );


            return false;
        }


        if (
            submissionData == null
        )
        {
            SetError(
                "Submission data is null."
            );


            return false;
        }


        string submissionID =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        if (
            string.IsNullOrWhiteSpace(
                submissionID
            )
        )
        {
            SetError(
                "Unable to generate submission ID."
            );


            return false;
        }


        Dictionary<string, object>
            data =
            new Dictionary<string, object>(
                submissionData
            );


        // -----------------------------------------------------
        // Always store these identifiers.
        // -----------------------------------------------------

        data["accountID"] =
            accountID;


        data["challengeID"] =
            challengeID;


        data["submissionID"] =
            submissionID;


        return
            await UpdateDocument(
                SUBMISSIONS_COLLECTION,
                submissionID,
                data
            );
    }


    // =========================================================
    // SUBMIT FINAL CHALLENGE
    // =========================================================
    //
    // IMPORTANT:
    //
    // Once isSubmitted becomes TRUE, the challenge cannot
    // be submitted again.
    //
    // =========================================================

    public async Task<bool>
        SubmitChallenge(
            string accountID,
            string challengeID,
            Dictionary<string, object>
                submissionData)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            SetError(
                "Account ID is empty."
            );


            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            SetError(
                "Challenge ID is empty."
            );


            return false;
        }


        // -----------------------------------------------------
        // CHECK EXISTING SUBMISSION
        // -----------------------------------------------------

        bool alreadySubmitted =
            await HasSubmittedChallenge(
                accountID,
                challengeID
            );


        if (alreadySubmitted)
        {
            SetError(
                "This challenge has already been submitted."
            );


            return false;
        }


        if (
            submissionData == null
        )
        {
            submissionData =
                new Dictionary<string, object>();
        }


        // -----------------------------------------------------
        // ADD FINAL SUBMISSION FLAGS
        // -----------------------------------------------------

        submissionData["accountID"] =
            accountID;


        submissionData["challengeID"] =
            challengeID;


        submissionData["submissionID"] =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        submissionData["isSubmitted"] =
            true;


        submissionData["completedDate"] =
            DateTime.UtcNow
                .ToString(
                    "yyyy-MM-dd HH:mm:ss"
                );


        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        bool saved =
            await SaveSubmission(
                accountID,
                challengeID,
                submissionData
            );


        if (!saved)
        {
            return false;
        }


        Debug.Log(
            "FirebaseManager: Challenge submitted successfully: " +
            challengeID
        );


        return true;
    }


    // =========================================================
    // GET SUBMISSION
    // =========================================================

    public async Task<DocumentSnapshot>
        GetSubmission(
            string accountID,
            string challengeID)
    {
        string submissionID =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        if (
            string.IsNullOrWhiteSpace(
                submissionID
            )
        )
        {
            return null;
        }


        return
            await GetDocument(
                SUBMISSIONS_COLLECTION,
                submissionID
            );
    }


    // =========================================================
    // GET ALL SUBMISSIONS FOR USER
    // =========================================================

    public async Task<
        List<DocumentSnapshot>>
        GetUserSubmissions(
            string accountID)
    {
        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            return
                new List<DocumentSnapshot>();
        }


        return
            await GetDocumentsByField(
                SUBMISSIONS_COLLECTION,
                "accountID",
                accountID
            );
    }


    // =========================================================
    // GET ALL SUBMISSIONS FOR CHALLENGE
    // =========================================================
    //
    // Useful later for:
    //
    // Leaderboard
    //
    // =========================================================

    public async Task<
        List<DocumentSnapshot>>
        GetChallengeSubmissions(
            string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return
                new List<DocumentSnapshot>();
        }


        return
            await GetDocumentsByField(
                SUBMISSIONS_COLLECTION,
                "challengeID",
                challengeID
            );
    }


    // =========================================================
    // GET COMPLETED SUBMISSIONS FOR CHALLENGE
    // =========================================================

    public async Task<
        List<DocumentSnapshot>>
        GetCompletedChallengeSubmissions(
            string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return
                new List<DocumentSnapshot>();
        }


        List<DocumentSnapshot>
            results =
            new List<DocumentSnapshot>();


        if (!await WaitUntilReady())
        {
            return results;
        }


        try
        {
            Query query =
                db.Collection(
                    SUBMISSIONS_COLLECTION
                )
                .WhereEqualTo(
                    "challengeID",
                    challengeID
                )
                .WhereEqualTo(
                    "isSubmitted",
                    true
                );


            QuerySnapshot snapshot =
                await query.GetSnapshotAsync();


            if (
                snapshot == null
            )
            {
                return results;
            }


            foreach (
                DocumentSnapshot document
                in snapshot.Documents)
            {
                if (
                    document != null &&
                    document.Exists
                )
                {
                    results.Add(
                        document
                    );
                }
            }


            Debug.Log(
                "FirebaseManager: Found " +
                results.Count +
                " completed submission(s) for challenge " +
                challengeID
            );


            return results;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to get completed challenge submissions: " +
                exception.Message
            );


            return results;
        }
    }


    // =========================================================
    // =========================================================
    // GENERIC FIRESTORE METHODS
    // =========================================================
    // =========================================================


    // =========================================================
    // GET DOCUMENT
    // =========================================================

    public async Task<DocumentSnapshot>
        GetDocument(
            string collection,
            string documentID)
    {
        LastError = "";


        if (!await WaitUntilReady())
        {
            return null;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return null;
        }


        if (
            string.IsNullOrWhiteSpace(
                documentID
            )
        )
        {
            SetError(
                "Document ID is empty."
            );


            return null;
        }


        try
        {
            DocumentReference reference =
                db.Collection(
                    collection
                )
                .Document(
                    documentID
                );


            DocumentSnapshot document =
                await reference
                    .GetSnapshotAsync();


            return document;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to get document " +
                collection +
                "/" +
                documentID +
                ": " +
                exception.Message
            );


            return null;
        }
    }


    // =========================================================
    // GET ALL DOCUMENTS
    // =========================================================

    public async Task<
        List<DocumentSnapshot>>
        GetAllDocuments(
            string collection)
    {
        LastError = "";


        List<DocumentSnapshot>
            results =
            new List<DocumentSnapshot>();


        if (!await WaitUntilReady())
        {
            return results;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return results;
        }


        try
        {
            QuerySnapshot snapshot =
                await db.Collection(
                    collection
                )
                .GetSnapshotAsync();


            if (
                snapshot == null
            )
            {
                return results;
            }


            foreach (
                DocumentSnapshot document
                in snapshot.Documents)
            {
                if (
                    document != null &&
                    document.Exists
                )
                {
                    results.Add(
                        document
                    );
                }
            }


            Debug.Log(
                "FirebaseManager: Loaded " +
                results.Count +
                " document(s) from " +
                collection
            );


            return results;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to get documents from " +
                collection +
                ": " +
                exception.Message
            );


            return results;
        }
    }


    // =========================================================
    // GET DOCUMENTS BY FIELD
    // =========================================================

    public async Task<
        List<DocumentSnapshot>>
        GetDocumentsByField(
            string collection,
            string field,
            object value)
    {
        LastError = "";


        List<DocumentSnapshot>
            results =
            new List<DocumentSnapshot>();


        if (!await WaitUntilReady())
        {
            return results;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return results;
        }


        if (
            string.IsNullOrWhiteSpace(
                field
            )
        )
        {
            SetError(
                "Field name is empty."
            );


            return results;
        }


        if (
            value == null
        )
        {
            SetError(
                "Query value is null."
            );


            return results;
        }


        try
        {
            Query query =
                db.Collection(
                    collection
                )
                .WhereEqualTo(
                    field,
                    value
                );


            QuerySnapshot snapshot =
                await query.GetSnapshotAsync();


            if (
                snapshot == null
            )
            {
                return results;
            }


            foreach (
                DocumentSnapshot document
                in snapshot.Documents)
            {
                if (
                    document != null &&
                    document.Exists
                )
                {
                    results.Add(
                        document
                    );
                }
            }


            Debug.Log(
                "FirebaseManager: Query returned " +
                results.Count +
                " document(s)."
            );


            return results;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to query " +
                collection +
                " by " +
                field +
                ": " +
                exception.Message
            );


            return results;
        }
    }


    // =========================================================
    // SAVE DOCUMENT
    // =========================================================

    public async Task<bool>
        SaveDocument(
            string collection,
            string documentID,
            Dictionary<string, object>
                data)
    {
        LastError = "";


        if (!await WaitUntilReady())
        {
            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                documentID
            )
        )
        {
            SetError(
                "Document ID is empty."
            );


            return false;
        }


        if (
            data == null
        )
        {
            SetError(
                "Document data is null."
            );


            return false;
        }


        try
        {
            DocumentReference reference =
                db.Collection(
                    collection
                )
                .Document(
                    documentID
                );


            await reference.SetAsync(
                data
            );


            Debug.Log(
                "FirebaseManager: Document saved: " +
                collection +
                "/" +
                documentID
            );


            return true;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to save document " +
                collection +
                "/" +
                documentID +
                ": " +
                exception.Message
            );


            return false;
        }
    }


    // =========================================================
    // UPDATE DOCUMENT
    // =========================================================
    //
    // MergeAll:
    //
    // Existing fields remain.
    //
    // =========================================================

    public async Task<bool>
        UpdateDocument(
            string collection,
            string documentID,
            Dictionary<string, object>
                data)
    {
        LastError = "";


        if (!await WaitUntilReady())
        {
            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                documentID
            )
        )
        {
            SetError(
                "Document ID is empty."
            );


            return false;
        }


        if (
            data == null
        )
        {
            SetError(
                "Update data is null."
            );


            return false;
        }


        try
        {
            DocumentReference reference =
                db.Collection(
                    collection
                )
                .Document(
                    documentID
                );


            await reference.SetAsync(
                data,
                SetOptions.MergeAll
            );


            Debug.Log(
                "FirebaseManager: Document updated: " +
                collection +
                "/" +
                documentID
            );


            return true;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to update document " +
                collection +
                "/" +
                documentID +
                ": " +
                exception.Message
            );


            return false;
        }
    }


    // =========================================================
    // DELETE DOCUMENT
    // =========================================================

    public async Task<bool>
        DeleteDocument(
            string collection,
            string documentID)
    {
        LastError = "";


        if (!await WaitUntilReady())
        {
            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                collection
            )
        )
        {
            SetError(
                "Collection name is empty."
            );


            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                documentID
            )
        )
        {
            SetError(
                "Document ID is empty."
            );


            return false;
        }


        try
        {
            DocumentReference reference =
                db.Collection(
                    collection
                )
                .Document(
                    documentID
                );


            await reference.DeleteAsync();


            Debug.Log(
                "FirebaseManager: Document deleted: " +
                collection +
                "/" +
                documentID
            );


            return true;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to delete document " +
                collection +
                "/" +
                documentID +
                ": " +
                exception.Message
            );


            return false;
        }
    }


    // =========================================================
    // DOCUMENT EXISTS
    // =========================================================

    public async Task<bool>
        DocumentExists(
            string collection,
            string documentID)
    {
        DocumentSnapshot document =
            await GetDocument(
                collection,
                documentID
            );


        return
            document != null &&
            document.Exists;
    }


    // =========================================================
    // GET FIELD
    // =========================================================

    public async Task<T>
        GetField<T>(
            string collection,
            string documentID,
            string field,
            T defaultValue = default)
    {
        DocumentSnapshot document =
            await GetDocument(
                collection,
                documentID
            );


        if (
            document == null ||
            !document.Exists
        )
        {
            return defaultValue;
        }


        if (
            !document.ContainsField(
                field
            )
        )
        {
            return defaultValue;
        }


        try
        {
            return
                document.GetValue<T>(
                    field
                );
        }
        catch (
            Exception exception)
        {
            Debug.LogWarning(
                "FirebaseManager: Failed to read field " +
                field +
                ": " +
                exception.Message
            );


            return defaultValue;
        }
    }


    // =========================================================
    // DELETE DOCUMENTS BY FIELD
    // =========================================================

    public async Task<bool>
        DeleteDocumentsByField(
            string collection,
            string field,
            object value)
    {
        LastError = "";


        if (!await WaitUntilReady())
        {
            return false;
        }


        List<DocumentSnapshot>
            documents =
            await GetDocumentsByField(
                collection,
                field,
                value
            );


        if (
            documents == null ||
            documents.Count == 0
        )
        {
            return true;
        }


        try
        {
            WriteBatch batch =
                db.StartBatch();


            int deleteCount = 0;


            foreach (
                DocumentSnapshot document
                in documents)
            {
                if (
                    document == null ||
                    !document.Exists
                )
                {
                    continue;
                }


                batch.Delete(
                    document.Reference
                );


                deleteCount++;
            }


            if (
                deleteCount == 0
            )
            {
                return true;
            }


            await batch.CommitAsync();


            Debug.Log(
                "FirebaseManager: Deleted " +
                deleteCount +
                " document(s)."
            );


            return true;
        }
        catch (
            Exception exception)
        {
            SetError(
                "Failed to delete documents: " +
                exception.Message
            );


            return false;
        }
    }


    // =========================================================
    // RESET ERROR
    // =========================================================

    public void Reset()
    {
        LastError = "";
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
            "FirebaseManager: " +
            message
        );
    }
}