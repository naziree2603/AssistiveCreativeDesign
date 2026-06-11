using UnityEngine;

public class TextToSpeechManager : MonoBehaviour
{
    public void Speak(string text)
    {
        Debug.Log("TTS: " + text);

#if UNITY_ANDROID
    ...
#endif
    }
}
