using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ExtraManaPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameManager gameManager;
    public PointsText currencyText;
    public Button watchButton;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private bool initializeAds;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
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
        currencyText.SetPoints(gameManager.currency);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());

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

    public void GetMana()
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
        Debug.Log("Ad Completed - Reward the player with mana.");
        AddMana();
    }

    public void AddMana()
    {
        moneyAudioSource?.Play();
        currencyText.AddPoints(10);
        watchButton.interactable = false;

        StartCoroutine(AddAfterDelay(10));
    }

    private IEnumerator AddAfterDelay(int amount)
    {
        yield return new WaitForSeconds(0.75f);

        gameManager.currency += amount;
        gameManager.currencyText.AddPoints(amount);
        gameManager.shopPopUp.RefreshView();

        ResetPopUp();
    }


    private void HandleAdSkipped()
    {
        Debug.Log("Ad Skipped - No reward given.");
    }
}