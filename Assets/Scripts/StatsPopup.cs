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
        var regularLineBreak = "<line-height=15>\n</line-height>\n";
        var text = "<size=45>Stats</size>";
        text += "<line-height=20>\n</line-height>\n";
        text += "<size=25>High Score</size>\n";
        text += $"<size=45>{saveObject.HighScore}</size>{regularLineBreak}";
        text += "<size=25>Highest Level</size>\n";
        text += $"<size=45>{saveObject.Statistics.LongestWinStreak + 1}</size>{regularLineBreak}";
        text += "<size=25>Daily Play Streak</size>\n";
        text += $"<size=45>{saveObject.Statistics.DailyPlayStreak}</size>{regularLineBreak}";
        text += "<size=25>Most Money</size>\n";
        text += $"<size=45>${saveObject.Statistics.MostMoney}</size>{regularLineBreak}";
        var longestWinningWord = string.IsNullOrEmpty(saveObject.Statistics.LongestWinningWord) ? "N/A" : saveObject.Statistics.LongestWinningWord;
        text += "<size=25>Longest Winning Word</size>\n";
        text += $"<color=green>{longestWinningWord}</color>{regularLineBreak}";
        var longestLosingWord = string.IsNullOrEmpty(saveObject.Statistics.LongestLosingWord) ? "N/A" : saveObject.Statistics.LongestLosingWord;
        text += "<size=25>Longest Losing Word</size>\n";
        text += $"<color=red>{longestLosingWord}</color>{regularLineBreak}";
        text += "<size=25>Most Points in a Round</size>\n";
        text += $"<size=45>{saveObject.Statistics.MostPointsPerRound}</size>";
        if (string.IsNullOrEmpty(saveObject.Statistics.MostPointsPerRoundWord))
        {
            text += regularLineBreak;
        }
        else
        {
            text += $"<line-height=0>\n</line-height>\n<size=25>(<color=green>{saveObject.Statistics.MostPointsPerRoundWord}</color>)</size>{regularLineBreak}";
        }
        text += "<size=25>Games Played</size>\n";
        text += $"<size=45>{saveObject.Statistics.GamesPlayed}</size>{regularLineBreak}";
        text += "<size=25>Avg. Winning Word Length</size>\n";
        var lengthOfAverageWinningWord = saveObject.Statistics.WinningWords.Count > 0 ? saveObject.Statistics.WinningWords.Average(w => w.Length) : 0;
        text += $"<size=45>{Math.Round(lengthOfAverageWinningWord)}</size>{regularLineBreak}";
        text += "<size=25>Frequent 1st Letter</size>\n";
        var frequentStartingLetter = GetFrequentStartingLetter(saveObject.Statistics.FrequentStartingLetter);
        text += $"<size=45>{frequentStartingLetter}</size>{regularLineBreak}";
        text += "<size=25>Skunks</size>\n";
        text += $"<size=45>{saveObject.Statistics.Skunks}</size>{regularLineBreak}";

        statsText.text = text;

        statsContentRect.sizeDelta = new Vector2(statsContentRect.sizeDelta.x, 1100);

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