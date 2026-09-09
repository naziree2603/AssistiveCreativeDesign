using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    // =========================================================
    // WEBSITE LINKS
    // =========================================================

    [Header("Website Links")]

    [SerializeField]
    private string forgotPasswordURL =
        "https://yourwebsite.com/forgot-password";

    [SerializeField]
    private string aboutURL =
        "https://yourwebsite.com/about";

    [SerializeField]
    private string privacyPolicyURL =
        "https://yourwebsite.com/privacy-policy";

    [SerializeField]
    private string deleteAccountURL =
        "https://yourwebsite.com/delete-account";


    // =========================================================
    // HOW TO PLAY PANEL
    // =========================================================

    [Header("How To Play")]

    [SerializeField]
    private GameObject howToPlayPanel;


    // =========================================================
    // FORGOT PASSWORD
    // =========================================================

    public void OpenForgotPassword()
    {
        OpenWebsite(
            forgotPasswordURL
        );
    }


    // =========================================================
    // ABOUT
    // =========================================================

    public void OpenAbout()
    {
        OpenWebsite(
            aboutURL
        );
    }


    // =========================================================
    // PRIVACY POLICY
    // =========================================================

    public void OpenPrivacyPolicy()
    {
        OpenWebsite(
            privacyPolicyURL
        );
    }


    // =========================================================
    // DELETE ACCOUNT
    // =========================================================

    public void OpenDeleteAccount()
    {
        OpenWebsite(
            deleteAccountURL
        );
    }


    // =========================================================
    // HOW TO PLAY
    // =========================================================

    public void OpenHowToPlay()
    {
        if (howToPlayPanel == null)
        {
            Debug.LogWarning(
                "SettingsManager: How To Play Panel is not assigned yet."
            );

            return;
        }

        howToPlayPanel.SetActive(true);
    }


    // =========================================================
    // CLOSE HOW TO PLAY
    // =========================================================

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }


    // =========================================================
    // OPEN WEBSITE
    // =========================================================

    private void OpenWebsite(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning(
                "SettingsManager: Website URL is empty."
            );

            return;
        }


        Debug.Log(
            "SettingsManager: Opening website: " +
            url
        );


        Application.OpenURL(url);
    }
}