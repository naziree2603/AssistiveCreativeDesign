using TMPro;
using UnityEngine;

public class MicButton : MonoBehaviour
{
    public TMP_InputField targetInput;
    public VoiceInputManager voiceManager;

    public void ToggleMic()
    {
        voiceManager.SetTargetInput(targetInput);

        voiceManager.ToggleMic();
    }
}
