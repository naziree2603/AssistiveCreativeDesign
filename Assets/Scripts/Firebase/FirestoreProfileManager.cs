    //using Firebase.Firestore;
    //using System.Collections.Generic;
    //using System.Threading.Tasks;
    //using System.Linq;
    //using UnityEngine;

    //public class FirestoreProfileManager : MonoBehaviour
    //{
    //    public static FirestoreProfileManager Instance;

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

    //        db = FirebaseFirestore.DefaultInstance;
    //    }

    //    public async Task SaveProfile(ProfileData profile)
    //    {
    //        Dictionary<string, object> data =
    //            new Dictionary<string, object>();

    //        data["accountID"] = profile.accountID;
    //        data["participantID"] = profile.participantID;
    //        data["participantName"] = profile.participantName;
    //        data["institution"] = profile.institution;
    //        data["categoryType"] = profile.categoryType;
    //        data["subCategory"] = profile.subCategory;
    //        data["profileCompleted"] = profile.profileCompleted;

    //        await db.Collection("profiles")
    //            .Document(profile.documentID)
    //            .SetAsync(data, SetOptions.MergeAll);

    //        Debug.Log("Profile Saved");
    //    }

    //    public async Task<ProfileData> LoadProfile(string accountID)
    //    {
    //        QuerySnapshot snapshot =
    //            await db.Collection("profiles")
    //            .WhereEqualTo("accountID", accountID)
    //            .GetSnapshotAsync();

    //        if (snapshot.Count == 0)
    //            return null;

    //        DocumentSnapshot document = snapshot.Documents.First();

    //        Dictionary<string, object> data =
    //            document.ToDictionary();

    //        ProfileData profile =
    //            new ProfileData();

    //        profile.documentID = document.Id;
    //        profile.accountID = accountID;
    //        profile.participantID = GetString(data, "participantID");
    //        profile.participantName = GetString(data, "participantName");
    //        profile.institution = GetString(data, "institution");
    //        profile.categoryType = GetString(data, "categoryType");
    //        profile.subCategory = GetString(data, "subCategory");

    //        if (data.ContainsKey("profileCompleted"))
    //            profile.profileCompleted =
    //                (bool)data["profileCompleted"];

    //        return profile;
    //    }

    //    public async Task<bool> LoadCurrentProfile()
    //    {
    //        string accountID =
    //            FirestoreAccountManager.Instance.CurrentAccount.documentID;

    //        ProfileData profile =
    //            await LoadProfile(accountID);

    //        if (profile == null)
    //        {
    //            ProfileManager.Instance.CurrentProfile = null;

    //            return false;
    //        }

    //        ProfileManager.Instance.CurrentProfile = profile;

    //        Debug.Log("Profile Loaded");

    //        return true;
    //    }

    //    string GetString(Dictionary<string, object> data, string key)
    //    {
    //        if (!data.ContainsKey(key))
    //            return "";

    //        return data[key]?.ToString() ?? "";
    //    }
    //}