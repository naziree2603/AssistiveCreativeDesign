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

    public void Login()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (username == "")
        {
            messageText.text = "Please enter username.";
            return;
        }

        if (password == "")
        {
            messageText.text = "Please enter password.";
            return;
        }

        AccountData account = AccountSaveSystem.Load(username);

        if (account == null)
        {
            messageText.text = "Account not found.";
            return;
        }

        if (account.password != password)
        {
            messageText.text = "Incorrect password.";
            return;
        }

        // Login success
        AccountManager.Instance.CurrentAccount = account;

        ParticipantManager.Instance.CurrentParticipant = account.participant;

        posterSystem.LoadParticipant();

        messageText.text = "Login successful.";

        loginPanel.SetActive(false);

        posterSystem.CloseAllPanels();

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
        // Reset current account
        AccountManager.Instance.CurrentAccount = null;

        // Reset all poster data/UI
        posterSystem.ResetSystem();

        // Hide all pages
        mainMenuPanel.SetActive(false);

        // Return to Login
        loginPanel.SetActive(true);

        // Clear login fields
        usernameInput.text = "";
        passwordInput.text = "";

        messageText.text = "";

        AndroidTTS.Speak(
            "You have logged out."
        );
    }

    public void DeleteAllAccounts()
    {
        AccountSaveSystem.DeleteAllAccounts();

        messageText.text = "All accounts deleted.";

        Debug.Log("All accounts deleted.");
    }

    public void ShowAccounts()
    {
        List<string> accounts =
            AccountSaveSystem.GetAllAccounts();

        foreach (string account in accounts)
        {
            Debug.Log(account);
        }
    }
}