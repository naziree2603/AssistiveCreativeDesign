using UnityEngine;

public class TextToSpeechManager : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR

    AndroidJavaObject tts;

    private void Start()
    {
        AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        tts = new AndroidJavaObject(
            "android.speech.tts.TextToSpeech",
            activity,
            null
        );
    }

    public void Speak(string text)
    {
        if(tts != null)
        {
            tts.Call<int>(
                "speak",
                text,
                0,
                null,
                "POSTER_DESC"
            );
        }
    }

#else

    public void Speak(string text)
    {
        Debug.Log("TTS Speak Called");
        Debug.Log(text);
    }

#endif
}