using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LeaderboardLoader
{
    public static List<ParticipantData> LoadAll()
    {
        List<ParticipantData> participants = new List<ParticipantData>();

        string folder = Path.Combine(Application.persistentDataPath, "Participants");

        if (!Directory.Exists(folder))
            return participants;

        string[] files = Directory.GetFiles(folder, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);

            ParticipantData data =
                JsonUtility.FromJson<ParticipantData>(json);

            participants.Add(data);
        }

        participants.Sort((a, b) => b.score.CompareTo(a.score));

        return participants;
    }
}