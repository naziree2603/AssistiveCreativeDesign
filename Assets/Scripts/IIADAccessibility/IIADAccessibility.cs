/// <summary>
/// Older helper kept for compatibility. Use IIADSpeech directly in new code.
/// </summary>
public static class IIADAccessibility
{
    public static bool IsEnabled() => IIADSpeech.IsAnyOutputOn;

    public static void Speak(string message) => IIADSpeech.Say(message);

    public static void Stop() => IIADSpeech.Stop();
}
