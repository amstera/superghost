using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.Services.Analytics;

public class RedoLevelPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameManager gameManager;
    public TextMeshProUGUI bodyText;
    public Button watchButton;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private bool initializeAds;
    private SaveObject saveObject;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        saveObject = SaveManager.Load();
        ResetPopUp();
    }

    void OnDestroy()
    {
        if (initializeAds)
        {
            AdsManager.Instance.rewardedAd.OnAdCompleted -= HandleAdCompleted;
            AdsManager.Instance.rewardedAd.OnAdSkipped -= HandleAdSkipped;
        }
    }

    public void Show()
    {
        clickAudioSource?.Play();

        watchButton.interactable = true;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());

        string adText = "Watch an ad to get a <color=green>second chance</color>";
        watchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Watch Ad";
        if (!saveObject.HasRedoneLevel)
        {
            adText = "Here's a one-time chance for a <color=green>second shot</color>";
            watchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Retry Level";
        }
        bodyText.text = $"When you lose a game, it's a <color=red>PERMANENT DEATH</color>\n\n{adText} at <line-height=55>\n<color=yellow>Level {gameManager.copySaveObject.CurrentLevel + 1}</color>";

        // Subscribe to ad events
        if (!initializeAds)
        {
            AdsManager.Instance.rewardedAd.OnAdCompleted += HandleAdCompleted;
            AdsManager.Instance.rewardedAd.OnAdSkipped += HandleAdSkipped;
            initializeAds = true;
        }
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
        popUpGameObject.transform.localScale = Vector3.zero;
        float currentTime = 0;
        while (currentTime < scaleDuration)
        {
            currentTime += Time.deltaTime;
            popUpGameObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, currentTime / scaleDuration);
            yield return null;
        }
    }

    public void RedoLevel()
    {
        clickAudioSource?.Play();

        if (saveObject.HasRedoneLevel)
        {
            AdsManager.Instance.rewardedAd.ShowAd();
        }
        else
        {
            RestartLevel();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void Hide()
    {
        clickAudioSource?.Play();

        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void HandleAdCompleted()
    {
        Debug.Log("Ad Completed - Reward the player.");

        RestartLevel();

        var adWatchedEvent = new CustomEvent("watchedAd")
            {
                { "source", "redo_level" },
                { "games_played", saveObject.Statistics.GamesPlayed },
                { "current_level", saveObject.CurrentLevel + 1 },
                { "difficulty", saveObject.Difficulty.ToString() },
                { "high_score", saveObject.Statistics.HighScore },
                { "total_wins", saveObject.Statistics.NormalWins + saveObject.Statistics.EasyWins +  saveObject.Statistics.HardWins },
                { "highest_level", Mathf.Max(saveObject.Statistics.EasyHighestLevel, saveObject.Statistics.HighestLevel, saveObject.Statistics.HardHighestLevel) + 1 }
            };
        AnalyticsService.Instance.RecordEvent(adWatchedEvent);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartLevel()
    {
        watchButton.interactable = false;
        var copySaveObject = gameManager.copySaveObject;
        if (copySaveObject == null)
        {
            Debug.Log("Save object wasn't copied. This shouldn't happen.");
            return;
        }

        saveObject.CurrentLevel = copySaveObject.CurrentLevel;
        saveObject.Currency = copySaveObject.Currency;
        saveObject.ShopItemIds = copySaveObject.ShopItemIds.ToList();
        saveObject.RestrictedChars = copySaveObject.RestrictedChars.ToDictionary(entry => entry.Key, entry => entry.Value);
        saveObject.ChosenCriteria = copySaveObject.ChosenCriteria.ToDictionary(entry => entry.Key, entry => entry.Value);
        saveObject.HasRedoneLevel = true;
        SaveManager.Save(saveObject);
    }

    private void HandleAdSkipped()
    {
        Debug.Log("Ad Skipped - No reward given.");
    }
}