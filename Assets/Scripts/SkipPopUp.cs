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
    public Button levelSkipButton, skipButton;
    public GameManager gameManager;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private SaveObject saveObject;
    private int amountToEarn;

    private void Awake()
    {
        saveObject = SaveManager.Load();
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        StartCoroutine(ButtonPopAnimation());
        clickAudioSource?.Play();

        amountToEarn = Mathf.Min(30, 5 * saveObject.CurrentLevel + 5);
        bodyText.text = $"You can optionally skip to <color=yellow>Level {saveObject.CurrentLevel + 2}</color>!\n\n<line-height=45>In exchange, get <color=green>{amountToEarn}¤</color></line-height>";
        skipButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Skip (<color=green>+{amountToEarn}¤</color>)";
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
        currencyText.AddPoints(amountToEarn);
        skipButton.interactable = false;

        StartCoroutine(SkipAfterDelay());
    }

    private IEnumerator SkipAfterDelay()
    {
        yield return new WaitForSeconds(0.75f);

        saveObject.Currency = gameManager.currency + amountToEarn;
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

    private IEnumerator ButtonPopAnimation()
    {
        Vector3 originalScale = levelSkipButton.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        float duration = 0.1f;

        // Scale up
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            levelSkipButton.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            levelSkipButton.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        levelSkipButton.transform.localScale = originalScale;
    }
}