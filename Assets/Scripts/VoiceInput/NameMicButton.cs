using TMPro;
using UnityEngine;

public class NameMicButton : MonoBehaviour
{
    public TMP_InputField nameInput;
    public VoiceInputManager voiceManager;

    public void StartNameMic()
    {
        voiceManager.SetTargetInput(nameInput);

        voiceManager.StartMic();
    }
}