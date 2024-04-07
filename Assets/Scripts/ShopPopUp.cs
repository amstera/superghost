using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class ShopPopUp : MonoBehaviour
{
    public GameManager gameManager;
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public ScrollRect scrollRect;
    public PointsText currencyText;

    public TextMeshProUGUI hintTitleText, shuffleTitleText, multiplierTitleText;
    public Button hintButton, shuffleButton, multiplierButton;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.25f;

    private Vector3 originalScale;
    private int hintCost;
    private int shuffleCost = 5;
    private int multiplierCost;
    private int currency;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show(int currency, string substring, Difficulty difficulty, bool roundEnded)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float multiplier = difficulty == Difficulty.Hard ? 2 : difficulty == Difficulty.Easy ? 0.5f : 1;
        hintCost = roundEnded ? 0 : (int)Mathf.Round(Mathf.Max(substring.Length * multiplier, 1));
        multiplierCost = hintCost * 4;
        this.currency = currency;

        currencyText.SetPoints(currency);

        SetUpHint();
        SetUpShuffle();
        SetUpMultiplier();

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());

        StartCoroutine(ScrollToTop());
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

    private void SetUpHint()
    {
        hintButton.interactable = gameManager.IsPlayerTurn() && currency >= hintCost;
        hintTitleText.text = $"Hint - <color=green>${hintCost}</color>";
    }

    private void SetUpShuffle()
    {
        shuffleButton.interactable = gameManager.IsDoneRound() && currency >= shuffleCost;
        shuffleTitleText.text = $"Shuffle Letters - <color=green>${shuffleCost}</color>";
    }

    private void SetUpMultiplier()
    {
        multiplierButton.interactable = !gameManager.HasBonusMultiplier && !gameManager.IsGameEnded() && currency >= multiplierCost;
        multiplierTitleText.text = $"2x Multiplier - <color=green>${multiplierCost}</color>";
    }

    public void GetHint()
    {
        if (hintCost <= currency)
        {
            moneyAudioSource?.Play();

            currencyText.AddPoints(-hintCost);
            hintButton.interactable = false;
            StartCoroutine(ShowHint());
        }
    }

    public void GetShuffle()
    {
        if (shuffleCost <= currency)
        {
            moneyAudioSource?.Play();

            currencyText.AddPoints(-shuffleCost);
            shuffleButton.interactable = false;
            StartCoroutine(ShowShuffle());
        }
    }

    public void GetMultiplier()
    {
        if (multiplierCost <= currency)
        {
            moneyAudioSource?.Play();

            currencyText.AddPoints(-multiplierCost);
            multiplierButton.interactable = false;
            StartCoroutine(ShowMultiplier());
        }
    }

    private IEnumerator ShowHint()
    {
        yield return new WaitForSeconds(GetTimeToWait(hintCost));

        gameManager.ShowHint(hintCost);
        Hide();
    }

    private IEnumerator ShowShuffle()
    {
        yield return new WaitForSeconds(GetTimeToWait(shuffleCost));

        gameManager.ShuffleComboLetters(shuffleCost);
        Hide();
    }

    private IEnumerator ShowMultiplier()
    {
        yield return new WaitForSeconds(GetTimeToWait(multiplierCost));

        gameManager.EnableMultiplier(multiplierCost);
        Hide();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator ScrollToTop()
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1.0f;
    }

    private float GetTimeToWait(int cost)
    {
        return cost == 1 ? 0.2f : cost < 5 ? 0.35f : 0.65f;
    }
}