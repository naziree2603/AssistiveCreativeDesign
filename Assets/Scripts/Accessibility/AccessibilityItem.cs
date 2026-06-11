using UnityEngine;
using UnityEngine.UI;

public class AccessibilityItem : MonoBehaviour
{
    public string accessibilityLabel;

    private Image image;

    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();

        if (image != null)
        {
            originalColor = image.color;
        }
    }

    public void Highlight()
    {
        AccessibilityItem[] all =
            FindObjectsOfType<AccessibilityItem>();

        foreach (var item in all)
        {
            item.RemoveHighlight();
        }

        if (image != null)
        {
            image.color = Color.yellow;
        }
    }

    public void RemoveHighlight()
    {
        if (image != null)
        {
            image.color = originalColor;
        }
    }
}