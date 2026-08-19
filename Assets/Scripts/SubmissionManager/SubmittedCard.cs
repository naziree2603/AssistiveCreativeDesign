using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubmittedCard : MonoBehaviour
{
    // =========================================================
    // EVENT
    // =========================================================

    [Header("Event")]

    [SerializeField]
    private TMP_Text challengeTitleText;


    // =========================================================
    // SCORE
    // =========================================================

    [Header("Score")]

    [SerializeField]
    private TMP_Text scoreText;


    // =========================================================
    // DATE
    // =========================================================

    [Header("Submission Date")]

    [SerializeField]
    private TMP_Text dateText;


    // =========================================================
    // ACTION
    // =========================================================

    [Header("Actions")]

    [SerializeField]
    private Button openButton;


    // =========================================================
    // INTERNAL DATA
    // =========================================================

    private SubmissionManager.SubmissionData submission;


    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        SubmissionManager.SubmissionData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "SubmittedCard: Submission data is null."
            );

            return;
        }


        submission =
            data;


        // -----------------------------------------------------
        // EVENT NAME
        // -----------------------------------------------------

        if (challengeTitleText != null)
        {
            challengeTitleText.text =
                string.IsNullOrWhiteSpace(
                    data.challengeTitle
                )
                    ? "Untitled Event"
                    : data.challengeTitle;
        }


        // -----------------------------------------------------
        // SCORE
        // -----------------------------------------------------

        if (scoreText != null)
        {
            scoreText.text =
                data.score +
                "/100";
        }


        // -----------------------------------------------------
        // DATE
        // -----------------------------------------------------

        if (dateText != null)
        {
            dateText.text =
                string.IsNullOrWhiteSpace(
                    data.completedDate
                )
                    ? "-"
                    : data.completedDate;
        }


        // -----------------------------------------------------
        // OPEN BUTTON
        // -----------------------------------------------------

        SetupOpenButton();
    }


    // =========================================================
    // SETUP BUTTON
    // =========================================================

    private void SetupOpenButton()
    {
        if (openButton == null)
        {
            return;
        }


        openButton.onClick.RemoveAllListeners();


        openButton.onClick.AddListener(
            OpenSubmission
        );
    }


    // =========================================================
    // OPEN SUBMISSION
    // =========================================================

    public void OpenSubmission()
    {
        if (submission == null)
        {
            Debug.LogWarning(
                "SubmittedCard: No submission assigned."
            );

            return;
        }


        if (SubmissionManager.Instance == null)
        {
            Debug.LogError(
                "SubmittedCard: SubmissionManager is not available."
            );

            return;
        }


        SubmissionManager.Instance
            .OpenSubmission(
                submission
            );
    }


    // =========================================================
    // GET SUBMISSION
    // =========================================================

    public SubmissionManager.SubmissionData
        GetSubmission()
    {
        return submission;
    }


    // =========================================================
    // GET SUBMISSION ID
    // =========================================================

    public string GetSubmissionID()
    {
        if (submission == null)
        {
            return "";
        }


        return submission.submissionID;
    }


    // =========================================================
    // GET SCORE
    // =========================================================

    public int GetScore()
    {
        if (submission == null)
        {
            return 0;
        }


        return submission.score;
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void Clear()
    {
        submission =
            null;


        if (challengeTitleText != null)
        {
            challengeTitleText.text =
                "";
        }


        if (scoreText != null)
        {
            scoreText.text =
                "";
        }


        if (dateText != null)
        {
            dateText.text =
                "";
        }
    }
}