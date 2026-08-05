using TMPro;
using UnityEngine;

public class RegisterManager : MonoBehaviour
{
    [Header("Input")]

    public TMP_InputField emailInput;

    public TMP_InputField usernameInput;

    public TMP_InputField passwordInput;

    public TMP_InputField confirmPasswordInput;

    public TMP_Text messageText;

    [Header("Panels")]

    public GameObject loginPanel;

    public GameObject registerPanel;

    public async void Register()
    {
        string email =emailInput.text.Trim().ToLower();

        string username =
            usernameInput.text.Trim();

        string password =
            passwordInput.text;

        string confirmPassword =
            confirmPasswordInput.text;


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


        if (string.IsNullOrWhiteSpace(email))
        {
            messageText.text =
                "Please enter email.";

            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            messageText.text =
                "Please enter a valid email.";

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

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            messageText.text =
                "Please confirm password.";

            return;
        }

        if (password != confirmPassword)
        {
            messageText.text =
                "Passwords do not match.";

            return;
        }


        FirestoreAccountManager.RegisterResult result = await FirestoreAccountManager.Instance.Register(username,email,password);

        switch (result)
        {
            case FirestoreAccountManager.RegisterResult.UsernameExists:

                messageText.text =
                    "Username already exists.";

                return;

            case FirestoreAccountManager.RegisterResult.EmailExists:

                messageText.text =
                    "Email already registered.";

                return;

            case FirestoreAccountManager.RegisterResult.Success:

                messageText.text =
                    "Registration successful.";

                usernameInput.text = "";
                emailInput.text = "";
                passwordInput.text = "";
                confirmPasswordInput.text = "";

                registerPanel.SetActive(false);
                loginPanel.SetActive(true);

                break;
        }


    }

    public void Back()
    {
        registerPanel.SetActive(false);

        loginPanel.SetActive(true);
    }
}