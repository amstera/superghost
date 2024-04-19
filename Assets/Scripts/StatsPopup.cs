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
    public GameObject popUpGameObject;
    public TextMeshProUGUI statsText;

    public RectTransform statsContentRect;
    public ScrollRect statsScrollRect;

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

        text += "High Score\n";
        text += $"<color=green>{saveObject.Statistics.HighScore}</color>";
        text += regularLineBreak;

        text += "Daily Play Streak\n";
        text += $"<color=green>{saveObject.Statistics.DailyPlayStreak}</color>";
        text += regularLineBreak;

        text += "<color=yellow>Normal</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.NormalWins}</color>";
        text += regularLineBreak;

        text += "<color=red>Hard</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.HardWins}</color>";
        text += regularLineBreak;

        text += "<color=green>Easy</color> Run Wins\n";
        text += $"<color=green>{saveObject.Statistics.EasyWins}</color>";
        text += regularLineBreak;

        text += "Games Played\n";
        text += $"<color=green>{saveObject.Statistics.GamesPlayed}</color>";
        text += regularLineBreak;

        text += "Most Money\n";
        text += $"<color=green>${saveObject.Statistics.MostMoney}</color>";
        text += regularLineBreak;

        var longestWinningWord = string.IsNullOrEmpty(saveObject.Statistics.LongestWinningWord) ? "N/A" : saveObject.Statistics.LongestWinningWord;
        text += "Longest Win Word\n";
        text += $"<color=green>{saveObject.Statistics.LongestWinningWord}</color>";
        text += regularLineBreak;

        var longestLosingWord = string.IsNullOrEmpty(saveObject.Statistics.LongestLosingWord) ? "N/A" : saveObject.Statistics.LongestLosingWord;
        text += "Longest Loss Word\n";
        text += $"<color=red>{saveObject.Statistics.LongestLosingWord}</color>";
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
            text += $"(<color=green>{saveObject.Statistics.MostPointsPerRoundWord}</color>)";
            text += regularLineBreak;
        }

        var lengthOfAverageWinningWord = saveObject.Statistics.WinningWords.Count > 0 ? Math.Round(saveObject.Statistics.WinningWords.Average(w => w.Length)) : 0;
        text += "Avg. Winning Word Length\n";
        text += $"<color=green>{lengthOfAverageWinningWord}</color>";
        text += regularLineBreak;

        var frequentStartingLetter = GetFrequentStartingLetter(saveObject.Statistics.FrequentStartingLetter);
        text += "Frequent 1st Letter\n";
        text += $"<color=green>{frequentStartingLetter}</color>";
        text += regularLineBreak;

        text += "Skunks\n";
        text += $"<color=green>{saveObject.Statistics.Skunks}</color>";

        statsText.text = text;

        statsContentRect.sizeDelta = new Vector2(statsContentRect.sizeDelta.x, 1275);

        StartCoroutine(ScrollToTop());
    }

    private IEnumerator ScrollToTop()
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        statsScrollRect.verticalNormalizedPosition = 1.0f;
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
}