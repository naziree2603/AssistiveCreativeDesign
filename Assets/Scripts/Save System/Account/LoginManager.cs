using System;
using UnityEngine;
using TMPro;

public class LoginManager : MonoBehaviour
{
    // =========================================================
    // LOGIN UI
    // =========================================================

    [Header("Login UI")]

    [SerializeField]
    private TMP_InputField usernameInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private TMP_Text messageText;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsLoggingIn
    {
        get;
        private set;
    }


    public string LastError
    {
        get;
        private set;
    }


    // =========================================================
    // UNITY
    // =========================================================
    //
    // IMPORTANT:
    //
    // There is NO Start() method here.
    //
    // StartupManager is responsible for:
    //
    // Splash
    // Firebase initialization
    // AutoLogin
    // Welcome
    // Main Dashboard
    //
    // LoginManager is responsible ONLY for:
    //
    // Manual Login
    // Open Register
    // Back
    // Logout
    //
    // =========================================================


    // =========================================================
    // LOGIN
    // =========================================================

    public async void Login()
    {
        // -----------------------------------------------------
        // PREVENT DOUBLE LOGIN
        // -----------------------------------------------------

        if (IsLoggingIn)
        {
            return;
        }


        LastError = "";


        // -----------------------------------------------------
        // CHECK USERNAME INPUT
        // -----------------------------------------------------

        string username =
            usernameInput != null
                ? usernameInput.text.Trim()
                : "";


        // -----------------------------------------------------
        // CHECK PASSWORD INPUT
        // -----------------------------------------------------

        string password =
            passwordInput != null
                ? passwordInput.text
                : "";


        // -----------------------------------------------------
        // VALIDATE USERNAME
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                username
            )
        )
        {
            SetMessage(
                "Please enter username."
            );

            return;
        }


        // -----------------------------------------------------
        // VALIDATE PASSWORD
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                password
            )
        )
        {
            SetMessage(
                "Please enter password."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK ACCOUNT MANAGER
        // -----------------------------------------------------

        if (
            AccountManager.Instance == null
        )
        {
            SetMessage(
                "Account Manager is not available."
            );

            return;
        }


        // -----------------------------------------------------
        // START LOGIN
        // -----------------------------------------------------

        IsLoggingIn = true;


        SetInputInteractable(
            false
        );


        SetMessage(
            "Logging in..."
        );


        try
        {
            Debug.Log(
                "LoginManager: Starting manual login..."
            );


            // -------------------------------------------------
            // LOGIN THROUGH ACCOUNT MANAGER
            // -------------------------------------------------

            bool success =
                await AccountManager.Instance
                    .Login(
                        username,
                        password
                    );


            // -------------------------------------------------
            // LOGIN FAILED
            // -------------------------------------------------

            if (!success)
            {
                string error =
                    AccountManager.Instance
                        .LastError;


                if (
                    string.IsNullOrWhiteSpace(
                        error
                    )
                )
                {
                    error =
                        "Login failed. Please try again.";
                }


                LastError =
                    error;


                SetMessage(
                    error
                );


                Debug.LogWarning(
                    "LoginManager: Login failed. " +
                    error
                );


                return;
            }


            // -------------------------------------------------
            // LOGIN SUCCESS
            // -------------------------------------------------

            Debug.Log(
                "LoginManager: Login successful."
            );


            // -------------------------------------------------
            // CLEAR LOGIN FORM
            // -------------------------------------------------

            ClearLoginFields();


            SetMessage("");


            // -------------------------------------------------
            // OPEN MAIN DASHBOARD
            // -------------------------------------------------
            //
            // IMPORTANT:
            //
            // StartupManager is NOT involved here.
            //
            // This is a manual login after the user has
            // already reached the Login Panel.
            //
            // -------------------------------------------------

            if (
                UIManager.Instance != null
            )
            {
                UIManager.Instance
                    .OnLoginSuccess();
            }
            else
            {
                Debug.LogError(
                    "LoginManager: UIManager is not available."
                );
            }
        }
        catch (Exception exception)
        {
            LastError =
                exception.Message;


            SetMessage(
                "Login failed: " +
                exception.Message
            );


            Debug.LogError(
                "LoginManager: Login exception: " +
                exception
            );
        }
        finally
        {
            IsLoggingIn = false;


            SetInputInteractable(
                true
            );
        }
    }


    // =========================================================
    // OPEN REGISTER
    // =========================================================
    //
    // Login Panel
    //      ↓
    // Register Button
    //      ↓
    // Register Panel
    //
    // =========================================================

    public void OpenRegister()
    {
        if (
            UIManager.Instance == null
        )
        {
            Debug.LogError(
                "LoginManager: UIManager is not available."
            );

            return;
        }


        UIManager.Instance
            .ShowRegister();


        Debug.Log(
            "LoginManager: Opening Register Panel."
        );
    }


    // =========================================================
    // BACK TO WELCOME
    // =========================================================
    //
    // Login Panel
    //      ↓
    // Back Button
    //      ↓
    // Welcome Panel
    //
    // =========================================================

    public void BackToWelcome()
    {
        if (
            StartupManager.Instance != null
        )
        {
            StartupManager.Instance
                .ShowWelcome();


            Debug.Log(
                "LoginManager: Back to Welcome."
            );


            return;
        }


        Debug.LogWarning(
            "LoginManager: StartupManager is not available."
        );


        // Fallback
        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowLogin();
        }
    }


    // =========================================================
    // BACK TO LOGIN
    // =========================================================
    //
    // This method can still be used from Register Panel.
    //
    // Register
    //    ↓
    // Back
    //    ↓
    // Login
    //
    // =========================================================

    public void BackToLogin()
    {
        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowLogin();


            Debug.Log(
                "LoginManager: Back to Login."
            );
        }
    }


    // =========================================================
    // LOGOUT
    // =========================================================
    //
    // IMPORTANT:
    //
    // Normally logout should be connected to:
    //
    // UIManager → Logout()
    //
    // But this method is kept for compatibility with
    // existing buttons or scripts.
    //
    // =========================================================

    public void Logout()
    {
        if (
            AccountManager.Instance != null
        )
        {
            AccountManager.Instance
                .Logout();
        }


        // -----------------------------------------------------
        // RESET LOGIN FORM
        // -----------------------------------------------------

        ClearLoginFields();


        SetMessage("");


        IsLoggingIn = false;


        LastError = "";


        // -----------------------------------------------------
        // GO TO WELCOME
        // -----------------------------------------------------

        if (
            StartupManager.Instance != null
        )
        {
            StartupManager.Instance
                .ShowWelcome();
        }
        else if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowLogin();
        }


        Debug.Log(
            "LoginManager: User logged out."
        );
    }


    // =========================================================
    // LOGIN SUCCESS
    // =========================================================
    //
    // Compatibility method.
    //
    // You normally DON'T need to connect a button directly
    // to this.
    //
    // Login() automatically calls it after successful login.
    //
    // =========================================================

    public void OnLoginSuccess()
    {
        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .OnLoginSuccess();
        }
    }


    // =========================================================
    // REGISTER SUCCESS
    // =========================================================
    //
    // Compatibility method.
    //
    // RegisterManager can call this after successful
    // registration.
    //
    // =========================================================

    public void OnRegisterSuccess()
    {
        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .OnRegisterSuccess();
        }
    }


    // =========================================================
    // CLEAR LOGIN FIELDS
    // =========================================================

    private void ClearLoginFields()
    {
        if (
            usernameInput != null
        )
        {
            usernameInput.text =
                "";
        }


        if (
            passwordInput != null
        )
        {
            passwordInput.text =
                "";
        }
    }


    // =========================================================
    // MESSAGE
    // =========================================================

    private void SetMessage(
        string message)
    {
        if (
            messageText != null
        )
        {
            messageText.text =
                message;
        }


        if (
            !string.IsNullOrWhiteSpace(
                message
            )
        )
        {
            Debug.Log(
                "LoginManager: " +
                message
            );
        }
    }


    // =========================================================
    // INPUT STATE
    // =========================================================

    private void SetInputInteractable(
        bool value)
    {
        if (
            usernameInput != null
        )
        {
            usernameInput.interactable =
                value;
        }


        if (
            passwordInput != null
        )
        {
            passwordInput.interactable =
                value;
        }
    }


    // =========================================================
    // LOGIN STATUS
    // =========================================================

    public bool IsLoggedIn()
    {
        if (
            AccountManager.Instance == null
        )
        {
            return false;
        }


        return AccountManager.Instance
            .IsUserLoggedIn();
    }


    // =========================================================
    // GET USERNAME
    // =========================================================

    public string GetUsername()
    {
        if (
            AccountManager.Instance == null
        )
        {
            return "";
        }


        return AccountManager.Instance
            .GetCurrentUsername();
    }


    // =========================================================
    // GET ACCOUNT ID
    // =========================================================

    public string GetAccountID()
    {
        if (
            AccountManager.Instance == null
        )
        {
            return "";
        }


        return AccountManager.Instance
            .GetCurrentAccountId();
    }


    // =========================================================
    // RESET LOGIN
    // =========================================================

    public void ResetLogin()
    {
        IsLoggingIn =
            false;


        LastError =
            "";
         

        ClearLoginFields();


        SetMessage("");


        SetInputInteractable(
            true
        );


        Debug.Log(
            "LoginManager: Login state reset."
        );
    }
}