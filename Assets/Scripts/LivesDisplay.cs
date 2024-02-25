using TMPro;
using UnityEngine;

public class LivesDisplay : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public string livesString = "GHOST";
    public Color defaultColor = new Color(0.2392157f, 0.2392157f, 0.2392157f); // #3D3D3D
    public Color lostLifeColor = Color.white;
    private int currentLifeIndex = 0;

    void Start()
    {
        if (livesText == null)
        {
            Debug.LogError("LivesDisplay: No TextMeshProUGUI component assigned.");
            return;
        }

        UpdateLivesDisplay();
    }

    public void LoseLife()
    {
        if (!IsGameOver())
        {
            currentLifeIndex++;
            UpdateLivesDisplay();
        }
    }

    void UpdateLivesDisplay()
    {
        string displayText = "";
        for (int i = 0; i < livesString.Length; i++)
        {
            if (i < currentLifeIndex)
            {
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(lostLifeColor)}>{livesString[i]}</color>";
            }
            else
            {
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{livesString[i]}</color>";
            }
        }
        livesText.text = displayText;
    }

    public bool IsGameOver()
    {
        return currentLifeIndex >= livesString.Length;
    }
}