using UnityEngine;
using System.IO;

public static class JsonSaveSystem
{
    static string FolderPath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "Participants");
        }
    }

    public static void Save(ParticipantData data)
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        string json = JsonUtility.ToJson(data, true);

        string path = Path.Combine(FolderPath, data.participantID + ".json");

        File.WriteAllText(path, json);
    }

    public static ParticipantData Load(string id)
    {
        string path = Path.Combine(FolderPath, id + ".json");

        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<ParticipantData>(File.ReadAllText(path));
    }

    public static void DeleteAll()
    {
        string folder = Path.Combine(
            Application.persistentDataPath,
            "Participants");

        if (!Directory.Exists(folder))
            return;

        string[] files = Directory.GetFiles(folder, "*.json");

        foreach (string file in files)
        {
            File.Delete(file);
        }

        Debug.Log("All participant files deleted.");
    }
}