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

    public async void LoadLeaderboard()
    {
        if (FirestoreManager.Instance == null)
        {
            Debug.LogError("FirestoreManager not found.");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        List<ParticipantData> participants =
            await FirestoreManager.Instance.LoadLeaderboard();

        for (int i = 0; i < participants.Count; i++)
        {
            LeaderboardItem item =
                Instantiate(itemPrefab, content);

            item.Setup(participants[i], i + 1);
        }
    }
}