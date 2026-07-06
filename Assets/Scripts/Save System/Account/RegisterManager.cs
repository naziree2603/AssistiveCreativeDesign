using TMPro;
using UnityEngine;

public class RegisterManager : MonoBehaviour
{
    [Header("Input")]

    public TMP_InputField usernameInput;

    public TMP_InputField passwordInput;

    public TMP_Text messageText;

    [Header("Panels")]

    public GameObject loginPanel;

    public GameObject registerPanel;

    public void Register()
    {
        string username =
            usernameInput.text.Trim();

        string password =
            passwordInput.text;


        if (username == "")
        {
            messageText.text =
                "Please enter username.";

            return;
        }

        if (password == "")
        {
            messageText.text =
                "Please enter password.";

            return;
        }


        if (AccountSaveSystem.Exists(username))
        {
            messageText.text =
                "Username already exists.";

            return;
        }

        AccountData account =
            new AccountData();

        account.username = username;

        account.password = password;

        account.participant =
            new ParticipantData();

        AccountSaveSystem.Save(account);

        messageText.text =
            "Registration successful.";

        
    }

    public void Back()
    {
        registerPanel.SetActive(false);

        loginPanel.SetActive(true);
    }
}