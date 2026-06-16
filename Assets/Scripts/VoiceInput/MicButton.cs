using TMPro;
using UnityEngine;

public class MicButton : MonoBehaviour
{
    public TMP_InputField targetInput;
    public VoiceInputManager voiceManager;

    public void StartMic()
    {
        voiceManager.SetTargetInput(targetInput);
        voiceManager.StartMic();
    }
}