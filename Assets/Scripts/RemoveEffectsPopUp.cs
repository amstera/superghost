using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class RemoveEffectsPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameManager gameManager;
    public ActiveEffectsText activeEffectsText;
    public PointsText currencyText;
    public TextMeshProUGUI bodyText;
    public Button sellButton;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        sellButton.interactable = true;
        currencyText.SetPoints(gameManager.currency);
        int sellMoney = GetValueOfItems();
        bodyText.text = $"Remove all active items and get\n<color=green>${sellMoney}</color>";

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

    public void RemoveEffects()
    {
        moneyAudioSource?.Play();
        int sellMoney = GetValueOfItems();
        currencyText.AddPoints(sellMoney);
        sellButton.interactable = false;

        StartCoroutine(RemoveAfterDelay(sellMoney));
    }

    private IEnumerator RemoveAfterDelay(int sellMoney)
    {
        yield return new WaitForSeconds(0.75f);

        gameManager.currency += sellMoney;
        gameManager.currencyText.AddPoints(sellMoney);
        activeEffectsText.ClearAll();
        gameManager.ClearActiveEffects(true);

        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private int GetValueOfItems()
    {
        int value = 0;
        var activeEffects = activeEffectsText.GetEffects();
        foreach (var effect in activeEffects)
        {
            value += effect.cost;
        }

        return Mathf.Max(1, Mathf.RoundToInt(value * 0.5f));
    }
}