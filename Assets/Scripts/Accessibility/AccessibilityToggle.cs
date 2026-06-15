using UnityEngine;
using TMPro;

public class AccessibilityToggle : MonoBehaviour
{
    public GameObject accessibilityManager;
    public TMP_Text buttonText;

    private bool accessibilityEnabled = true;

    public void ToggleAccessibility()
    {
        accessibilityEnabled = !accessibilityEnabled;

        accessibilityManager.SetActive(accessibilityEnabled);

        buttonText.text =
            accessibilityEnabled ?
            "Accessibility ON" :
            "Accessibility OFF";
    }
}