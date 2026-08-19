using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class ParticipantManager : MonoBehaviour
{
    public static ParticipantManager Instance { get; private set; }


    // =========================================================
    // FIRESTORE COLLECTIONS
    // =========================================================

    private const string ACCOUNTS_COLLECTION = "accounts";
    private const string SUBMISSIONS_COLLECTION = "submissions";


    // =========================================================
    // CURRENT PARTICIPANT / SUBMISSION
    // =========================================================

    public ParticipantData CurrentParticipant
    {
        get;
        private set;
    }


    // =========================================================
    // CURRENT CHALLENGE
    // =========================================================

    private string currentChallengeID = "";
    private string currentChallengeTitle = "";
    private string currentEventCode = "";


    public string CurrentChallengeID
    {
        get { return currentChallengeID; }
    }


    public string CurrentChallengeTitle
    {
        get { return currentChallengeTitle; }
    }


    public string CurrentEventCode
    {
        get { return currentEventCode; }
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsLoading
    {
        get;
        private set;
    }


    public bool IsSaving
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (CurrentParticipant == null)
        {
            CurrentParticipant =
                new ParticipantData();
        }
    }


    // =========================================================
    // PROFILE
    // =========================================================

    public void StartParticipant(
        string participantName,
        string institution,
        string categoryType,
        string subCategory)
    {
        EnsureParticipant();

        CurrentParticipant.participantName =
            SafeTrim(participantName);

        CurrentParticipant.institution =
            SafeTrim(institution);

        CurrentParticipant.categoryType =
            SafeTrim(categoryType);

        CurrentParticipant.subCategory =
            SafeTrim(subCategory);

        CurrentParticipant.accountID =
            GetCurrentAccountID();

        SynchronizeProfileToAccountManager();

        LastError = "";

        Debug.Log(
            "ParticipantManager: Participant profile updated."
        );
    }


    // =========================================================
    // SET PARTICIPANT DETAILS
    // =========================================================

    public void SetParticipantDetails(
        string participantName,
        string institution,
        string categoryType,
        string subCategory)
    {
        StartParticipant(
            participantName,
            institution,
            categoryType,
            subCategory
        );
    }


    // =========================================================
    // VALIDATE PARTICIPANT PROFILE
    // =========================================================

    public bool ValidateParticipant()
    {
        EnsureParticipant();

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.participantName))
        {
            SetError(
                "Please enter participant name."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.institution))
        {
            SetError(
                "Please enter institution."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.categoryType))
        {
            SetError(
                "Please select a category."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.subCategory))
        {
            SetError(
                "Please select a subcategory."
            );

            return false;
        }

        return true;
    }


    // =========================================================
    // SAVE PARTICIPANT PROFILE
    // =========================================================

    public async Task<bool> SaveProfile()
    {
        LastError = "";

        if (!await CheckFirebaseReady())
            return false;

        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
        {
            SetError(
                "Account ID is missing."
            );

            return false;
        }

        EnsureParticipant();

        if (!ValidateParticipant())
            return false;

        try
        {
            Dictionary<string, object> profileData =
                new Dictionary<string, object>
                {
                    {
                        "participantName",
                        CurrentParticipant.participantName ?? ""
                    },

                    {
                        "institution",
                        CurrentParticipant.institution ?? ""
                    },

                    {
                        "categoryType",
                        CurrentParticipant.categoryType ?? ""
                    },

                    {
                        "subCategory",
                        CurrentParticipant.subCategory ?? ""
                    }
                };


            Dictionary<string, object> accountData =
                new Dictionary<string, object>
                {
                    {
                        "participant",
                        profileData
                    }
                };


            bool saved =
                await FirebaseManager.Instance
                    .UpdateDocument(
                        ACCOUNTS_COLLECTION,
                        accountID,
                        accountData
                    );


            if (!saved)
            {
                SetError(
                    "Failed to save participant profile."
                );

                return false;
            }


            SynchronizeProfileToAccountManager();


            Debug.Log(
                "ParticipantManager: Profile saved successfully."
            );


            return true;
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to save profile: " +
                exception.Message
            );

            return false;
        }
    }


    // =========================================================
    // SAVE
    // =========================================================

    public async Task<bool> Save()
    {
        LastError = "";


        // ---------------------------------------------------------
        // NO ACTIVE CHALLENGE
        // ---------------------------------------------------------
        //
        // Main Dashboard → Participant Details
        //
        // Save profile only.
        //
        // ---------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
            currentChallengeID))
        {
            return await SaveProfile();
        }


        // ---------------------------------------------------------
        // ACTIVE CHALLENGE
        // ---------------------------------------------------------
        //
        // Challenge → Participant Details
        //
        // Save challenge submission.
        //
        // ---------------------------------------------------------

        return await SaveCurrentSubmission();
    }


    // =========================================================
    // START / JOIN CHALLENGE
    // =========================================================

    public async Task<bool> StartChallenge(
        string challengeID,
        string challengeTitle)
    {
        return await StartChallenge(
            challengeID,
            challengeTitle,
            ""
        );
    }


    public async Task<bool> StartChallenge(
        string challengeID,
        string challengeTitle,
        string eventCode)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(challengeID))
        {
            SetError(
                "Challenge ID is empty."
            );

            return false;
        }

        if (!await CheckFirebaseReady())
            return false;

        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
        {
            SetError(
                "Please login first."
            );

            return false;
        }


        // -----------------------------------------------------
        // LOAD PERMANENT PROFILE
        // -----------------------------------------------------

        ParticipantData profile = GetPermanentProfile();

        if (profile == null)
        {
            // ---------------------------------------------------------
            // NEW ACCOUNT
            // ---------------------------------------------------------
            //
            // Participant details have not been entered yet.
            // This is allowed because the challenge should lead
            // the user to the Participant Details panel.
            //
            // ---------------------------------------------------------

            profile =
                new ParticipantData();

            profile.accountID =
                accountID;


            if (
                AccountManager.Instance != null &&
                AccountManager.Instance.CurrentAccount != null
            )
            {
                profile.username =
                    AccountManager.Instance
                        .CurrentAccount
                        .username;


                AccountManager.Instance
                    .CurrentAccount
                    .participant =
                    profile;
            }


            Debug.Log(
                "ParticipantManager: " +
                "No permanent participant profile found. " +
                "Creating empty profile for new challenge."
            );
        }


        // -----------------------------------------------------
        // CHECK EXISTING SUBMISSION
        // -----------------------------------------------------

        ParticipantData existingSubmission =
            await LoadSubmissionForChallenge(
                accountID,
                challengeID
            );


        if (
            existingSubmission != null &&
            existingSubmission.isSubmitted
        )
        {
            SetError(
                "You have already submitted this challenge. Only one attempt is allowed."
            );

            Debug.LogWarning(
                "ParticipantManager: Challenge already submitted: " +
                challengeID
            );

            return false;
        }


        // -----------------------------------------------------
        // SET CURRENT CHALLENGE
        // -----------------------------------------------------

        currentChallengeID =
            SafeTrim(challengeID);

        currentChallengeTitle =
            SafeTrim(challengeTitle);

        currentEventCode =
            SafeTrim(eventCode);


        // -----------------------------------------------------
        // EXISTING UNFINISHED SUBMISSION
        // -----------------------------------------------------

        if (existingSubmission != null)
        {
            // -----------------------------------------------------
            // RESTORE EXISTING UNFINISHED CHALLENGE
            // -----------------------------------------------------

            CurrentParticipant =
                existingSubmission;

            CurrentParticipant.accountID =
                accountID;

            // Restore runtime challenge state
            currentChallengeID =
                SafeTrim(existingSubmission.challengeID);

            currentChallengeTitle =
                SafeTrim(existingSubmission.challengeTitle);

            // Restore event code if it exists in the submission
            currentEventCode =
                !string.IsNullOrWhiteSpace(eventCode)
                    ? SafeTrim(eventCode)
                    : SafeTrim(existingSubmission.eventCode);

            CurrentParticipant.eventCode =
                currentEventCode;

            Debug.Log(
                "ParticipantManager: Existing unfinished challenge loaded. " +
                "Challenge = " +
                currentChallengeTitle +
                " | ID = " +
                currentChallengeID +
                " | Submitted = " +
                existingSubmission.isSubmitted
            );

            return true;
        }


        // -----------------------------------------------------
        // CREATE NEW SUBMISSION
        // -----------------------------------------------------

        ParticipantData newSubmission =
            CreateNewChallengeSubmission(
                profile,
                accountID,
                challengeID,
                challengeTitle
            );

        CurrentParticipant =
            newSubmission;

        Debug.Log(
            "ParticipantManager: New challenge started: " +
            challengeTitle
        );

        return true;
    }


    // =========================================================
    // SET CHALLENGE
    // =========================================================

    public void SetChallenge(
    string challengeID,
    string challengeTitle)
    {
        currentChallengeID =
            SafeTrim(challengeID);

        currentChallengeTitle =
            SafeTrim(challengeTitle);

        EnsureParticipant();

        CurrentParticipant.challengeID =
            currentChallengeID;

        CurrentParticipant.challengeTitle =
            currentChallengeTitle;

        CurrentParticipant.eventCode =
            currentEventCode;

        Debug.Log(
            "ParticipantManager: Challenge set: " +
            currentChallengeTitle
        );
    }


    // =========================================================
    // SET CHALLENGE + EVENT
    // =========================================================

    public void SetChallenge(
        string challengeID,
        string challengeTitle,
        string eventCode)
    {
        SetChallenge(
            challengeID,
            challengeTitle
        );

        SetEventCode(eventCode);
    }


    // =========================================================
    // SET EVENT CODE
    // =========================================================

    public void SetEventCode(string eventCode)
    {
        currentEventCode =
            SafeTrim(eventCode);

        EnsureParticipant();

        CurrentParticipant.eventCode =
            currentEventCode;

        Debug.Log(
            "ParticipantManager: Event code set: " +
            currentEventCode
        );
    }


    // =========================================================
    // CREATE NEW CHALLENGE SUBMISSION
    // =========================================================

    private ParticipantData
        CreateNewChallengeSubmission(
            ParticipantData profile,
            string accountID,
            string challengeID,
            string challengeTitle)
    {
        ParticipantData submission =
            new ParticipantData();


        submission.accountID =
            accountID;

        submission.username =
            profile != null
                ? profile.username
                : "";


        submission.participantName =
            profile != null
                ? profile.participantName
                : "";

        submission.institution =
            profile != null
                ? profile.institution
                : "";

        submission.categoryType =
            profile != null
                ? profile.categoryType
                : "";

        submission.subCategory =
            profile != null
                ? profile.subCategory
                : "";


        submission.challengeID =
            SafeTrim(challengeID);

        submission.challengeTitle =
            SafeTrim(challengeTitle);

        submission.eventCode =
            currentEventCode;

        submission.submissionID =
            BuildSubmissionID(
                accountID,
                challengeID
            );


        // IMPORTANT
        submission.isSubmitted = false;

        submission.lastPage =
            "Participant";


        return submission;
    }


    // =========================================================
    // SAVE CURRENT SUBMISSION
    // =========================================================

    public async Task<bool>
        SaveCurrentSubmission()
    {
        LastError = "";

        if (CurrentParticipant == null)
        {
            SetError(
                "Current submission is not available."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.challengeID))
        {
            SetError(
                "No challenge is currently selected."
            );

            return false;
        }

        if (!await CheckFirebaseReady())
            return false;

        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
        {
            SetError(
                "Account ID is missing."
            );

            return false;
        }

        IsSaving = true;

        try
        {
            CurrentParticipant.accountID =
                accountID;

            if (string.IsNullOrWhiteSpace(
                CurrentParticipant.submissionID))
            {
                CurrentParticipant.submissionID =
                    BuildSubmissionID(
                        accountID,
                        CurrentParticipant.challengeID
                    );
            }


            Dictionary<string, object>
                submissionData =
                ToSubmissionDictionary(
                    CurrentParticipant
                );


            FirebaseFirestore db =
                FirebaseManager.Instance
                    .GetFirestore();


            if (db == null)
            {
                SetError(
                    "Firestore is not available."
                );

                return false;
            }


            DocumentReference reference =
                db.Collection(
                    SUBMISSIONS_COLLECTION
                )
                .Document(
                    CurrentParticipant.submissionID
                );


            await reference.SetAsync(
                submissionData,
                SetOptions.MergeAll
            );


            Debug.Log(
                "ParticipantManager: Submission saved: " +
                CurrentParticipant.submissionID
            );


            return true;
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to save submission: " +
                exception.Message
            );

            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }


    // =========================================================
    // SUBMIT FINAL CHALLENGE
    // =========================================================

    public async Task<bool>
        SubmitCurrentChallenge()
    {
        LastError = "";

        if (CurrentParticipant == null)
        {
            SetError(
                "Submission data is not available."
            );

            return false;
        }


        if (CurrentParticipant.isSubmitted)
        {
            SetError(
                "This challenge has already been submitted."
            );

            return false;
        }


        if (!CurrentParticipant.HasPrompt())
        {
            SetError(
                "Prompt is missing."
            );

            return false;
        }


        if (!CurrentParticipant.HasPoster())
        {
            SetError(
                "Poster is missing."
            );

            return false;
        }


        if (!CurrentParticipant.HasFinalExplanation())
        {
            SetError(
                "Final explanation is missing."
            );

            return false;
        }


        // -----------------------------------------------------
        // MARK AS SUBMITTED
        // -----------------------------------------------------

        CurrentParticipant.MarkSubmitted();


        // -----------------------------------------------------
        // SAVE FINAL SUBMISSION
        // -----------------------------------------------------

        bool saved =
            await SaveCurrentSubmission();


        if (!saved)
        {
            // Firebase failed.
            // Allow user to retry.

            CurrentParticipant.isSubmitted =
                false;

            return false;
        }


        Debug.Log(
            "ParticipantManager: Challenge submitted successfully."
        );


        return true;
    }


    // =========================================================
    // CHECK WHETHER USER CAN JOIN CHALLENGE
    // =========================================================

    public async Task<bool>
        CanJoinChallenge(
            string challengeID)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(challengeID))
            return false;

        if (!await CheckFirebaseReady())
            return false;

        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
            return false;

        ParticipantData submission =
            await LoadSubmissionForChallenge(
                accountID,
                challengeID
            );


        // No submission.
        if (submission == null)
            return true;


        // Submission still in progress.
        if (!submission.isSubmitted)
            return true;


        // Already submitted.
        return false;
    }


    // =========================================================
    // CHECK WHETHER CHALLENGE WAS SUBMITTED
    // =========================================================

    public async Task<bool>
        HasSubmittedChallenge(
            string challengeID)
    {
        if (string.IsNullOrWhiteSpace(challengeID))
            return false;

        if (!await CheckFirebaseReady())
            return false;

        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
            return false;

        ParticipantData submission =
            await LoadSubmissionForChallenge(
                accountID,
                challengeID
            );

        return
            submission != null &&
            submission.isSubmitted;
    }


    // =========================================================
    // LOAD CURRENT ACCOUNT PROFILE
    // =========================================================

    public async Task<bool>
        LoadCurrentAccountParticipant()
    {
        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
            return false;

        return await LoadProfile(accountID);
    }


    // =========================================================
    // LOAD PROFILE
    // =========================================================

    public async Task<bool>
        LoadProfile(string accountID)
    {
        LastError = "";

        if (string.IsNullOrWhiteSpace(accountID))
        {
            SetError(
                "Account ID is empty."
            );

            return false;
        }

        if (!await CheckFirebaseReady())
            return false;

        IsLoading = true;

        try
        {
            DocumentSnapshot account =
                await FirebaseManager.Instance
                    .GetDocument(
                        ACCOUNTS_COLLECTION,
                        accountID
                    );


            if (account == null || !account.Exists)
            {
                CurrentParticipant =
                    new ParticipantData();

                CurrentParticipant.accountID =
                    accountID;

                SynchronizeProfileToAccountManager();

                return false;
            }


            ParticipantData profile =
                new ParticipantData();

            profile.accountID =
                accountID;


            if (account.ContainsField("username"))
            {
                profile.username =
                    GetString(
                        account,
                        "username"
                    );
            }


            if (account.ContainsField("participant"))
            {
                Dictionary<string, object>
                    profileMap =
                    account.GetValue<
                        Dictionary<string, object>
                    >("participant");


                profile.participantName =
                    GetString(
                        profileMap,
                        "participantName"
                    );

                profile.institution =
                    GetString(
                        profileMap,
                        "institution"
                    );

                profile.categoryType =
                    GetString(
                        profileMap,
                        "categoryType"
                    );

                profile.subCategory =
                    GetString(
                        profileMap,
                        "subCategory"
                    );
            }


            CurrentParticipant =
                profile;

            SynchronizeProfileToAccountManager();


            Debug.Log(
                "ParticipantManager: Profile loaded."
            );


            return HasParticipantData();
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to load profile: " +
                exception.Message
            );

            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }


    // =========================================================
    // LOAD SUBMISSION FOR CHALLENGE
    // =========================================================

    public async Task<ParticipantData>
        LoadSubmissionForChallenge(
            string accountID,
            string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(accountID) ||
            string.IsNullOrWhiteSpace(challengeID)
        )
        {
            return null;
        }

        if (!await CheckFirebaseReady())
            return null;

        try
        {
            string submissionID =
                BuildSubmissionID(
                    accountID,
                    challengeID
                );


            FirebaseFirestore db =
                FirebaseManager.Instance
                    .GetFirestore();


            if (db == null)
                return null;


            DocumentSnapshot snapshot =
                await db.Collection(
                    SUBMISSIONS_COLLECTION
                )
                .Document(submissionID)
                .GetSnapshotAsync();


            if (snapshot == null || !snapshot.Exists)
                return null;


            return
                FromSubmissionSnapshot(snapshot);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ParticipantManager: Failed to load submission: " +
                exception.Message
            );

            return null;
        }
    }


    // =========================================================
    // LOAD CURRENT SUBMISSION
    // =========================================================

    public async Task<bool>LoadCurrentSubmission()
    {
        string accountID =
            GetCurrentAccountID();

        if (string.IsNullOrWhiteSpace(accountID))
            return false;

        if (string.IsNullOrWhiteSpace(
            currentChallengeID))
            return false;

        ParticipantData submission =
            await LoadSubmissionForChallenge(
                accountID,
                currentChallengeID
            );

        if (submission == null)
            return false;

        CurrentParticipant =
            submission;

        // Restore runtime challenge state
        currentChallengeID =
            submission.challengeID ?? "";

        currentChallengeTitle =
            submission.challengeTitle ?? "";

        currentEventCode =
            submission.eventCode ?? "";

        CurrentParticipant.accountID =
            accountID;

        Debug.Log(
            "ParticipantManager: Current submission restored. " +
            "Challenge = " +
            currentChallengeTitle +
            " | Event = " +
            currentEventCode +
            " | Submitted = " +
            CurrentParticipant.isSubmitted
        );

        return true;
    }


    // =========================================================
    // LOAD ALL USER SUBMISSIONS
    // =========================================================

    public async Task<List<ParticipantData>>
        LoadAllSubmissions()
    {
        List<ParticipantData> submissions =
            new List<ParticipantData>();


        string accountID =
            GetCurrentAccountID();


        if (string.IsNullOrWhiteSpace(accountID))
            return submissions;


        if (!await CheckFirebaseReady())
            return submissions;


        try
        {
            FirebaseFirestore db =
                FirebaseManager.Instance
                    .GetFirestore();


            if (db == null)
                return submissions;


            Query query =
                db.Collection(
                    SUBMISSIONS_COLLECTION
                )
                .WhereEqualTo(
                    "accountID",
                    accountID
                );


            QuerySnapshot snapshot =
                await query.GetSnapshotAsync();


            if (snapshot == null)
                return submissions;


            foreach (
                DocumentSnapshot document
                in snapshot.Documents)
            {
                if (
                    document == null ||
                    !document.Exists
                )
                {
                    continue;
                }


                ParticipantData submission =
                    FromSubmissionSnapshot(document);


                if (submission != null)
                {
                    submissions.Add(submission);
                }
            }


            Debug.Log(
                "ParticipantManager: Loaded " +
                submissions.Count +
                " submission(s)."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "ParticipantManager: Failed to load submissions: " +
                exception.Message
            );
        }


        return submissions;
    }


    // =========================================================
    // LOAD SUBMISSION DATA
    // =========================================================

    public void LoadSubmissionData(
    SubmissionManager.SubmissionData submission)
    {
        if (submission == null)
        {
            SetError(
                "Submission data is null."
            );

            return;
        }

        EnsureParticipant();

        // =====================================================
        // ACCOUNT
        // =====================================================

        CurrentParticipant.accountID =
            submission.accountID;

        CurrentParticipant.username =
            submission.username;


        // =====================================================
        // SUBMISSION
        // =====================================================

        CurrentParticipant.submissionID =
            submission.submissionID;

        CurrentParticipant.isSubmitted =
            submission.isSubmitted;

        CurrentParticipant.completedDate =
            submission.completedDate;

        CurrentParticipant.lastPage =
            submission.lastPage;


        // =====================================================
        // CHALLENGE
        // =====================================================

        CurrentParticipant.challengeID =
            submission.challengeID;

        CurrentParticipant.challengeTitle =
            submission.challengeTitle;

        CurrentParticipant.eventCode =
            submission.eventCode;


        // =====================================================
        // PARTICIPANT
        // =====================================================

        CurrentParticipant.participantName =
            submission.participantName;

        CurrentParticipant.institution =
            submission.institution;

        CurrentParticipant.categoryType =
            submission.categoryType;

        CurrentParticipant.subCategory =
            submission.subCategory;


        // =====================================================
        // PROMPT
        // =====================================================

        CurrentParticipant.prompt =
            submission.prompt;

        CurrentParticipant.promptUsed =
            submission.promptUsed;


        // =====================================================
        // IMAGES / STORAGE
        // =====================================================

        CurrentParticipant.originalImageUrl =
            submission.originalImageUrl;

        CurrentParticipant.revisedImageUrl =
            submission.revisedImageUrl;

        CurrentParticipant.posterImageUrl =
            submission.posterImageUrl;

        CurrentParticipant.storagePath =
            submission.storagePath;


        // =====================================================
        // DESCRIPTION
        // =====================================================

        CurrentParticipant.posterDescription =
            submission.posterDescription;


        // =====================================================
        // REVISION
        // =====================================================

        CurrentParticipant.revisionPrompt =
            submission.revisionPrompt;

        CurrentParticipant.revisionHistory =
            submission.revisionHistory;

        CurrentParticipant.revisionCount =
            submission.revisionCount;


        // =====================================================
        // FINAL EXPLANATION
        // =====================================================

        CurrentParticipant.finalExplanation =
            submission.finalExplanation;


        // =====================================================
        // SCORE
        // =====================================================

        CurrentParticipant.score =
            submission.score;

        CurrentParticipant.promptQuality =
            submission.promptQuality;

        CurrentParticipant.posterMessage =
            submission.posterMessage;

        CurrentParticipant.designQuality =
            submission.designQuality;

        CurrentParticipant.accessibilityUnderstanding =
            submission.accessibilityUnderstanding;

        CurrentParticipant.revisionProcessScore =
            submission.revisionProcessScore;

        CurrentParticipant.finalExplanationScore =
            submission.finalExplanationScore;


        // =====================================================
        // FEEDBACK
        // =====================================================

        CurrentParticipant.feedback =
            submission.feedback;

        CurrentParticipant.improvementSuggestion =
            submission.improvementSuggestion;


        // =====================================================
        // RESTORE CURRENT CHALLENGE
        // =====================================================

        currentChallengeID =
            submission.challengeID ?? "";

        currentChallengeTitle =
            submission.challengeTitle ?? "";

        currentEventCode =
            submission.eventCode ?? "";


        Debug.Log(
            "ParticipantManager: Submission loaded. " +
            "Challenge = " +
            CurrentParticipant.challengeTitle +
            " | Submitted = " +
            CurrentParticipant.isSubmitted
        );
    }


    // =========================================================
    // DESIGN DATA
    // =========================================================

    public void SetPrompt(string prompt)
    {
        EnsureParticipant();

        CurrentParticipant.prompt =
            SafeTrim(prompt);
    }


    public void SetPromptUsed(string promptUsed)
    {
        EnsureParticipant();

        CurrentParticipant.promptUsed =
            SafeTrim(promptUsed);
    }


    public void SetOriginalImage(string imageUrl)
    {
        EnsureParticipant();

        CurrentParticipant.originalImageUrl =
            SafeTrim(imageUrl);

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.posterImageUrl))
        {
            CurrentParticipant.posterImageUrl =
                CurrentParticipant.originalImageUrl;
        }
    }


    public void SetPosterImage(string imageUrl)
    {
        EnsureParticipant();

        CurrentParticipant.posterImageUrl =
            SafeTrim(imageUrl);
    }


    public void SetRevisedImage(string imageUrl)
    {
        EnsureParticipant();

        CurrentParticipant.revisedImageUrl =
            SafeTrim(imageUrl);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            CurrentParticipant.posterImageUrl =
                imageUrl;
        }
    }


    public void SetStoragePath(string path)
    {
        EnsureParticipant();

        CurrentParticipant.storagePath =
            SafeTrim(path);
    }


    public void SetPosterDescription(string description)
    {
        EnsureParticipant();

        CurrentParticipant.posterDescription =
            SafeTrim(description);
    }


    public void SetRevision(
        string revisionPrompt,
        string revisedImageUrl)
    {
        EnsureParticipant();

        CurrentParticipant.revisionPrompt =
            SafeTrim(revisionPrompt);

        CurrentParticipant.revisedImageUrl =
            SafeTrim(revisedImageUrl);

        if (!string.IsNullOrWhiteSpace(
            revisedImageUrl))
        {
            CurrentParticipant.posterImageUrl =
                revisedImageUrl;
        }
    }


    public void SetRevisionCount(int count)
    {
        EnsureParticipant();

        CurrentParticipant.revisionCount =
            Mathf.Clamp(
                count,
                0,
                3
            );
    }


    public int IncrementRevisionCount()
    {
        EnsureParticipant();

        if (CurrentParticipant.revisionCount >= 3)
            return 3;

        CurrentParticipant.revisionCount++;

        return CurrentParticipant.revisionCount;
    }


    public int GetRevisionCount()
    {
        if (CurrentParticipant == null)
            return 0;

        return CurrentParticipant.revisionCount;
    }


    public void SetFinalExplanation(string explanation)
    {
        EnsureParticipant();

        CurrentParticipant.finalExplanation =
            SafeTrim(explanation);
    }


    // =========================================================
    // SCORE
    // =========================================================

    public void SetScore(
    int score,
    int promptQuality,
    int posterMessage,
    int designQuality,
    int accessibilityUnderstanding,
    int revisionProcessScore,
    int finalExplanationScore)
    {
        EnsureParticipant();

        CurrentParticipant.SetScore(
            promptQuality,
            posterMessage,
            designQuality,
            accessibilityUnderstanding,
            revisionProcessScore,
            finalExplanationScore
        );

        // Keep calculated total
        CurrentParticipant.score =
            Mathf.Clamp(score, 0, 100);
    }


    // =========================================================
    // FEEDBACK
    // =========================================================

    public void SetFeedback(
        string feedback,
        string improvementSuggestion)
    {
        EnsureParticipant();

        CurrentParticipant.feedback =
            feedback;

        CurrentParticipant.improvementSuggestion =
            improvementSuggestion;
    }


    // =========================================================
    // LAST PAGE
    // =========================================================

    public void SetLastPage(string pageName)
    {
        EnsureParticipant();

        CurrentParticipant.lastPage =
            SafeTrim(pageName);
    }


    public string GetLastPage()
    {
        if (CurrentParticipant == null)
            return "";

        return CurrentParticipant.lastPage;
    }


    // =========================================================
    // MARK SUBMITTED
    // =========================================================

    public void MarkSubmitted()
    {
        EnsureParticipant();

        CurrentParticipant.MarkSubmitted();

        Debug.Log(
            "ParticipantManager: Challenge marked submitted."
        );
    }


    // =========================================================
    // SET SUBMISSION DATE
    // =========================================================

    public void SetCompletedDate(string completedDate)
    {
        EnsureParticipant();

        CurrentParticipant.completedDate =
            SafeTrim(completedDate);
    }


    // =========================================================
    // GENERATE SUBMISSION ID
    // =========================================================

    public string EnsureSubmissionID()
    {
        EnsureParticipant();

        if (string.IsNullOrWhiteSpace(
            CurrentParticipant.submissionID))
        {
            string accountID =
                GetCurrentAccountID();

            CurrentParticipant.submissionID =
                BuildSubmissionID(
                    accountID,
                    CurrentParticipant.challengeID
                );
        }

        return CurrentParticipant.submissionID;
    }


    // =========================================================
    // GET LATEST POSTER
    // =========================================================

    public string GetLatestPosterUrl()
    {
        if (CurrentParticipant == null)
            return "";

        return
            CurrentParticipant.GetLatestPosterUrl();
    }


    // =========================================================
    // CHECK PARTICIPANT
    // =========================================================

    public bool HasParticipant()
    {
        return CurrentParticipant != null;
    }


    // =========================================================
    // CHECK PARTICIPANT DATA
    // =========================================================

    public bool HasParticipantData()
    {
        if (CurrentParticipant == null)
            return false;

        return
            !string.IsNullOrWhiteSpace(
                CurrentParticipant.participantName
            );
    }


    // =========================================================
    // CHECK PARTICIPANT DETAILS
    // =========================================================

    public bool HasParticipantDetails()
    {
        if (CurrentParticipant == null)
            return false;

        return
            CurrentParticipant.HasParticipantDetails();
    }


    // =========================================================
    // CHECK CHALLENGE
    // =========================================================

    public bool HasChallenge()
    {
        if (CurrentParticipant == null)
            return false;

        return CurrentParticipant.HasChallenge();
    }


    // =========================================================
    // CHECK PROMPT
    // =========================================================

    public bool HasPrompt()
    {
        if (CurrentParticipant == null)
            return false;

        return CurrentParticipant.HasPrompt();
    }


    // =========================================================
    // CHECK GENERATED POSTER
    // =========================================================

    public bool HasGeneratedPoster()
    {
        if (CurrentParticipant == null)
            return false;

        return CurrentParticipant.HasPoster();
    }


    // =========================================================
    // CHECK SCORE
    // =========================================================

    public bool HasScore()
    {
        if (CurrentParticipant == null)
            return false;

        return CurrentParticipant.HasScore();
    }


    // =========================================================
    // CHECK SUBMITTED
    // =========================================================

    public bool IsSubmitted()
    {
        if (CurrentParticipant == null)
            return false;

        return CurrentParticipant.isSubmitted;
    }


    // =========================================================
    // CLEAR DESIGN DATA
    // =========================================================

    public void ClearDesignData()
    {
        if (CurrentParticipant == null)
            return;


        string accountID =
            GetCurrentAccountID();

        string username =
            CurrentParticipant.username;

        string participantName =
            CurrentParticipant.participantName;

        string institution =
            CurrentParticipant.institution;

        string categoryType =
            CurrentParticipant.categoryType;

        string subCategory =
            CurrentParticipant.subCategory;


        CurrentParticipant.ResetChallengeData();


        CurrentParticipant.accountID =
            accountID;

        CurrentParticipant.username =
            username;

        CurrentParticipant.participantName =
            participantName;

        CurrentParticipant.institution =
            institution;

        CurrentParticipant.categoryType =
            categoryType;

        CurrentParticipant.subCategory =
            subCategory;

        currentChallengeID = "";
        currentChallengeTitle = "";
        currentEventCode = "";


        Debug.Log(
            "ParticipantManager: Current challenge data cleared."
        );
    }


    // =========================================================
    // RESET PARTICIPANT
    // =========================================================

    public void ResetParticipant()
    {
        CurrentParticipant =
            new ParticipantData();

        currentChallengeID = "";
        currentChallengeTitle = "";
        currentEventCode = "";

        IsLoading = false;
        IsSaving = false;
        LastError = "";

        Debug.Log(
            "ParticipantManager: Local participant state reset."
        );
    }


    // =========================================================
    // CLEAR CURRENT PARTICIPANT
    // =========================================================

    public void ClearCurrentParticipant()
    {
        CurrentParticipant = null;

        currentChallengeID = "";
        currentChallengeTitle = "";
        currentEventCode = "";

        IsLoading = false;
        IsSaving = false;
        LastError = "";

        Debug.Log(
            "ParticipantManager: Current participant cleared."
        );
    }


    // =========================================================
    // CLEAR CURRENT LOCAL CHALLENGE
    // =========================================================

    public void ClearCurrentChallenge()
    {
        if (CurrentParticipant == null)
            return;


        string accountID =
            GetCurrentAccountID();

        string username =
            CurrentParticipant.username;

        string participantName =
            CurrentParticipant.participantName;

        string institution =
            CurrentParticipant.institution;

        string categoryType =
            CurrentParticipant.categoryType;

        string subCategory =
            CurrentParticipant.subCategory;


        CurrentParticipant.ResetChallengeData();


        CurrentParticipant.accountID =
            accountID;

        CurrentParticipant.username =
            username;

        CurrentParticipant.participantName =
            participantName;

        CurrentParticipant.institution =
            institution;

        CurrentParticipant.categoryType =
            categoryType;

        CurrentParticipant.subCategory =
            subCategory;


        currentChallengeID = "";
        currentChallengeTitle = "";
        currentEventCode = "";


        Debug.Log(
            "ParticipantManager: Current challenge cleared."
        );
    }


    // =========================================================
    // GET CURRENT PARTICIPANT
    // =========================================================

    public ParticipantData GetParticipant()
    {
        return CurrentParticipant;
    }


    public ParticipantData GetCurrentParticipant()
    {
        return CurrentParticipant;
    }


    // =========================================================
    // SYNCHRONIZE PROFILE TO ACCOUNT MANAGER
    // =========================================================

    private void SynchronizeProfileToAccountManager()
    {
        if (
            AccountManager.Instance == null ||
            AccountManager.Instance.CurrentAccount == null ||
            CurrentParticipant == null
        )
        {
            return;
        }


        ParticipantData profile =
            AccountManager.Instance
                .CurrentAccount
                .participant;


        if (profile == null)
        {
            profile =
                new ParticipantData();
        }


        profile.accountID =
            AccountManager.Instance
                .CurrentAccount
                .accountId;

        profile.username =
            AccountManager.Instance
                .CurrentAccount
                .username;

        profile.participantName =
            CurrentParticipant.participantName;

        profile.institution =
            CurrentParticipant.institution;

        profile.categoryType =
            CurrentParticipant.categoryType;

        profile.subCategory =
            CurrentParticipant.subCategory;


        AccountManager.Instance
            .CurrentAccount
            .participant =
            profile;
    }


    // =========================================================
    // GET PERMANENT PROFILE
    // =========================================================

    private ParticipantData GetPermanentProfile()
    {
        if (AccountManager.Instance == null)
            return null;

        if (AccountManager.Instance.CurrentAccount == null)
            return null;

        return
            AccountManager.Instance
                .CurrentAccount
                .participant;
    }


    // =========================================================
    // ENSURE PARTICIPANT
    // =========================================================

    private void EnsureParticipant()
    {
        if (CurrentParticipant == null)
        {
            CurrentParticipant =
                new ParticipantData();
        }
    }


    // =========================================================
    // FIRESTORE SUBMISSION DICTIONARY
    // =========================================================

    private Dictionary<string, object>
        ToSubmissionDictionary(
            ParticipantData participant)
    {
        if (participant == null)
        {
            return
                new Dictionary<string, object>();
        }


        return
            new Dictionary<string, object>
            {
                {
                    "accountID",
                    participant.accountID ?? ""
                },

                {
                    "username",
                    participant.username ?? ""
                },

                {
                    "submissionID",
                    participant.submissionID ?? ""
                },

                {
                    "challengeID",
                    participant.challengeID ?? ""
                },

                {
                    "challengeTitle",
                    participant.challengeTitle ?? ""
                },

                {
                    "participantName",
                    participant.participantName ?? ""
                },

                {
                    "institution",
                    participant.institution ?? ""
                },

                {
                    "categoryType",
                    participant.categoryType ?? ""
                },

                {
                    "subCategory",
                    participant.subCategory ?? ""
                },

                {
                    "prompt",
                    participant.prompt ?? ""
                },

                {
                    "promptUsed",
                    participant.promptUsed ?? ""
                },

                {
                    "originalImageUrl",
                    participant.originalImageUrl ?? ""
                },

                {
                    "storagePath",
                    participant.storagePath ?? ""
                },

                {
                    "posterDescription",
                    participant.posterDescription ?? ""
                },

                {
                    "revisionPrompt",
                    participant.revisionPrompt ?? ""
                },

                {
                    "revisionHistory",
                    participant.revisionHistory ?? ""
                },

                {
                    "revisionCount",
                    participant.revisionCount
                },

                {
                    "revisedImageUrl",
                    participant.revisedImageUrl ?? ""
                },

                {
                    "posterImageUrl",
                    participant.posterImageUrl ?? ""
                },

                {
                    "finalExplanation",
                    participant.finalExplanation ?? ""
                },

                {
                    "score",
                    participant.score
                },

                {
                    "promptQuality",
                    participant.promptQuality
                },

                {
                    "posterMessage",
                    participant.posterMessage
                },

                {
                    "designQuality",
                    participant.designQuality
                },

                {
                    "accessibilityUnderstanding",
                    participant.accessibilityUnderstanding
                },

                {
                    "revisionProcessScore",
                    participant.revisionProcessScore
                },

                {
                    "finalExplanationScore",
                    participant.finalExplanationScore
                },

                {
                    "feedback",
                    participant.feedback ?? ""
                },

                {
                    "improvementSuggestion",
                    participant.improvementSuggestion ?? ""
                },

                // =================================================
                // ONLY SUBMISSION STATUS FIELD
                // =================================================

                {
                    "isSubmitted",
                    participant.isSubmitted
                },

                {
                    "completedDate",
                    participant.completedDate ?? ""
                },

                {
                    "lastPage",
                    participant.lastPage ?? ""
                },

                {
                    "eventCode",
                    participant.eventCode ?? ""
                }
            };
    }


    // =========================================================
    // FIRESTORE → SUBMISSION
    // =========================================================

    private ParticipantData
        FromSubmissionSnapshot(
            DocumentSnapshot document)
    {
        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        ParticipantData submission =
            new ParticipantData();


        submission.accountID =
            GetString(document, "accountID");

        submission.username =
            GetString(document, "username");

        submission.submissionID =
            GetString(document, "submissionID");

        submission.challengeID =
            GetString(document, "challengeID");

        submission.challengeTitle =
            GetString(document, "challengeTitle");

        submission.eventCode =
            GetString(document, "eventCode");

        submission.participantName =
            GetString(document, "participantName");

        submission.institution =
            GetString(document, "institution");

        submission.categoryType =
            GetString(document, "categoryType");

        submission.subCategory =
            GetString(document, "subCategory");

        submission.prompt =
            GetString(document, "prompt");

        submission.promptUsed =
            GetString(document, "promptUsed");

        submission.originalImageUrl =
            GetString(document, "originalImageUrl");

        submission.storagePath =
            GetString(document, "storagePath");

        submission.posterDescription =
            GetString(document, "posterDescription");

        submission.revisionPrompt =
            GetString(document, "revisionPrompt");

        submission.revisionHistory =
            GetString(document, "revisionHistory");

        submission.revisionCount =
            GetInt(document, "revisionCount");

        submission.revisedImageUrl =
            GetString(document, "revisedImageUrl");

        submission.posterImageUrl =
            GetString(document, "posterImageUrl");

        submission.finalExplanation =
            GetString(document, "finalExplanation");

        submission.score =
            GetInt(document, "score");

        submission.promptQuality =
            GetInt(document, "promptQuality");

        submission.posterMessage =
            GetInt(document, "posterMessage");

        submission.designQuality =
            GetInt(document, "designQuality");

        submission.accessibilityUnderstanding =
            GetInt(document, "accessibilityUnderstanding");

        submission.revisionProcessScore =
            GetInt(document, "revisionProcessScore");

        submission.finalExplanationScore =
            GetInt(document, "finalExplanationScore");

        submission.feedback =
            GetString(document, "feedback");

        submission.improvementSuggestion =
            GetString(document, "improvementSuggestion");


        // =====================================================
        // ONLY READ isSubmitted
        // =====================================================

        submission.isSubmitted =
            GetBool(document, "isSubmitted");


        submission.completedDate =
            GetString(document, "completedDate");

        submission.lastPage =
            GetString(document, "lastPage");


        return submission;
    }


    // =========================================================
    // BUILD SUBMISSION ID
    // =========================================================

    private string BuildSubmissionID(
        string accountID,
        string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(accountID) ||
            string.IsNullOrWhiteSpace(challengeID)
        )
        {
            return "";
        }


        return
            accountID.Trim() +
            "_" +
            challengeID.Trim();
    }


    // =========================================================
    // GET CURRENT ACCOUNT ID
    // =========================================================

    private string GetCurrentAccountID()
    {
        if (AccountManager.Instance == null)
            return "";

        if (!AccountManager.Instance.IsUserLoggedIn())
            return "";

        return
            AccountManager.Instance
                .GetCurrentAccountId();
    }


    // =========================================================
    // CHECK FIREBASE
    // =========================================================

    private async Task<bool>
        CheckFirebaseReady()
    {
        if (FirebaseManager.Instance == null)
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        bool ready =
            await FirebaseManager.Instance
                .WaitUntilReady();


        if (!ready)
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        return true;
    }


    // =========================================================
    // GET STRING FROM DOCUMENT
    // =========================================================

    private string GetString(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(field)
        )
        {
            return "";
        }


        try
        {
            object value =
                document.GetValue<object>(field);

            if (value == null)
                return "";

            return value.ToString();
        }
        catch
        {
            return "";
        }
    }


    // =========================================================
    // GET STRING FROM DICTIONARY
    // =========================================================

    private string GetString(
        Dictionary<string, object> data,
        string key)
    {
        if (
            data == null ||
            !data.ContainsKey(key) ||
            data[key] == null
        )
        {
            return "";
        }


        try
        {
            return data[key].ToString();
        }
        catch
        {
            return "";
        }
    }


    // =========================================================
    // GET INT
    // =========================================================

    private int GetInt(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(field)
        )
        {
            return 0;
        }


        try
        {
            object value =
                document.GetValue<object>(field);


            if (value is int)
                return (int)value;

            if (value is long)
                return (int)(long)value;

            if (value is double)
                return (int)(double)value;

            if (value is float)
                return (int)(float)value;


            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }


    // =========================================================
    // GET BOOL
    // =========================================================

    private bool GetBool(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(field)
        )
        {
            return false;
        }


        try
        {
            object value =
                document.GetValue<object>(field);


            if (value is bool)
                return (bool)value;


            return Convert.ToBoolean(value);
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // SAFE TRIM
    // =========================================================

    private string SafeTrim(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value.Trim();
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(string message)
    {
        LastError = message;

        Debug.LogError(
            "ParticipantManager: " +
            message
        );
    }

    public void RestartCurrentChallenge()
    {
        if (CurrentParticipant == null)
            return;

        string accountID =
            GetCurrentAccountID();

        string username =
            CurrentParticipant.username;

        string participantName =
            CurrentParticipant.participantName;

        string institution =
            CurrentParticipant.institution;

        string categoryType =
            CurrentParticipant.categoryType;

        string subCategory =
            CurrentParticipant.subCategory;

        // KEEP challenge information
        string challengeID =
            CurrentParticipant.challengeID;

        string challengeTitle =
            CurrentParticipant.challengeTitle;

        string eventCode =
            CurrentParticipant.eventCode;

        string submissionID =
            CurrentParticipant.submissionID;


        // Reset ONLY challenge/design progress
        CurrentParticipant.ResetChallengeData();


        // Restore account/profile information
        CurrentParticipant.accountID =
            accountID;

        CurrentParticipant.username =
            username;

        CurrentParticipant.participantName =
            participantName;

        CurrentParticipant.institution =
            institution;

        CurrentParticipant.categoryType =
            categoryType;

        CurrentParticipant.subCategory =
            subCategory;


        // Restore challenge information
        CurrentParticipant.challengeID =
            challengeID;

        CurrentParticipant.challengeTitle =
            challengeTitle;

        CurrentParticipant.eventCode =
            eventCode;

        CurrentParticipant.submissionID =
            submissionID;


        // Start from first page
        CurrentParticipant.lastPage =
            "IdeaPrompt";


        Debug.Log(
            "ParticipantManager: Challenge restarted from Idea Prompt. " +
            "Challenge = " +
            challengeTitle
        );
    }
}