using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

public class SaveManager : MonoBehaviour
{
    private static string saveFilePath = Application.persistentDataPath + "/savefile.json";
    private static SaveObject cachedSaveObject = null;
    private static object saveLock = new object();

    public static void Save(SaveObject saveObject)
    {
        lock (saveLock)
        {
            string json = JsonConvert.SerializeObject(saveObject);
            string encryptedJson = CryptoManager.EncryptString(json);
            File.WriteAllText(saveFilePath, encryptedJson);
            cachedSaveObject = saveObject;
        }
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
        lock (saveLock)
        {
            cachedSaveObject = new SaveObject();

            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
        }
    }
}

[Preserve]
[Serializable]
public class SaveObject
{
    public bool EnableSound = true;
    public bool HasSeenTutorial;
    public int Currency = 5;
    public int CurrentLevel;
    public HatType HatType = HatType.None;
    public Dictionary<int, List<int>> ChosenCriteria = new Dictionary<int, List<int>>();
    public List<int> ShopItemIds = new List<int>();
    public Dictionary<int, char> RestrictedChars = new Dictionary<int, char>();
    public Difficulty Difficulty = Difficulty.Normal;
    public Statistics Statistics = new Statistics();
    public Statistics RunStatistics = new Statistics();
}

[Preserve]
[Serializable]
public class Statistics
{
    public int HighScore;
    public string LongestWinningWord = "";
    public string LongestLosingWord = "";
    public string MostPointsPerRoundWord = "";
    public Dictionary<string, int> FrequentStartingLetter = new Dictionary<string, int>();
    public int MostPointsPerRound;
    public DateTime LastIncrementDate = DateTime.MinValue;
    public int DailyPlayStreak;
    public int HighestLevel;
    public int EasyHighestLevel;
    public int HardHighestLevel;
    public int GamesPlayed;
    public int MostMoney = 5;
    public int HardWins;
    public int NormalWins;
    public int EasyWins;
    public int EasyGameWins;
    public int NormalGameWins;
    public int HardGameWins;
    public Dictionary<int, int> UsedShopItemIds = new Dictionary<int, int>();
    public List<string> WinningWords = new List<string>();
}


[Preserve]
[Serializable]
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}