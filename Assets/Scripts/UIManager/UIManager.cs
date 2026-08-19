using System;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }


    // =========================================================
    // MAIN PANELS
    // =========================================================

    [Header("Main Panels")]

    [SerializeField]
    private GameObject loginPanel;

    [SerializeField]
    private GameObject registerPanel;

    [SerializeField]
    private GameObject mainMenuPanel;

    [SerializeField]
    private GameObject challengePanel;

    [SerializeField]
    private GameObject participantPanel;

    [SerializeField]
    private GameObject designWorkspacePanel;

    [SerializeField]
    private GameObject leaderboardPanel;

    [SerializeField]
    private GameObject submittedPanel;

    [SerializeField]
    private GameObject settingsPanel;


    // =========================================================
    // POPUP
    // =========================================================

    [Header("Popup")]

    [SerializeField]
    private GameObject popupPanel;

    [SerializeField]
    private TMP_Text popupMessageText;


    // =========================================================
    // STATUS
    // =========================================================

    [Header("Status")]

    [SerializeField]
    private TMP_Text statusText;


    // =========================================================
    // SETTINGS INFORMATION
    // =========================================================

    [Header("Settings Information")]

    [TextArea(5, 20)]
    [SerializeField]
    private string aboutMessage =
        "About this application.";


    [TextArea(5, 30)]
    [SerializeField]
    private string privacyPolicyMessage =
        "Privacy Policy information.";


    [TextArea(5, 30)]
    [SerializeField]
    private string termsConditionsMessage =
        "Terms and Conditions information.";


    // =========================================================
    // POPUP ACTION
    // =========================================================

    private enum PopupAction
    {
        None,

        DeleteAccount,

        ContinueToPrompt,

        LeaveChallenge
    }


    private PopupAction currentPopupAction =
        PopupAction.None;


    // =========================================================
    // DELETE ACCOUNT STATE
    // =========================================================

    private bool isDeletingAccount = false;


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
    }


    private void Start()
    {
        ClosePopup();
    }


    // =========================================================
    // HIDE ALL PANELS
    // =========================================================

    public void HideAllPanels()
    {
        SetActive(loginPanel, false);

        SetActive(registerPanel, false);

        SetActive(mainMenuPanel, false);

        SetActive(challengePanel, false);

        SetActive(participantPanel, false);

        SetActive(designWorkspacePanel, false);

        SetActive(leaderboardPanel, false);

        SetActive(submittedPanel, false);

        SetActive(settingsPanel, false);
    }


    // =========================================================
    // LOGIN
    // =========================================================

    public void ShowLogin()
    {
        HideAllPanels();

        SetActive(
            loginPanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Login"
        );
    }


    // =========================================================
    // REGISTER
    // =========================================================

    public void ShowRegister()
    {
        HideAllPanels();

        SetActive(
            registerPanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Register"
        );
    }


    // =========================================================
    // MAIN DASHBOARD
    // =========================================================

    public void ShowMainMenu()
    {
        HideAllPanels();

        SetActive(
            mainMenuPanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Main Dashboard"
        );
    }


    // =========================================================
    // MAIN DASHBOARD ALIAS
    // =========================================================

    public void ShowDashboard()
    {
        ShowMainMenu();
    }


    // =========================================================
    // OPEN PARTICIPANT FROM MAIN DASHBOARD
    // =========================================================
    //
    // Flow:
    //
    // Main Dashboard
    //       ↓
    // Participant Details
    //       ↓
    // Save
    //       ↓
    // Main Dashboard
    //
    // =========================================================

    public void OpenParticipantFromMainDashboard()
    {
        SetParticipantEntryMode(
            CompetitionManager.ParticipantEntryMode.MainDashboard
        );


        ShowParticipant();


        Debug.Log(
            "UIManager: Participant opened from Main Dashboard."
        );
    }


    // =========================================================
    // OPEN PARTICIPANT FROM CHALLENGE
    // =========================================================
    //
    // Flow:
    //
    // Challenge
    //       ↓
    // Join Event
    //       ↓
    // Participant Details
    //       ↓
    // Save
    //       ↓
    // Idea Prompt
    //
    // =========================================================

    public void OpenParticipantFromChallenge()
    {
        SetParticipantEntryMode(
            CompetitionManager.ParticipantEntryMode.ChallengeJoin
        );


        ShowParticipant();


        Debug.Log(
            "UIManager: Participant opened from Challenge."
        );
    }


    // =========================================================
    // SET PARTICIPANT ENTRY MODE
    // =========================================================

    private void SetParticipantEntryMode(
        CompetitionManager.ParticipantEntryMode mode)
    {
        if (
            CompetitionManager.Instance == null
        )
        {
            Debug.LogWarning(
                "UIManager: CompetitionManager is not available."
            );

            return;
        }


        CompetitionManager.Instance
            .CurrentParticipantEntryMode =
            mode;
    }


    // =========================================================
    // CHALLENGE
    // =========================================================

    public void ShowChallenge()
    {
        HideAllPanels();

        SetActive(
            challengePanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Challenge"
        );


        // -----------------------------------------------------
        // LOAD CHALLENGES
        // -----------------------------------------------------

        if (
            CompetitionManager.Instance != null
        )
        {
            CompetitionManager.Instance
                .OpenCompetition();
        }
    }


    // =========================================================
    // PARTICIPANT
    // =========================================================

    public void ShowParticipant()
    {
        HideAllPanels();

        SetActive(
            participantPanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Participant"
        );
    }


    // =========================================================
    // DESIGN WORKSPACE
    // =========================================================

    public void ShowDesignWorkspace()
    {
        HideAllPanels();

        SetActive(
            designWorkspacePanel,
            true
        );

        ClosePopup();

        Debug.Log(
            "UIManager: Design Workspace"
        );
    }


    // =========================================================
    // OPEN DESIGN WORKSPACE
    // =========================================================

    public void OpenDesignWorkspace()
    {
        ShowDesignWorkspace();
    }

    // =========================================================
    // OPEN IDEA PROMPT
    // =========================================================

    public void OpenIdeaPrompt()
    {
        Debug.Log(
            "UIManager: OpenIdeaPrompt() CALLED."
        );


        // -----------------------------------------------------
        // CHECK DESIGN MANAGER
        // -----------------------------------------------------

        if (DesignManager.Instance == null)
        {
            Debug.LogError(
                "UIManager: DesignManager.Instance is NULL!"
            );

            return;
        }


        Debug.Log(
            "UIManager: DesignManager found."
        );


        // -----------------------------------------------------
        // OPEN DESIGN WORKSPACE
        // -----------------------------------------------------

        HideAllPanels();


        if (designWorkspacePanel == null)
        {
            Debug.LogError(
                "UIManager: Design Workspace Panel is NOT assigned!"
            );

            return;
        }


        designWorkspacePanel.SetActive(true);


        Debug.Log(
            "UIManager: Design Workspace Panel activated."
        );


        // -----------------------------------------------------
        // ASK DESIGN MANAGER TO OPEN PROMPT
        // -----------------------------------------------------

        DesignManager.Instance.OpenPrompt();


        Debug.Log(
            "UIManager: DesignManager.OpenPrompt() called."
        );
    }



    // =========================================================
    // CONTINUE TO IDEA PROMPT POPUP
    // =========================================================

    public void ShowContinueToPromptPopup()
    {
        if (popupPanel == null)
        {
            Debug.LogWarning(
                "UIManager: Popup Panel is not assigned."
            );

            return;
        }

        currentPopupAction =
            PopupAction.ContinueToPrompt;

        SetParticipantEntryMode(
            CompetitionManager.ParticipantEntryMode.ChallengeJoin
        );

        if (popupMessageText != null)
        {
            popupMessageText.text =
                "Participant details already exist.\n\n" +
                "Would you like to continue your previous challenge?";
        }

        popupPanel.SetActive(true);

        Debug.Log(
            "UIManager: Continue Challenge popup opened."
        );
    }


    // =========================================================
    // CANCEL CONTINUE POPUP
    // =========================================================
    //
    // Popup
    //    ↓
    // Cancel
    //    ↓
    // Participant Details
    //
    // =========================================================

    public void CancelContinueToPrompt()
    {
        ClosePopup();


        SetParticipantEntryMode(
            CompetitionManager.ParticipantEntryMode.ChallengeJoin
        );


        ShowParticipant();


        Debug.Log(
            "UIManager: Continue to prompt cancelled."
        );
    }


    // =========================================================
    // LEADERBOARD
    // =========================================================

    public void ShowLeaderboard()
    {
        HideAllPanels();

        SetActive(
            leaderboardPanel,
            true
        );

        ClosePopup();

        Debug.Log(
            "UIManager: Leaderboard"
        );


        if (
            LeaderboardManager.Instance != null
        )
        {
            LeaderboardManager.Instance
                .LoadLeaderboard();
        }
    }


    // =========================================================
    // SUBMITTED
    // =========================================================

    public void ShowSubmitted()
    {
        HideAllPanels();

        SetActive(
            submittedPanel,
            true
        );

        ClosePopup();

        Debug.Log(
            "UIManager: Submitted"
        );


        if (
            SubmissionManager.Instance != null
        )
        {
            SubmissionManager.Instance
                .LoadMySubmissions();
        }
    }


    // =========================================================
    // SETTINGS
    // =========================================================

    public void ShowSettings()
    {
        HideAllPanels();

        SetActive(
            settingsPanel,
            true
        );

        ClosePopup();

        SetStatus("");

        Debug.Log(
            "UIManager: Settings"
        );
    }


    // =========================================================
    // ABOUT
    // =========================================================

    public void ShowAbout()
    {
        ShowInformationPopup(
            aboutMessage
        );
    }


    // =========================================================
    // PRIVACY POLICY
    // =========================================================

    public void ShowPrivacyPolicy()
    {
        ShowInformationPopup(
            privacyPolicyMessage
        );
    }


    // =========================================================
    // TERMS & CONDITIONS
    // =========================================================

    public void ShowTermsConditions()
    {
        ShowInformationPopup(
            termsConditionsMessage
        );
    }


    // =========================================================
    // INFORMATION POPUP
    // =========================================================

    private void ShowInformationPopup(
        string message)
    {
        if (popupPanel == null)
        {
            Debug.LogWarning(
                "UIManager: Popup Panel is not assigned."
            );

            return;
        }


        currentPopupAction =
            PopupAction.None;


        if (popupMessageText != null)
        {
            popupMessageText.text =
                message;
        }


        popupPanel.SetActive(true);
    }


    // =========================================================
    // BACK TO SETTINGS
    // =========================================================

    public void BackToSettings()
    {
        ShowSettings();
    }


    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }


    // =========================================================
    // BACK TO CHALLENGE
    // =========================================================

    public void BackToChallenge()
    {
        ShowChallenge();
    }


    // =========================================================
    // BACK TO PARTICIPANT
    // =========================================================

    public void BackToParticipant()
    {
        ShowParticipant();
    }


    // =========================================================
    // BACK TO DESIGN WORKSPACE
    // =========================================================

    public void BackToDesignWorkspace()
    {
        ShowDesignWorkspace();
    }


    // =========================================================
    // DELETE ACCOUNT
    // =========================================================

    public void AskDeleteAccount()
    {
        if (isDeletingAccount)
        {
            return;
        }


        if (
            AccountManager.Instance == null
        )
        {
            ShowInformationPopup(
                "Account Manager is not available."
            );

            return;
        }


        if (
            !AccountManager.Instance
                .IsUserLoggedIn()
        )
        {
            ShowInformationPopup(
                "Please login first."
            );

            return;
        }


        ShowDeleteAccountPopup();
    }


    // =========================================================
    // DELETE ACCOUNT POPUP
    // =========================================================

    private void ShowDeleteAccountPopup()
    {
        if (popupPanel == null)
        {
            Debug.LogWarning(
                "UIManager: Popup Panel is not assigned."
            );

            return;
        }


        currentPopupAction =
            PopupAction.DeleteAccount;


        if (popupMessageText != null)
        {
            popupMessageText.text =
                "Are you sure you want to delete your account?\n\n" +
                "All account data and submitted work will be permanently deleted.";
        }


        popupPanel.SetActive(true);
    }


    // =========================================================
    // CONFIRM POPUP
    // =========================================================

    public void ConfirmPopup()
    {
        switch (currentPopupAction)
        {
            // =====================================================
            // DELETE ACCOUNT
            // =====================================================

            case PopupAction.DeleteAccount:

                ConfirmDeleteAccount();

                break;


            // =====================================================
            // CONTINUE CHALLENGE
            // =====================================================

            case PopupAction.ContinueToPrompt:

                ClosePopup();

                OpenIdeaPrompt();

                break;


            // =====================================================
            // LEAVE CHALLENGE
            // =====================================================

            case PopupAction.LeaveChallenge:

                ClosePopup();

                // Clear only the CURRENT LOCAL challenge state.
                // Do NOT delete the Firestore submission.
                if (ParticipantManager.Instance != null)
                {
                    ParticipantManager.Instance
                        .ClearCurrentChallenge();
                }

                // Go directly to Main Dashboard.
                ShowMainMenu();

                Debug.Log(
                    "UIManager: Challenge left. " +
                    "Returning to Main Dashboard."
                );

                break;


            // =====================================================
            // NONE
            // =====================================================

            case PopupAction.None:

                ClosePopup();

                break;
        }
    }


    // =========================================================
    // CONFIRM DELETE ACCOUNT
    // =========================================================

    private async void ConfirmDeleteAccount()
    {
        if (isDeletingAccount)
        {
            return;
        }


        if (
            AccountManager.Instance == null
        )
        {
            ClosePopup();

            SetStatus(
                "Account Manager is not available."
            );

            return;
        }


        isDeletingAccount =
            true;


        if (popupMessageText != null)
        {
            popupMessageText.text =
                "Deleting your account.\n\nPlease wait...";
        }


        try
        {
            bool success =
                await AccountManager.Instance
                    .DeleteAccount();


            if (success)
            {
                isDeletingAccount =
                    false;


                ClosePopup();


                ShowLogin();


                SetStatus(
                    "Account deleted successfully."
                );


                Debug.Log(
                    "UIManager: Account deleted successfully."
                );
            }
            else
            {
                string error =
                    AccountManager.Instance
                        .LastError;


                isDeletingAccount =
                    false;


                ClosePopup();


                if (
                    string.IsNullOrWhiteSpace(
                        error
                    )
                )
                {
                    error =
                        "Failed to delete account.";
                }


                SetStatus(
                    error
                );
            }
        }
        catch (Exception exception)
        {
            isDeletingAccount =
                false;


            ClosePopup();


            SetStatus(
                "Account deletion failed: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // CLOSE POPUP
    // =========================================================

    public void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }


        currentPopupAction =
            PopupAction.None;
    }


    // =========================================================
    // LOGOUT
    // =========================================================

    public void Logout()
    {
        if (
            AccountManager.Instance != null
        )
        {
            AccountManager.Instance
                .Logout();
        }


        ClosePopup();


        // -----------------------------------------------------
        // CLEAR PARTICIPANT
        // -----------------------------------------------------

        if (
            ParticipantManager.Instance != null
        )
        {
            ParticipantManager.Instance
                .ClearCurrentParticipant();
        }


        // -----------------------------------------------------
        // RESET COMPETITION
        // -----------------------------------------------------

        if (
            CompetitionManager.Instance != null
        )
        {
            CompetitionManager.Instance
                .ResetCompetition();
        }


        // -----------------------------------------------------
        // SHOW LOGIN / WELCOME
        // -----------------------------------------------------

        if (
            StartupManager.Instance != null
        )
        {
            StartupManager.Instance
                .ShowWelcome();
        }
        else
        {
            ShowLogin();
        }


        Debug.Log(
            "UIManager: User logged out."
        );
    }


    // =========================================================
    // LOGIN SUCCESS
    // =========================================================

    public void OnLoginSuccess()
    {
        if (
            StartupManager.Instance != null
        )
        {
            StartupManager.Instance
                .ShowDashboard();
        }
        else
        {
            ShowMainMenu();
        }
    }


    // =========================================================
    // REGISTER SUCCESS
    // =========================================================

    public void OnRegisterSuccess()
    {
        if (
            StartupManager.Instance != null
        )
        {
            StartupManager.Instance
                .ShowDashboard();
        }
        else
        {
            ShowMainMenu();
        }
    }


    // =========================================================
    // OPEN COMPETITION
    // =========================================================

    public void OpenCompetition()
    {
        ShowChallenge();
    }


    // =========================================================
    // OPEN PARTICIPANT
    // =========================================================
    //
    // Default participant opening is from Main Dashboard.
    //
    // IMPORTANT:
    //
    // Your Main Dashboard Participant button should use:
    //
    // OpenParticipantFromMainDashboard()
    //
    // instead of this method if you want the entry mode
    // to be explicitly recorded.
    //
    // =========================================================

    public void OpenParticipant()
    {
        OpenParticipantFromMainDashboard();
    }


    // =========================================================
    // OPEN LEADERBOARD
    // =========================================================

    public void OpenLeaderboard()
    {
        ShowLeaderboard();
    }


    // =========================================================
    // OPEN SUBMITTED
    // =========================================================

    public void OpenSubmitted()
    {
        ShowSubmitted();
    }


    // =========================================================
    // OPEN SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        ShowSettings();
    }


    // =========================================================
    // GENERIC PANEL CONTROL
    // =========================================================

    private void SetActive(
        GameObject panel,
        bool value)
    {
        if (panel != null)
        {
            panel.SetActive(value);
        }
    }


    // =========================================================
    // STATUS
    // =========================================================

    public void SetStatus(
        string message)
    {
        if (statusText != null)
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
                "UIManager: " +
                message
            );
        }
    }


    // =========================================================
    // PANEL STATE
    // =========================================================

    public bool IsMainMenuOpen()
    {
        return IsActive(
            mainMenuPanel
        );
    }


    public bool IsChallengeOpen()
    {
        return IsActive(
            challengePanel
        );
    }


    public bool IsParticipantOpen()
    {
        return IsActive(
            participantPanel
        );
    }


    public bool IsDesignWorkspaceOpen()
    {
        return IsActive(
            designWorkspacePanel
        );
    }


    public bool IsSettingsOpen()
    {
        return IsActive(
            settingsPanel
        );
    }


    public bool IsLeaderboardOpen()
    {
        return IsActive(
            leaderboardPanel
        );
    }


    public bool IsSubmittedOpen()
    {
        return IsActive(
            submittedPanel
        );
    }


    private bool IsActive(
        GameObject panel)
    {
        return
            panel != null &&
            panel.activeSelf;
    }


    // =========================================================
    // RESET UI
    // =========================================================

    public void ResetUI()
    {
        ClosePopup();

        HideAllPanels();

        ShowLogin();
    }

    // =========================================================
    // LEAVE CHALLENGE POPUP
    // =========================================================

    public void ShowLeaveChallengePopup()
    {
        if (popupPanel == null)
        {
            Debug.LogWarning(
                "UIManager: Popup Panel is not assigned."
            );

            return;
        }

        currentPopupAction =
            PopupAction.LeaveChallenge;

        if (popupMessageText != null)
        {
            popupMessageText.text =
                "Are you sure you want to leave this challenge?";
        }

        popupPanel.SetActive(true);

        Debug.Log(
            "UIManager: Leave Challenge popup opened."
        );
    }

    public void CloseLoadingPanel()
    {
        // Put your actual loading panel here.

        Debug.Log(
            "UIManager: Loading panel closed."
        );
    }
}