using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // =========================================================
    // DESIGN MODE
    // =========================================================

    public enum DesignMode
    {
        None,
        Practice,
        Challenge
    }

    public DesignMode CurrentMode { get; private set; }


    // =========================================================
    // MAIN SCREENS
    // =========================================================

    [Header("Main Screens")]
    [SerializeField] private GameObject splashScreen;
    [SerializeField] private GameObject welcomeScreen;
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject registerScreen;
    [SerializeField] private GameObject mainDashboard;


    // =========================================================
    // GENERAL PANELS
    // =========================================================

    [Header("General Panels")]
    [SerializeField] private GameObject challengePanel;
    [SerializeField] private GameObject participantDetailsPanel;
    [SerializeField] private GameObject submittedPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject leaderboardPanel;


    // =========================================================
    // DESIGN WORKSPACE
    // =========================================================

    [Header("Design Workspace")]
    [SerializeField] private GameObject designWorkspace;


    // =========================================================
    // DESIGN WORKSPACE PANELS
    // =========================================================

    [Header("Design Workspace Panels")]
    [SerializeField] private GameObject ideaInputPanel;
    [SerializeField] private GameObject outputPanel;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private GameObject revisionPanel;
    [SerializeField] private GameObject submissionPanel;
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private GameObject feedbackImprovementPanel;


    // =========================================================
    // GLOBAL POPUP
    // =========================================================

    [Header("Global Popup")]
    [SerializeField] private GameObject globalPopup;
    [SerializeField] private TMP_Text statusText;


    // =========================================================
    // WORKSPACE NAVIGATION
    // =========================================================

    private GameObject[] workspacePanels;
    private int currentWorkspaceIndex = 0;


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
    }


    private void Start()
    {
        SetupWorkspacePanels();

        CurrentMode = DesignMode.None;

        HideGlobalPopup();

        ShowMainDashboard();
    }


    // =========================================================
    // SETUP WORKSPACE PANELS
    // =========================================================

    private void SetupWorkspacePanels()
    {
        workspacePanels = new GameObject[]
        {
            ideaInputPanel,
            outputPanel,
            descriptionPanel,
            revisionPanel,
            submissionPanel,
            scorePanel,
            feedbackImprovementPanel
        };
    }


    // =========================================================
    // MAIN SCREENS
    // =========================================================

    public void ShowSplash()
    {
        CurrentMode = DesignMode.None;

        HideAllScreens();

        if (splashScreen != null)
            splashScreen.SetActive(true);
    }


    public void ShowWelcome()
    {
        CurrentMode = DesignMode.None;

        HideAllScreens();

        if (welcomeScreen != null)
            welcomeScreen.SetActive(true);
    }


    public void ShowLogin()
    {
        CurrentMode = DesignMode.None;

        HideAllScreens();

        if (loginScreen != null)
            loginScreen.SetActive(true);
    }


    public void ShowRegister()
    {
        CurrentMode = DesignMode.None;

        HideAllScreens();

        if (registerScreen != null)
            registerScreen.SetActive(true);
    }


    public void ShowMainDashboard()
    {
        CurrentMode = DesignMode.None;

        HideAllScreens();

        if (mainDashboard != null)
            mainDashboard.SetActive(true);
    }


    // =========================================================
    // PRACTICE MODE
    // =========================================================

    public void StartPractice()
    {
        CurrentMode = DesignMode.Practice;

        ShowDesignWorkspace();
    }

    public void TestCurrentMode()
    {
        Debug.Log("Current Design Mode: " + CurrentMode);
    } 


    // =========================================================
    // CHALLENGE MODE
    // =========================================================

    public void StartChallenge()
    {
        CurrentMode = DesignMode.Challenge;

        ShowChallenge();
    }


    // =========================================================
    // GENERAL PANELS
    // =========================================================

    public void ShowChallenge()
    {
        HideAllScreens();

        if (challengePanel != null)
            challengePanel.SetActive(true);
    }


    public void ShowParticipantDetails()
    {
        HideAllScreens();

        if (participantDetailsPanel != null)
            participantDetailsPanel.SetActive(true);
    }


    public void ShowSubmitted()
    {
        HideAllScreens();

        if (submittedPanel != null)
            submittedPanel.SetActive(true);
    }


    public void ShowSettings()
    {
        HideAllScreens();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }


    public void ShowLeaderboard()
    {
        HideAllScreens();

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }


    // =========================================================
    // DESIGN WORKSPACE
    // =========================================================

    public void ShowDesignWorkspace()
    {
        HideAllScreens();

        if (designWorkspace != null)
            designWorkspace.SetActive(true);

        ShowIdeaInput();
    }


    // =========================================================
    // WORKSPACE PANELS
    // =========================================================

    public void ShowIdeaInput()
    {
        ShowWorkspacePanel(0);
    }


    public void ShowOutput()
    {
        ShowWorkspacePanel(1);
    }


    public void ShowDescription()
    {
        ShowWorkspacePanel(2);
    }


    public void ShowRevision()
    {
        ShowWorkspacePanel(3);
    }


    public void ShowSubmission()
    {
        ShowWorkspacePanel(4);
    }


    public void ShowScore()
    {
        ShowWorkspacePanel(5);
    }


    public void ShowFeedbackImprovement()
    {
        ShowWorkspacePanel(6);
    }


    // =========================================================
    // WORKSPACE PREVIOUS / NEXT
    // =========================================================

    public void NextWorkspacePanel()
    {
        if (workspacePanels == null || workspacePanels.Length == 0)
            return;

        // Prevent going beyond Feedback & Improvement.
        if (currentWorkspaceIndex >= workspacePanels.Length - 1)
        {
            HandleWorkspaceEnd();
            return;
        }

        currentWorkspaceIndex++;

        ShowWorkspacePanel(currentWorkspaceIndex);
    }


    public void PreviousWorkspacePanel()
    {
        if (workspacePanels == null || workspacePanels.Length == 0)
            return;

        // If user is at the first panel, return to the previous screen.
        if (currentWorkspaceIndex <= 0)
        {
            HandleWorkspaceBack();
            return;
        }

        currentWorkspaceIndex--;

        ShowWorkspacePanel(currentWorkspaceIndex);
    }


    private void ShowWorkspacePanel(int index)
    {
        if (workspacePanels == null)
            SetupWorkspacePanels();

        if (index < 0 || index >= workspacePanels.Length)
            return;

        HideWorkspacePanels();

        currentWorkspaceIndex = index;

        if (workspacePanels[index] != null)
            workspacePanels[index].SetActive(true);
    }


    // =========================================================
    // WORKSPACE END
    // =========================================================

    private void HandleWorkspaceEnd()
    {
        // Practice:
        // Feedback & Improvement → Main Dashboard

        if (CurrentMode == DesignMode.Practice)
        {
            ShowMainDashboard();
            return;
        }

        // Challenge:
        // Feedback & Improvement → Leaderboard

        if (CurrentMode == DesignMode.Challenge)
        {
            ShowLeaderboard();
            return;
        }

        ShowMainDashboard();
    }


    // =========================================================
    // WORKSPACE BACK
    // =========================================================

    private void HandleWorkspaceBack()
    {
        ShowMainDashboard();
    }


    // =========================================================
    // GET CURRENT DESIGN MODE
    // =========================================================

    public bool IsPracticeMode()
    {
        return CurrentMode == DesignMode.Practice;
    }


    public bool IsChallengeMode()
    {
        return CurrentMode == DesignMode.Challenge;
    }


    // =========================================================
    // GLOBAL POPUP
    // =========================================================

    public void ShowStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (globalPopup != null)
            globalPopup.SetActive(true);
    }


    public void HideGlobalPopup()
    {
        if (globalPopup != null)
            globalPopup.SetActive(false);
    }


    // =========================================================
    // HIDE ALL TOP-LEVEL SCREENS
    // =========================================================

    private void HideAllScreens()
    {
        if (splashScreen != null)
            splashScreen.SetActive(false);

        if (welcomeScreen != null)
            welcomeScreen.SetActive(false);

        if (loginScreen != null)
            loginScreen.SetActive(false);

        if (registerScreen != null)
            registerScreen.SetActive(false);

        if (mainDashboard != null)
            mainDashboard.SetActive(false);

        if (challengePanel != null)
            challengePanel.SetActive(false);

        if (participantDetailsPanel != null)
            participantDetailsPanel.SetActive(false);

        if (submittedPanel != null)
            submittedPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (designWorkspace != null)
            designWorkspace.SetActive(false);
    }


    // =========================================================
    // HIDE WORKSPACE PANELS
    // =========================================================

    private void HideWorkspacePanels()
    {
        if (workspacePanels == null)
            return;

        foreach (GameObject panel in workspacePanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    // =========================================================
    // EXIT APPLICATION
    // =========================================================

    public void ExitApplication()
    {
        Debug.Log("Exit Application");

        Application.Quit();
    }
}