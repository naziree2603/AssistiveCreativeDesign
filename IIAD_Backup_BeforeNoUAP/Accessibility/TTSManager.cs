using UnityEngine;

public class TTSManager : MonoBehaviour
{
    void Start()
    {
        AndroidTTS.Initialize();

    }

    private void OnDestroy()
    {
        AndroidTTS.Shutdown();
    }
}