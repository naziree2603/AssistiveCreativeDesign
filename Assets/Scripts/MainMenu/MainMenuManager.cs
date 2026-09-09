//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.Audio;
//using UnityEngine.SocialPlatforms.Impl;
//using static AccessibilityToggle;
//using static UnityEngine.UIElements.UxmlAttributeDescription;

//public class MainMenuManager : MonoBehaviour
//{
//    public SubmittedManager submittedManager;

//    public LeaderboardManager leaderboardManager;

//    public FullPosterImageAPI posterSystem;


//    [Header("Panel Page")]
//    public GameObject mainMenuPanel;
//    public GameObject participantPanel;
//    public GameObject promptPanel;
//    public GameObject outputPanel;
//    public GameObject descriptionPanel;
//    public GameObject revisionPanel;
//    public GameObject finalExplanationPanel;
//    public GameObject scorePanel;
//    public GameObject leaderboardPanel;
//    public GameObject posterReviewPanel;
//    public GameObject submittedPanel;
//    public GameObject challengePanel;

//    [Header("Main Menu Objects")]
//    public GameObject historyButton;
//    public GameObject leaderboardButton;


//    void Start()
//    {

//    }



//    void WelcomeSpeech()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Welcome to AI Assistive Design."
//        );

//    }

//    public void GoHome()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Returning to Main Menu.");

//        posterSystem.HideLoading();

//        posterSystem.ResetSystem();
//        posterSystem.PrepareForNewChallenge();

//        ParticipantManager.Instance.ResetParticipant();
//        ChallengeManager.Instance.ResetChallenge();

//        posterSystem.CloseAllPanels();

//        mainMenuPanel.SetActive(true);
//    }



//    public void OpenInstructions()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Opening instructions. Page 1 of 4. " +

//            "Welcome to AI Assistive Design. " +

//            "An AI powered poster creation platform designed to support blind and visually impaired participants in creating accessible posters independently. " +

//            "How to use. " +

//            "Step 1. Enter Participant Details. " +
//            "Fill in your name, institution, and competition category. " +


//            "Step 2. Create a Poster Prompt. " +
//            "Describe your poster idea using text or voice. " +
//            "Then press Generate Poster to create your AI generated poster. " +

//            "Press Next to continue to Page 2."
//        );
//    }

//    public void Page2()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Page 2 of 4. " +

//            "Step 3. Review the Generated Poster. " +
//            "The system will automatically generate a poster and provide an audio description of the image. " +
//            "Listen carefully to ensure the poster matches your intended idea. " +

//            "Step 4. Revise the Poster if needed. " +
//            "Modify your prompt and regenerate the poster until you are satisfied with the result. " +

//            "Step 5. Provide the Final Explanation. " +
//            "Describe your poster concept, message, target audience, and accessibility considerations. " +

//            "Press Next to continue to Page 3."
//        );
//    }

//    public void Page3()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Page 3 of 4. " +

//            "Step 6. AI Score Calculation. " +
//            "Press Calculate Score. " +
//            "The AI will evaluate your submission based on the competition assessment rubric. " +
//            "Your score and feedback will be displayed automatically. " +

//            "Step 7. Leaderboard. " +
//            "View the final scores and rankings of all participants. " +

//            "Accessibility Gestures. " +
//            "Swipe left or right to move between accessible elements. " +
//            "Double tap to activate the selected button. " +
//            "Accessibility Mode can be turned on or off using the Accessibility button. " +

//            "Press Next to continue to Page 4."
//            );
//    }
//    public void Page4()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Page 4 of 4. " +

//            "Panel Flow. " +
//            "Participant Details. " +
//            "Generate Poster Prompt. " +
//            "Generated Poster. " +
//            "Revise Poster. " +
//            "Final Explanation. " +
//            "AI Score Calculation. " +
//            "Leaderboard. " +

//            "Tips. " +
//            "Speak clearly when using voice input. " +
//            "Wait until the microphone has finished listening before speaking again. " +
//            "Ensure your device has an active internet connection for AI services. " +
//            "Listen carefully to the poster description before submitting your final explanation. " +
//            "Thank you for using AI Assistive Design. " +
//            "Good luck and enjoy creating accessible AI powered posters."
//            );
//    }

//    public void OpenProfile()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Participant details page."
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(true);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//        challengePanel.SetActive(false);
//    }

//    public void BackToMainMenu()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Back to Main Menu Page. Welcome to AI Assistive Design. " +
//            "Swipe left or right to navigate menu items. " +
//            "Double tap to activate a button."
//        );

//        posterSystem.CloseAllPanels();

//        mainMenuPanel.SetActive(true);
//    }

//    public void OpenChallengePanel()
//    {
//        mainMenuPanel.SetActive(false);
//        challengePanel.SetActive(true);

//        ChallengeManager.Instance.LoadChallenges();

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Challenge Page");
//    }


//    public void GoToPosterPrompt()
//    {


//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Poster Prompt Page");

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(true);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//        challengePanel.SetActive(false);
//    }

//    public void OpenPrompt()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Poster Prompt Page");
//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(true);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//        challengePanel.SetActive(false);
//    }
//    public void OpenSubmitted()
//    {
//        if (!historyButton.activeSelf)
//        {
//            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//                "History is unavailable. Complete at least one challenge first."
//            );
//            return;
//        }

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Poster Submitted Page. ");


//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//        submittedPanel.SetActive(true);

//        SubmittedManager manager = submittedPanel.GetComponent<SubmittedManager>();

//        manager.LoadSubmitted();
//    }

//    public void GoToGeneratedPoster()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Generated Poster Image Page."
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(true);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//    }

//    public void GoToDescriptionPage()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Description Poster Page."
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(true);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//    }

//    public void GoToRevisePage()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Revise Poster Page."
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(true);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//    }

//    public void GoToFinalExplainPage()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Final Explainantion Page. " +
//            "Explain your poster concept, message, and accessibility considerations. "
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(true);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(false);
//    }

//    public void GoToScorePage()
//    {

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Score Full Poster Page."
//        );

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(true);
//        leaderboardPanel.SetActive(false);
//    }

//    public void GoToLeaderboardPage()
//    {
//        if (!leaderboardButton.activeSelf)
//        {
//            AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//                "Leaderboard is unavailable. Complete at least one challenge first."
//            );
//            return;
//        }

//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation(
//            "Leaderboard Page."
//        );

//        leaderboardManager.LoadLeaderboard();

//        mainMenuPanel.SetActive(false);
//        participantPanel.SetActive(false);
//        promptPanel.SetActive(false);
//        outputPanel.SetActive(false);
//        descriptionPanel.SetActive(false);
//        revisionPanel.SetActive(false);
//        finalExplanationPanel.SetActive(false);
//        scorePanel.SetActive(false);
//        leaderboardPanel.SetActive(true);
//    }


//    public void ExitApp()
//    {
//        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("Closing application.");

//        Application.Quit();
//    }

//    private void OnDestroy()
//    {
//        AndroidTTS.Shutdown();
//    }

//    public void OpenProfilePanel()
//    {
//        challengePanel.SetActive(false);

//        participantPanel.SetActive(true);
//    }

//    public async Task RefreshButtons()
//    {
//        string accountID =
//            FirestoreAccountManager.Instance.CurrentAccount.documentID;

//        bool hasSubmission =
//            await FirestoreEntryManager.Instance
//                .HasCompletedSubmission(accountID);

//        historyButton.SetActive(hasSubmission);
//        leaderboardButton.SetActive(hasSubmission);
//    }
//}

