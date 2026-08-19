//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Firebase.Firestore;
//using UnityEngine;

//public class FirestoreEntryManager : MonoBehaviour
//{
//    public static FirestoreEntryManager Instance;

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

//    public async Task SaveEntry(string accountID, ParticipantData p)
//    {
//        Dictionary<string, object> data = new Dictionary<string, object>();

//        //--------------------------------
//        // Account
//        //--------------------------------

//        data["accountID"] = accountID;

//        data["entryID"] = p.entryID;

//        //--------------------------------
//        // Challenge
//        //--------------------------------

//        data["challengeID"] = p.challengeID;
//        data["challengeTitle"] = p.challengeTitle;


//        //--------------------------------
//        // Participant
//        //--------------------------------

//        data["participantID"] = p.participantID;
//        data["participantName"] = p.participantName;
//        data["institution"] = p.institution;
//        data["categoryType"] = p.categoryType;
//        data["subCategory"] = p.subCategory;

//        //--------------------------------
//        // Current Page
//        //--------------------------------

//        data["lastPage"] = p.lastPage;

//        //--------------------------------
//        // Prompt
//        //--------------------------------

//        data["prompt"] = p.prompt;
//        data["promptUsed"] = p.promptUsed;

//        //--------------------------------
//        // AI Image
//        //--------------------------------

//        data["originalImageUrl"] = p.originalImageUrl;
//        data["revisedImageUrl"] = p.revisedImageUrl;
//        data["storagePath"] = p.storagePath;

//        //--------------------------------
//        // Description
//        //--------------------------------

//        data["posterDescription"] = p.posterDescription;

//        //--------------------------------
//        // Revision
//        //--------------------------------

//        data["revisionPrompt"] = p.revisionPrompt;
//        data["revisionCount"] = p.revisionCount;

//        //--------------------------------
//        // Final Explanation
//        //--------------------------------

//        data["finalExplanation"] = p.finalExplanation;

//        //--------------------------------
//        // Score
//        //--------------------------------

//        data["score"] = p.score;

//        data["promptQuality"] = p.promptQuality;
//        data["posterMessage"] = p.posterMessage;
//        data["designQuality"] = p.designQuality;
//        data["accessibilityUnderstanding"] = p.accessibilityUnderstanding;
//        data["revisionProcessScore"] = p.revisionProcessScore;
//        data["finalExplanationScore"] = p.finalExplanationScore;

//        //--------------------------------
//        // Feedback
//        //--------------------------------

//        data["feedback"] = p.feedback;
//        data["improvementSuggestion"] = p.improvementSuggestion;

//        //--------------------------------
//        // Date
//        //--------------------------------

//        data["createdDate"] = p.createdDate;

//        data["isCompleted"] = p.isCompleted;

//        data["completedDate"] = p.completedDate;

//        await db.Collection("entries").Document(p.entryID).SetAsync(data, SetOptions.MergeAll);

//        Debug.Log("===== SAVE CHECK =====");
//        Debug.Log("EntryID: " + p.entryID);
//        Debug.Log("Participant Name: [" + p.participantName + "]");
//        Debug.Log("Institution: [" + p.institution + "]");
//        Debug.Log("Challenge: " + p.challengeTitle);
//        Debug.Log("Last Page: " + p.lastPage);

//        Debug.Log("Entry Saved Successfully");

//        Debug.Log("===== SAVE ENTRY =====");
//        Debug.Log("AccountID : " + accountID);
//        Debug.Log("EntryID : " + p.entryID);
//        Debug.Log("Participant : " + p.participantName);
//        Debug.Log("Challenge : " + p.challengeTitle);
//    }

//    public async Task<List<ParticipantData>> LoadLeaderboard(string challengeID)
//    {
//        Debug.Log("===== FIRESTORE LEADERBOARD =====");
//        Debug.Log("ChallengeID Query = " + challengeID); 

//        List<ParticipantData> participants = new List<ParticipantData>();

//        try
//        {
//            QuerySnapshot snapshot = await db.Collection("entries").WhereEqualTo("challengeID", challengeID).WhereEqualTo("isCompleted", true).GetSnapshotAsync();
//            Debug.Log("Documents Found = " + snapshot.Count);


//            foreach (DocumentSnapshot document in snapshot.Documents)
//            {
//                Dictionary<string, object> data = document.ToDictionary();

//                ParticipantData p = new ParticipantData();

//                p.entryID = GetString(data, "entryID");
//                p.challengeID = GetString(data, "challengeID");
//                p.challengeTitle = GetString(data, "challengeTitle");

//                p.participantID = GetString(data, "participantID");
//                p.participantName = GetString(data, "participantName");
//                p.institution = GetString(data, "institution");
//                p.categoryType = GetString(data, "categoryType");
//                p.subCategory = GetString(data, "subCategory");

//                if (data.ContainsKey("score"))
//                    p.score = float.Parse(data["score"].ToString());

//                participants.Add(p);
//            }
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError(ex);
//        }


//        participants.Sort((a, b) => b.score.CompareTo(a.score));

//        return participants;
//    }

//    public async Task<ParticipantData> LoadEntry(string entryID)
//    {
//        DocumentSnapshot snapshot =
//            await db.Collection("entries")
//            .Document(entryID)
//            .GetSnapshotAsync();

//        if (!snapshot.Exists)
//            return null;

//        Dictionary<string, object> data =
//            snapshot.ToDictionary();

//        ParticipantData p =
//            new ParticipantData();

//        p.entryID = GetString(data, "entryID");
//        p.challengeID = GetString(data, "challengeID");
//        p.challengeTitle = GetString(data, "challengeTitle");

//        p.participantID = GetString(data, "participantID");
//        p.participantName = GetString(data, "participantName");
//        p.institution = GetString(data, "institution");
//        p.categoryType = GetString(data, "categoryType");
//        p.subCategory = GetString(data, "subCategory");

//        p.lastPage = GetString(data, "lastPage");
//        p.promptUsed = GetString(data, "promptUsed");
//        p.storagePath = GetString(data, "storagePath");
//        p.createdDate = GetString(data, "createdDate");

//        p.prompt = GetString(data, "prompt");
//        p.posterDescription = GetString(data, "posterDescription");
//        p.revisionPrompt = GetString(data, "revisionPrompt");
//        p.finalExplanation = GetString(data, "finalExplanation");

//        p.originalImageUrl = GetString(data, "originalImageUrl");
//        p.revisedImageUrl = GetString(data, "revisedImageUrl");

//        p.feedback = GetString(data, "feedback");
//        p.improvementSuggestion = GetString(data, "improvementSuggestion");

//        if (data.ContainsKey("revisionCount"))
//            p.revisionCount =
//                int.Parse(data["revisionCount"].ToString());

//        if (data.ContainsKey("score"))
//            p.score =
//                float.Parse(data["score"].ToString());

//        if (data.ContainsKey("promptQuality"))
//            p.promptQuality =
//                int.Parse(data["promptQuality"].ToString());

//        if (data.ContainsKey("posterMessage"))
//            p.posterMessage =
//                int.Parse(data["posterMessage"].ToString());

//        if (data.ContainsKey("designQuality"))
//            p.designQuality =
//                int.Parse(data["designQuality"].ToString());

//        if (data.ContainsKey("accessibilityUnderstanding"))
//            p.accessibilityUnderstanding =
//                int.Parse(data["accessibilityUnderstanding"].ToString());

//        if (data.ContainsKey("revisionProcessScore"))
//            p.revisionProcessScore =
//                int.Parse(data["revisionProcessScore"].ToString());

//        if (data.ContainsKey("finalExplanationScore"))
//            p.finalExplanationScore =
//                int.Parse(data["finalExplanationScore"].ToString());

//        if (data.ContainsKey("isCompleted"))
//            p.isCompleted =
//                bool.Parse(data["isCompleted"].ToString());

//        p.completedDate = GetString(data, "completedDate");

//        return p;

//    }

//    public async Task<bool> HasCompletedChallenge(
//    string accountID,
//    string challengeID)
//    {
//        QuerySnapshot snapshot =
//            await db.Collection("entries")
//            .WhereEqualTo("accountID", accountID)
//            .WhereEqualTo("challengeID", challengeID)
//            .WhereEqualTo("isCompleted", true)
//            .GetSnapshotAsync();

//        return snapshot.Count > 0;
//    }

//    public async Task<List<ParticipantData>> LoadSubmittedEntries(string accountID)
//    {
//        List<ParticipantData> entries = new List<ParticipantData>();

//        QuerySnapshot snapshot = await db.Collection("entries").WhereEqualTo("accountID", accountID).GetSnapshotAsync();

//        Debug.Log("Searching AccountID = " + accountID);
//        Debug.Log("Entries Found = " + snapshot.Count);

//        foreach (DocumentSnapshot document in snapshot.Documents)
//        {
//            Dictionary<string, object> data = document.ToDictionary();

//            ParticipantData p = new ParticipantData();

//            p.entryID = GetString(data, "entryID");
//            p.challengeID = GetString(data, "challengeID");
//            p.challengeTitle = GetString(data, "challengeTitle");

//            p.participantName = GetString(data, "participantName");
//            p.institution = GetString(data, "institution");
//            p.categoryType = GetString(data, "categoryType");
//            p.subCategory = GetString(data, "subCategory");

//            p.prompt = GetString(data, "prompt");

//            p.posterDescription = GetString(data, "posterDescription");
//            p.revisionPrompt = GetString(data, "revisionPrompt");
//            p.finalExplanation = GetString(data, "finalExplanation");

//            p.originalImageUrl = GetString(data, "originalImageUrl");
//            p.revisedImageUrl = GetString(data, "revisedImageUrl");

//            p.createdDate = GetString(data, "createdDate");

//            if (data.ContainsKey("score"))
//                p.score = float.Parse(data["score"].ToString());

//            entries.Add(p);
//        }

//        entries.Sort((a, b) => b.score.CompareTo(a.score));

//        return entries;
//    }

//    public async Task<ParticipantData> GetUnfinishedEntry(
//    string accountID,
//    string challengeID)
//    {
//        QuerySnapshot snapshot = await db.Collection("entries")
//            .WhereEqualTo("accountID", accountID)
//            .WhereEqualTo("challengeID", challengeID)
//            .WhereEqualTo("isCompleted", false)
//            .Limit(1)
//            .GetSnapshotAsync();

//        foreach (DocumentSnapshot document in snapshot.Documents)
//        {
//            return await LoadEntry(document.Id);
//        }

//        return null;
//    }

//    public async Task<bool> HasCompletedSubmission(string accountID)
//    {
//        QuerySnapshot snapshot = await db.Collection("entries")
//            .WhereEqualTo("accountID", accountID)
//            .WhereEqualTo("isCompleted", true)
//            .Limit(1)
//            .GetSnapshotAsync();

//        return snapshot.Count > 0;
//    }

//    private string GetString(Dictionary<string, object> data, string key)
//    {
//        if (!data.ContainsKey(key))
//            return "";

//        if (data[key] == null)
//            return "";

//        return data[key].ToString();
//    }
//}