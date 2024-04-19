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

[Serializable]
public class SaveObject
{
    public bool EnableSound = true;
    public bool HasSeenTutorial;
    public int Currency = 5;
    public int CurrentLevel;
    public List<int> ShopItemIds = new List<int>();
    public (char Char, int Level) RestrictedChar;
    public HashSet<char> UsedLetters = new HashSet<char>();
    public Difficulty Difficulty = Difficulty.Normal;
    public Statistics Statistics = new Statistics();
    public Statistics RunStatistics = new Statistics();
}

[Serializable]
public class Statistics
{
    public int HighScore;
    public string LongestWinningWord = "";
    public string LongestLosingWord = "";
    public string MostPointsPerRoundWord = "";
    public Dictionary<string, int> FrequentStartingLetter = new Dictionary<string, int>();
    public int MostPointsPerRound;
    public int Skunks;
    public DateTime LastIncrementDate = DateTime.MinValue;
    public int DailyPlayStreak;
    public int HighestLevel;
    public int GamesPlayed;
    public int MostMoney = 5;
    public int HardWins;
    public int NormalWins;
    public int EasyWins;
    public Dictionary<int, int> UsedShopItemIds = new Dictionary<int, int>();
    public List<string> WinningWords = new List<string>();
}


[System.Serializable]
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}