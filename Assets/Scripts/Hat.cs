using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Hat : MonoBehaviour
{
    public Image hatImage;
    public List<HatData> hatDataList = new List<HatData>();

    private Dictionary<HatType, HatData> hatDataDictionary;
    private SaveObject saveObject;

    void Start()
    {
        hatDataDictionary = new Dictionary<HatType, HatData>();
        foreach (HatData data in hatDataList)
        {
            hatDataDictionary[data.hatType] = data;
        }

        saveObject = SaveManager.Load();
        UpdateHat(saveObject.HatType);
    }

    public void UpdateHat(HatType hatType)
    {
        if (hatType == HatType.None)
        {
            hatImage.color = new Color(1, 1, 1, 0); // Make the image invisible
            return;
        }

        if (hatDataDictionary.TryGetValue(hatType, out HatData data))
        {
            ApplyHatData(data);
        }
        else
        {
            Debug.LogWarning("Hat type not found: " + hatType);
        }
    }

    private void ApplyHatData(HatData data)
    {
        hatImage.sprite = data.sprite;
        hatImage.rectTransform.sizeDelta = new Vector2(data.width, data.height);
        hatImage.rectTransform.anchoredPosition = new Vector2(data.xPos, data.yPos);
        hatImage.color = new Color(1, 1, 1, 1); // Make the image visible
    }
}

public enum HatType
{
    None,
    Fedora,
    Toque,
    Cowboy,
    Cap,
    Crown,
    Devil,
    Jester,
    Party,
    Steampunk,
    Taco,
    Top,
    Wizard
}

[System.Serializable]
public class HatData
{
    public HatType hatType;
    public Sprite sprite;
    public string name;
    public string description;
    public float width;
    public float height;
    public float xPos;
    public float yPos;
}

