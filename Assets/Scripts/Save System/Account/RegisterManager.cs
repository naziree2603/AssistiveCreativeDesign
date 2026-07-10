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

    public async void Register()
    {
        string username =
            usernameInput.text.Trim();

        string password =
            passwordInput.text;


        if (username == "")
        {
            messageText.text = "Please enter username.";
            return;
        }

        if (username.Length < 3)
        {
            messageText.text =
                "Username must be at least 3 characters.";

            return;
        }

        if (username.Contains(" "))
        {
            messageText.text =
                "Username cannot contain spaces.";

            return;
        }


        if (password == "")
        {
            messageText.text = "Please enter password.";
            return;
        }

        if (password.Length < 6)
        {
            messageText.text = "Password must be at least 6 characters.";
            return;
        }


        bool success = await FirestoreAccountManager.Instance.Register(username, password);

        if (!success)
        {
            messageText.text = "Registration failed.";

            return;
        }

        messageText.text ="Registration successful.";


    }

    public void Back()
    {
        registerPanel.SetActive(false);

        loginPanel.SetActive(true);
    }
}