using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class RunInfoPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI statsText;
    public GameManager gameManager;
    public Difficulty difficulty;

    public GameObject newLevel, newGameScore, newRoundScore;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private SaveObject saveObject;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        // Load settings
        saveObject = SaveManager.Load();

        // Set up stats
        ConfigureStats();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());
    }

    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, currentTime / fadeDuration);
            yield return null;
        }
    }

    private IEnumerator ScaleIn()
    {
        popUpGameObject.transform.localScale = Vector3.zero; // Ensure it starts from zero
        float currentTime = 0;
        while (currentTime < scaleDuration)
        {
            currentTime += Time.deltaTime;
            popUpGameObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, currentTime / scaleDuration);
            yield return null;
        }
    }

    public void Hide()
    {
        clickAudioSource?.Play();

        StopAllCoroutines();
        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ConfigureStats()
    {
        var regularLineBreak = "<line-height=10>\n</line-height>\n";
        string text = "";

        text += "Final Level\n";
        text += $"<color=green>{saveObject.RunStatistics.HighestLevel + 1}/10</color>";
        text += regularLineBreak;

        newLevel.SetActive(saveObject.RunStatistics.SetNewHighLevel);

        text += "Difficulty\n";
        if (difficulty== Difficulty.Easy)
        {
            text += $"<color=green>EASY</color>";
        }
        else if (difficulty == Difficulty.Normal)
        {
            text += $"<color=yellow>NORMAL</color>";
        }
        else
        {
            text += $"<color=red>HARD</color>";
        }
        text += regularLineBreak;

        text += "Best Game Score\n";
        text += $"<color=green>{saveObject.RunStatistics.HighScore}</color>";
        text += regularLineBreak;

        newGameScore.SetActive(saveObject.RunStatistics.SetNewHighScore);

        text += "Final Mana\n";
        text += "<line-height=0>\n";
        text += $"<color=green>{saveObject.RunStatistics.MostMoney}¤</color>";
        text += regularLineBreak;

        var mostUsedItem = GetMostUsedItem(saveObject.RunStatistics.UsedShopItemIds);
        text += "Most Used Power\n";
        text += $"<color=green>{mostUsedItem}</color>";
        text += regularLineBreak;

        text += "Best Round Score\n";
        text += $"<color=green>{saveObject.RunStatistics.MostPointsPerRound}</color>";
        if (!string.IsNullOrEmpty(saveObject.RunStatistics.MostPointsPerRoundWord))
        {
            text += "<line-height=0>\n</line-height>\n";
            text += $"({saveObject.RunStatistics.MostPointsPerRoundWord})";
        }

        newRoundScore.SetActive(saveObject.RunStatistics.SetNewRoundHighScore);

        statsText.text = text;
    }

    private string GetMostUsedItem(Dictionary<int, int> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return "N/A";
        }

        int idOfHighestValue = 0;
        int highestValue = int.MinValue;

        foreach (var pair in dictionary)
        {
            if (pair.Value > highestValue)
            {
                highestValue = pair.Value;
                idOfHighestValue = pair.Key;
            }
        }

        return gameManager.shopPopUp.shopItems.FirstOrDefault(s => s.id == idOfHighestValue)?.title ?? "N/A";
    }
}
