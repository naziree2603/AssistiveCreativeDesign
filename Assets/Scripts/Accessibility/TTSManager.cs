using UnityEngine;

public class TTSManager : MonoBehaviour
{
    void Start()
    {
        AndroidTTS.Initialize();

        Debug.Log(
            "TTS Status: " +
            AndroidTTS.GetTTSStatus()
        );
    }

    private void OnDestroy()
    {
        AndroidTTS.Shutdown();
    }
}