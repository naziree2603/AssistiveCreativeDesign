    using Firebase;
    using Firebase.Firestore;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;
    using UnityEngine;
    using System.Linq;

    public class FirestoreAccountManager : MonoBehaviour
    {
        public static FirestoreAccountManager Instance;

        public AccountData CurrentAccount;

        FirebaseFirestore db;



        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

    public enum LoginResult
    {
        Success,
        UserNotFound,
        WrongPassword
    }

    private async void Start()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        Debug.Log("Firebase Status: " + status);

        if (status == DependencyStatus.Available)
        {
            db = FirebaseFirestore.DefaultInstance;
            Debug.Log("Firestore Ready");
        }
        else
        {
            Debug.LogError("Firebase failed: " + status);
        }
    }

    public async Task<bool> Register(string username, string password)
        {
            // Check if username already exists
            QuerySnapshot snapshot =
                await db.Collection("users")
                .WhereEqualTo("username", username)
                .GetSnapshotAsync();

            if (snapshot.Count > 0)
            {
                Debug.Log("Username already exists.");

                return false;
            }

            Dictionary<string, object> data = new Dictionary<string, object>();

        //--------------------------------
        // Login
        //--------------------------------

        data["username"] = username;
        data["password"] = password;


        //--------------------------------
        // Date
        //--------------------------------

        data["createdDate"] =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        await db.Collection("users").AddAsync(data);

            Debug.Log("Register Success");

            return true;
        }

    public async Task<LoginResult> Login(string username, string password)
    {
        QuerySnapshot snapshot =
            await db.Collection("users")
            .WhereEqualTo("username", username)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            Debug.Log("Username not found.");
            return LoginResult.UserNotFound;
        }

        DocumentSnapshot document = snapshot.Documents.First();

        Dictionary<string, object> data = document.ToDictionary();

        if (data["password"].ToString() != password)
        {
            Debug.Log("Wrong password.");
            return LoginResult.WrongPassword;
        }

        CurrentAccount = new AccountData();

        CurrentAccount.documentID = document.Id;
        CurrentAccount.username = GetString(data, "username");
        CurrentAccount.password = GetString(data, "password");

        MainMenuManager menu = FindFirstObjectByType<MainMenuManager>();

        if (menu != null)
        {
            await menu.RefreshButtons();
        }

        Debug.Log("Login Success");

        return LoginResult.Success;
    }

    private string GetString(Dictionary<string, object> data, string key)
        {
            if (!data.ContainsKey(key))
                return "";

            if (data[key] == null)
                return "";

            return data[key].ToString();
        }

    }