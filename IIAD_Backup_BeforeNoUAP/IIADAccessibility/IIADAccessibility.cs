using UnityEngine;

public static class IIADAccessibility
{
    // =========================================================
    // CHECK ACCESSIBILITY
    // =========================================================

    public static bool IsEnabled()
    {
        try
        {
            return AccessibilityToggle.AccessibilityEnabled;
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // SPEAK
    // =========================================================

    public static void Speak(string message)
    {
        if (!IsEnabled())
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(message);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "IIADAccessibility: Speech failed: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // STOP SPEECH
    // =========================================================

    public static void Stop()
    {
        if (!IsEnabled())
            return;

        try
        {
            // Only use this if your existing
            // AccessibilitySpeech system provides
            // a stop method.
        }
        catch
        {
        }
    }
}