    //using Firebase;
    //using Firebase.Firestore;
    //using System.Collections.Generic;
    //using System.Threading.Tasks;
    //using System;
    //using UnityEngine;
    //using System.Linq;

    //public class FirestoreAccountManager : MonoBehaviour
    //{
    //    public static FirestoreAccountManager Instance;

    //    public AccountData CurrentAccount;

    //    public bool IsReady { get; private set; }

    //    FirebaseFirestore db;



    //    private void Awake()
    //    {
    //        if (Instance == null)
    //        {
    //            Instance = this;
    //            DontDestroyOnLoad(gameObject);
    //        }
    //        else
    //        {
    //            Destroy(gameObject);
    //        }
    //    }

    //public enum LoginResult
    //{
    //    Success,
    //    UserNotFound,
    //    WrongPassword
    //}

    //public enum RegisterResult
    //{
    //    Success,
    //    UsernameExists,
    //    EmailExists
    //}

    //private async void Start()
    //{
    //    var status = await FirebaseApp.CheckAndFixDependenciesAsync();

    //    Debug.Log("Firebase Status: " + status);

    //    if (status == DependencyStatus.Available)
    //    {
    //        db = FirebaseFirestore.DefaultInstance;

    //        IsReady = true;
    //        Debug.Log("Firestore Ready");
    //    }
    //    else
    //    {
    //        Debug.LogError("Firebase failed: " + status);
    //    }
    //}

    //public async Task <RegisterResult> Register(string username,string email,string password)
    //{
    //    // Check if username already exists
    //    QuerySnapshot usernameSnapshot = await db.Collection("users").WhereEqualTo("username", username).GetSnapshotAsync();

    //    if (usernameSnapshot.Count > 0)
    //    {
    //        return RegisterResult.UsernameExists;
    //    }

    //    QuerySnapshot emailSnapshot = await db.Collection("users").WhereEqualTo("email", email).GetSnapshotAsync();

    //    if (emailSnapshot.Count > 0)
    //    {
    //        return RegisterResult.EmailExists;
    //    }



    //    Dictionary<string, object> data = new Dictionary<string, object>();

    //    //--------------------------------
    //    // Login
    //    //--------------------------------

    //    data["username"] = username;
    //    data["email"] = email;
    //    data["password"] = password;


    //    //--------------------------------
    //    // Date
    //    //--------------------------------

    //    data["createdDate"] =
    //        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    //    await db.Collection("users").AddAsync(data);

    //        Debug.Log("Register Success");

    //    return RegisterResult.Success;
    //}

    //public async Task<LoginResult> Login(string username, string password)
    //{
    //    QuerySnapshot snapshot =
    //        await db.Collection("users")
    //        .WhereEqualTo("username", username)
    //        .GetSnapshotAsync();

    //    if (snapshot.Count == 0)
    //    {
    //        Debug.Log("Username not found.");
    //        return LoginResult.UserNotFound;
    //    }

    //    DocumentSnapshot document = snapshot.Documents.First();

    //    Dictionary<string, object> data = document.ToDictionary();

    //    if (data["password"].ToString() != password)
    //    {
    //        Debug.Log("Wrong password.");
    //        return LoginResult.WrongPassword;
    //    }

    //    CurrentAccount = new AccountData();

    //    CurrentAccount.documentID = document.Id;
    //    CurrentAccount.username = GetString(data, "username");
    //    CurrentAccount.password = GetString(data, "password");

    //    PlayerPrefs.SetString("LastLoginUserID", document.Id);

    //    PlayerPrefs.Save();

    //    MainMenuManager menu = FindFirstObjectByType<MainMenuManager>();

    //    if (menu != null)
    //    {
    //        await menu.RefreshButtons();
    //    }

    //    Debug.Log("Login Success");

    //    return LoginResult.Success;
    //}

    //public async Task<bool> AutoLogin()
    //{
    //    string documentID =
    //        PlayerPrefs.GetString(
    //            "LastLoginUserID",
    //            "");

    //    if (string.IsNullOrEmpty(documentID))
    //        return false;

    //    DocumentSnapshot doc =
    //        await db.Collection("users")
    //        .Document(documentID)
    //        .GetSnapshotAsync();

    //    if (!doc.Exists)
    //        return false;

    //    Dictionary<string, object> data =
    //        doc.ToDictionary();

    //    CurrentAccount = new AccountData();

    //    MainMenuManager menu = FindFirstObjectByType<MainMenuManager>();

    //    if (menu != null)
    //    {
    //        await menu.RefreshButtons();
    //    }

    //    CurrentAccount.documentID =
    //        documentID;

    //    CurrentAccount.username =
    //        data["username"].ToString();

    //    CurrentAccount.password =
    //        data["password"].ToString();

    //    return true;
    //}

    //private string GetString(Dictionary<string, object> data, string key)
    //    {
    //        if (!data.ContainsKey(key))
    //            return "";

    //        if (data[key] == null)
    //            return "";

    //        return data[key].ToString();
    //    }

    //}