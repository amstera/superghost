using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

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

        bodyText.text = $"When you lose a game, it's a  <color=red>PERMA DEATH!</color>\n\nBut you can watch an ad to get a second chance at <color=green>Level {gameManager.copySaveObject.CurrentLevel + 1}</color>";

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

        AdsManager.Instance.rewardedAd.ShowAd();
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
        SaveManager.Save(saveObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleAdSkipped()
    {
        Debug.Log("Ad Skipped - No reward given.");
    }
}