using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionManager : MonoBehaviour
{
    public static CompetitionManager Instance { get; private set; }


    // =========================================================
    // FIREBASE
    // =========================================================

    private const string CHALLENGES_COLLECTION =
        "challenges";


    // =========================================================
    // CURRENT CHALLENGE
    // =========================================================

    public ChallengeData CurrentChallenge
    {
        get;
        private set;
    }


    // =========================================================
    // PARTICIPANT ENTRY MODE
    // =========================================================

    public enum ParticipantEntryMode
    {
        MainDashboard,

        ChallengeJoin
    }


    public ParticipantEntryMode CurrentParticipantEntryMode
    {
        get;
        set;
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsEventJoined
    {
        get;
        private set;
    }


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


    // =========================================================
    // UI
    // =========================================================

    [Header("Challenge UI")]

    [SerializeField]
    private TMP_Dropdown challengeDropdown;


    [SerializeField]
    private TMP_InputField eventCodeInput;


    [SerializeField]
    private Button joinButton;


    [SerializeField]
    private TMP_Text statusText;


    // =========================================================
    // CHALLENGE LIST
    // =========================================================

    private List<ChallengeData>
        availableChallenges =
        new List<ChallengeData>();


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


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );


        CurrentParticipantEntryMode =
            ParticipantEntryMode.MainDashboard;
    }


    // =========================================================
    // START
    // =========================================================

    private async void Start()
    {
        await LoadChallenges();
    }


    // =========================================================
    // LOAD CHALLENGES
    // =========================================================

    public async Task<bool> LoadChallenges()
    {
        LastError = "";


        if (IsLoading)
        {
            return false;
        }


        if (
            FirebaseManager.Instance == null
        )
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        IsLoading = true;


        try
        {
            List<DocumentSnapshot>
                documents =
                await FirebaseManager.Instance
                    .GetAllDocuments(
                        CHALLENGES_COLLECTION
                    );


            availableChallenges.Clear();


            if (documents != null)
            {
                foreach (
                    DocumentSnapshot document
                    in documents
                )
                {
                    if (
                        document == null ||
                        !document.Exists
                    )
                    {
                        continue;
                    }


                    ChallengeData challenge =
                        DocumentToChallenge(
                            document
                        );


                    if (challenge == null)
                    {
                        continue;
                    }


                    if (!challenge.isActive)
                    {
                        continue;
                    }


                    availableChallenges.Add(
                        challenge
                    );
                }
            }


            PopulateDropdown();


            Debug.Log(
                "CompetitionManager: Loaded " +
                availableChallenges.Count +
                " active challenge(s)."
            );


            return true;
        }
        catch (Exception exception)
        {
            SetError(
                "Failed to load challenges: " +
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
    // OPEN COMPETITION
    // =========================================================

    public async void OpenCompetition()
    {
        LastError = "";


        CurrentParticipantEntryMode =
            ParticipantEntryMode.ChallengeJoin;


        if (
            availableChallenges.Count == 0
        )
        {
            await LoadChallenges();
        }


        if (
            CurrentChallenge == null &&
            availableChallenges.Count > 0
        )
        {
            SelectChallenge(0);
        }
    }


    // =========================================================
    // OPEN PARTICIPANT FROM MAIN DASHBOARD
    // =========================================================

    public void OpenParticipantFromMainDashboard()
    {
        CurrentParticipantEntryMode =
            ParticipantEntryMode.MainDashboard;


        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowParticipant();
        }


        Debug.Log(
            "CompetitionManager: Participant opened from Main Dashboard."
        );
    }


    // =========================================================
    // OPEN PARTICIPANT FROM CHALLENGE
    // =========================================================

    public void OpenParticipantFromChallenge()
    {
        CurrentParticipantEntryMode =
            ParticipantEntryMode.ChallengeJoin;


        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowParticipant();
        }
    }


    // =========================================================
    // POPULATE DROPDOWN
    // =========================================================

    private void PopulateDropdown()
    {
        if (
            challengeDropdown == null
        )
        {
            Debug.LogWarning(
                "CompetitionManager: Challenge Dropdown is not assigned."
            );

            return;
        }


        challengeDropdown.ClearOptions();


        List<string> options =
            new List<string>();


        foreach (
            ChallengeData challenge
            in availableChallenges
        )
        {
            if (challenge == null)
            {
                continue;
            }


            options.Add(
                GetChallengeDisplayTitle(
                    challenge
                )
            );
        }


        if (options.Count == 0)
        {
            return;
        }


        challengeDropdown.AddOptions(
            options
        );


        challengeDropdown.value =
            0;


        challengeDropdown.RefreshShownValue();


        SelectChallenge(0);
    }


    // =========================================================
    // DROPDOWN CHANGED
    // =========================================================

    public void OnChallengeDropdownChanged(
        int index)
    {
        SelectChallenge(index);
    }


    // =========================================================
    // SELECT CHALLENGE
    // =========================================================

    public bool SelectChallenge(
        int index)
    {
        if (
            index < 0 ||
            index >= availableChallenges.Count
        )
        {
            SetError(
                "Invalid challenge selection."
            );

            return false;
        }


        ChallengeData challenge =
            availableChallenges[index];


        if (challenge == null)
        {
            SetError(
                "Selected challenge is unavailable."
            );

            return false;
        }


        CurrentChallenge =
            challenge;


        IsEventJoined =
            false;


        if (
            eventCodeInput != null
        )
        {
            eventCodeInput.text =
                "";
        }


        SetStatus(
            "Selected: " +
            GetChallengeDisplayTitle(
                challenge
            )
        );


        Debug.Log(
            "CompetitionManager: Selected challenge = " +
            challenge.challengeID
        );


        return true;
    }


    // =========================================================
    // SELECT CHALLENGE BY ID
    // =========================================================

    public bool SelectChallengeByID(
        string challengeID)
    {
        if (
            string.IsNullOrWhiteSpace(
                challengeID
            )
        )
        {
            return false;
        }


        for (
            int i = 0;
            i < availableChallenges.Count;
            i++
        )
        {
            ChallengeData challenge =
                availableChallenges[i];


            if (challenge == null)
            {
                continue;
            }


            if (
                string.Equals(
                    challenge.challengeID,
                    challengeID,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                CurrentChallenge =
                    challenge;


                IsEventJoined =
                    false;


                if (
                    challengeDropdown != null
                )
                {
                    challengeDropdown.value =
                        i;


                    challengeDropdown.RefreshShownValue();
                }


                if (
                    eventCodeInput != null
                )
                {
                    eventCodeInput.text =
                        "";
                }


                SetStatus(
                    "Selected: " +
                    GetChallengeDisplayTitle(
                        challenge
                    )
                );


                return true;
            }
        }


        SetError(
            "Challenge not found."
        );


        return false;
    }


    // =========================================================
    // JOIN EVENT
    // =========================================================
    //
    // FLOW:
    //
    // Challenge
    //     ↓
    // Event Code
    //     ↓
    // Verify Account
    //     ↓
    // Check Existing Submission
    //     ↓
    // If Submitted → BLOCK
    // If Unfinished → CONTINUE
    // If New → CREATE
    //
    // =========================================================

    public async void JoinEvent()
    {
        if (IsLoading)
        {
            return;
        }


        LastError = "";


        CurrentParticipantEntryMode =
            ParticipantEntryMode.ChallengeJoin;


        // -----------------------------------------------------
        // CHECK ACCOUNT
        // -----------------------------------------------------

        if (
            AccountManager.Instance == null
        )
        {
            SetError(
                "Account Manager is not available."
            );

            return;
        }


        if (
            !AccountManager.Instance
                .IsUserLoggedIn()
        )
        {
            SetError(
                "Please login first."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK CHALLENGE
        // -----------------------------------------------------

        if (
            CurrentChallenge == null
        )
        {
            if (
                challengeDropdown == null ||
                !SelectChallenge(
                    challengeDropdown.value
                )
            )
            {
                SetError(
                    "Please select an event."
                );

                return;
            }
        }


        // -----------------------------------------------------
        // CHECK ACTIVE
        // -----------------------------------------------------

        if (
            !CurrentChallenge.isActive
        )
        {
            SetError(
                "This event is not currently active."
            );

            return;
        }


        // -----------------------------------------------------
        // GET EVENT CODE
        // -----------------------------------------------------

        string enteredCode =
            eventCodeInput != null
                ? eventCodeInput.text.Trim()
                : "";


        if (
            string.IsNullOrWhiteSpace(
                enteredCode
            )
        )
        {
            SetError(
                "Please enter the event code."
            );

            return;
        }


        // -----------------------------------------------------
        // VERIFY EVENT CODE
        // -----------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                CurrentChallenge.eventCode
            )
        )
        {
            if (
                !string.Equals(
                    enteredCode,
                    CurrentChallenge.eventCode,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                SetError(
                    "Invalid event code."
                );


                if (
                    UIManager.Instance != null
                )
                {
                    UIManager.Instance
                        .ShowChallenge();
                }


                return;
            }
        }


        // -----------------------------------------------------
        // START LOADING
        // -----------------------------------------------------

        IsLoading =
            true;


        if (
            joinButton != null
        )
        {
            joinButton.interactable =
                false;
        }


        try
        {
            string accountID =
                AccountManager.Instance
                    .GetCurrentAccountId();


            if (
                string.IsNullOrWhiteSpace(
                    accountID
                )
            )
            {
                SetError(
                    "Account ID is missing."
                );

                return;
            }


            // -------------------------------------------------
            // CHECK EXISTING SUBMISSION
            // -------------------------------------------------

            ParticipantData existingSubmission =
                await ParticipantManager.Instance
                    .LoadSubmissionForChallenge(
                        accountID,
                        CurrentChallenge.challengeID
                    );


            bool hasExistingSubmission =
                existingSubmission != null;


            // -------------------------------------------------
            // ALREADY SUBMITTED
            // -------------------------------------------------

            if (
                hasExistingSubmission &&
                existingSubmission.isSubmitted
            )
            {
                SetError(
                    "You have already submitted this challenge. Only one attempt is allowed."
                );


                Debug.LogWarning(
                    "CompetitionManager: Challenge already submitted: " +
                    CurrentChallenge.challengeID
                );


                return;
            }


            // -------------------------------------------------
            // START CHALLENGE
            // -------------------------------------------------
            //
            // ParticipantManager handles:
            //
            // New challenge
            // Existing unfinished challenge
            //
            // -------------------------------------------------

            bool challengeStarted =
                await ParticipantManager.Instance
                    .StartChallenge(
                        CurrentChallenge.challengeID,
                        CurrentChallenge.title,
                        CurrentChallenge.eventCode
                    );




            if (!challengeStarted)
            {
                SetError(
                    ParticipantManager.Instance
                        .LastError
                );


                if (
                    string.IsNullOrWhiteSpace(
                        LastError
                    )
                )
                {
                    SetError(
                        "Unable to start challenge."
                    );
                }


                return;
            }

            // =====================================================
            // LEAVE PRACTICE MODE
            // =====================================================

            if (PracticeManager.Instance != null &&
                PracticeManager.Instance.IsPracticeMode)
            {
                PracticeManager.Instance.ClearPractice();
            }

            // =====================================================
            // FORCE COMPETITION MODE
            // =====================================================

            if (DesignManager.Instance != null)
            {
                DesignManager.Instance.SetDesignMode(
                    DesignMode.Competition
                );
            }

            // -------------------------------------------------
            // SWITCH FROM PRACTICE → COMPETITION
            // -------------------------------------------------

            if (
                PracticeManager.Instance != null &&
                PracticeManager.Instance.IsPracticeMode
            )
            {
                PracticeManager.Instance.ClearPractice();

                Debug.Log(
                    "CompetitionManager: Practice mode cleared."
                );
            }

            if (
                DesignManager.Instance != null
            )
            {
                DesignManager.Instance.SetDesignMode(
                    DesignMode.Competition
                );

                Debug.Log(
                    "CompetitionManager: Switched to Competition Mode."
                );
            }


            // -------------------------------------------------
            // CHECK PARTICIPANT DETAILS
            // -------------------------------------------------

            bool hasParticipantDetails =
                ParticipantManager.Instance
                    .HasParticipantDetails();


            // -------------------------------------------------
            // DETERMINE NEW / EXISTING
            // -------------------------------------------------

            bool isNewChallenge =
                !hasExistingSubmission;


            Debug.Log(
                "CompetitionManager: Challenge started."
            );


            Debug.Log(
                "CompetitionManager: Existing submission = " +
                hasExistingSubmission
            );


            Debug.Log(
                "CompetitionManager: Participant details = " +
                hasParticipantDetails
            );


            // -------------------------------------------------
            // SAVE CURRENT STATE
            // -------------------------------------------------

            bool saved =
                await SaveChallengeToParticipant();


            if (!saved)
            {
                SetError(
                    "Unable to save challenge information."
                );

                return;
            }


            // -------------------------------------------------
            // EVENT JOINED
            // -------------------------------------------------

            IsEventJoined =
                true;


            Debug.Log(
                "CompetitionManager: Event joined successfully."
            );


            // -------------------------------------------------
            // NEXT STEP
            // -------------------------------------------------

            await HandleSuccessfulJoin(
                hasParticipantDetails,
                isNewChallenge
            );
        }
        catch (
            Exception exception
        )
        {
            SetError(
                "Failed to join event: " +
                exception.Message
            );


            Debug.LogError(
                "CompetitionManager: JoinEvent exception: " +
                exception
            );
        }
        finally
        {
            IsLoading =
                false;


            if (
                joinButton != null
            )
            {
                joinButton.interactable =
                    true;
            }
        }
    }


    // =========================================================
    // LOAD CURRENT PARTICIPANT
    // =========================================================
    //
    // Compatibility helper.
    //
    // The old code used:
    //
    //     LoadForAccount()
    //
    // That method no longer exists.
    //
    // Current ParticipantManager uses:
    //
    //     LoadProfile()
    //
    // =========================================================

    private async Task<bool>
        LoadCurrentParticipant()
    {
        if (
            ParticipantManager.Instance == null
        )
        {
            Debug.LogWarning(
                "CompetitionManager: ParticipantManager is not available."
            );

            return false;
        }


        if (
            AccountManager.Instance == null
        )
        {
            Debug.LogWarning(
                "CompetitionManager: AccountManager is not available."
            );

            return false;
        }


        if (
            !AccountManager.Instance
                .IsUserLoggedIn()
        )
        {
            SetError(
                "Please login first."
            );

            return false;
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
            SetError(
                "Account ID is missing."
            );

            return false;
        }


        return await ParticipantManager.Instance
            .LoadProfile(
                accountID
            );
    }


    // =========================================================
    // CHECK DIFFERENT CHALLENGE
    // =========================================================

    private bool IsDifferentChallenge()
    {
        if (
            ParticipantManager.Instance == null
        )
        {
            return true;
        }


        ParticipantData participant =
            ParticipantManager.Instance
                .CurrentParticipant;


        if (participant == null)
        {
            return true;
        }


        if (
            string.IsNullOrWhiteSpace(
                participant.challengeID
            )
        )
        {
            return true;
        }


        if (
            CurrentChallenge == null
        )
        {
            return true;
        }


        bool sameChallenge =
            string.Equals(
                participant.challengeID,
                CurrentChallenge.challengeID,
                StringComparison.OrdinalIgnoreCase
            );


        return !sameChallenge;
    }


    // =========================================================
    // HANDLE SUCCESSFUL JOIN
    // =========================================================

    private async Task HandleSuccessfulJoin(
    bool hasParticipantDetails,
    bool isNewChallenge)
    {
        if (ParticipantManager.Instance == null)
        {
            Debug.LogWarning(
                "CompetitionManager: ParticipantManager is not available."
            );

            if (UIManager.Instance != null)
            {
                UIManager.Instance
                    .OpenParticipantFromChallenge();
            }

            return;
        }


        // =========================================================
        // NEW CHALLENGE WITHOUT PARTICIPANT DETAILS
        // =========================================================

        if (!hasParticipantDetails)
        {
            Debug.Log(
                "CompetitionManager: Participant details do not exist."
            );

            SetStatus(
                "Please enter your participant details."
            );

            if (UIManager.Instance != null)
            {
                UIManager.Instance
                    .OpenParticipantFromChallenge();
            }

            return;
        }


        // =========================================================
        // NEW CHALLENGE WITH PARTICIPANT DETAILS
        // =========================================================

        if (isNewChallenge)
        {
            Debug.Log(
                "CompetitionManager: New challenge ready."
            );

            SetStatus(
                "New challenge ready."
            );


            // -----------------------------------------------------
            // RESET DESIGN WORKSPACE
            // -----------------------------------------------------

            if (DesignManager.Instance != null)
            {
                DesignManager.Instance
                    .PrepareForNewChallenge();

                Debug.Log(
                    "CompetitionManager: DesignManager prepared for new challenge."
                );
            }


            // -----------------------------------------------------
            // OPEN IDEA PROMPT
            // -----------------------------------------------------

            if (UIManager.Instance != null)
            {
                UIManager.Instance
                    .OpenIdeaPrompt();
            }

            return;
        }


        // =========================================================
        // EXISTING UNFINISHED CHALLENGE
        // =========================================================

        Debug.Log(
            "CompetitionManager: Existing unfinished progress found."
        );

        SetStatus(
            "Existing participant progress found."
        );


        // ---------------------------------------------------------
        // SHOW CONTINUE POPUP
        // ---------------------------------------------------------

        if (DesignManager.Instance != null)
        {
            DesignManager.Instance
                .ShowContinueChallengePopup();
        }

        await Task.CompletedTask;
    }


    // =========================================================
    // SAVE CHALLENGE TO PARTICIPANT
    // =========================================================

    private async Task<bool>
    SaveChallengeToParticipant()
    {
        if (
            ParticipantManager.Instance == null
        )
        {
            Debug.LogWarning(
                "CompetitionManager: ParticipantManager is not available."
            );

            return false;
        }


        if (
            CurrentChallenge == null
        )
        {
            Debug.LogWarning(
                "CompetitionManager: Current challenge is null."
            );

            return false;
        }


        // ---------------------------------------------------------
        // SET CURRENT CHALLENGE
        // ---------------------------------------------------------

        ParticipantManager.Instance
            .SetChallenge(
                CurrentChallenge.challengeID,
                CurrentChallenge.title,
                CurrentChallenge.eventCode
            );


        // ---------------------------------------------------------
        // SAVE CHALLENGE SUBMISSION ONLY
        // ---------------------------------------------------------
        //
        // IMPORTANT:
        //
        // Do NOT call ParticipantManager.Save() here.
        //
        // Save() may involve profile saving/validation.
        //
        // At this stage a NEW participant may not have
        // participant details yet.
        //
        // ---------------------------------------------------------

        bool saved =
            await ParticipantManager.Instance
                .SaveCurrentSubmission();


        if (!saved)
        {
            Debug.LogWarning(
                "CompetitionManager: Failed to save challenge."
            );


            Debug.LogWarning(
                "ParticipantManager error: " +
                ParticipantManager.Instance.LastError
            );


            return false;
        }


        Debug.Log(
            "CompetitionManager: Challenge saved successfully."
        );


        return true;
    }


    // =========================================================
    // GET CHALLENGE DISPLAY TITLE
    // =========================================================

    private string GetChallengeDisplayTitle(
        ChallengeData challenge)
    {
        if (
            challenge == null
        )
        {
            return "";
        }


        if (
            string.IsNullOrWhiteSpace(
                challenge.title
            )
        )
        {
            return challenge.challengeID;
        }


        return challenge.title;
    }


    // =========================================================
    // IS EVENT SELECTED
    // =========================================================

    public bool IsEventSelected()
    {
        return
            CurrentChallenge != null &&
            !string.IsNullOrWhiteSpace(
                CurrentChallenge.challengeID
            );
    }


    // =========================================================
    // HAS JOINED EVENT
    // =========================================================

    public bool HasJoinedEvent()
    {
        return IsEventJoined;
    }


    // =========================================================
    // GET CURRENT CHALLENGE ID
    // =========================================================

    public string GetCurrentChallengeID()
    {
        if (
            CurrentChallenge == null
        )
        {
            return "";
        }


        return CurrentChallenge.challengeID;
    }


    // =========================================================
    // GET CURRENT CHALLENGE TITLE
    // =========================================================

    public string GetCurrentChallengeTitle()
    {
        if (
            CurrentChallenge == null
        )
        {
            return "";
        }


        return CurrentChallenge.title;
    }


    // =========================================================
    // GET CURRENT EVENT CODE
    // =========================================================

    public string GetCurrentEventCode()
    {
        if (
            CurrentChallenge == null
        )
        {
            return "";
        }


        return CurrentChallenge.eventCode;
    }


    // =========================================================
    // GET CURRENT CHALLENGE
    // =========================================================

    public ChallengeData GetCurrentChallenge()
    {
        return CurrentChallenge;
    }


    // =========================================================
    // GET AVAILABLE CHALLENGES
    // =========================================================

    public List<ChallengeData>
        GetAvailableChallenges()
    {
        return new List<ChallengeData>(
            availableChallenges
        );
    }


    // =========================================================
    // CLOSE COMPETITION
    // =========================================================

    public void CloseCompetition()
    {
        // ---------------------------------------------------------
        // CLEAR LOCAL CHALLENGE STATE
        // ---------------------------------------------------------

        if (ParticipantManager.Instance != null)
        {
            ParticipantManager.Instance
                .ClearCurrentChallenge();
        }


        // ---------------------------------------------------------
        // CLEAR COMPETITION STATE
        // ---------------------------------------------------------

        ClearCurrentChallenge();


        // ---------------------------------------------------------
        // GO DIRECTLY TO MAIN DASHBOARD
        // ---------------------------------------------------------

        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .ShowMainMenu();
        }


        Debug.Log(
            "CompetitionManager: Challenge closed. " +
            "Returned to Main Dashboard."
        );
    }


    // =========================================================
    // CLEAR CURRENT CHALLENGE
    // =========================================================

    public void ClearCurrentChallenge()
    {
        CurrentChallenge =
            null;


        IsEventJoined =
            false;


        LastError =
            "";


        CurrentParticipantEntryMode =
            ParticipantEntryMode.MainDashboard;


        if (
            eventCodeInput != null
        )
        {
            eventCodeInput.text =
                "";
        }


        if (
            challengeDropdown != null
        )
        {
            if (
                availableChallenges.Count > 0
            )
            {
                challengeDropdown.value =
                    0;


                challengeDropdown.RefreshShownValue();
            }
        }


        SetStatus("");
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetCompetition()
    {
        ClearCurrentChallenge();


        IsLoading =
            false;


        LastError =
            "";


        if (
            joinButton != null
        )
        {
            joinButton.interactable =
                true;
        }
    }


    // =========================================================
    // DOCUMENT → CHALLENGE
    // =========================================================

    private ChallengeData DocumentToChallenge(
        DocumentSnapshot document)
    {
        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        ChallengeData challenge =
            new ChallengeData();


        // -----------------------------------------------------
        // ID
        // -----------------------------------------------------

        challenge.challengeID =
            document.Id;


        // -----------------------------------------------------
        // BASIC INFORMATION
        // -----------------------------------------------------

        challenge.title =
            GetString(
                document,
                "title"
            );


        challenge.description =
            GetString(
                document,
                "description"
            );


        challenge.location =
            GetString(
                document,
                "location"
            );


        challenge.bannerUrl =
            GetString(
                document,
                "bannerUrl"
            );


        // -----------------------------------------------------
        // DATE
        // -----------------------------------------------------

        challenge.startDate =
            GetString(
                document,
                "startDate"
            );


        challenge.endDate =
            GetString(
                document,
                "endDate"
            );


        // -----------------------------------------------------
        // EVENT CODE
        // -----------------------------------------------------

        challenge.eventCode =
            GetString(
                document,
                "eventCode"
            );


        // -----------------------------------------------------
        // ACTIVE
        // -----------------------------------------------------

        challenge.isActive =
            GetBool(
                document,
                "isActive"
            );


        return challenge;
    }


    // =========================================================
    // GET STRING FROM FIREBASE
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
    // GET BOOL FROM FIREBASE
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
    // STATUS
    // =========================================================

    private void SetStatus(
        string message)
    {
        LastError = "";


        if (
            statusText != null
        )
        {
            statusText.text =
                message;
        }


        if (
            !string.IsNullOrWhiteSpace(
                message
            )
        )
        {
            Debug.Log(
                "CompetitionManager: " +
                message
            );
        }
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void SetError(
        string message)
    {
        LastError =
            message;


        if (
            statusText != null
        )
        {
            statusText.text =
                message;
        }


        Debug.LogError(
            "CompetitionManager: " +
            message
        );
    }


    // =========================================================
    // DATA
    // =========================================================

    [Serializable]
    public class ChallengeData
    {
        public string challengeID;

        public string title;

        public string description;

        public string location;

        public string bannerUrl;

        public string startDate;

        public string endDate;

        public string eventCode;

        public bool isActive;
    }
}