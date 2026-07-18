using System;
using UnityEngine;

public class ParticipantManager : MonoBehaviour
{
    public static ParticipantManager Instance;

    public ParticipantData CurrentParticipant;

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

    //--------------------------------------------------
    // Create New Participant
    //--------------------------------------------------

    public void CreateNewParticipant()
    {
        Debug.Log("===== CREATE NEW ENTRY =====");
        Debug.Log(Environment.StackTrace);

        CurrentParticipant = new ParticipantData();

        CurrentParticipant.participantID = System.Guid.NewGuid().ToString();

        CurrentParticipant.entryID = Guid.NewGuid().ToString();

        CurrentParticipant.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        CurrentParticipant.prompt = "";

        CurrentParticipant.posterDescription = "";

        CurrentParticipant.revisionPrompt = "";

        CurrentParticipant.finalExplanation = "";

        CurrentParticipant.originalImageUrl = "";

        CurrentParticipant.revisedImageUrl = "";

        CurrentParticipant.promptUsed = "";

        CurrentParticipant.storagePath = "";

        CurrentParticipant.feedback = "";

        CurrentParticipant.improvementSuggestion = "";

        CurrentParticipant.revisionCount = 0;

        CurrentParticipant.score = 0;

        CurrentParticipant.promptQuality = 0;
        CurrentParticipant.posterMessage = 0;
        CurrentParticipant.designQuality = 0;
        CurrentParticipant.accessibilityUnderstanding = 0;
        CurrentParticipant.revisionProcessScore = 0;
        CurrentParticipant.finalExplanationScore = 0;

        CurrentParticipant.lastPage = "";

        CurrentParticipant.isCompleted = false;

        CurrentParticipant.completedDate = "";


    }

    //--------------------------------------------------
    // Save Current Participant
    //--------------------------------------------------

    public async System.Threading.Tasks.Task Save()
    {
        string accountID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

        if (string.IsNullOrEmpty(accountID))
            return;

        if (CurrentParticipant == null)
        {
            Debug.LogError("CurrentParticipant is NULL.");
            return;
        }

        await FirestoreEntryManager.Instance.SaveEntry(
            accountID,
            CurrentParticipant);

        Debug.Log("Participant saved to Entries.");
    }

    //--------------------------------
    // LOAD
    //--------------------------------

    //public async System.Threading.Tasks.Task Load()
    //{
    //    string documentID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

    //    if (string.IsNullOrEmpty(documentID))
    //        return;

    //    CurrentParticipant = null;

    //    CurrentParticipant =
    //        await FirestoreManager.Instance.LoadParticipant(documentID);

    //    if (CurrentParticipant == null)
    //    {
    //        CreateNewParticipant();

    //        await Save();   // Save an empty participant for the new account
    //    }
    //}

    public void InitializeNewEntry(ProfileData profile, ChallengeData challenge)
    {
        Debug.Log("Profile = " + profile);

        if (profile == null)
        {
            Debug.LogError("CurrentProfile is NULL!");
            return;
        }

        if (challenge == null)
        {
            Debug.LogError("CurrentChallenge is NULL!");
            return;
        }

        CreateNewParticipant();

        CurrentParticipant.participantID = profile.participantID;
        CurrentParticipant.participantName = profile.participantName;
        CurrentParticipant.institution = profile.institution;
        CurrentParticipant.categoryType = profile.categoryType;
        CurrentParticipant.subCategory = profile.subCategory;

        CurrentParticipant.challengeID = challenge.challengeID;
        CurrentParticipant.challengeTitle = challenge.title;
    }


    //--------------------------------------------------
    // Clear Current Participant
    //--------------------------------------------------

    public void ResetParticipant()
    {
        CurrentParticipant = null;
    }
}