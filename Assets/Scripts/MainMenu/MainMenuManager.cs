using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject instructionPanel;
    public GameObject participantPanel;

    void Start()
    {
        WelcomeSpeech();
    }

    void WelcomeSpeech()
    {
        Debug.Log("Welcome Speech");

        // UAP Speak
        UAP_AccessibilityManager.Say(
            "Welcome to Assistive Creative Design. Swipe left or right to navigate menu items. Double tap to activate a button.",
            false,
            true
        );
    }

    public void OpenInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionPanel.SetActive(true);

        UAP_AccessibilityManager.Say(
            "Opening instructions. Page 1 of 4.",
            false,
            true
        );
    }

    public void StartApplication()
    {
        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(true);

        UAP_AccessibilityManager.Say(
            "Participant details page.",
            false,
            true
        );
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}