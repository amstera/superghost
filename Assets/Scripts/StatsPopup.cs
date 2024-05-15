using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class StatsPopup : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject, statsPage, unlocksPage;
    public Button statsButton, unlocksButton;
    public TextMeshProUGUI statsText;
    public GameManager gameManager;
    public UnlockItem unlockPrefab;
    public Hat hat;

    public RectTransform statsContentRect, unlocksContentRect;
    public ScrollRect statsScrollRect, unlocksScrollRect;

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

        saveObject = SaveManager.Load();

        ConfigureStats();
        ConfigureUnlocks();

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

    public void PressStatsTab()
    {
        clickAudioSource?.Play();

        statsButton.interactable = false;
        unlocksButton.interactable = true;
        statsPage.gameObject.SetActive(true);
        unlocksPage.gameObject.SetActive(false);
    }

    public void PressUnlocksTab()
    {
        clickAudioSource?.Play();

        statsButton.interactable = true;
        unlocksButton.interactable = false;
        statsPage.gameObject.SetActive(false);
        unlocksPage.gameObject.SetActive(true);
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

        text += "Game High Score\n";
        text += $"<color=green>{saveObject.Statistics.HighScore}</color>";
        text += regularLineBreak;

        text += "Daily Play Streak\n";
        text += $"<color=green>{saveObject.Statistics.DailyPlayStreak}</color>";
        text += regularLineBreak;

        text += "<color=yellow>Normal</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.NormalWins}</color>";
        text += "<line-height=-5>\n</line-height>\n";
        text += $"<size=30>(<color=green>{saveObject.Statistics.HighestLevel + 1}/10</color>)</size>";
        text += regularLineBreak;

        text += "<color=red>Hard</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.HardWins}</color>";
        text += "<line-height=-5>\n</line-height>\n";
        text += $"<size=30>(<color=green>{saveObject.Statistics.HardHighestLevel + 1}/10</color>)</size>";
        text += regularLineBreak;

        text += "<color=green>Easy</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.EasyWins}</color>";
        text += "<line-height=-5>\n</line-height>\n";
        text += $"<size=30>(<color=green>{saveObject.Statistics.EasyHighestLevel + 1}/10</color>)</size>";
        text += regularLineBreak;

        text += "Games Played\n";
        text += $"<color=green>{saveObject.Statistics.GamesPlayed}</color>";
        text += regularLineBreak;

        text += "Most Points / Round\n";
        text += $"<color=green>{saveObject.Statistics.MostPointsPerRound}</color>";
        if (string.IsNullOrEmpty(saveObject.Statistics.MostPointsPerRoundWord))
        {
            text += regularLineBreak;
        }
        else
        {
            text += "<line-height=-5>\n</line-height>\n";
            text += $"(<color=green><size=30>{saveObject.Statistics.MostPointsPerRoundWord}</size></color>)";
            text += regularLineBreak;
        }

        var mostUsedItem = GetMostUsedItem(saveObject.Statistics.UsedShopItemIds);
        text += "Most Used Item\n";
        text += $"<color=green>{mostUsedItem}</color>";
        text += regularLineBreak;


        text += "Most Money\n";
        text += $"<color=green>${saveObject.Statistics.MostMoney}</color>";
        text += regularLineBreak;

        var longestWinningWord = string.IsNullOrEmpty(saveObject.Statistics.LongestWinningWord) ? "N/A" : saveObject.Statistics.LongestWinningWord;
        text += "Longest Win Word\n";
        text += $"<color=green>{longestWinningWord}</color>";
        text += regularLineBreak;

        var longestLosingWord = string.IsNullOrEmpty(saveObject.Statistics.LongestLosingWord) ? "N/A" : saveObject.Statistics.LongestLosingWord;
        text += "Longest Lose Word\n";
        text += $"<color=red>{longestLosingWord}</color>";
        text += regularLineBreak;

        var lengthOfAverageWinningWord = saveObject.Statistics.WinningWords.Count > 0 ? Math.Round(saveObject.Statistics.WinningWords.Average(w => w.Length)) : 0;
        text += "Avg. Winning Word Length\n";
        text += $"<color=green>{lengthOfAverageWinningWord}</color>";
        text += regularLineBreak;

        var frequentStartingLetter = GetFrequentStartingLetter(saveObject.Statistics.FrequentStartingLetter);
        text += "Frequent 1st Letter\n";
        text += $"<color=green>{frequentStartingLetter}</color>";
        text += regularLineBreak;

        statsText.text = text;

        statsContentRect.sizeDelta = new Vector2(statsContentRect.sizeDelta.x, 1375);

        StartCoroutine(ScrollToTop(statsScrollRect));
    }

    public void ConfigureUnlocks()
    {
        // Clear existing unlock items
        foreach (Transform child in unlocksContentRect)
        {
            Destroy(child.gameObject);
        }

        var layoutGroup = unlocksContentRect.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(0, 0, 15, 30);

        // Repopulate unlock items
        foreach (var hatData in hat.hatDataList)
        {
            var unlockItem = Instantiate(unlockPrefab, unlocksContentRect);
            unlockItem.statsPopup = this;
            unlockItem.Init(hatData.hatType, CheckIfUnlocked(hatData.hatType), saveObject.HatType == hatData.hatType, hatData.sprite, hatData.name, hatData.description);;
        }

        StartCoroutine(ScrollToTop(unlocksScrollRect));
    }

    public void OnUnlockItemClicked(UnlockItem clickedItem)
    {
        clickAudioSource?.Play();

        hat.UpdateHat(clickedItem.hatType);
        saveObject.HatType = clickedItem.hatType;
        SaveManager.Save(saveObject);

        foreach (Transform sibling in unlocksContentRect)
        {
            var siblingUnlockItem = sibling.GetComponent<UnlockItem>();
            if (siblingUnlockItem != clickedItem)
            {
                siblingUnlockItem.Enabled = false;
            }
        }
    }

    private bool CheckIfUnlocked(HatType type)
    {
        switch (type)
        {
            case HatType.None:
                return true;
            case HatType.Toque:
                return saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0;
            case HatType.Steampunk:
                return saveObject.Statistics.EasyHighestLevel >= 4 || saveObject.Statistics.HighestLevel >= 4 || saveObject.Statistics.HardHighestLevel >= 4;
            case HatType.Wizard:
                return saveObject.Statistics.HighScore >= 150;
            case HatType.Fedora:
                return saveObject.Statistics.HardGameWins > 0;
            case HatType.Jester:
                return saveObject.Statistics.EasyHighestLevel >= 9 || saveObject.Statistics.HighestLevel >= 9 || saveObject.Statistics.HardHighestLevel >= 9;
            case HatType.Party:
                return saveObject.Statistics.EasyWins > 0 || saveObject.Statistics.NormalWins > 0 || saveObject.Statistics.HardWins > 0;
            case HatType.Cap:
                return saveObject.Statistics.HardHighestLevel >= 4;
            case HatType.Cowboy:
                return saveObject.Statistics.HighScore >= 250;
            case HatType.Devil:
                return saveObject.Statistics.MostPointsPerRound >= 150;
            case HatType.Crown:
                return saveObject.Statistics.HardWins > 0;
            case HatType.Taco:
                return saveObject.Statistics.EasyWins >= 5 || saveObject.Statistics.NormalWins >= 5 || saveObject.Statistics.HardWins >= 5;
            case HatType.Top:
                return saveObject.Statistics.NormalWins >= 10 || saveObject.Statistics.HardWins >= 10;
        }

        return false;
    }

    private IEnumerator ScrollToTop(ScrollRect scrollRect)
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1.0f;
    }

    private string GetFrequentStartingLetter(Dictionary<string, int> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return "N/A";
        }

        string keyOfHighestValue = null;
        int highestValue = int.MinValue;

        foreach (var pair in dictionary)
        {
            if (pair.Value > highestValue)
            {
                highestValue = pair.Value;
                keyOfHighestValue = pair.Key;
            }
        }

        return keyOfHighestValue;
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