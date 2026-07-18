using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        bool success = await FirestoreAccountManager.Instance.Login(username, password);

        Debug.Log("Login Result : " + success);

        if (!success)
        {
            messageText.text = "Invalid username or password.";
            return;
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

        ProfileManager.Instance.ResetProfile();

        ParticipantManager.Instance.ResetParticipant();

        posterSystem.ResetSystem();

        loginPanel.SetActive(true);

        mainMenuPanel.SetActive(false);

        usernameInput.text = "";

        passwordInput.text = "";

        messageText.text = "";

        AndroidTTS.Speak("You have logged out.");
    }


}