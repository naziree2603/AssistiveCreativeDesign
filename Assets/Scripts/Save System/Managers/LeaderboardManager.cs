using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Prefab")]
    public LeaderboardItem itemPrefab;

    [Header("Content")]
    public Transform content;

    [Header("Challenge Title")]
    public TMP_Text challengeTitleText;

    void OnEnable()
    {
        LoadLeaderboard();
    }

    public async void LoadLeaderboard()
    {
        Debug.Log("===== LEADERBOARD =====");

        Debug.Log("CurrentChallenge = " +
            (ChallengeManager.Instance.CurrentChallenge == null
                ? "NULL"
                : ChallengeManager.Instance.CurrentChallenge.title));

        Debug.Log("ChallengeList Count = " +
            ChallengeManager.Instance.ChallengeList.Count);


        if (FirestoreEntryManager.Instance == null)
        {
            Debug.LogError("FirestoreEntryManager not found.");
            return;
        }

        if (ChallengeManager.Instance.CurrentChallenge != null)
        {
            challengeTitleText.text =
                ChallengeManager.Instance.CurrentChallenge.title;
        }
        else
        {
            challengeTitleText.text = "Leaderboard";
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (ChallengeManager.Instance.CurrentChallenge == null)
        {
            Debug.Log("No Challenge Selected");
            return;
        }

        List<ParticipantData> participants = await FirestoreEntryManager.Instance.LoadLeaderboard(ChallengeManager.Instance.CurrentChallenge.challengeID);

        for (int i = 0; i < participants.Count; i++)
        {
            LeaderboardItem item =
                Instantiate(itemPrefab, content);

            item.Setup(participants[i], i + 1);
        }


    }
}