using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Prefab")]
    public LeaderboardItem itemPrefab;

    [Header("Content")]
    public Transform content;

    void OnEnable()
    {
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        // Clear old rows
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        List<ParticipantData> participants =
            LeaderboardLoader.LoadAll();

        for (int i = 0; i < participants.Count; i++)
        {
            LeaderboardItem item =
                Instantiate(itemPrefab, content);

            item.Setup(participants[i], i + 1);
        }
    }
}