using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordToggle : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Eye Icon")]
    [SerializeField] private Image eyeIcon;

    [SerializeField] private Sprite showSprite;
    [SerializeField] private Sprite hideSprite;

    private bool isVisible = false;

    private void Start()
    {
        HidePassword();
    }

    public void TogglePassword()
    {
        if (isVisible)
        {
            HidePassword();
        }
        else
        {
            ShowPassword();
        }
    }

    private void ShowPassword()
    {
        isVisible = true;

        passwordInput.contentType =
            TMP_InputField.ContentType.Standard;

        passwordInput.ForceLabelUpdate();

        if (eyeIcon != null)
            eyeIcon.sprite = hideSprite;
    }

    private void HidePassword()
    {
        isVisible = false;

        passwordInput.contentType =
            TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();

        if (eyeIcon != null)
            eyeIcon.sprite = showSprite;
    }
}