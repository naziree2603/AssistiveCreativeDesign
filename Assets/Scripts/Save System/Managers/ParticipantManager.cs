using UnityEngine;

public class ParticipantManager : MonoBehaviour
{
    public static ParticipantManager Instance;

    public ParticipantData CurrentParticipant;

    void Awake()
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

    public void DeleteAll()
    {
        JsonSaveSystem.DeleteAll();
    }
}