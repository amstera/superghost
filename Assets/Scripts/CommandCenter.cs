using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class CommandCenter : MonoBehaviour
{
    public GameManager gameManager;
    public TextMeshProUGUI shopText;
    public Button shopButton;
    public ProceduralImage shopGlowOutline;
    private SaveObject saveObject;
    public GameObject spiralBackground;

    void Awake()
    {
        saveObject = SaveManager.Load();
    }

    void OnEnable()
    {
        AdjustAlphaBasedOnLevel();
    }

    public void AdjustAlphaBasedOnLevel()
    {
        float alpha = Mathf.Lerp(0, 60, Mathf.Min(1, (saveObject.CurrentLevel * 1.5f) / 9f) / 255f);

        Color currentColor = shopGlowOutline.color;
        currentColor.a = alpha;
        shopGlowOutline.color = currentColor;
    }

    public void UpdateState(List<GameCriterion> criteria, GameState gameState)
    {
        var minPointsCriterion = criteria.Find(c => c is ScoreAtLeastXPoints);
        shopText.gameObject.SetActive((gameManager.playerLivesText.LivesRemaining() == 1 && gameManager.currency >= 5)
            || (minPointsCriterion != null && !minPointsCriterion.IsMet(gameState) && gameManager.aiLivesText.LivesRemaining() <= 2 && gameManager.currency > 0));
        UpdateCurrency();
        shopButton.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = shopText.gameObject.activeSelf;
    }

    public void UpdateCurrency()
    {
        shopText.text = $"Use {gameManager.currency}\nMana (¤)";
    }
}
