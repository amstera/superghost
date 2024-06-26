using UnityEngine;

public class BackgroundSwirl : MonoBehaviour
{
    public Material fluidSwirlMaterial;
    
    private Color defaultNonBossColor = new Color32(144, 144, 144, 255);
    private Color defaultBossColor = new Color32(10, 110, 160, 255);
    private Color winColor = new Color32(40, 220, 20, 255);
    private Color loseColor = new Color32(255, 80, 95, 255);

    private SaveObject saveObject;

    void Start()
    {
        saveObject = SaveManager.Load();

        if (fluidSwirlMaterial != null)
        {
            var defaultColor = saveObject.CurrentLevel < 5 ? defaultNonBossColor : defaultBossColor;
            fluidSwirlMaterial.SetColor("_HighlightColor", defaultColor);
        }
    }

    public void UpdateLerp(int playerAIWinDiff, bool playerWon, bool aiWon)
    {
        if (fluidSwirlMaterial != null)
        {
            var defaultColor = saveObject.CurrentLevel < 5 ? defaultNonBossColor : defaultBossColor;
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
