using Firebase;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsReady = false;

    async void Start()
    {
        var status =
            await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == DependencyStatus.Available)
        {
            IsReady = true;

            Debug.Log("Firebase Ready");
        }
        else
        {
            Debug.LogError(status);
        }
    }
}