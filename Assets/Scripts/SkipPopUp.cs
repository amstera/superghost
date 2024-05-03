using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class SkipPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI bodyText;
    public PointsText currencyText;
    public Button skipButton;
    public GameManager gameManager;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private SaveObject saveObject;

    private void Awake()
    {
        saveObject = SaveManager.Load();
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        bodyText.text = $"This level is skippable!\n\nSkip <color=yellow>Level {saveObject.CurrentLevel + 1}</color> and get <color=green>$10</color>";
        currencyText.SetPoints(gameManager.currency);

        StopAllCoroutines(); // Ensure no other animations are running
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
        popUpGameObject.transform.localScale = Vector3.zero;
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

        ResetPopUp();
    }

    public void Skip()
    {
        moneyAudioSource?.Play();
        currencyText.AddPoints(10);
        skipButton.interactable = false;

        StartCoroutine(SkipAfterDelay());
    }

    private IEnumerator SkipAfterDelay()
    {
        yield return new WaitForSeconds(0.75f);

        saveObject.Currency = gameManager.currency + 10;
        saveObject.CurrentLevel++;
        var visibleShopItems = gameManager.shopPopUp.GetVisibleShopItems();
        if (visibleShopItems.Count > 0)
        {
            saveObject.ShopItemIds.Clear();
            saveObject.ShopItemIds = visibleShopItems.Select(s => s.id).ToList();
        }

        if (saveObject.Currency > saveObject.Statistics.MostMoney)
        {
            saveObject.Statistics.MostMoney = saveObject.Currency;
        }

        gameManager.UpdateLevelStats();

        SaveManager.Save(saveObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}