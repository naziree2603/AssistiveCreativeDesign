using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubmittedItem : MonoBehaviour
{
    public TMP_Text challengeText;
    public TMP_Text scoreText;
    public TMP_Text dateText;

    public Button openButton;

    private ParticipantData participant;

    public void Setup(ParticipantData data)
    {
        participant = data;

        challengeText.text = data.challengeTitle;
        scoreText.text = data.score.ToString("0");
        dateText.text = data.createdDate;

        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(OpenSubmission);
    }

    void OpenSubmission()
    {
        SubmittedManager.Instance.OpenSubmission(participant);
    }
}