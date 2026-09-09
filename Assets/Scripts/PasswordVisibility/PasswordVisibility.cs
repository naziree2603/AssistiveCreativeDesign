using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordVisibility : MonoBehaviour
{
    [Header("Password Input")]
    [SerializeField]
    private TMP_InputField passwordInput;

    [Header("Eye Button")]
    [SerializeField]
    private Button eyeButton;

    [Header("Eye Sprites")]
    [SerializeField]
    private Sprite eyeOpenSprite;

    [SerializeField]
    private Sprite eyeClosedSprite;

    private bool isPasswordVisible = false;


    private void Start()
    {
        SetPasswordVisibility(false);
    }


    public void TogglePasswordVisibility()
    {
        if (passwordInput == null)
            return;

        SetPasswordVisibility(
            !isPasswordVisible
        );
    }


    private void SetPasswordVisibility(bool visible)
    {
        isPasswordVisible = visible;


        // =====================================================
        // PASSWORD
        // =====================================================

        passwordInput.contentType =
            visible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();


        // =====================================================
        // BUTTON SPRITE
        // =====================================================

        if (eyeButton != null)
        {
            Image buttonImage =
                eyeButton.GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.sprite =
                    visible
                        ? eyeOpenSprite
                        : eyeClosedSprite;
            }
        }
    }


    public void ResetPasswordVisibility()
    {
        SetPasswordVisibility(false);
    }
}