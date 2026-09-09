using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlayManager : MonoBehaviour
{
    // =========================================================
    // SETTINGS PANEL
    // =========================================================

    [Header("Settings Panel")]
    [SerializeField]
    private GameObject settingsPanel;


    // =========================================================
    // HOW TO PLAY PANEL
    // =========================================================

    [Header("How To Play Panel")]
    [SerializeField]
    private GameObject howToPlayPanel;


    // =========================================================
    // PAGES
    // =========================================================

    [Header("Pages")]
    [SerializeField]
    private GameObject[] pages;


    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Navigation Buttons")]
    [SerializeField]
    private Button previousButton;

    [SerializeField]
    private Button nextButton;


    // =========================================================
    // TEXT
    // =========================================================

    [Header("Page Indicator")]
    [SerializeField]
    private TMP_Text pageIndicator;


    [SerializeField]
    private TMP_Text nextButtonText;


    // =========================================================
    // ACCESSIBILITY
    // =========================================================

    [Header("Accessibility Page Descriptions")]

    [Tooltip(
        "Voice description for each How To Play page. " +
        "Element 0 = Page 1, Element 1 = Page 2, etc."
    )]
    [TextArea(2, 8)]
    [SerializeField]
    private string[] pageSpeech;


    // =========================================================
    // ACCESSIBILITY DELAY
    // =========================================================

    [Header("Accessibility Timing")]

    [Tooltip(
        "Small delay before reading a newly opened page. " +
        "This helps accessibility speech after the panel becomes active."
    )]
    [SerializeField]
    private float speechDelay = 0.25f;


    // =========================================================
    // STATE
    // =========================================================

    private int currentPage = 0;

    private Coroutine speechCoroutine;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }


        HideAllPages();
    }


    // =========================================================
    // OPEN HOW TO PLAY
    // =========================================================

    public void OpenHowToPlay()
    {
        Debug.Log(
            "HowToPlayManager: Opening How To Play."
        );


        // -----------------------------------------------------
        // STOP PREVIOUS SPEECH
        // -----------------------------------------------------

        StopScheduledSpeech();


        // -----------------------------------------------------
        // CLOSE SETTINGS
        // -----------------------------------------------------

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }


        // -----------------------------------------------------
        // OPEN HOW TO PLAY
        // -----------------------------------------------------

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }


        // -----------------------------------------------------
        // START FROM PAGE 1
        // -----------------------------------------------------

        currentPage = 0;


        // -----------------------------------------------------
        // UPDATE UI
        // -----------------------------------------------------

        UpdatePage(false);


        // -----------------------------------------------------
        // SPEAK AFTER PANEL IS ACTIVE
        // -----------------------------------------------------

        SchedulePageSpeech();
    }


    // =========================================================
    // NEXT
    // =========================================================

    public void NextPage()
    {
        Debug.Log(
            "HowToPlayManager: Next Page. Current = " +
            currentPage
        );


        if (
            pages == null ||
            pages.Length == 0
        )
        {
            Debug.LogWarning(
                "HowToPlayManager: Pages array is empty!"
            );

            return;
        }


        // -----------------------------------------------------
        // LAST PAGE
        // -----------------------------------------------------

        if (
            currentPage >=
            pages.Length - 1
        )
        {
            CloseHowToPlay();

            return;
        }


        // -----------------------------------------------------
        // NEXT PAGE
        // -----------------------------------------------------

        currentPage++;


        UpdatePage(true);
    }


    // =========================================================
    // PREVIOUS
    // =========================================================

    public void PreviousPage()
    {
        Debug.Log(
            "HowToPlayManager: Previous Page. Current = " +
            currentPage
        );


        if (
            pages == null ||
            pages.Length == 0
        )
        {
            return;
        }


        if (currentPage <= 0)
        {
            // -------------------------------------------------
            // PAGE 1
            // -------------------------------------------------
            //
            // Do not navigate backwards.
            // Re-read Page 1 instead.
            //
            // -------------------------------------------------

            SchedulePageSpeech();

            return;
        }


        // -----------------------------------------------------
        // PREVIOUS PAGE
        // -----------------------------------------------------

        currentPage--;


        UpdatePage(true);
    }


    // =========================================================
    // UPDATE PAGE
    // =========================================================

    private void UpdatePage(
        bool announcePage)
    {
        if (
            pages == null ||
            pages.Length == 0
        )
        {
            Debug.LogWarning(
                "HowToPlayManager: No pages assigned!"
            );

            return;
        }


        // -----------------------------------------------------
        // ACTIVATE CORRECT PAGE
        // -----------------------------------------------------

        for (
            int i = 0;
            i < pages.Length;
            i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(
                    i == currentPage
                );
            }
        }


        // -----------------------------------------------------
        // PREVIOUS BUTTON
        // -----------------------------------------------------

        if (previousButton != null)
        {
            previousButton.interactable =
                currentPage > 0;
        }


        // -----------------------------------------------------
        // NEXT / DONE TEXT
        // -----------------------------------------------------

        if (nextButtonText != null)
        {
            nextButtonText.text =
                currentPage ==
                pages.Length - 1
                    ? "DONE"
                    : "NEXT";
        }


        // -----------------------------------------------------
        // PAGE INDICATOR
        // -----------------------------------------------------

        if (pageIndicator != null)
        {
            pageIndicator.text =
                (currentPage + 1) +
                " / " +
                pages.Length;
        }


        // -----------------------------------------------------
        // ANNOUNCE
        // -----------------------------------------------------

        if (announcePage)
        {
            SchedulePageSpeech();
        }
    }


    // =========================================================
    // SPEAK CURRENT PAGE
    // =========================================================

    private void SpeakCurrentPage()
    {
        if (
            pages == null ||
            pages.Length == 0
        )
        {
            return;
        }


        string pageText = "";


        // -----------------------------------------------------
        // GET CUSTOM PAGE DESCRIPTION
        // -----------------------------------------------------

        if (
            pageSpeech != null &&
            currentPage >= 0 &&
            currentPage < pageSpeech.Length
        )
        {
            pageText =
                pageSpeech[currentPage];
        }


        // -----------------------------------------------------
        // FALLBACK
        // -----------------------------------------------------

        if (
            string.IsNullOrWhiteSpace(
                pageText
            )
        )
        {
            pageText =
                "How To Play. Page " +
                (currentPage + 1) +
                " of " +
                pages.Length +
                ".";
        }


        // -----------------------------------------------------
        // FINAL MESSAGE
        // -----------------------------------------------------

        string message =
            "How To Play. " +
            "Page " +
            (currentPage + 1) +
            " of " +
            pages.Length +
            ". " +
            pageText;


        Debug.Log(
            "HowToPlayManager: Speaking page " +
            (currentPage + 1) +
            ": " +
            message
        );


        Speak(message);
    }


    // =========================================================
    // SCHEDULE PAGE SPEECH
    // =========================================================

    private void SchedulePageSpeech()
    {
        StopScheduledSpeech();


        speechCoroutine =
            StartCoroutine(
                SpeakPageAfterDelay()
            );
    }


    // =========================================================
    // SPEAK PAGE AFTER DELAY
    // =========================================================

    private IEnumerator SpeakPageAfterDelay()
    {
        // -----------------------------------------------------
        // WAIT FOR PANEL / PAGE TO BECOME ACTIVE
        // -----------------------------------------------------

        yield return null;


        if (speechDelay > 0f)
        {
            yield return new WaitForSeconds(
                speechDelay
            );
        }


        // -----------------------------------------------------
        // CHECK PANEL
        // -----------------------------------------------------

        if (
            howToPlayPanel == null ||
            !howToPlayPanel.activeInHierarchy
        )
        {
            speechCoroutine = null;

            yield break;
        }


        // -----------------------------------------------------
        // SPEAK
        // -----------------------------------------------------

        SpeakCurrentPage();


        speechCoroutine = null;
    }


    // =========================================================
    // STOP SCHEDULED SPEECH
    // =========================================================

    private void StopScheduledSpeech()
    {
        if (speechCoroutine != null)
        {
            StopCoroutine(
                speechCoroutine
            );

            speechCoroutine = null;
        }
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void CloseHowToPlay()
    {
        Debug.Log(
            "HowToPlayManager: Closing How To Play."
        );


        StopScheduledSpeech();


        // -----------------------------------------------------
        // CLOSE HOW TO PLAY
        // -----------------------------------------------------

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }


        // -----------------------------------------------------
        // RETURN SETTINGS
        // -----------------------------------------------------

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }


        // -----------------------------------------------------
        // OPTIONAL ANNOUNCEMENT
        // -----------------------------------------------------

        Speak(
            "How To Play closed. " +
            "Returned to Settings."
        );
    }


    // =========================================================
    // HIDE ALL PAGES
    // =========================================================

    private void HideAllPages()
    {
        if (pages == null)
        {
            return;
        }


        for (
            int i = 0;
            i < pages.Length;
            i++
        )
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(false);
            }
        }
    }


    // =========================================================
    // ACCESSIBILITY
    // =========================================================

    private void Speak(string message)
    {
        try
        {
            if (!AccessibilityToggle.AccessibilityEnabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            AccessibilityToggle
                .AccessibilitySpeech
                .SpeakNavigation(message);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "HowToPlayManager: Accessibility speech failed: " +
                exception.Message
            );
        }
    }
}