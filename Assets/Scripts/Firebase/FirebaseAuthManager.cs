using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;

    FirebaseAuth auth;



    void Awake()
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

    async void Start()
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;

            Debug.Log("Firebase Ready");
        }
        else
        {
            Debug.LogError(
                "Firebase Error : " + status);
        }
    }

    public async System.Threading.Tasks.Task<bool> Register(string email, string password, string username)
    {
        try
        {
            // Create Authentication account
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            string uid = result.User.UserId;

            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            Dictionary<string, object> data = new Dictionary<string, object>();

            data["username"] = username;

            await db.Collection("users").Document(uid).SetAsync(data);

            Debug.Log("Register Success");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);

            return false;
        }
    }

    public async System.Threading.Tasks.Task<bool> Login(string email, string password)
    {
        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);



            Debug.Log("Login Success");

            return true;
        }
        catch (System.Exception e)
        {

            Debug.LogError(e);

            return false;
        }
    }

    public bool IsLoggedIn()
    {
        return auth.CurrentUser != null;
    }

    public void Logout()
    {
        auth.SignOut();

        Debug.Log("Logout");
    }

    public string GetUID()
    {
        if (auth.CurrentUser == null)
            return "";

        return auth.CurrentUser.UserId;
    }
}