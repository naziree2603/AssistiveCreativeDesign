//using System.Collections.Generic;
//using UnityEngine;

//public class SubmittedManager : MonoBehaviour
//{
//    public FullPosterImageAPI posterSystem;

//    public MainMenuManager mainMenuManager;

//    public GameObject submittedPanel;


//    public static SubmittedManager Instance;

//    public SubmittedItem itemPrefab;

//    public Transform content;

//    void Awake()
//    {
//        Instance = this;
//    }

//    void OnEnable()
//    {
        
//    }

//    public async void OpenSubmission(ParticipantData data)
//    {
//        ParticipantData fullEntry =
//            await FirestoreEntryManager.Instance.LoadEntry(data.entryID);

//        if (fullEntry == null)
//        {
//            Debug.LogError("Entry not found.");
//            return;
//        }

//        ParticipantManager.Instance.CurrentParticipant = fullEntry;

//        ChallengeData challenge = new ChallengeData
//        {
//            challengeID = fullEntry.challengeID,
//            title = fullEntry.challengeTitle
//        };

//        ChallengeManager.Instance.SetCurrentChallenge(challenge);

//        posterSystem.LoadParticipant();

//        submittedPanel.SetActive(false);

//        mainMenuManager.GoToScorePage();

//        Debug.Log("History Loaded");
//    }

//    public async void LoadSubmitted()
//    {
//        if (FirestoreAccountManager.Instance == null)
//        {
//            Debug.LogError("AccountManager Missing");
//            return;
//        }

//        if (FirestoreAccountManager.Instance.CurrentAccount == null)
//        {
//            Debug.LogError("User not logged in yet.");
//            return;
//        }

//        Debug.Log("===== Submitted =====");

//        Debug.Log("content = " + content);

//        Debug.Log("FirestoreAccountManager = " + FirestoreAccountManager.Instance);

//        if (FirestoreAccountManager.Instance != null)
//            Debug.Log("CurrentAccount = " + FirestoreAccountManager.Instance.CurrentAccount);

//        Debug.Log("FirestoreEntryManager = " + FirestoreEntryManager.Instance);

//        if (content == null)
//        {
//            Debug.LogError("Content is NULL");
//            return;
//        }

//        if (FirestoreAccountManager.Instance == null)
//        {
//            Debug.LogError("FirestoreAccountManager is NULL");
//            return;
//        }

//        if (FirestoreAccountManager.Instance.CurrentAccount == null)
//        {
//            Debug.LogError("CurrentAccount is NULL");
//            return;
//        }

//        if (FirestoreEntryManager.Instance == null)
//        {
//            Debug.LogError("FirestoreEntryManager is NULL");
//            return;
//        }

//        foreach (Transform child in content)
//            Destroy(child.gameObject);

//        string accountID =
//            FirestoreAccountManager.Instance.CurrentAccount.documentID;

//        Debug.Log("AccountID = " + accountID);

//        List<ParticipantData> list =
//            await FirestoreEntryManager.Instance.LoadSubmittedEntries(accountID);

//        Debug.Log("Entries = " + list.Count);

//        foreach (ParticipantData participant in list)
//        {
//            SubmittedItem item =
//                Instantiate(itemPrefab, content);

//            item.Setup(participant);
//        }
//    }
//}