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

        bool success = await FirebaseAuthManager.Instance.Login(username + "@iiad.com", password);

        if (!success)
        {
            messageText.text = "Invalid username or password.";

            return;
        }

        // Wait until Firestore finishes loading
        await ParticipantManager.Instance.Load();

        posterSystem.LoadParticipant();

        messageText.text = "Login successful.";

        loginPanel.SetActive(false);

        mainMenuPanel.SetActive(true);

        AndroidTTS.Speak("Login successful. Opening main menu.");
    }

    public void OpenRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    public void Logout()
    {
        FirebaseAuthManager.Instance.Logout();

        ParticipantManager.Instance.ResetParticipant();

        posterSystem.ResetSystem();

        mainMenuPanel.SetActive(false);
        loginPanel.SetActive(true);

        usernameInput.text = "";
        passwordInput.text = "";

        messageText.text = "";

        AndroidTTS.Speak("You have logged out.");
    }


}