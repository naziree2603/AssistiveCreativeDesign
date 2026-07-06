using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LeaderboardLoader
{
    public static List<ParticipantData> LoadAll()
    {
        List<ParticipantData> participants =
            new List<ParticipantData>();

        string folder =
            Path.Combine(
                Application.persistentDataPath,
                "Accounts");

        if (!Directory.Exists(folder))
            return participants;

        string[] files =
            Directory.GetFiles(folder, "*.json");

        foreach (string file in files)
        {
            string json =
                File.ReadAllText(file);

            AccountData account =
                JsonUtility.FromJson<AccountData>(json);

            if (account == null)
                continue;

            if (account.participant == null)
                continue;

            if (string.IsNullOrEmpty(
                account.participant.participantName))
                continue;

            participants.Add(account.participant);
        }

        participants.Sort((a, b) =>
            b.score.CompareTo(a.score));

        return participants;
    }
}