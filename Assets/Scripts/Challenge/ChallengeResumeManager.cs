using UnityEngine;

public class ChallengeResumeManager : MonoBehaviour
{
    public GameObject promptPanel;
    public GameObject outputPanel;
    public GameObject descriptionPanel;
    public GameObject revisionPanel;
    public GameObject finalExplainationPanel;
    public GameObject scorePanel;

    public void ResumeChallenge()
    {
        CloseAllPanels();

        ParticipantData data =
            ParticipantManager.Instance.CurrentParticipant;

        switch (data.lastPage)
        {
            case "Prompt":
                promptPanel.SetActive(true);
                break;

            case "Output":
                outputPanel.SetActive(true);
                break;

            case "Description":
                descriptionPanel.SetActive(true);
                break;

            case "Revision":
                revisionPanel.SetActive(true);
                break;

            case "Review":
                finalExplainationPanel.SetActive(true);
                break;

            case "Score":
                scorePanel.SetActive(true);
                break;

            default:
                promptPanel.SetActive(true);
                break;
        }
    }

    void CloseAllPanels()
    {
        promptPanel.SetActive(false);
        outputPanel.SetActive(false);
        descriptionPanel.SetActive(false);
        revisionPanel.SetActive(false);
        finalExplainationPanel.SetActive(false);
        scorePanel.SetActive(false);
    }
}