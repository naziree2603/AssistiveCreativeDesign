using TMPro;
using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text categoryText;
    public TMP_Text institutionText;
    public TMP_Text scoreText;

    public void Setup(ParticipantData data, int rank)
    {
        rankText.text = rank.ToString();

        nameText.text = data.participantName;

        categoryText.text = data.category;

        institutionText.text = data.institution;

        scoreText.text = data.score.ToString("0");
    }
}