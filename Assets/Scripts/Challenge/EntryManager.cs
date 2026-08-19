//using UnityEngine;

//public class EntryManager : MonoBehaviour
//{
//    public static EntryManager Instance;

//    public ParticipantData CurrentEntry;

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
//    }

//    public void CreateNewEntry()
//    {
//        CurrentEntry = new ParticipantData();

//        CurrentEntry.createdDate =
//            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//    }
//}