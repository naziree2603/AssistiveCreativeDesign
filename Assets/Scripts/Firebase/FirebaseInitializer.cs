using Firebase;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    async void Start()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == DependencyStatus.Available)
        {
            Debug.Log("✅ Firebase Ready");
        }
        else
        {
            Debug.LogError(status);
        }
    }
}