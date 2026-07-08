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


        bool success = await FirebaseAuthManager.Instance.Register(username + "@iiad.com", password, username);

        if (!success)
        {
            messageText.text =
                "Registration failed.";

            return;
        }

        messageText.text =
            "Registration successful.";


    }

    public void Back()
    {
        registerPanel.SetActive(false);

        loginPanel.SetActive(true);
    }
}