using System.Threading.Tasks;
using UnityEngine;

public class StartupManager : MonoBehaviour
{
    public static StartupManager Instance { get; private set; }

    [Header("Startup Panels")]
    [SerializeField]
    private GameObject splashPanel;

    [SerializeField]
    private GameObject welcomePanel;

    [SerializeField]
    private GameObject mainDashboardPanel;

    [SerializeField]
    private GameObject loginPanel;

    [SerializeField]
    private GameObject registerPanel;


    [Header("Settings")]
    [SerializeField]
    private float minimumSplashTime = 2f;


    private async void Start()
    {
        // Immediately hide EVERYTHING
        HideAllPanels();

        // Show ONLY splash
        if (splashPanel != null)
        {
            splashPanel.SetActive(true);
        }

        Debug.Log(
            "StartupManager: Splash displayed."
        );

        // Start the application flow
        await StartApplication();
    }

    private void HideAllPanels()
    {
        if (splashPanel != null)
            splashPanel.SetActive(false);

        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (loginPanel != null)
            loginPanel.SetActive(false);

        if (registerPanel != null)
            registerPanel.SetActive(false);

        if (mainDashboardPanel != null)
            mainDashboardPanel.SetActive(false);
    }

    private async Task StartApplication()
    {
        float startTime =
            Time.realtimeSinceStartup;


        // =====================================================
        // SHOW SPLASH
        // =====================================================

        ShowOnly(
            splashPanel
        );


        Debug.Log(
            "StartupManager: Splash started."
        );


        // =====================================================
        // WAIT FOR FIREBASE
        // =====================================================

        if (FirebaseManager.Instance == null)
        {
            Debug.LogError(
                "StartupManager: FirebaseManager not found."
            );

            await WaitMinimumSplash(
                startTime
            );

            ShowWelcome();

            return;
        }


        bool firebaseReady =
            await FirebaseManager.Instance
                .WaitUntilReady();


        if (!firebaseReady)
        {
            Debug.LogError(
                "StartupManager: Firebase failed."
            );

            await WaitMinimumSplash(
                startTime
            );

            ShowWelcome();

            return;
        }


        Debug.Log(
            "StartupManager: Firebase ready."
        );


        // =====================================================
        // AUTO LOGIN
        // =====================================================

        bool loggedIn = false;


        if (AccountManager.Instance != null)
        {
            loggedIn =
                await AccountManager.Instance
                    .AutoLogin();
        }


        // =====================================================
        // KEEP SPLASH FOR MINIMUM TIME
        // =====================================================

        await WaitMinimumSplash(
            startTime
        );


        // =====================================================
        // SHOW CORRECT PANEL
        // =====================================================

        if (loggedIn)
        {
            Debug.Log(
                "StartupManager: Existing user found."
            );

            ShowDashboard();
        }
        else
        {
            Debug.Log(
                "StartupManager: No logged-in user."
            );

            ShowWelcome();
        }
    }


    // =========================================================
    // WAIT
    // =========================================================

    private async Task WaitMinimumSplash(
        float startTime)
    {
        float elapsed =
            Time.realtimeSinceStartup -
            startTime;


        float remaining =
            minimumSplashTime -
            elapsed;


        if (remaining > 0)
        {
            await Task.Delay(
                Mathf.RoundToInt(
                    remaining * 1000f
                )
            );
        }
    }


    // =========================================================
    // SHOW WELCOME
    // =========================================================

    public void ShowWelcome()
    {
        ShowOnly(
            welcomePanel
        );
    }


    // =========================================================
    // SHOW LOGIN
    // =========================================================

    public void ShowLogin()
    {
        ShowOnly(
            loginPanel
        );
    }


    // =========================================================
    // SHOW REGISTER
    // =========================================================

    public void ShowRegister()
    {
        ShowOnly(
            registerPanel
        );
    }


    // =========================================================
    // SHOW DASHBOARD
    // =========================================================

    public void ShowDashboard()
    {
        ShowOnly(
            mainDashboardPanel
        );
    }


    // =========================================================
    // SHOW ONLY ONE PANEL
    // =========================================================

    private void ShowOnly(
        GameObject target)
    {
        if (splashPanel != null)
        {
            splashPanel.SetActive(false);
        }


        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }


        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }


        if (registerPanel != null)
        {
            registerPanel.SetActive(false);
        }


        if (mainDashboardPanel != null)
        {
            mainDashboardPanel.SetActive(false);
        }


        if (target != null)
        {
            target.SetActive(true);
        }
    }
}