using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AccountSaveSystem
{
    static string FolderPath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                "Accounts");
        }
    }

    public static void Save(AccountData account)
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        string json =
            JsonUtility.ToJson(account, true);

        string path =
            Path.Combine(
                FolderPath,
                account.username + ".json");

        File.WriteAllText(path, json);
    }

    public static AccountData Load(string username)
    {
        string path =
            Path.Combine(
                FolderPath,
                username + ".json");

        if (!File.Exists(path))
            return null;

        string json =
            File.ReadAllText(path);

        return JsonUtility.FromJson<AccountData>(json);
    }

    public static bool Exists(string username)
    {
        string path =
            Path.Combine(
                FolderPath,
                username + ".json");

        return File.Exists(path);
    }

    public static List<string> GetAllAccounts()
    {
        List<string> accounts = new List<string>();

        if (!Directory.Exists(FolderPath))
            return accounts;

        string[] files = Directory.GetFiles(FolderPath, "*.json");

        foreach (string file in files)
        {
            accounts.Add(Path.GetFileNameWithoutExtension(file));
        }

        return accounts;
    }


    public static void DeleteAllAccounts()
    {
        if (!Directory.Exists(FolderPath))
            return;

        string[] files = Directory.GetFiles(FolderPath, "*.json");

        foreach (string file in files)
        {
            File.Delete(file);
        }

        Debug.Log("All accounts deleted.");
    }
}