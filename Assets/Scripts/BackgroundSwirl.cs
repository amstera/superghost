using UnityEngine;

public class BackgroundSwirl : MonoBehaviour
{
    public Material fluidSwirlMaterial;
    
    private Color defaultNonBossColor = new Color32(75, 100, 100, 255);
    private Color defaultBossColor = new Color32(10, 110, 160, 255);
    private Color defaultEndlessColor = new Color32(160, 10, 155, 255);
    private Color winColor = new Color32(35, 200, 15, 255);
    private Color loseColor = new Color32(180, 35, 50, 255);

    private SaveObject saveObject;

    void Start()
    {
        saveObject = SaveManager.Load();

        if (fluidSwirlMaterial != null)
        {
            var defaultColor = saveObject.CurrentLevel < 5 ? defaultNonBossColor : saveObject.CurrentLevel < 10 ? defaultBossColor : defaultEndlessColor;
            fluidSwirlMaterial.SetColor("_HighlightColor", defaultColor);
        }
    }

    public void UpdateLerp(int playerAIWinDiff, bool playerWon, bool aiWon)
    {
        if (fluidSwirlMaterial != null)
        {
            var defaultColor = saveObject.CurrentLevel < 5 ? defaultNonBossColor : saveObject.CurrentLevel < 10 ? defaultBossColor : defaultEndlessColor;
            Color targetColor;

            if (playerWon)
            {
                playerAIWinDiff = 5;
            }
            else if (aiWon)
            {
                playerAIWinDiff = -5;
            }

            if (playerAIWinDiff > 0)
            {
                targetColor = Color.Lerp(defaultColor, winColor, Mathf.Clamp01(playerAIWinDiff / 5f));
            }
            else if (playerAIWinDiff < 0)
            {
                targetColor = Color.Lerp(defaultColor, loseColor, Mathf.Clamp01(-playerAIWinDiff / 5f));
            }
            else
            {
                targetColor = defaultColor;
            }

            fluidSwirlMaterial.SetColor("_HighlightColor", targetColor);
        }
    }
}
