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

    public async void Save()
    {
        if (CurrentParticipant == null)
            return;

        string uid =
            FirebaseAuthManager.Instance.GetUID();

        if (string.IsNullOrEmpty(uid))
            return;

        await FirestoreManager.Instance.SaveParticipant(
            uid,
            CurrentParticipant
        );
    }

    //--------------------------------
    // LOAD
    //--------------------------------

    public async System.Threading.Tasks.Task Load()
    {
        string uid =
            FirebaseAuthManager.Instance.GetUID();

        if (string.IsNullOrEmpty(uid))
            return;

        CurrentParticipant =
            await FirestoreManager.Instance.LoadParticipant(uid);

        if (CurrentParticipant == null)
        {
            CreateNewParticipant();

            Debug.Log("New participant created.");
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