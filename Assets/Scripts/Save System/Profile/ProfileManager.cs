using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance;

    public ProfileData CurrentProfile;

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

    public void ResetProfile()
    {
        CurrentProfile = null;
    }
} 