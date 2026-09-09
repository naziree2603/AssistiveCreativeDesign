using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
public class SubmissionManager : MonoBehaviour
{
    public static SubmissionManager Instance { get; private set; }

    // =========================================================
    // FIREBASE
    // =========================================================

    private const string SUBMISSIONS_COLLECTION =
        "submissions";


    // =========================================================
    // UI
    // =========================================================

    [Header("Submitted Panel")]

    [SerializeField]
    private GameObject submittedPanel;

    [SerializeField]
    private Transform submittedContent;

    [SerializeField]
    private GameObject submittedCardPrefab;

    [SerializeField]
    private TMP_Text statusText;


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


    public List<SubmissionData> CurrentSubmissions
    {
        get;
        private set;
    } = new List<SubmissionData>();


    public SubmissionData SelectedSubmission
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
    // OPEN SUBMITTED PANEL
    // =========================================================

    public async void OpenSubmitted()
    {
        LastError = "";

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSubmitted();
        }
        else if (submittedPanel != null)
        {
            submittedPanel.SetActive(true);
        }

        await LoadMySubmissions();
    }


    // =========================================================
    // LOAD MY SUBMISSIONS
    // =========================================================
    //
    // Loads submissions belonging ONLY to the current account.
    //
    // Firestore:
    //
    // submissions/
    //      {accountID}_{challengeID}
    //
    // =========================================================

    public async Task LoadMySubmissions()
    {
        LastError = "";

        // -----------------------------------------------------
        // PREVENT MULTIPLE LOADS
        // -----------------------------------------------------

        if (IsLoading)
        {
            Debug.Log(
                "SubmissionManager: Submission loading already in progress."
            );

            return;
        }

        IsLoading = true;


        // -----------------------------------------------------
        // ACCOUNT MANAGER
        // -----------------------------------------------------

        if (AccountManager.Instance == null)
        {
            SetError(
                "Account Manager is not available."
            );

            IsLoading = false;
            return;
        }


        if (!AccountManager.Instance.IsUserLoggedIn())
        {
            SetError(
                "Please login first."
            );

            IsLoading = false;
            return;
        }


        // -----------------------------------------------------
        // FIREBASE
        // -----------------------------------------------------

        if (FirebaseManager.Instance == null)
        {
            SetError(
                "Firebase Manager is not available."
            );

            IsLoading = false;
            return;
        }


        if (!await FirebaseManager.Instance.WaitUntilReady())
        {
            SetError(
                "Firebase is not ready."
            );

            IsLoading = false;
            return;
        }


        // -----------------------------------------------------
        // ACCOUNT ID
        // -----------------------------------------------------

        string accountID =
            AccountManager.Instance.GetCurrentAccountId();


        if (string.IsNullOrWhiteSpace(accountID))
        {
            SetError(
                "Account ID is missing."
            );

            IsLoading = false;
            return;
        }


        try
        {
            SetStatus(
                "Loading submitted work..."
            );


            // -------------------------------------------------
            // CLEAR OLD CARDS
            // -------------------------------------------------

            ClearCards();


            // -------------------------------------------------
            // GET SUBMISSIONS
            // -------------------------------------------------

            List<DocumentSnapshot> documents =
                await FirebaseManager.Instance
                    .GetDocumentsByField(
                        SUBMISSIONS_COLLECTION,
                        "accountID",
                        accountID
                    );


            List<SubmissionData> submissions =
                new List<SubmissionData>();


            // -------------------------------------------------
            // CONVERT FIREBASE DOCUMENTS
            // -------------------------------------------------

            if (documents != null)
            {
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


                    SubmissionData submission =
                        DocumentToSubmission(
                            document
                        );


                    if (submission == null)
                    {
                        continue;
                    }


                    // -------------------------------------------------
                    // ONLY SHOW FINAL SUBMISSIONS
                    // -------------------------------------------------

                    if (!submission.isSubmitted)
                    {
                        continue;
                    }


                    // -------------------------------------------------
                    // PREVENT DUPLICATES
                    // -------------------------------------------------

                    bool duplicate =
                        submissions.Any(
                            existing =>
                                existing != null &&
                                existing.submissionID ==
                                submission.submissionID
                        );


                    if (duplicate)
                    {
                        Debug.LogWarning(
                            "SubmissionManager: Duplicate submission ignored: " +
                            submission.submissionID
                        );

                        continue;
                    }


                    submissions.Add(
                        submission
                    );
                }
            }


            // -------------------------------------------------
            // SORT NEWEST FIRST
            // -------------------------------------------------

            submissions.Sort(
                CompareSubmissionDates
            );


            CurrentSubmissions =
                submissions;


            // -------------------------------------------------
            // CREATE CARDS
            // -------------------------------------------------

            CreateSubmittedCards(
                CurrentSubmissions
            );


            // -------------------------------------------------
            // STATUS
            // -------------------------------------------------

            if (
                CurrentSubmissions.Count == 0
            )
            {
                SetStatus(
                    "No submitted work found."
                );
            }
            else
            {
                SetStatus(
                    CurrentSubmissions.Count +
                    " submitted work(s)."
                );
            }


            Debug.Log(
                "SubmissionManager: Loaded " +
                CurrentSubmissions.Count +
                " submission(s)."
            );
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to load submissions: " +
                exception.Message
            );
        }
        finally
        {
            IsLoading = false;
        }
    }


    // =========================================================
    // COMPATIBILITY
    // =========================================================

    public async Task LoadSubmissions()
    {
        await LoadMySubmissions();
    }


    // =========================================================
    // CREATE SUBMITTED CARDS
    // =========================================================

    private void CreateSubmittedCards(
    List<SubmissionData> submissions)
    {
        if (submittedContent == null)
        {
            SetError(
                "Submitted Content is not assigned."
            );

            return;
        }

        if (submittedCardPrefab == null)
        {
            SetError(
                "Submitted Card prefab is not assigned."
            );

            return;
        }

        if (submissions == null)
        {
            return;
        }

        HashSet<string> createdIDs =
            new HashSet<string>();

        foreach (
            SubmissionData submission
            in submissions)
        {
            if (submission == null)
            {
                continue;
            }

            if (
                string.IsNullOrWhiteSpace(
                    submission.submissionID
                )
            )
            {
                continue;
            }

            // -------------------------------------------------
            // EXTRA DUPLICATE PROTECTION
            // -------------------------------------------------

            if (!createdIDs.Add(
                submission.submissionID
            ))
            {
                Debug.LogWarning(
                    "SubmissionManager: Skipping duplicate card: " +
                    submission.submissionID
                );

                continue;
            }

            GameObject cardObject =
                Instantiate(
                    submittedCardPrefab,
                    submittedContent
                );

            if (cardObject == null)
            {
                continue;
            }

            SubmittedCard card =
                cardObject.GetComponent<SubmittedCard>();

            if (card == null)
            {
                Debug.LogError(
                    "SubmittedCard component is missing from the submitted card prefab."
                );

                Destroy(cardObject);

                continue;
            }

            card.Setup(
                submission
            );
        }
    }


    // =========================================================
    // SAVE CURRENT SUBMISSION
    // =========================================================
    //
    // IMPORTANT:
    //
    // ParticipantManager is now the MAIN owner of submission
    // persistence.
    //
    // We therefore DO NOT manually create another Firestore
    // document here.
    //
    // This prevents duplicate / recursive saving.
    //
    // =========================================================

    public async Task<bool>
        SaveCurrentSubmission()
    {
        LastError = "";


        if (IsSaving)
        {
            return false;
        }


        if (ParticipantManager.Instance == null)
        {
            SetError(
                "Participant Manager is not available."
            );

            return false;
        }


        ParticipantData participant =
            ParticipantManager.Instance
                .CurrentParticipant;


        if (participant == null)
        {
            SetError(
                "Participant data is not available."
            );

            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                participant.challengeID
            )
        )
        {
            SetError(
                "No challenge is currently selected."
            );

            return false;
        }


        IsSaving = true;


        try
        {
            bool saved =
                await ParticipantManager.Instance
                    .SaveCurrentSubmission();


            if (!saved)
            {
                SetError(
                    ParticipantManager.Instance
                        .LastError
                );


                return false;
            }


            Debug.Log(
                "SubmissionManager: Current submission saved successfully."
            );


            return true;
        }
        catch (Exception exception)
        {
            SetError(
                "Submission save failed: " +
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
    // SAVE COMPLETED / FINAL SUBMISSION
    // =========================================================
    //
    // This method performs the FINAL submission.
    //
    // ParticipantManager handles:
    //
    // 1. Validation
    // 2. isSubmitted = true
    // 3. Firestore save
    // 4. Rollback if saving fails
    //
    // =========================================================

    public async Task<bool>
        SaveCompletedSubmission()
    {
        LastError = "";


        if (IsSaving)
        {
            return false;
        }


        if (ParticipantManager.Instance == null)
        {
            SetError(
                "Participant Manager is not available."
            );

            return false;
        }


        IsSaving = true;


        try
        {
            bool submitted =
                await ParticipantManager.Instance
                    .SubmitCurrentChallenge();


            if (!submitted)
            {
                SetError(
                    ParticipantManager.Instance
                        .LastError
                );


                return false;
            }


            Debug.Log(
                "SubmissionManager: Final challenge submitted successfully."
            );


            return true;
        }
        catch (Exception exception)
        {
            SetError(
                "Final submission failed: " +
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
    // FINAL SUBMISSION
    // =========================================================
    //
    // Compatibility alias.
    //
    // Existing scripts may call:
    //
    //     Submit()
    //
    // =========================================================

    public async Task<bool>
        Submit()
    {
        return await SaveCompletedSubmission();
    }


    // =========================================================
    // OPEN SUBMISSION
    // =========================================================
    //
    // Used by SubmittedCard.
    //
    // =========================================================

    public async void OpenSubmission(
    SubmissionData submission)
    {
        if (submission == null)
        {
            Debug.LogWarning(
                "SubmissionManager: Submission is null."
            );

            return;
        }

        if (ParticipantManager.Instance == null)
        {
            Debug.LogError(
                "SubmissionManager: ParticipantManager is not available."
            );

            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError(
                "SubmissionManager: UIManager is not available."
            );

            return;
        }

        SelectedSubmission =
            submission;

        Debug.Log(
            "SubmissionManager: Opening submitted design: " +
            submission.submissionID
        );


        // -----------------------------------------------------
        // LOAD SUBMISSION
        // -----------------------------------------------------

        ParticipantManager.Instance
            .LoadSubmissionData(
                submission
            );


        // -----------------------------------------------------
        // SET EVENT CONTEXT
        // -----------------------------------------------------

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SetChallenge(
                submission.challengeID,
                submission.challengeTitle
            );
        }


        // -----------------------------------------------------
        // READ-ONLY MODE
        // -----------------------------------------------------

        if (DesignManager.Instance != null)
        {
            DesignManager.Instance
                .SetSubmittedViewMode(true);

            // Restore the poster from local storage or the
            // Firestore poster chunks before opening the score
            // page. This makes submitted designs viewable on
            // another device using the same account.
            await DesignManager.Instance
                .RestoreCurrentSubmissionPosterAsync();
        }


        // -----------------------------------------------------
        // OPEN WORKSPACE
        // -----------------------------------------------------

        UIManager.Instance
            .ShowDesignWorkspace();


        // -----------------------------------------------------
        // START AT SCORE
        // -----------------------------------------------------

        if (DesignManager.Instance != null)
        {
            DesignManager.Instance
                .OpenScore();
        }
    }


    // =========================================================
    // GET SELECTED SUBMISSION
    // =========================================================

    public SubmissionData
        GetSelectedSubmission()
    {
        return SelectedSubmission;
    }


    // =========================================================
    // CLEAR SELECTED SUBMISSION
    // =========================================================

    public void ClearSelectedSubmission()
    {
        SelectedSubmission =
            null;
    }




    // =========================================================
    // FIND SUBMISSION FOR CHALLENGE
    // =========================================================
    //
    // Useful for challenge buttons.
    //
    // =========================================================

    public async Task<SubmissionData>
        GetSubmissionForChallenge(
            string challengeID)
    {
        LastError = "";


        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return null;
        }


        if (AccountManager.Instance == null)
        {
            return null;
        }


        if (
            !AccountManager.Instance
                .IsUserLoggedIn()
        )
        {
            return null;
        }


        string accountID =
            AccountManager.Instance
                .GetCurrentAccountId();


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            return null;
        }


        if (FirebaseManager.Instance == null)
        {
            return null;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            return null;
        }


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
            {
                return null;
            }


            DocumentSnapshot document =
                await db.Collection(
                    SUBMISSIONS_COLLECTION
                )
                .Document(
                    submissionID
                )
                .GetSnapshotAsync();


            if (
                document == null ||
                !document.Exists
            )
            {
                return null;
            }


            return DocumentToSubmission(
                document
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "SubmissionManager: Failed to get challenge submission: " +
                exception.Message
            );


            return null;
        }
    }


    // =========================================================
    // CHECK SUBMITTED
    // =========================================================

    public async Task<bool>
        HasSubmittedChallenge(
            string challengeID)
    {
        SubmissionData submission =
            await GetSubmissionForChallenge(
                challengeID
            );


        if (submission == null)
        {
            return false;
        }


        return submission.isSubmitted;
    }


    // =========================================================
    // DOCUMENT → SUBMISSION
    // =========================================================

    private SubmissionData
        DocumentToSubmission(
            DocumentSnapshot document)
    {
        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        SubmissionData submission =
            new SubmissionData();


        // -----------------------------------------------------
        // IDENTIFICATION
        // -----------------------------------------------------

        submission.submissionID =
            GetString(
                document,
                "submissionID"
            );


        if (
            string.IsNullOrWhiteSpace(
                submission.submissionID
            )
        )
        {
            submission.submissionID =
                document.Id;
        }


        submission.accountID =
            GetString(
                document,
                "accountID"
            );


        submission.username =
            GetString(
                document,
                "username"
            );


        // -----------------------------------------------------
        // PARTICIPANT
        // -----------------------------------------------------

        submission.participantName =
            GetString(
                document,
                "participantName"
            );


        submission.institution =
            GetString(
                document,
                "institution"
            );


        submission.categoryType =
            GetString(
                document,
                "categoryType"
            );


        submission.subCategory =
            GetString(
                document,
                "subCategory"
            );


        // -----------------------------------------------------
        // CHALLENGE
        // -----------------------------------------------------

        submission.challengeID =
            GetString(
                document,
                "challengeID"
            );


        submission.challengeTitle =
            GetString(
                document,
                "challengeTitle"
            );


        submission.eventCode =
            GetString(
                document,
                "eventCode"
            );


        // -----------------------------------------------------
        // DESIGN
        // -----------------------------------------------------

        submission.prompt =
            GetString(
                document,
                "prompt"
            );


        submission.promptUsed =
            GetString(
                document,
                "promptUsed"
            );


        submission.posterDescription =
            GetString(
                document,
                "posterDescription"
            );


        submission.revisionPrompt =
            GetString(
                document,
                "revisionPrompt"
            );


        submission.revisionHistory =
            GetString(
                document,
                "revisionHistory"
            );


        submission.storagePath =
            GetString(
                document,
                "storagePath"
            );


        submission.revisionCount =
            GetInt(
                document,
                "revisionCount"
            );


        submission.finalExplanation =
            GetString(
                document,
                "finalExplanation"
            );


        // -----------------------------------------------------
        // IMAGES
        // -----------------------------------------------------

        submission.originalImageUrl =
            GetString(
                document,
                "originalImageUrl"
            );


        submission.revisedImageUrl =
            GetString(
                document,
                "revisedImageUrl"
            );


        submission.posterImageUrl =
            GetString(
                document,
                "posterImageUrl"
            );


        // -----------------------------------------------------
        // SCORE
        // -----------------------------------------------------

        submission.score =
            GetInt(
                document,
                "score"
            );


        submission.promptQuality =
            GetInt(
                document,
                "promptQuality"
            );


        submission.posterMessage =
            GetInt(
                document,
                "posterMessage"
            );


        submission.designQuality =
            GetInt(
                document,
                "designQuality"
            );


        submission.accessibilityUnderstanding =
            GetInt(
                document,
                "accessibilityUnderstanding"
            );


        submission.revisionProcessScore =
            GetInt(
                document,
                "revisionProcessScore"
            );


        submission.finalExplanationScore =
            GetInt(
                document,
                "finalExplanationScore"
            );


        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        submission.feedback =
            GetString(
                document,
                "feedback"
            );


        submission.improvementSuggestion =
            GetString(
                document,
                "improvementSuggestion"
            );


        // -----------------------------------------------------
        // STATUS
        // -----------------------------------------------------

        submission.isSubmitted =
            GetBool(
                document,
                "isSubmitted"
            );


        // -----------------------------------------------------
        // COMPATIBILITY
        // -----------------------------------------------------

        submission.isCompleted =
            submission.isSubmitted;


        // -----------------------------------------------------
        // DATE
        // -----------------------------------------------------

        submission.completedDate =
            GetString(
                document,
                "completedDate"
            );


        // -----------------------------------------------------
        // LAST PAGE
        // -----------------------------------------------------

        submission.lastPage =
            GetString(
                document,
                "lastPage"
            );


        return submission;
    }


    // =========================================================
    // BUILD SUBMISSION ID
    // =========================================================
    //
    // One account + one challenge = one submission.
    //
    // Example:
    //
    // abc123_challenge001
    //
    // =========================================================

    private string BuildSubmissionID(
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
            accountID.Trim() +
            "_" +
            challengeID.Trim();
    }


    // =========================================================
    // SORT SUBMISSIONS
    // =========================================================

    private int CompareSubmissionDates(
        SubmissionData a,
        SubmissionData b)
    {
        if (
            a == null &&
            b == null
        )
        {
            return 0;
        }


        if (a == null)
        {
            return 1;
        }


        if (b == null)
        {
            return -1;
        }


        DateTime dateA;
        DateTime dateB;


        bool validA =
            DateTime.TryParse(
                a.completedDate,
                out dateA
            );


        bool validB =
            DateTime.TryParse(
                b.completedDate,
                out dateB
            );


        if (
            !validA &&
            !validB
        )
        {
            return 0;
        }


        if (!validA)
        {
            return 1;
        }


        if (!validB)
        {
            return -1;
        }


        return dateB.CompareTo(
            dateA
        );
    }


    // =========================================================
    // GET STRING
    // =========================================================

    private string GetString(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(
                field
            )
        )
        {
            return "";
        }


        try
        {
            object value =
                document.GetValue<object>(
                    field
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
    // GET INT
    // =========================================================

    private int GetInt(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(
                field
            )
        )
        {
            return 0;
        }


        try
        {
            object value =
                document.GetValue<object>(
                    field
                );


            if (value is int)
            {
                return (int)value;
            }


            if (value is long)
            {
                return (int)(long)value;
            }


            if (value is double)
            {
                return (int)(double)value;
            }


            if (value is float)
            {
                return (int)(float)value;
            }


            return Convert.ToInt32(
                value
            );
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
            !document.ContainsField(
                field
            )
        )
        {
            return false;
        }


        try
        {
            object value =
                document.GetValue<object>(
                    field
                );


            if (value is bool)
            {
                return (bool)value;
            }


            return Convert.ToBoolean(
                value
            );
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // CLEAR CARDS
    // =========================================================

    public void ClearCards()
    {
        CurrentSubmissions.Clear();


        if (submittedContent == null)
        {
            return;
        }


        for (
            int i =
                submittedContent.childCount - 1;

            i >= 0;

            i--
        )
        {
            Transform child =
                submittedContent.GetChild(
                    i
                );


            if (child != null)
            {
                Destroy(
                    child.gameObject
                );
            }
        }
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void Clear()
    {
        ClearCards();


        SelectedSubmission =
            null;


        IsLoading =
            false;


        IsSaving =
            false;


        LastError =
            "";
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public async void RefreshSubmissions()
    {
        await LoadMySubmissions();
    }


    // =========================================================
    // CLOSE SUBMITTED PANEL
    // =========================================================

    public void CloseSubmittedPanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();

            return;
        }


        if (submittedPanel != null)
        {
            submittedPanel.SetActive(false);
        }
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(
        string message)
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }


        Debug.Log(
            "SubmissionManager: " +
            message
        );
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(
        string message)
    {
        if (
            string.IsNullOrWhiteSpace(
                message
            )
        )
        {
            message =
                "Unknown submission error.";
        }


        LastError =
            message;


        SetStatus(
            message
        );


        Debug.LogError(
            "SubmissionManager: " +
            message
        );
    }


    // =========================================================
    // SUBMISSION DATA
    // =========================================================

    [Serializable]
    public class SubmissionData
    {
        // -----------------------------------------------------
        // IDENTIFICATION
        // -----------------------------------------------------

        public string submissionID;

        public string accountID;

        public string username;


        // -----------------------------------------------------
        // PARTICIPANT
        // -----------------------------------------------------

        public string participantName;

        public string institution;

        public string categoryType;

        public string subCategory;


        // -----------------------------------------------------
        // CHALLENGE
        // -----------------------------------------------------

        public string challengeID;

        public string challengeTitle;

        public string eventCode;


        // -----------------------------------------------------
        // DESIGN
        // -----------------------------------------------------

        public string prompt;

        public string promptUsed;

        public string posterDescription;

        public string revisionPrompt;

        public string revisionHistory;

        public int revisionCount;

        public string finalExplanation;

        public string storagePath;


        // -----------------------------------------------------
        // IMAGES
        // -----------------------------------------------------

        public string originalImageUrl;

        public string revisedImageUrl;

        public string posterImageUrl;


        // -----------------------------------------------------
        // SCORE
        // -----------------------------------------------------

        public int score;

        public int promptQuality;

        public int posterMessage;

        public int designQuality;

        public int accessibilityUnderstanding;

        public int revisionProcessScore;

        public int finalExplanationScore;


        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        public string feedback;

        public string improvementSuggestion;


        // -----------------------------------------------------
        // STATUS
        // -----------------------------------------------------

        public bool isSubmitted;


        // -----------------------------------------------------
        // OLD COMPATIBILITY
        // -----------------------------------------------------
        //
        // Some old scripts may still use:
        //
        // submission.isCompleted
        //
        // Keep this field so those scripts don't immediately
        // break.
        //
        // New system should use isSubmitted.
        //
        // -----------------------------------------------------

        public bool isCompleted;


        // -----------------------------------------------------
        // DATE
        // -----------------------------------------------------

        public string completedDate;


        // -----------------------------------------------------
        // LAST PAGE
        // -----------------------------------------------------

        public string lastPage;
    }
}