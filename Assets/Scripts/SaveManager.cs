using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    private static string saveFilePath = Application.persistentDataPath + "/savefile.json";
    private static SaveObject cachedSaveObject = null;

    public static void Save(SaveObject saveObject)
    {
        string json = JsonUtility.ToJson(saveObject);
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
            cachedSaveObject = JsonUtility.FromJson<SaveObject>(decryptedJson);
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
}
