using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class MainMenuManager : MonoBehaviour
{

    [Header("Panel Page")]
    public GameObject mainMenuPanel;
    public GameObject participantPanel;
    public GameObject promptPanel;
    public GameObject outputPanel;
    public GameObject descriptionPanel;
    public GameObject revisionPanel;
    public GameObject finalExplanationPanel;
    public GameObject scorePanel;
    public GameObject leaderboardPanel;
    public GameObject posterReviewPanel;


    void Start()
    {
        WelcomeSpeech();
    }

  

    void WelcomeSpeech()
    {
        AndroidTTS.Speak(
            "Welcome to Assistive Creative Design. Swipe left or right to navigate menu items. Double tap to activate a button."
        );

    }

    public void GoHome()
    {
        AndroidTTS.Speak(
            "Returning to Main Menu."
        );

        mainMenuPanel.SetActive(true);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    

    public void OpenInstructions()
    {
        AndroidTTS.Speak(
            "Opening instructions. Page 1 of 4. " +

            "Welcome to Assistive Creative Design. " +

            "An AI powered poster creation platform designed for blind and visually impaired participants. " +

            "How to use. " +

            "Step 1, Enter participant information. " +
            "Fill in your name, institution, and category. " +
            "You may type using the keyboard, or use the microphone button for voice input. " +

            "Step 2, Create a poster prompt. " +
            "Describe your poster idea using text or voice. " +
            "Press Generate Poster to create an AI generated poster image. " +

            "Press Next to go to page 2. "
        );
    }

    public void Page2()
    {
        AndroidTTS.Speak(
            "Page 2 of 4. " +

            "Step 3, Review the generated poster. " +

            "The system will automatically describe the poster content. " +

            "Use accessibility features to listen to the full description. " +

            "Step 4, Provide a final explanation. " +

            "Explain your poster concept, message, and accessibility considerations. " +
            
            "Step 5, Calculate your score. " +

            "The AI will evaluate your submission using the competition rubric. " +

            "Feedback and improvement suggestions will be displayed automatically. " +

            "Press Next to go to page 3. "
        );
    }

    public void Page3()
    {
        AndroidTTS.Speak(
            "Page 3 of 4. " +

            "ACCESSIBILITY GESTURES. " +

            "Swipe left or right to navigate between accessible elements. " +

            "Double tap to activate the selected button. " +

            "Accessibility mode can be enabled or disabled from the Accessibility button. " +

            "PANEL FLOW. " +

            "1, Participant Details. " +

            "2, Generate Poster Prompt. " +

            "3, Poster Description. " +

            "4, Revise Poster " +

            "5, Final Explaination. " +

            "6, AI Score Calculation"
            );
    }
    public void Page4()
    {
        AndroidTTS.Speak(
            "Page 4 of 4 " +

            "TIPS. " +

            "Speak clearly when using voice input. " +

            "Wait for the microphone to stop listening before continuing. " +

            "Ensure internet connection is available for AI services. " +

            "Listen carefully to the AI description before submitting your final explanation. " +

            "Goodluck and enjoy creating accessible AI-powered posters. "
            );
    }

    public void StartApplication()
    {
    
        AndroidTTS.Speak(
            "Participant details page. Fill in your name, institution, and category."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(true);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void BackToMainMenu()
    {

        AndroidTTS.Speak(
            "Back to Main Menu Page. Welcome to Assistive Creative Design. " +
            "Swipe left or right to navigate menu items. " +
            "Double tap to activate a button."
        );
    }
      

    public void GoToPosterPrompt()
    {

        AndroidTTS.Speak(
            "Poster Promt Page"
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(true);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void GoToGeneratedPoster()
    {

        AndroidTTS.Speak(
            "Generated Poster Image Page."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(true);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void GoToDescriptionPage()
    {

        AndroidTTS.Speak(
            "Description Poster Page."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(true);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void GoToRevisePage()
    {

        AndroidTTS.Speak(
            "Revise Poster Page."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(true);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void GoToFinalExplainPage()
    {

        AndroidTTS.Speak(
            "Final Explainantion Page. " +
            "Explain your poster concept, message, and accessibility considerations. "
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(true);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    public void GoToScorePage()
    {

        AndroidTTS.Speak(
            "Score Full Poster Page."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(true);
        leaderboardPanel.SetActive(false);
    }

    public void GoToLeaderboardPage()
    {

        AndroidTTS.Speak(
            "Leaderboard Page."
        );

        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(false);
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplanationPanel.SetActive(false);
        scorePanel.SetActive(false);
        leaderboardPanel.SetActive(true);
    }


    public void ExitApp()
    {
        AndroidTTS.Speak("Closing application.");

        Application.Quit();
    }

    private void OnDestroy()
    {
        AndroidTTS.Shutdown();
    }
}