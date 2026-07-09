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

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
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

        // Account
        data["username"] = username;
        data["password"] = password;

        // Participant
        data["participantID"] = Guid.NewGuid().ToString();
        data["participantName"] = "";
        data["institution"] = "";
        data["category"] = "";
        data["prompt"] = "";

        data["promptUsed"] = "";
        data["storagePath"] = "";

        data["originalImageUrl"] = "";
        data["revisedImageUrl"] = "";

        data["posterDescription"] = "";
        data["revisionPrompt"] = "";
        data["finalExplanation"] = "";

        data["score"] = 0f;

        data["promptQuality"] = 0;
        data["posterMessage"] = 0;
        data["designQuality"] = 0;
        data["accessibilityUnderstanding"] = 0;
        data["revisionProcessScore"] = 0;
        data["finalExplanationScore"] = 0;

        data["feedback"] = "";
        data["improvementSuggestion"] = "";

        data["lastPage"] = "";

        data["revisionCount"] = 0;

        data["createdDate"] =
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        await db.Collection("users").AddAsync(data);

        Debug.Log("Register Success");

        return true;
    }

    public async Task<bool> Login(string username, string password)
    {
        QuerySnapshot snapshot =
            await db.Collection("users")
            .WhereEqualTo("username", username)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            Debug.Log("Username not found.");
            return false;
        }

        DocumentSnapshot document = snapshot.Documents.First();

        Dictionary<string, object> data = document.ToDictionary();

        if (data["password"].ToString() != password)
        {
            Debug.Log("Wrong password.");
            return false;
        }

        CurrentAccount = new AccountData();

        CurrentAccount.documentID = document.Id;
        CurrentAccount.username = username;
        CurrentAccount.password = password;

        Debug.Log("Login Success");

        return true;
    }
}