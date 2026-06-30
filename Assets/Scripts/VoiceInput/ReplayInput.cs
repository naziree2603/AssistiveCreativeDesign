using TMPro;
using UnityEngine;

public class ReplayInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void Replay()
    {
        if (inputField == null)
            return;

        string text = inputField.text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            AndroidTTS.Speak("The input field is empty.");
            return;
        }

        AndroidTTS.StopSpeaking();
        AndroidTTS.Speak(text);
    }
}