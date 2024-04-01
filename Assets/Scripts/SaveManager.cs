using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class SaveManager : MonoBehaviour
{
    private static string saveFilePath = Application.persistentDataPath + "/savefile.json";
    private static SaveObject cachedSaveObject = null;

    public static void Save(SaveObject saveObject)
    {
        string json = JsonConvert.SerializeObject(saveObject);
        string encryptedJson = CryptoManager.EncryptString(json);
        File.WriteAllText(saveFilePath, encryptedJson);

        cachedSaveObject = saveObject;
    }

    public static SaveObject Load()
    {
        if (cachedSaveObject != null)
        {
            return cachedSaveObject;
        }

        if (File.Exists(saveFilePath))
        {
            string encryptedJson = File.ReadAllText(saveFilePath);
            string decryptedJson = CryptoManager.DecryptString(encryptedJson);
            cachedSaveObject = JsonConvert.DeserializeObject<SaveObject>(decryptedJson);
            if (cachedSaveObject.Statistics == null)
            {
                cachedSaveObject.Statistics = new Statistics();
            }

            return cachedSaveObject;
        }

        cachedSaveObject = new SaveObject();
        return cachedSaveObject;
    }

    public static void Clear()
    {
        cachedSaveObject = new SaveObject();

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

}

[System.Serializable]
public class SaveObject
{
    public int HighScore;
    public bool EnableSound = true;
    public bool HasSeenTutorial;
    public Difficulty Difficulty = Difficulty.Normal;
    public Statistics Statistics = new Statistics();
}

[System.Serializable]
public class Statistics
{
    public string LongestWinningWord = "";
    public string LongestLosingWord = "";
    public string MostPointsPerRoundWord = "";
    public Dictionary<string, int> FrequentStartingLetter = new Dictionary<string, int>();
    public int MostPointsPerRound;
    public int Skunks;
    public DateTime LastIncrementDate = DateTime.MinValue;
    public int DailyPlayStreak;
    public int LongestWinStreak;
    public int WinStreak;
    public int GamesPlayed;
    public List<string> WinningWords = new List<string>();
}


[System.Serializable]
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}