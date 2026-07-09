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
        CurrentParticipant = new ParticipantData();

        CurrentParticipant.participantID = System.Guid.NewGuid().ToString();

        CurrentParticipant.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //--------------------------------------------------
    // Save Current Participant
    //--------------------------------------------------

    public async System.Threading.Tasks.Task Save()
    {
        string documentID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

        if (string.IsNullOrEmpty(documentID))
            return;

        await FirestoreManager.Instance.SaveParticipant(documentID, CurrentParticipant);
    }

    //--------------------------------
    // LOAD
    //--------------------------------

    public async System.Threading.Tasks.Task Load()
    {
        string documentID = FirestoreAccountManager.Instance.CurrentAccount.documentID;

        if (string.IsNullOrEmpty(documentID))
            return;

        CurrentParticipant = null;

        CurrentParticipant =
            await FirestoreManager.Instance.LoadParticipant(documentID);

        if (CurrentParticipant == null)
        {
            CreateNewParticipant();

            await Save();   // Save an empty participant for the new account
        }
    }

    //--------------------------------------------------
    // Clear Current Participant
    //--------------------------------------------------

    public void ResetParticipant()
    {
        CurrentParticipant = null;
    }
}