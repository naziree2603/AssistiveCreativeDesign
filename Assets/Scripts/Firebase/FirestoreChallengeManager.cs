using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirestoreChallengeManager : MonoBehaviour
{
    public static FirestoreChallengeManager Instance;

    FirebaseFirestore db;

    async void Start()
    {
        List<ChallengeData> list =
            await FirestoreChallengeManager.Instance.LoadChallenges();

        foreach (ChallengeData challenge in list)
        {
            Debug.Log(challenge.title);

            Debug.Log(challenge.description);

            Debug.Log(challenge.location);

            Debug.Log(challenge.isActive);
        }
    }

    private void Awake()
    {
        Instance = this;

        db = FirebaseFirestore.DefaultInstance;


    }

    //------------------------------------------------
    // Load All Challenges
    //------------------------------------------------

    public async Task<List<ChallengeData>> LoadChallenges()
    {
        List<ChallengeData> challengeList =
            new List<ChallengeData>();

        QuerySnapshot snapshot =
            await db.Collection("challenges")
            .GetSnapshotAsync();

        Debug.Log("Challenge Count : " + snapshot.Count);

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            Dictionary<string, object> data =
                document.ToDictionary();

            ChallengeData challenge =
                new ChallengeData();

            challenge.challengeID = document.Id;

            challenge.title = GetString(data, "title");

            challenge.description = GetString(data, "description");

            challenge.location = GetString(data, "location");

            challenge.bannerUrl = GetString(data, "bannerUrl");


            challenge.startDate = GetString(data, "startDate");

            challenge.endDate = GetString(data, "endDate");

            challenge.isActive = GetBool(data, "isActive");

            if (data.ContainsKey("eventCode"))
                challenge.eventCode = data["eventCode"].ToString();

            challengeList.Add(challenge);

            Debug.Log(
                "Loaded Challenge : "
                + challenge.title);
        }

        return challengeList;
    }

    //---------------------------------------------
    // Helpers
    //---------------------------------------------

    string GetString(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key))
            return "";

        if (data[key] == null)
            return "";

        return data[key].ToString();
    }

    bool GetBool(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key))
            return false;

        return (bool)data[key];
    }
}