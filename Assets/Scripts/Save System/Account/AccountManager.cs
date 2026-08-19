using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }


    // =========================================================
    // FIREBASE
    // =========================================================

    private const string ACCOUNTS_COLLECTION =
        "accounts";


    // =========================================================
    // PLAYER PREFS
    // =========================================================

    private const string LAST_LOGIN_ACCOUNT_ID =
        "LastLoginAccountID";

    private const string LAST_LOGIN_USERNAME =
        "LastLoginUsername";

    private const string LAST_LOGIN_EMAIL =
        "LastLoginEmail";


    // =========================================================
    // CURRENT ACCOUNT
    // =========================================================
    //
    // CurrentAccount contains:
    //
    // accountId
    // email
    // username
    // password
    // participant profile
    //
    // IMPORTANT:
    //
    // Challenge submission data is NOT stored here.
    //
    // Challenge data is stored in:
    //
    // submissions/{accountID}_{challengeID}
    //
    // =========================================================

    public AccountData CurrentAccount
    {
        get;
        private set;
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsProcessing
    {
        get;
        private set;
    }


    public bool IsLoggedIn
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

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    // =========================================================
    // REGISTER
    // =========================================================
    //
    // Registration requires:
    //
    // email
    // username
    // password
    //
    // Firestore:
    //
    // accounts
    //     └── accountID
    //           ├── accountId
    //           ├── email
    //           ├── username
    //           ├── password
    //           └── participant
    //
    // Challenge data is NOT stored here.
    //
    // =========================================================

    public async Task<bool> Register(
        string email,
        string username,
        string password)
    {
        LastError = "";


        // -----------------------------------------------------
        // CHECK PROCESSING
        // -----------------------------------------------------

        if (IsProcessing)
        {
            SetError(
                "Account operation is already processing."
            );

            return false;
        }


        // -----------------------------------------------------
        // CLEAN INPUT
        // -----------------------------------------------------

        email =
            email != null
                ? email.Trim()
                : "";


        username =
            username != null
                ? username.Trim()
                : "";


        // -----------------------------------------------------
        // VALIDATE EMAIL
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                email
            )
        )
        {
            SetError(
                "Email is required."
            );

            return false;
        }


        if (
            !IsValidEmail(
                email
            )
        )
        {
            SetError(
                "Please enter a valid email address."
            );

            return false;
        }


        // -----------------------------------------------------
        // VALIDATE USERNAME
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                username
            )
        )
        {
            SetError(
                "Username is required."
            );

            return false;
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
            SetError(
                "Password is required."
            );

            return false;
        }


        if (
            password.Length < 6
        )
        {
            SetError(
                "Password must be at least 6 characters."
            );

            return false;
        }


        // -----------------------------------------------------
        // FIREBASE MANAGER
        // -----------------------------------------------------

        if (
            FirebaseManager.Instance == null
        )
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        // -----------------------------------------------------
        // WAIT FOR FIREBASE
        // -----------------------------------------------------

        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        IsProcessing = true;


        try
        {
            // -------------------------------------------------
            // CHECK USERNAME
            // -------------------------------------------------

            List<DocumentSnapshot>
                existingUsernameAccounts =
                await FirebaseManager.Instance
                    .GetDocumentsByField(
                        ACCOUNTS_COLLECTION,
                        "username",
                        username
                    );


            if (
                existingUsernameAccounts != null &&
                existingUsernameAccounts.Count > 0
            )
            {
                SetError(
                    "Username already exists."
                );

                return false;
            }


            // -------------------------------------------------
            // CHECK EMAIL
            // -------------------------------------------------

            List<DocumentSnapshot>
                existingEmailAccounts =
                await FirebaseManager.Instance
                    .GetDocumentsByField(
                        ACCOUNTS_COLLECTION,
                        "email",
                        email
                    );


            if (
                existingEmailAccounts != null &&
                existingEmailAccounts.Count > 0
            )
            {
                SetError(
                    "Email is already registered."
                );

                return false;
            }


            // -------------------------------------------------
            // CREATE ACCOUNT ID
            // -------------------------------------------------

            string accountID =
                Guid.NewGuid().ToString();


            // -------------------------------------------------
            // CREATE EMPTY PARTICIPANT PROFILE
            // -----------------------------------------------------

            ParticipantData participant =
                new ParticipantData();


            participant.accountID =
                accountID;


            participant.email =
                email;


            participant.username =
                username;


            // -------------------------------------------------
            // CREATE ACCOUNT
            // -------------------------------------------------

            AccountData account =
                new AccountData
                {
                    accountId =
                        accountID,

                    email =
                        email,

                    username =
                        username,

                    password =
                        password,

                    participant =
                        participant
                };


            // -------------------------------------------------
            // FIRESTORE DATA
            // -------------------------------------------------

            Dictionary<string, object>
                data =
                AccountToDictionary(
                    account
                );


            // -------------------------------------------------
            // SAVE ACCOUNT
            // -------------------------------------------------

            bool saved =
                await FirebaseManager.Instance
                    .SaveDocument(
                        ACCOUNTS_COLLECTION,
                        accountID,
                        data
                    );


            if (!saved)
            {
                SetError(
                    "Failed to create account: " +
                    FirebaseManager.Instance.LastError
                );

                return false;
            }


            // -------------------------------------------------
            // SET CURRENT ACCOUNT
            // -------------------------------------------------

            CurrentAccount =
                account;


            IsLoggedIn =
                true;


            // -------------------------------------------------
            // SAVE LOGIN SESSION
            // -------------------------------------------------

            SaveLoginSession(
                account
            );


            // -------------------------------------------------
            // SYNCHRONIZE PARTICIPANT MANAGER
            // -------------------------------------------------

            SynchronizeParticipantManager(
                participant
            );


            Debug.Log(
                "AccountManager: Account registered successfully."
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            SetError(
                "Registration failed: " +
                exception.Message
            );


            return false;
        }
        finally
        {
            IsProcessing =
                false;
        }
    }


    // =========================================================
    // LOGIN
    // =========================================================
    //
    // Login still uses:
    //
    // username
    // password
    //
    // Email is loaded from Firestore automatically.
    //
    // =========================================================

    public async Task<bool> Login(
        string username,
        string password)
    {
        LastError = "";


        // -----------------------------------------------------
        // CHECK PROCESSING
        // -----------------------------------------------------

        if (IsProcessing)
        {
            SetError(
                "Account operation is already processing."
            );

            return false;
        }


        // -----------------------------------------------------
        // CLEAN USERNAME
        // -----------------------------------------------------

        username =
            username != null
                ? username.Trim()
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
            SetError(
                "Username is required."
            );

            return false;
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
            SetError(
                "Password is required."
            );

            return false;
        }


        // -----------------------------------------------------
        // FIREBASE
        // -----------------------------------------------------

        if (
            FirebaseManager.Instance == null
        )
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        IsProcessing = true;


        try
        {
            // -------------------------------------------------
            // FIND ACCOUNT BY USERNAME
            // -------------------------------------------------

            List<DocumentSnapshot>
                documents =
                await FirebaseManager.Instance
                    .GetDocumentsByField(
                        ACCOUNTS_COLLECTION,
                        "username",
                        username
                    );


            if (
                documents == null ||
                documents.Count == 0
            )
            {
                SetError(
                    "Username not found."
                );

                return false;
            }


            DocumentSnapshot document =
                documents[0];


            if (
                document == null ||
                !document.Exists
            )
            {
                SetError(
                    "Account not found."
                );

                return false;
            }


            // -------------------------------------------------
            // GET PASSWORD
            // -------------------------------------------------

            string savedPassword =
                GetString(
                    document,
                    "password"
                );


            if (
                string.IsNullOrEmpty(
                    savedPassword
                )
            )
            {
                SetError(
                    "Account password data is missing."
                );

                return false;
            }


            // -------------------------------------------------
            // CHECK PASSWORD
            // -------------------------------------------------

            if (
                savedPassword != password
            )
            {
                SetError(
                    "Incorrect password."
                );

                return false;
            }


            // -------------------------------------------------
            // CONVERT FIRESTORE → ACCOUNT
            // -------------------------------------------------

            AccountData account =
                DocumentToAccount(
                    document
                );


            if (account == null)
            {
                SetError(
                    "Failed to load account data."
                );

                return false;
            }


            // -------------------------------------------------
            // ENSURE ACCOUNT ID
            // -------------------------------------------------

            if (
                string.IsNullOrWhiteSpace(
                    account.accountId
                )
            )
            {
                account.accountId =
                    document.Id;
            }


            // -------------------------------------------------
            // ENSURE PARTICIPANT
            // -------------------------------------------------

            if (
                account.participant == null
            )
            {
                account.participant =
                    new ParticipantData();
            }


            // -------------------------------------------------
            // RESTORE ACCOUNT INFORMATION
            // -------------------------------------------------

            account.participant.accountID =
                account.accountId;


            account.participant.email =
                account.email;


            account.participant.username =
                account.username;


            // -------------------------------------------------
            // SET CURRENT ACCOUNT
            // -------------------------------------------------

            CurrentAccount =
                account;


            IsLoggedIn =
                true;


            // -------------------------------------------------
            // SAVE LOGIN SESSION
            // -------------------------------------------------

            SaveLoginSession(
                account
            );


            // -------------------------------------------------
            // SYNCHRONIZE PARTICIPANT PROFILE
            // -------------------------------------------------

            SynchronizeParticipantManager(
                account.participant
            );


            Debug.Log(
                "AccountManager: Login successful."
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            SetError(
                "Login failed: " +
                exception.Message
            );


            return false;
        }
        finally
        {
            IsProcessing =
                false;
        }
    }


    // =========================================================
    // AUTO LOGIN
    // =========================================================
    //
    // Loads the last account saved in PlayerPrefs.
    //
    // IMPORTANT:
    //
    // This restores only the account/profile.
    //
    // Challenge submission is loaded separately.
    //
    // =========================================================

    public async Task<bool> AutoLogin()
    {
        LastError = "";


        string accountID =
            PlayerPrefs.GetString(
                LAST_LOGIN_ACCOUNT_ID,
                ""
            );


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            return false;
        }


        if (
            FirebaseManager.Instance == null
        )
        {
            return false;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            return false;
        }


        IsProcessing = true;


        try
        {
            // -------------------------------------------------
            // GET ACCOUNT
            // -------------------------------------------------

            DocumentSnapshot document =
                await FirebaseManager.Instance
                    .GetDocument(
                        ACCOUNTS_COLLECTION,
                        accountID
                    );


            if (
                document == null ||
                !document.Exists
            )
            {
                ClearLoginSession();

                return false;
            }


            // -------------------------------------------------
            // CONVERT ACCOUNT
            // -------------------------------------------------

            AccountData account =
                DocumentToAccount(
                    document
                );


            if (account == null)
            {
                ClearLoginSession();

                return false;
            }


            // -------------------------------------------------
            // ENSURE ACCOUNT ID
            // -------------------------------------------------

            if (
                string.IsNullOrWhiteSpace(
                    account.accountId
                )
            )
            {
                account.accountId =
                    document.Id;
            }


            // -------------------------------------------------
            // ENSURE PARTICIPANT
            // -------------------------------------------------

            if (
                account.participant == null
            )
            {
                account.participant =
                    new ParticipantData();
            }


            // -------------------------------------------------
            // RESTORE ACCOUNT INFORMATION
            // -------------------------------------------------

            account.participant.accountID =
                account.accountId;


            account.participant.email =
                account.email;


            account.participant.username =
                account.username;


            // -------------------------------------------------
            // SET CURRENT ACCOUNT
            // -------------------------------------------------

            CurrentAccount =
                account;


            IsLoggedIn =
                true;


            // -------------------------------------------------
            // SYNCHRONIZE PARTICIPANT PROFILE
            // -------------------------------------------------

            SynchronizeParticipantManager(
                account.participant
            );


            Debug.Log(
                "AccountManager: Auto login successful."
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            Debug.LogWarning(
                "AccountManager: AutoLogin failed: " +
                exception.Message
            );


            CurrentAccount =
                null;


            IsLoggedIn =
                false;


            ClearLoginSession();


            return false;
        }
        finally
        {
            IsProcessing =
                false;
        }
    }


    // =========================================================
    // LOGOUT
    // =========================================================
    //
    // Logout ONLY clears local memory.
    //
    // Firebase account and submissions remain.
    //
    // =========================================================

    public void Logout()
    {
        CurrentAccount =
            null;


        IsLoggedIn =
            false;


        LastError =
            "";


        ClearLoginSession();


        // -----------------------------------------------------
        // RESET PARTICIPANT
        // -----------------------------------------------------

        if (
            ParticipantManager.Instance != null
        )
        {
            ParticipantManager.Instance
                .ResetParticipant();
        }


        // -----------------------------------------------------
        // RESET COMPETITION
        // -----------------------------------------------------

        if (
            CompetitionManager.Instance != null
        )
        {
            CompetitionManager.Instance
                .ResetCompetition();
        }


        // -----------------------------------------------------
        // CLEAR SUBMISSION
        // -----------------------------------------------------

        if (
            SubmissionManager.Instance != null
        )
        {
            SubmissionManager.Instance
                .Clear();
        }


        Debug.Log(
            "AccountManager: Logged out."
        );
    }


    // =========================================================
    // DELETE ACCOUNT
    // =========================================================
    //
    // Deletes:
    //
    // accounts/{accountID}
    //
    // IMPORTANT:
    //
    // Challenge submissions are currently NOT deleted here.
    //
    // =========================================================

    public async Task<bool> DeleteAccount()
    {
        LastError = "";


        if (IsProcessing)
        {
            SetError(
                "Account operation is already processing."
            );

            return false;
        }


        if (!IsLoggedIn)
        {
            SetError(
                "No account is currently logged in."
            );

            return false;
        }


        if (CurrentAccount == null)
        {
            SetError(
                "Current account is not available."
            );

            return false;
        }


        if (FirebaseManager.Instance == null)
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        string accountID =
            CurrentAccount.accountId;


        if (
            string.IsNullOrWhiteSpace(
                accountID
            )
        )
        {
            SetError(
                "Account ID is missing."
            );

            return false;
        }


        IsProcessing = true;


        try
        {
            // -------------------------------------------------
            // DELETE ACCOUNT
            // -------------------------------------------------

            bool deleted =
                await FirebaseManager.Instance
                    .DeleteDocument(
                        ACCOUNTS_COLLECTION,
                        accountID
                    );


            if (!deleted)
            {
                SetError(
                    "Failed to delete account: " +
                    FirebaseManager.Instance.LastError
                );

                return false;
            }


            // -------------------------------------------------
            // RESET ACCOUNT
            // -------------------------------------------------

            CurrentAccount =
                null;


            IsLoggedIn =
                false;


            ClearLoginSession();


            // -------------------------------------------------
            // RESET PARTICIPANT
            // -------------------------------------------------

            if (
                ParticipantManager.Instance != null
            )
            {
                ParticipantManager.Instance
                    .ResetParticipant();
            }


            // -------------------------------------------------
            // RESET COMPETITION
            // -------------------------------------------------

            if (
                CompetitionManager.Instance != null
            )
            {
                CompetitionManager.Instance
                    .ResetCompetition();
            }


            // -------------------------------------------------
            // CLEAR SUBMISSION
            // -------------------------------------------------

            if (
                SubmissionManager.Instance != null
            )
            {
                SubmissionManager.Instance
                    .Clear();
            }


            Debug.Log(
                "AccountManager: Account deleted successfully."
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            SetError(
                "Account deletion failed: " +
                exception.Message
            );


            return false;
        }
        finally
        {
            IsProcessing =
                false;
        }
    }


    // =========================================================
    // SAVE CURRENT ACCOUNT
    // =========================================================
    //
    // Saves ONLY:
    //
    // account information
    // participant profile
    //
    // It does NOT save challenge submission data.
    //
    // =========================================================

    public async Task<bool> SaveCurrentAccount()
    {
        LastError = "";


        if (CurrentAccount == null)
        {
            SetError(
                "Current account is not available."
            );

            return false;
        }


        if (
            string.IsNullOrWhiteSpace(
                CurrentAccount.accountId
            )
        )
        {
            SetError(
                "Account ID is missing."
            );

            return false;
        }


        if (FirebaseManager.Instance == null)
        {
            SetError(
                "Firebase Manager is not available."
            );

            return false;
        }


        if (
            !await FirebaseManager.Instance
                .WaitUntilReady()
        )
        {
            SetError(
                "Firebase is not ready."
            );

            return false;
        }


        try
        {
            // -------------------------------------------------
            // MAKE SURE PARTICIPANT EXISTS
            // -------------------------------------------------

            if (
                CurrentAccount.participant == null
            )
            {
                CurrentAccount.participant =
                    new ParticipantData();
            }


            // -------------------------------------------------
            // RESTORE ACCOUNT IDENTIFIERS
            // -------------------------------------------------

            CurrentAccount.participant.accountID =
                CurrentAccount.accountId;


            CurrentAccount.participant.email =
                CurrentAccount.email;


            CurrentAccount.participant.username =
                CurrentAccount.username;


            // -------------------------------------------------
            // CREATE FIRESTORE DATA
            // -------------------------------------------------

            Dictionary<string, object>
                data =
                AccountToDictionary(
                    CurrentAccount
                );


            // -------------------------------------------------
            // SAVE
            // -------------------------------------------------

            bool saved =
                await FirebaseManager.Instance
                    .UpdateDocument(
                        ACCOUNTS_COLLECTION,
                        CurrentAccount.accountId,
                        data
                    );


            if (!saved)
            {
                SetError(
                    "Failed to save account: " +
                    FirebaseManager.Instance.LastError
                );

                return false;
            }


            Debug.Log(
                "AccountManager: Current account saved."
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            SetError(
                "Failed to save account: " +
                exception.Message
            );


            return false;
        }
    }


    // =========================================================
    // GET CURRENT ACCOUNT ID
    // =========================================================

    public string GetCurrentAccountId()
    {
        if (CurrentAccount == null)
        {
            return "";
        }


        return
            CurrentAccount.accountId;
    }


    // =========================================================
    // GET CURRENT USERNAME
    // =========================================================

    public string GetCurrentUsername()
    {
        if (CurrentAccount == null)
        {
            return "";
        }


        return
            CurrentAccount.username;
    }


    // =========================================================
    // GET CURRENT EMAIL
    // =========================================================

    public string GetCurrentEmail()
    {
        if (CurrentAccount == null)
        {
            return "";
        }


        return
            CurrentAccount.email;
    }


    // =========================================================
    // CHECK LOGIN
    // =========================================================

    public bool IsUserLoggedIn()
    {
        return
            IsLoggedIn &&
            CurrentAccount != null;
    }


    // =========================================================
    // GET CURRENT ACCOUNT
    // =========================================================

    public AccountData GetCurrentAccount()
    {
        return
            CurrentAccount;
    }


    // =========================================================
    // SAVE LOGIN SESSION
    // =========================================================

    private void SaveLoginSession(
        AccountData account)
    {
        if (account == null)
        {
            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                account.accountId
            )
        )
        {
            return;
        }


        PlayerPrefs.SetString(
            LAST_LOGIN_ACCOUNT_ID,
            account.accountId
        );


        PlayerPrefs.SetString(
            LAST_LOGIN_USERNAME,
            account.username ?? ""
        );


        PlayerPrefs.SetString(
            LAST_LOGIN_EMAIL,
            account.email ?? ""
        );


        PlayerPrefs.Save();
    }


    // =========================================================
    // CLEAR LOGIN SESSION
    // =========================================================

    private void ClearLoginSession()
    {
        PlayerPrefs.DeleteKey(
            LAST_LOGIN_ACCOUNT_ID
        );


        PlayerPrefs.DeleteKey(
            LAST_LOGIN_USERNAME
        );


        PlayerPrefs.DeleteKey(
            LAST_LOGIN_EMAIL
        );


        // -----------------------------------------------------
        // OLD COMPATIBILITY KEY
        // -----------------------------------------------------

        PlayerPrefs.DeleteKey(
            "LastLoginUserID"
        );


        PlayerPrefs.DeleteKey(
            "LastLoginUsername"
        );


        PlayerPrefs.Save();
    }


    // =========================================================
    // VALIDATE EMAIL
    // =========================================================
    //
    // Examples:
    //
    // user@gmail.com          TRUE
    // user.name@gmail.com     TRUE
    // user@gmail.com.my       TRUE
    //
    // user                   FALSE
    // user@                   FALSE
    // @gmail.com              FALSE
    // user@gmail              FALSE
    // user@gmail.             FALSE
    // user @gmail.com         FALSE
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
        // EMAIL LENGTH
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
        // BASIC EMAIL REGEX
        // -----------------------------------------------------

        const string pattern =
            @"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@" +
            @"[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$";


        if (
            !Regex.IsMatch(
                email,
                pattern
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
        // GET LOCAL PART
        // -----------------------------------------------------

        string localPart =
            email.Substring(
                0,
                atIndex
            );


        // -----------------------------------------------------
        // GET DOMAIN
        // -----------------------------------------------------

        string domain =
            email.Substring(
                atIndex + 1
            );


        if (
            string.IsNullOrWhiteSpace(
                localPart
            ) ||
            string.IsNullOrWhiteSpace(
                domain
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN MUST CONTAIN DOT
        // -----------------------------------------------------

        int dotIndex =
            domain.LastIndexOf(
                '.'
            );


        if (
            dotIndex <= 0 ||
            dotIndex >= domain.Length - 1
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // DOMAIN MUST NOT START / END WITH HYPHEN
        // -----------------------------------------------------

        if (
            domain.StartsWith(
                "-"
            ) ||
            domain.EndsWith(
                "-"
            )
        )
        {
            return false;
        }


        // -----------------------------------------------------
        // CHECK DOMAIN LABELS
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


            if (
                part.StartsWith(
                    "-"
                ) ||
                part.EndsWith(
                    "-"
                )
            )
            {
                return false;
            }
        }


        // -----------------------------------------------------
        // VALID
        // -----------------------------------------------------

        return true;
    }


    // =========================================================
    // DOCUMENT → ACCOUNT
    // =========================================================

    private AccountData DocumentToAccount(
        DocumentSnapshot document)
    {
        if (
            document == null ||
            !document.Exists
        )
        {
            return null;
        }


        AccountData account =
            new AccountData();


        // -----------------------------------------------------
        // ACCOUNT ID
        // -----------------------------------------------------

        account.accountId =
            GetString(
                document,
                "accountId"
            );


        if (
            string.IsNullOrWhiteSpace(
                account.accountId
            )
        )
        {
            account.accountId =
                document.Id;
        }


        // -----------------------------------------------------
        // EMAIL
        // -----------------------------------------------------

        account.email =
            GetString(
                document,
                "email"
            );


        // -----------------------------------------------------
        // USERNAME
        // -----------------------------------------------------

        account.username =
            GetString(
                document,
                "username"
            );


        // -----------------------------------------------------
        // PASSWORD
        // -----------------------------------------------------

        account.password =
            GetString(
                document,
                "password"
            );


        // -----------------------------------------------------
        // PARTICIPANT PROFILE
        // -----------------------------------------------------

        account.participant =
            null;


        if (
            document.ContainsField(
                "participant"
            )
        )
        {
            try
            {
                Dictionary<string, object>
                    participantMap =
                    document.GetValue<
                        Dictionary<string, object>
                    >(
                        "participant"
                    );


                account.participant =
                    ParticipantFromDictionary(
                        participantMap
                    );
            }
            catch (
                Exception exception
            )
            {
                Debug.LogWarning(
                    "AccountManager: Failed to load participant profile: " +
                    exception.Message
                );


                account.participant =
                    null;
            }
        }


        // -----------------------------------------------------
        // CREATE EMPTY PROFILE IF MISSING
        // -----------------------------------------------------

        if (
            account.participant == null
        )
        {
            account.participant =
                new ParticipantData();
        }


        // -----------------------------------------------------
        // RESTORE ACCOUNT IDENTIFIERS
        // -----------------------------------------------------

        account.participant.accountID =
            account.accountId;


        account.participant.email =
            account.email;


        account.participant.username =
            account.username;


        return account;
    }


    // =========================================================
    // ACCOUNT → DICTIONARY
    // =========================================================
    //
    // Only permanent account/profile information is saved.
    //
    // Challenge submission data is NOT stored here.
    //
    // =========================================================

    private Dictionary<string, object>
        AccountToDictionary(
            AccountData account)
    {
        if (account == null)
        {
            return
                new Dictionary<string, object>();
        }


        return
            new Dictionary<string, object>
            {
                {
                    "accountId",
                    account.accountId ?? ""
                },

                {
                    "email",
                    account.email ?? ""
                },

                {
                    "username",
                    account.username ?? ""
                },

                {
                    "password",
                    account.password ?? ""
                },

                {
                    "participant",
                    ParticipantToDictionary(
                        account.participant
                    )
                }
            };
    }


    // =========================================================
    // PARTICIPANT → DICTIONARY
    // =========================================================
    //
    // ONLY permanent participant profile fields are saved.
    //
    // Challenge fields such as:
    //
    // prompt
    // poster
    // revision
    // score
    // feedback
    // submissionID
    //
    // are NOT saved here.
    //
    // =========================================================

    private Dictionary<string, object>
        ParticipantToDictionary(
            ParticipantData participant)
    {
        if (participant == null)
        {
            participant =
                new ParticipantData();
        }


        return
            new Dictionary<string, object>
            {
                {
                    "participantName",
                    participant.participantName ?? ""
                },

                {
                    "institution",
                    participant.institution ?? ""
                },

                {
                    "categoryType",
                    participant.categoryType ?? ""
                },

                {
                    "subCategory",
                    participant.subCategory ?? ""
                }
            };
    }


    // =========================================================
    // PARTICIPANT DICTIONARY → PARTICIPANT
    // =========================================================
    //
    // Loads ONLY permanent participant profile fields.
    //
    // =========================================================

    private ParticipantData
        ParticipantFromDictionary(
            Dictionary<string, object> data)
    {
        if (data == null)
        {
            return null;
        }


        ParticipantData participant =
            new ParticipantData();


        // -----------------------------------------------------
        // PARTICIPANT DETAILS
        // -----------------------------------------------------

        participant.participantName =
            GetString(
                data,
                "participantName"
            );


        participant.institution =
            GetString(
                data,
                "institution"
            );


        participant.categoryType =
            GetString(
                data,
                "categoryType"
            );


        participant.subCategory =
            GetString(
                data,
                "subCategory"
            );


        return participant;
    }


    // =========================================================
    // SYNCHRONIZE PARTICIPANT MANAGER
    // =========================================================
    //
    // Transfers ONLY the permanent profile.
    //
    // Does NOT load:
    //
    // challengeID
    // submissionID
    // prompt
    // poster
    // score
    // revision
    // feedback
    //
    // Those are loaded separately by ParticipantManager.
    //
    // =========================================================

    private void SynchronizeParticipantManager(
        ParticipantData participant)
    {
        if (
            ParticipantManager.Instance == null
        )
        {
            return;
        }


        // -----------------------------------------------------
        // RESET LOCAL PARTICIPANT STATE
        // -----------------------------------------------------

        ParticipantManager.Instance
            .ResetParticipant();


        // -----------------------------------------------------
        // NO PROFILE
        // -----------------------------------------------------

        if (participant == null)
        {
            return;
        }


        // -----------------------------------------------------
        // ACCOUNT INFORMATION
        // -----------------------------------------------------

        ParticipantManager.Instance
            .CurrentParticipant
            .accountID =
                CurrentAccount != null
                    ? CurrentAccount.accountId
                    : participant.accountID;


        ParticipantManager.Instance
            .CurrentParticipant
            .email =
                CurrentAccount != null
                    ? CurrentAccount.email
                    : participant.email;


        ParticipantManager.Instance
            .CurrentParticipant
            .username =
                CurrentAccount != null
                    ? CurrentAccount.username
                    : participant.username;


        // -----------------------------------------------------
        // PERMANENT PARTICIPANT PROFILE
        // -----------------------------------------------------

        ParticipantManager.Instance
            .CurrentParticipant
            .participantName =
                participant.participantName;


        ParticipantManager.Instance
            .CurrentParticipant
            .institution =
                participant.institution;


        ParticipantManager.Instance
            .CurrentParticipant
            .categoryType =
                participant.categoryType;


        ParticipantManager.Instance
            .CurrentParticipant
            .subCategory =
                participant.subCategory;


        Debug.Log(
            "AccountManager: Participant profile synchronized."
        );
    }


    // =========================================================
    // GET STRING FROM FIRESTORE DOCUMENT
    // =========================================================

    private string GetString(
        DocumentSnapshot document,
        string field)
    {
        if (
            document == null ||
            !document.Exists ||
            !document.ContainsField(
                field
            )
        )
        {
            return "";
        }


        try
        {
            object value =
                document.GetValue<object>(
                    field
                );


            if (value == null)
            {
                return "";
            }


            return
                value.ToString();
        }
        catch
        {
            return "";
        }
    }


    // =========================================================
    // GET STRING FROM DICTIONARY
    // =========================================================

    private string GetString(
        Dictionary<string, object> data,
        string field)
    {
        if (
            data == null ||
            !data.ContainsKey(
                field
            ) ||
            data[field] == null
        )
        {
            return "";
        }


        try
        {
            return
                data[field].ToString();
        }
        catch
        {
            return "";
        }
    }


    // =========================================================
    // SET ERROR
    // =========================================================

    private void SetError(
        string message)
    {
        LastError =
            message;


        Debug.LogError(
            "AccountManager: " +
            message
        );
    }


    // =========================================================
    // ACCOUNT DATA
    // =========================================================

    [Serializable]
    public class AccountData
    {
        public string accountId;

        public string email;

        public string username;

        public string password;

        public ParticipantData participant;
    }
}