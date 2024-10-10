using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class CommandCenter : MonoBehaviour
{
    public GameManager gameManager;
    public Button shopButton;
    public ProceduralImage shopGlowOutline;
    private SaveObject saveObject;
    public PowersModal powersModal;

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
        int playerLives = gameManager.playerLivesText.LivesRemaining();
        int aiLives = gameManager.aiLivesText.LivesRemaining();
        int playerCurrency = gameManager.currency;
        int gamesPlayed = saveObject.Statistics.GamesPlayed;
        int currentLevel = saveObject.CurrentLevel;

        var minPointsCriterion = criteria.Find(c => c is ScoreAtLeastXPoints) as ScoreAtLeastXPoints;
        bool minCriteriaNotMet = minPointsCriterion != null && !minPointsCriterion.IsMet(gameState) && aiLives <= 2;

        bool showPowersNotice = false;
        if (playerLives == 1 && playerCurrency >= 5 && gamesPlayed > 0) // 1 life left
        {
            showPowersNotice = true;
        }
        else if (minCriteriaNotMet && playerCurrency > 0) // not hitting enough points
        {
            float pointsRatio = (float)gameState.Points / minPointsCriterion.GetPoints();
            float requiredRatio = aiLives == 1 ? 1f : 0.5f;
            if (pointsRatio < requiredRatio)
            {
                showPowersNotice = true;
            }
        }

        if (showPowersNotice)
        {
            if (minCriteriaNotMet)
            {
                powersModal.ShowModal($"Use <color=yellow>Powers</color> to reach <color=yellow>{minPointsCriterion.GetFormattedPoints()} PTS</color>!");
            }
            else
            {
                powersModal.ShowModal($"Use <color=yellow>Powers</color> to beat <sprite=1><color=yellow>CASP</color>!");
            }
        }
        else if (currentLevel == 1 && aiLives == 1)
        {
            var useAtLeastItemsCriteria = criteria.Find(c => c is UseAtLeastXItems) as UseAtLeastXItems;
            if (useAtLeastItemsCriteria != null && !useAtLeastItemsCriteria.IsMet(gameState))
            {
                powersModal.ShowModal("You must use <color=yellow>1+ Power</color> to win!");
            }
        }
        else
        {
            powersModal.HideModal(0);
        }

        shopButton.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = showPowersNotice;
    }
}
