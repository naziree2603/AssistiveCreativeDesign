using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static AccessibilityToggle;
using static FirestoreAccountManager;

public class LoginManager : MonoBehaviour
{
    [Header("Login UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    public TMP_Text messageText;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject mainMenuPanel;

    public FullPosterImageAPI posterSystem;

    private async void Start()
    {
        // Wait until Firebase is ready
        while (FirestoreAccountManager.Instance == null)
            await System.Threading.Tasks.Task.Yield();

        while (!FirestoreAccountManager.Instance.IsReady)
            await System.Threading.Tasks.Task.Yield();

        bool success =
            await FirestoreAccountManager.Instance.AutoLogin();

        if (success)
        {
            Debug.Log("Auto Login Success");

            loginPanel.SetActive(false);

            mainMenuPanel.SetActive(true);

            await FindFirstObjectByType<MainMenuManager>()
                .RefreshButtons();
        }
        else
        {
            loginPanel.SetActive(true);

            mainMenuPanel.SetActive(false);
        }
    }

    public async void Login()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(username))
        {
            messageText.text = "Please enter username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            messageText.text = "Please enter password.";
            return;
        }

        LoginResult result = await FirestoreAccountManager.Instance.Login(username, password);

        switch (result)
        {
            case LoginResult.UserNotFound:

                messageText.text =
                    "Username not found.\nPlease create a new account.";

                AccessibilitySpeech.SpeakContent(
                    "Username not found. Please create a new account.");

                return;

            case LoginResult.WrongPassword:

                messageText.text =
                    "Incorrect password.\nPlease try again.";

                AccessibilitySpeech.SpeakContent(
                    "Incorrect password. Please try again."); 

                return;

            case LoginResult.Success:
                break;
        }

        // Reset previous session
        ProfileManager.Instance.ResetProfile();

        ParticipantManager.Instance.ResetParticipant();

        posterSystem.ResetSystem();

        Debug.Log("Login Success");

        loginPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
    }

    public void OpenRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    public void Logout()
    {
        FirestoreAccountManager.Instance.CurrentAccount = null;

        PlayerPrefs.DeleteKey("LastLoginUserID");

        PlayerPrefs.DeleteKey("LastLoginUsername");

        PlayerPrefs.Save();

        ProfileManager.Instance.ResetProfile();

        ParticipantManager.Instance.ResetParticipant();

        posterSystem.ResetSystem();

        loginPanel.SetActive(true);

        mainMenuPanel.SetActive(false);

        usernameInput.text = "";

        passwordInput.text = "";

        messageText.text = "";

        AccessibilityToggle.AccessibilitySpeech.SpeakNavigation("You have logged out.");
    }


}