using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParticipantRankCard : MonoBehaviour
{
    [Header("Rank")]
    [SerializeField]
    private TMP_Text rankText;


    [Header("Participant")]
    [SerializeField]
    private TMP_Text participantNameText;


    [SerializeField]
    private TMP_Text institutionText;


    [Header("Score")]
    [SerializeField]
    private TMP_Text scoreText;


    [Header("Optional")]
    [SerializeField]
    private TMP_Text categoryText;


    public void Setup(
        int rank,
        LeaderboardManager.LeaderboardEntry entry)
    {
        if (entry == null)
        {
            return;
        }


        if (rankText != null)
        {
            rankText.text =
                "#" + rank;
        }


        if (participantNameText != null)
        {
            participantNameText.text =
                string.IsNullOrWhiteSpace(
                    entry.participantName
                )
                    ? entry.username
                    : entry.participantName;
        }


        if (institutionText != null)
        {
            institutionText.text =
                entry.institution;
        }


        if (scoreText != null)
        {
            scoreText.text =
                entry.score +
                "/100";
        }


        if (categoryText != null)
        {
            if (
                string.IsNullOrWhiteSpace(
                    entry.subCategory
                )
            )
            {
                categoryText.text =
                    entry.categoryType;
            }
            else
            {
                categoryText.text =
                    entry.categoryType +
                    " - " +
                    entry.subCategory;
            }
        }
    }
}