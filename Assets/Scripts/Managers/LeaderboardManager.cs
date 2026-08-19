using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }


    // =========================================================
    // FIREBASE
    // =========================================================

    private const string SUBMISSIONS_COLLECTION =
        "submissions";

    private string selectedChallengeID = "";

    private string selectedChallengeTitle = "";


    // =========================================================
    // UI
    // =========================================================

    [Header("Leaderboard UI")]

    [SerializeField]
    private GameObject leaderboardPanel;


    [SerializeField]
    private Transform leaderboardContent;


    [SerializeField]
    private GameObject participantRankCardPrefab;


    [SerializeField]
    private TMP_Text statusText;


    [SerializeField]
    private TMP_Text eventTitleText;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsLoading
    {
        get;
        private set;
    }


    public string LastError
    {
        get;
        private set;
    }


    public List<LeaderboardEntry> CurrentLeaderboard
    {
        get;
        private set;
    } =
        new List<LeaderboardEntry>();


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // OPEN LEADERBOARD
    // =========================================================

    public async void OpenLeaderboard()
    {
        if (IsLoading)
        {
            Debug.Log(
                "LeaderboardManager: Already loading leaderboard."
            );

            return;
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        await LoadLeaderboard();
    }

    public void SetChallenge(
    string challengeID,
    string challengeTitle)
    {
        selectedChallengeID =
            challengeID;

        selectedChallengeTitle =
            challengeTitle;

        Debug.Log(
            "LeaderboardManager: Selected challenge = " +
            challengeTitle +
            " (" +
            challengeID +
            ")"
        );
    }



    // =========================================================
    // LOAD LEADERBOARD
    // =========================================================

    public async Task LoadLeaderboard()
    {
        LastError = "";

        // -------------------------------------------------
        // PREVENT MULTIPLE LOADS
        // -------------------------------------------------

        if (IsLoading)
        {
            Debug.Log(
                "LeaderboardManager: Leaderboard loading already in progress."
            );

            return;
        }

        IsLoading = true;

        try
        {
            // -------------------------------------------------
            // FIREBASE
            // -------------------------------------------------

            if (FirebaseManager.Instance == null)
            {
                SetError(
                    "Firebase Manager is not available."
                );

                return;
            }

            if (!await FirebaseManager.Instance
                    .WaitUntilReady())
            {
                SetError(
                    "Firebase is not ready."
                );

                return;
            }

            // -------------------------------------------------
            // GET CHALLENGE
            // -------------------------------------------------

            string challengeID =
                selectedChallengeID;

            string challengeTitle =
                selectedChallengeTitle;

            if (string.IsNullOrWhiteSpace(
                challengeID))
            {
                if (CompetitionManager.Instance == null)
                {
                    SetError(
                        "Competition Manager is not available."
                    );

                    return;
                }

                challengeID =
                    CompetitionManager.Instance
                        .GetCurrentChallengeID();

                challengeTitle =
                    CompetitionManager.Instance
                        .GetCurrentChallengeTitle();
            }

            if (string.IsNullOrWhiteSpace(
                challengeID))
            {
                SetError(
                    "No event is currently selected."
                );

                return;
            }

            if (eventTitleText != null)
            {
                eventTitleText.text =
                    challengeTitle;
            }

            // -------------------------------------------------
            // CLEAR OLD CARDS
            // -------------------------------------------------

            ClearLeaderboard();

            SetStatus(
                "Loading leaderboard..."
            );

            // -------------------------------------------------
            // GET SUBMISSIONS
            // -------------------------------------------------

            List<DocumentSnapshot> documents =
                await FirebaseManager.Instance
                    .GetDocumentsByField(
                        SUBMISSIONS_COLLECTION,
                        "challengeID",
                        challengeID
                    );

            if (documents == null ||
                documents.Count == 0)
            {
                SetStatus(
                    "No submissions found for this event."
                );

                return;
            }

            // -------------------------------------------------
            // CONVERT
            // -------------------------------------------------

            List<LeaderboardEntry> entries =
                new List<LeaderboardEntry>();

            foreach (
                DocumentSnapshot document
                in documents)
            {
                if (!document.Exists)
                    continue;

                LeaderboardEntry entry =
                    DocumentToLeaderboardEntry(
                        document
                    );

                if (entry == null)
                    continue;

                if (!entry.isSubmitted)
                    continue;

                entries.Add(entry);
            }

            // -------------------------------------------------
            // SORT
            // -------------------------------------------------

            entries =
                entries
                    .OrderByDescending(
                        entry => entry.score
                    )
                    .ThenBy(
                        entry => entry.completedDate
                    )
                    .ToList();

            CurrentLeaderboard =
                entries;

            // -------------------------------------------------
            // CREATE
            // -------------------------------------------------

            CreateLeaderboardCards(
                entries
            );

            SetStatus(
                entries.Count +
                " participant(s) found."
            );

            Debug.Log(
                "Leaderboard loaded: " +
                entries.Count +
                " participant(s)."
            );
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to load leaderboard: " +
                exception.Message
            );
        }
        finally
        {
            IsLoading = false;
        }
    }


    // =========================================================
    // CREATE LEADERBOARD CARDS
    // =========================================================

    private void CreateLeaderboardCards(
        List<LeaderboardEntry> entries)
    {
        if (leaderboardContent == null)
        {
            SetError(
                "Leaderboard Content is not assigned."
            );

            return;
        }


        if (participantRankCardPrefab == null)
        {
            SetError(
                "Participant Rank Card prefab is not assigned."
            );

            return;
        }


        for (
            int i = 0;
            i < entries.Count;
            i++
        )
        {
            LeaderboardEntry entry =
                entries[i];


            int rank =
                i + 1;


            GameObject cardObject =
                Instantiate(
                    participantRankCardPrefab,
                    leaderboardContent
                );


            ParticipantRankCard card =
                cardObject.GetComponent<
                    ParticipantRankCard
                >();


            if (card == null)
            {
                Debug.LogError(
                    "ParticipantRankCard component is missing from the prefab."
                );


                Destroy(
                    cardObject
                );


                continue;
            }


            card.Setup(
                rank,
                entry
            );
        }
    }


    // =========================================================
    // DOCUMENT → ENTRY
    // =========================================================

    private LeaderboardEntry
        DocumentToLeaderboardEntry(
            DocumentSnapshot document)
    {
        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        LeaderboardEntry entry =
            new LeaderboardEntry();


        entry.submissionID =
            GetString(
                document,
                "submissionID"
            );


        if (string.IsNullOrWhiteSpace(
            entry.submissionID))
        {
            entry.submissionID =
                document.Id;
        }


        entry.accountID =
            GetString(
                document,
                "accountID"
            );


        entry.username =
            GetString(
                document,
                "username"
            );


        entry.participantName =
            GetString(
                document,
                "participantName"
            );


        entry.institution =
            GetString(
                document,
                "institution"
            );


        entry.categoryType =
            GetString(
                document,
                "categoryType"
            );


        entry.subCategory =
            GetString(
                document,
                "subCategory"
            );


        entry.challengeID =
            GetString(
                document,
                "challengeID"
            );


        entry.challengeTitle =
            GetString(
                document,
                "challengeTitle"
            );


        entry.completedDate =
            GetString(
                document,
                "completedDate"
            );


        entry.posterImageUrl =
            GetString(
                document,
                "posterImageUrl"
            );


        entry.score =
            GetInt(
                document,
                "score"
            );


        entry.isSubmitted =
            GetBool(
                document,
                "isSubmitted"
            );


        return entry;
    }


    // =========================================================
    // GET STRING
    // =========================================================

    private string GetString(
        DocumentSnapshot document,
        string field)
    {
        if (!document.ContainsField(field))
        {
            return "";
        }


        try
        {
            return document.GetValue<string>(
                field
            );
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
        if (!document.ContainsField(field))
        {
            return 0;
        }


        try
        {
            return document.GetValue<int>(
                field
            );
        }
        catch
        {
            try
            {
                long value =
                    document.GetValue<long>(
                        field
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
    // GET BOOL
    // =========================================================

    private bool GetBool(
        DocumentSnapshot document,
        string field)
    {
        if (!document.ContainsField(field))
        {
            return false;
        }


        try
        {
            return document.GetValue<bool>(
                field
            );
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // CLEAR LEADERBOARD
    // =========================================================

    public void ClearLeaderboard()
    {
        CurrentLeaderboard.Clear();


        if (leaderboardContent == null)
        {
            return;
        }


        for (
            int i =
                leaderboardContent.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                leaderboardContent.GetChild(i);


            Destroy(
                child.gameObject
            );
        }
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public async void RefreshLeaderboard()
    {
        await LoadLeaderboard();
    }


    // =========================================================
    // GET CURRENT RANK
    // =========================================================

    public int GetCurrentUserRank()
    {
        if (
            AccountManager.Instance == null ||
            AccountManager.Instance.CurrentAccount == null
        )
        {
            return -1;
        }


        string accountID =
            AccountManager.Instance
                .CurrentAccount
                .accountId;


        for (
            int i = 0;
            i < CurrentLeaderboard.Count;
            i++
        )
        {
            if (
                CurrentLeaderboard[i]
                    .accountID ==
                accountID
            )
            {
                return i + 1;
            }
        }


        return -1;
    }


    // =========================================================
    // GET CURRENT USER ENTRY
    // =========================================================

    public LeaderboardEntry
        GetCurrentUserEntry()
    {
        if (
            AccountManager.Instance == null ||
            AccountManager.Instance.CurrentAccount == null
        )
        {
            return null;
        }


        string accountID =
            AccountManager.Instance
                .CurrentAccount
                .accountId;


        foreach (
            LeaderboardEntry entry
            in CurrentLeaderboard
        )
        {
            if (
                entry.accountID ==
                accountID
            )
            {
                return entry;
            }
        }


        return null;
    }


    // =========================================================
    // CLOSE LEADERBOARD
    // =========================================================

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
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
            "LeaderboardManager: " +
            message
        );
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(
        string message)
    {
        LastError =
            message;


        SetStatus(
            message
        );


        Debug.LogError(
            "LeaderboardManager: " +
            message
        );
    }


    // =========================================================
    // LEADERBOARD ENTRY
    // =========================================================

    [Serializable]
    public class LeaderboardEntry
    {
        public string submissionID;

        public string accountID;

        public string username;

        public string participantName;

        public string institution;

        public string categoryType;

        public string subCategory;

        public string challengeID;

        public string challengeTitle;

        public string completedDate;

        public string posterImageUrl;

        public int score;

        public bool isSubmitted;
    }
}