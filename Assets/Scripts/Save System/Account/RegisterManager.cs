using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class RegisterManager : MonoBehaviour
{
    // =========================================================
    // REGISTER UI
    // =========================================================

    [Header("Register UI")]

    [SerializeField]
    private TMP_InputField emailInput;

    [SerializeField]
    private TMP_InputField usernameInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private TMP_InputField confirmPasswordInput;

    [SerializeField]
    private TMP_Text messageText;


    // =========================================================
    // STATE
    // =========================================================

    public bool IsRegistering
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
    // REGISTER
    // =========================================================

    public async void Register()
    {
        if (IsRegistering)
        {
            return;
        }


        LastError = "";


        // =====================================================
        // GET INPUT
        // =====================================================

        string email =
            emailInput != null
                ? emailInput.text.Trim()
                : "";


        string username =
            usernameInput != null
                ? usernameInput.text.Trim()
                : "";


        string password =
            passwordInput != null
                ? passwordInput.text
                : "";


        string confirmPassword =
            confirmPasswordInput != null
                ? confirmPasswordInput.text
                : "";


        // =====================================================
        // EMAIL
        // =====================================================

        if (
            string.IsNullOrWhiteSpace(
                email
            )
        )
        {
            SetMessage(
                "Please enter email."
            );

            return;
        }


        // =====================================================
        // EMAIL FORMAT
        // =====================================================

        if (
            !IsValidEmail(
                email
            )
        )
        {
            SetMessage(
                "Please enter a valid email address."
            );

            return;
        }


        // =====================================================
        // USERNAME
        // =====================================================

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


        // =====================================================
        // PASSWORD
        // =====================================================

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


        // =====================================================
        // PASSWORD LENGTH
        // =====================================================

        if (
            password.Length < 6
        )
        {
            SetMessage(
                "Password must be at least 6 characters."
            );

            return;
        }


        // =====================================================
        // CONFIRM PASSWORD
        // =====================================================

        if (
            string.IsNullOrWhiteSpace(
                confirmPassword
            )
        )
        {
            SetMessage(
                "Please confirm your password."
            );

            return;
        }


        // =====================================================
        // PASSWORD MATCH
        // =====================================================

        if (
            password != confirmPassword
        )
        {
            SetMessage(
                "Passwords do not match."
            );

            return;
        }


        // =====================================================
        // ACCOUNT MANAGER
        // =====================================================

        if (
            AccountManager.Instance == null
        )
        {
            SetMessage(
                "Account Manager is not available."
            );

            return;
        }


        // =====================================================
        // START REGISTERING
        // =====================================================

        IsRegistering = true;


        SetInputInteractable(
            false
        );


        SetMessage(
            "Creating account..."
        );


        try
        {
            // =================================================
            // REGISTER ACCOUNT
            // =================================================
            //
            // IMPORTANT:
            //
            // New AccountManager requires:
            //
            // email
            // username
            // password
            //
            // =================================================

            bool success =
                await AccountManager.Instance
                    .Register(
                        email,
                        username,
                        password
                    );


            // =================================================
            // REGISTRATION FAILED
            // =================================================

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
                        "Registration failed. Please try again.";
                }


                SetMessage(
                    error
                );


                return;
            }


            // =================================================
            // REGISTRATION SUCCESS
            // =================================================

            Debug.Log(
                "RegisterManager: Registration successful."
            );


            // =================================================
            // CLEAR REGISTER INPUTS
            // =================================================

            ClearRegisterFields();


            SetMessage(
                ""
            );


            // =================================================
            // OPEN MAIN MENU
            // =================================================

            if (
                UIManager.Instance != null
            )
            {
                UIManager.Instance
                    .ShowMainMenu();
            }
        }
        catch (
            Exception exception
        )
        {
            LastError =
                exception.Message;


            SetMessage(
                "Registration failed: " +
                exception.Message
            );


            Debug.LogError(
                "RegisterManager: " +
                exception
            );
        }
        finally
        {
            IsRegistering =
                false;


            SetInputInteractable(
                true
            );
        }
    }


    // =========================================================
    // EMAIL VALIDATION
    // =========================================================
    //
    // Valid examples:
    //
    // user@gmail.com
    // user123@gmail.com
    // user.name@gmail.com
    // user.name@yahoo.com
    // user@company.com.my
    //
    // Invalid examples:
    //
    // user
    // user@
    // @gmail.com
    // user@gmail
    // user@gmail.
    // user @gmail.com
    // user@gmail..com
    //
    // =========================================================

    private bool IsValidEmail(
        string email)
    {
        if (
            string.IsNullOrWhiteSpace(
                email
            )
        )
        {
            return false;
        }


        email =
            email.Trim();


        // -----------------------------------------------------
        // MAX EMAIL LENGTH
        // -----------------------------------------------------

        if (
            email.Length > 254
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // NO SPACES
        // -----------------------------------------------------

        if (
            email.Contains(
                " "
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // EXACTLY ONE @
        // -----------------------------------------------------

        int atIndex =
            email.IndexOf(
                '@'
            );


        if (
            atIndex <= 0
        )
        {
            return false;
        }


        if (
            atIndex !=
            email.LastIndexOf(
                '@'
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // LOCAL PART
        // -----------------------------------------------------

        string localPart =
            email.Substring(
                0,
                atIndex
            );


        if (
            string.IsNullOrWhiteSpace(
                localPart
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN
        // -----------------------------------------------------

        string domain =
            email.Substring(
                atIndex + 1
            );


        if (
            string.IsNullOrWhiteSpace(
                domain
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN MUST HAVE DOT
        // -----------------------------------------------------

        int dotIndex =
            domain.LastIndexOf(
                '.'
            );


        if (
            dotIndex <= 0
        )
        {
            return false;
        }


        if (
            dotIndex >=
            domain.Length - 1
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN CANNOT HAVE CONSECUTIVE DOTS
        // -----------------------------------------------------

        if (
            domain.Contains(
                ".."
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN PARTS
        // -----------------------------------------------------

        string[] domainParts =
            domain.Split(
                '.'
            );


        foreach (
            string part
            in domainParts)
        {
            if (
                string.IsNullOrWhiteSpace(
                    part
                )
            )
            {
                return false;
            }


            // Domain labels cannot start with -
            if (
                part.StartsWith(
                    "-"
                )
            )
            {
                return false;
            }


            // Domain labels cannot end with -
            if (
                part.EndsWith(
                    "-"
                )
            )
            {
                return false;
            }
        }


        // -----------------------------------------------------
        // FINAL REGEX CHECK
        // -----------------------------------------------------

        const string pattern =
            @"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+" +
            @"@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$";


        return
            System.Text.RegularExpressions.Regex.IsMatch(
                email,
                pattern
            );
    }


    // =========================================================
    // BACK TO LOGIN
    // =========================================================

    public void BackToLogin()
    {
        ClearRegisterFields();


        SetMessage(
            ""
        );


        if (
            UIManager.Instance != null
        )
        {
            UIManager.Instance
                .ShowLogin();
        }
    }


    // =========================================================
    // OPEN LOGIN
    // =========================================================

    public void OpenLogin()
    {
        BackToLogin();
    }


    // =========================================================
    // CLEAR INPUTS
    // =========================================================

    private void ClearRegisterFields()
    {
        if (
            emailInput != null
        )
        {
            emailInput.text =
                "";
        }


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


        if (
            confirmPasswordInput != null
        )
        {
            confirmPasswordInput.text =
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
                "RegisterManager: " +
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
            emailInput != null
        )
        {
            emailInput.interactable =
                value;
        }


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


        if (
            confirmPasswordInput != null
        )
        {
            confirmPasswordInput.interactable =
                value;
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetRegister()
    {
        IsRegistering =
            false;
         

        LastError =
            "";


        ClearRegisterFields();


        SetMessage(
            ""
        );


        SetInputInteractable(
            true
        );
    }
}