using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject instructionPanel;
    public GameObject participantPanel;

    void Start()
    {
        AndroidTTS.Initialize();

        WelcomeSpeech();
    }

    void WelcomeSpeech()
    {
        Debug.Log("Welcome Speech");

        AndroidTTS.Speak(
            "Welcome to Assistive Creative Design. Swipe left or right to navigate menu items. Double tap to activate a button."
        );
    }

    public void OpenInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionPanel.SetActive(true);

        AndroidTTS.Speak(
            "Opening instructions. Page 1 of 4."
        );
    }

    public void StartApplication()
    {
        mainMenuPanel.SetActive(false);
        participantPanel.SetActive(true);

        AndroidTTS.Speak(
            "Participant details page."
        );
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