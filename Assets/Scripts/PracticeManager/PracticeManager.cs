using System;
using UnityEngine;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance { get; private set; }


    // =========================================================
    // PRACTICE STATE
    // =========================================================

    public bool IsPracticeMode
    {
        get;
        private set;
    }


    public PracticeData CurrentPractice
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
    }


    // =========================================================
    // START PRACTICE
    // =========================================================

    public void StartPractice()
    {
        Debug.Log(
            "PracticeManager: Starting Practice Mode."
        );


        // Enable Practice Mode
        IsPracticeMode = true;


        // Create a fresh practice session
        CurrentPractice =
            new PracticeData();


        // Tell DesignManager that we are entering
        // Practice Mode.
        if (DesignManager.Instance != null)
        {
            DesignManager.Instance
                .SetDesignMode(
                    DesignMode.Practice
                );

            DesignManager.Instance
                .PrepareForNewChallenge();
        }


        // Open Design Workspace
        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .ShowDesignWorkspace();

            UIManager.Instance
                .OpenIdeaPrompt();
        }


        Debug.Log(
            "PracticeManager: Practice Mode started."
        );
    }


    // =========================================================
    // RESTART PRACTICE
    // =========================================================

    public void RestartPractice()
    {
        Debug.Log(
            "PracticeManager: Restarting Practice."
        );

        StartPractice();
    }


    // =========================================================
    // EXIT PRACTICE
    // =========================================================

    public void ExitPractice()
    {
        Debug.Log(
            "PracticeManager: Exiting Practice Mode."
        );


        IsPracticeMode = false;

        CurrentPractice = null;


        // Reset DesignManager back to Competition mode
        if (DesignManager.Instance != null)
        {
            DesignManager.Instance
                .SetDesignMode(
                    DesignMode.Competition
                );

            DesignManager.Instance
                .PrepareForNewChallenge();
        }


        // Return to Main Dashboard
        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .ShowMainMenu();
        }
    }


    // =========================================================
    // CLEAR PRACTICE
    // =========================================================

    public void ClearPractice()
    {
        CurrentPractice =
            null;

        IsPracticeMode =
            false;


        Debug.Log(
            "PracticeManager: Practice data cleared."
        );
    }


    // =========================================================
    // PRACTICE DATA
    // =========================================================

    [Serializable]
    public class PracticeData
    {
        public string prompt;

        public string originalImageUrl;

        public string revisedImageUrl;

        public string posterDescription;

        public string finalExplanation;

        public string revisionPrompt;

        public int revisionCount;

        public string revisionHistory;

        public int score;

        public int promptQuality;

        public int posterMessage;

        public int designQuality;

        public int accessibilityUnderstanding;

        public int revisionProcessScore;

        public int finalExplanationScore;

        public string feedback;

        public string improvementSuggestion;
    }
}