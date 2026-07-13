using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    FirebaseFirestore db;

    private void Awake()
    {
        Instance = this;

        db = FirebaseFirestore.DefaultInstance;
    }


    //--------------------------------
    // SAVE WHOLE PARTICIPANT
    //--------------------------------

    public async Task SaveParticipant(string documentID, ParticipantData p)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["participantID"] = p.participantID;
        data["participantName"] = p.participantName;
        data["institution"] = p.institution;
        data["categoryType"] = p.categoryType;
        data["subCategory"] = p.subCategory;

        data["prompt"] = p.prompt;

        data["promptUsed"] = p.promptUsed;
        data["storagePath"] = p.storagePath;

        data["originalImageUrl"] = p.originalImageUrl;
        data["revisedImageUrl"] = p.revisedImageUrl;

        data["posterDescription"] = p.posterDescription;

        data["revisionPrompt"] = p.revisionPrompt;

        data["revisionCount"] = p.revisionCount;

        data["finalExplanation"] = p.finalExplanation;

        data["score"] = p.score;

        data["promptQuality"] = p.promptQuality;
        data["posterMessage"] = p.posterMessage;
        data["designQuality"] = p.designQuality;
        data["accessibilityUnderstanding"] = p.accessibilityUnderstanding;
        data["revisionProcessScore"] = p.revisionProcessScore;
        data["finalExplanationScore"] = p.finalExplanationScore;

        data["feedback"] = p.feedback;
        data["improvementSuggestion"] = p.improvementSuggestion;

        data["lastPage"] = p.lastPage;

        data["createdDate"] = p.createdDate;

        await db.Collection("users")
            .Document(documentID)
            .SetAsync(data, SetOptions.MergeAll);

        Debug.Log("Firestore Save Complete");
    }

    //--------------------------------
    // LOAD PARTICIPANT
    //--------------------------------

    public async Task<ParticipantData> LoadParticipant(string documentID)
    {
        DocumentSnapshot snapshot = await db.Collection("users").Document(documentID).GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.Log("No participant found.");

            return null;
        }

        Dictionary<string, object> data = snapshot.ToDictionary();

        ParticipantData p = new ParticipantData();

        p.participantName = GetString(data, "participantName");
        p.institution = GetString(data, "institution");
        p.categoryType = GetString(data, "categoryType");
        p.subCategory = GetString(data, "subCategory");
        p.prompt = GetString(data, "prompt");

        p.originalImageUrl = GetString(data, "originalImageUrl");
        p.revisedImageUrl = GetString(data, "revisedImageUrl");

        p.posterDescription = GetString(data, "posterDescription");
        p.revisionPrompt = GetString(data, "revisionPrompt");
        p.finalExplanation = GetString(data, "finalExplanation");
        p.feedback = GetString(data, "feedback");
        p.improvementSuggestion = GetString(data, "improvementSuggestion");
        p.lastPage = GetString(data, "lastPage");
        p.createdDate = GetString(data, "createdDate");
        p.promptUsed = GetString(data, "promptUsed");
        p.storagePath = GetString(data, "storagePath");

        if (data.ContainsKey("participantID"))
            p.participantID =
                data["participantID"].ToString();

        if (data.ContainsKey("revisionCount"))
            p.revisionCount =
                Convert.ToInt32(data["revisionCount"]);

        if (data.ContainsKey("score"))
            p.score =
                Convert.ToSingle(data["score"]);

        if (data.ContainsKey("promptQuality"))
            p.promptQuality =
                Convert.ToInt32(data["promptQuality"]);

        if (data.ContainsKey("posterMessage"))
            p.posterMessage =
                Convert.ToInt32(data["posterMessage"]);

        if (data.ContainsKey("designQuality"))
            p.designQuality =
                Convert.ToInt32(data["designQuality"]);

        if (data.ContainsKey("accessibilityUnderstanding"))
            p.accessibilityUnderstanding =
                Convert.ToInt32(data["accessibilityUnderstanding"]);

        if (data.ContainsKey("revisionProcessScore"))
            p.revisionProcessScore =
                Convert.ToInt32(data["revisionProcessScore"]);

        if (data.ContainsKey("finalExplanationScore"))
            p.finalExplanationScore =
                Convert.ToInt32(data["finalExplanationScore"]);

        Debug.Log("Participant Loaded");

        return p;
    }

    public async System.Threading.Tasks.Task<List<ParticipantData>> LoadLeaderboard()
    {
        List<ParticipantData> participants =
            new List<ParticipantData>();

        QuerySnapshot snapshot =
            await db.Collection("users").GetSnapshotAsync();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            Dictionary<string, object> data =
                document.ToDictionary();

            ParticipantData p =
                new ParticipantData();

            if (data.ContainsKey("participantID"))
                p.participantID = data["participantID"].ToString();

            if (data.ContainsKey("participantName"))
                p.participantName = data["participantName"].ToString();

            if (data.ContainsKey("institution"))
                p.institution = data["institution"].ToString();

            if (data.ContainsKey("categoryType"))
                p.categoryType = data["categoryType"].ToString();

            if (data.ContainsKey("score"))
                p.score =
                    float.Parse(data["score"].ToString());

            participants.Add(p);
        }

        participants.Sort((a, b) => b.score.CompareTo(a.score));

        return participants;
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