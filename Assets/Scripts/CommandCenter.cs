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
    public GameObject spiralBackground;
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
        var minPointsCriterion = criteria.Find(c => c is ScoreAtLeastXPoints) as ScoreAtLeastXPoints;
        bool minCriteriaNotMet = minPointsCriterion != null
                    && !minPointsCriterion.IsMet(gameState)
                    && gameManager.aiLivesText.LivesRemaining() <= 2;
        bool showPowersNotice = (gameManager.playerLivesText.LivesRemaining() == 1 && gameManager.currency >= 5 && saveObject.Statistics.GamesPlayed > 0)
            || (minCriteriaNotMet && gameManager.currency > 0);

        if (showPowersNotice)
        {
            if (minCriteriaNotMet && minPointsCriterion != null)
            {
                powersModal.ShowModal($"Use your <color=yellow>Powers</color> to reach <color=yellow>{minPointsCriterion.GetPoints()} PTS</color>!");
            }
            else
            {
                powersModal.ShowModal($"Use your <color=yellow>Powers</color> to beat <sprite=1><color=yellow>CASP</color>!");
            }
        }
        else if (saveObject.CurrentLevel == 1 && gameManager.aiLivesText.LivesRemaining() == 1)
        {
            var useAtLeastItemsCriteria = criteria.Find(c => c is UseAtLeastXItems) as UseAtLeastXItems;
            if (criteria != null && !useAtLeastItemsCriteria.IsMet(gameState))
            {
                powersModal.ShowModal($"You must use <color=yellow>1+ Power</color> to win the game!");
            }
        }
        else
        {
            powersModal.HideModal(0);
        }

        shopButton.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = showPowersNotice;
    }
}
