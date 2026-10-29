using UnityEngine;

/// <summary>
/// Kept for scenes that already have it. Starts the built-in Android TextToSpeech used by
/// Voice Guide (no UAP AndroidTTS plugin). IIADAccessibilityManager also does this, so
/// this component is optional.
/// </summary>
public class TTSManager : MonoBehaviour
{
    [Tooltip("Voice Guide language, e.g. en-US or ms-MY.")]
    public string language = "en-US";

    void Start() => IIADSpeech.Initialize(language);
}
