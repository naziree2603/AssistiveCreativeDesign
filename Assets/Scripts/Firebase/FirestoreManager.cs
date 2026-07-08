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

    public async Task SaveParticipant( string uid, ParticipantData p)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["participantID"] = p.participantID;
        data["participantName"] = p.participantName;
        data["institution"] = p.institution;
        data["category"] = p.category;

        data["prompt"] = p.prompt;

        data["promptUsed"] = p.promptUsed;
        data["storagePath"] = p.storagePath;

        data["originalImageUrl"] = p.originalImageUrl;
        data["revisedImageUrl"] = p.revisedImageUrl;

        data["originalLocalPath"] = p.originalLocalPath;
        data["revisedLocalPath"] = p.revisedLocalPath;

        data["posterDescription"] = p.posterDescription;

        data["revisionPrompt"] = p.revisionPrompt;

        data["finalExplanation"] = p.finalExplanation;

        data["score"] = p.score;

        data["promptQuality"] = p.promptQuality;
        data["posterMessage"] = p.posterMessage;
        data["designQuality"] = p.designQuality;
        data["accessibilityUnderstanding"] =
            p.accessibilityUnderstanding;

        data["revisionProcessScore"] =
            p.revisionProcessScore;

        data["finalExplanationScore"] =
            p.finalExplanationScore;

        data["feedback"] =
            p.feedback;

        data["improvementSuggestion"] =
            p.improvementSuggestion;

        data["lastPage"] =
            p.lastPage;

        await db.Collection("users")
            .Document(uid)
            .SetAsync(data, SetOptions.MergeAll);

        Debug.Log("Firestore Save Complete");
    }

    //--------------------------------
    // LOAD PARTICIPANT
    //--------------------------------

    public async Task<ParticipantData> LoadParticipant(string uid)
    {
        DocumentSnapshot snapshot =
            await db.Collection("users")
            .Document(uid)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.Log("No participant found.");

            return null;
        }

        Dictionary<string, object> data =
            snapshot.ToDictionary();

        ParticipantData p =
            new ParticipantData();

        if (data.ContainsKey("participantID"))
            p.participantID = data["participantID"].ToString();

        if (data.ContainsKey("participantName"))
            p.participantName = data["participantName"].ToString();

        if (data.ContainsKey("institution"))
            p.institution = data["institution"].ToString();

        if (data.ContainsKey("category"))
            p.category = data["category"].ToString();

        if (data.ContainsKey("prompt"))
            p.prompt = data["prompt"].ToString();

        if (data.ContainsKey("originalImageUrl"))
            p.originalImageUrl = data["originalImageUrl"].ToString();

        if (data.ContainsKey("revisedImageUrl"))
            p.revisedImageUrl = data["revisedImageUrl"].ToString();

        if (data.ContainsKey("originalLocalPath"))
            p.originalLocalPath = data["originalLocalPath"].ToString();

        if (data.ContainsKey("revisedLocalPath"))
            p.revisedLocalPath = data["revisedLocalPath"].ToString();

        if (data.ContainsKey("posterDescription"))
            p.posterDescription = data["posterDescription"].ToString();

        if (data.ContainsKey("revisionPrompt"))
            p.revisionPrompt = data["revisionPrompt"].ToString();

        if (data.ContainsKey("finalExplanation"))
            p.finalExplanation = data["finalExplanation"].ToString();

        if (data.ContainsKey("feedback"))
            p.feedback = data["feedback"].ToString();

        if (data.ContainsKey("improvementSuggestion"))
            p.improvementSuggestion =
                data["improvementSuggestion"].ToString();

        if (data.ContainsKey("lastPage"))
            p.lastPage = data["lastPage"].ToString();

        if (data.ContainsKey("createdDate"))
            p.createdDate = data["createdDate"].ToString();

        if (data.ContainsKey("promptUsed"))
            p.promptUsed = data["promptUsed"].ToString();

        if (data.ContainsKey("storagePath"))
            p.storagePath = data["storagePath"].ToString();

        if (data.ContainsKey("revisionCount"))
            p.revisionCount =
                int.Parse(data["revisionCount"].ToString());

        if (data.ContainsKey("score"))
            p.score =
                float.Parse(data["score"].ToString());

        if (data.ContainsKey("promptQuality"))
            p.promptQuality =
                int.Parse(data["promptQuality"].ToString());

        if (data.ContainsKey("posterMessage"))
            p.posterMessage =
                int.Parse(data["posterMessage"].ToString());

        if (data.ContainsKey("designQuality"))
            p.designQuality =
                int.Parse(data["designQuality"].ToString());

        if (data.ContainsKey("accessibilityUnderstanding"))
            p.accessibilityUnderstanding =
                int.Parse(data["accessibilityUnderstanding"].ToString());

        if (data.ContainsKey("revisionProcessScore"))
            p.revisionProcessScore =
                int.Parse(data["revisionProcessScore"].ToString());

        if (data.ContainsKey("finalExplanationScore"))
            p.finalExplanationScore =
                int.Parse(data["finalExplanationScore"].ToString());

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

            if (data.ContainsKey("category"))
                p.category = data["category"].ToString();

            if (data.ContainsKey("score"))
                p.score =
                    float.Parse(data["score"].ToString());

            participants.Add(p);
        }

        participants.Sort((a, b) => b.score.CompareTo(a.score));

        return participants;
    }


}