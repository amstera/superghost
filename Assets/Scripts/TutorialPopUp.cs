using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameObject[] pages;
    public Button previousButton, nextButton, closeButton;
    public Slider progressBar;
    public float fadeDuration = 0.5f;

    public GameManager gameManager;
    public SaveObject saveObject;

    public AudioSource clickAudioSource;

    private int currentPageIndex = 0;
    private bool showCloseButton;
    private bool hasWonGame;
    private bool hasWonRun;
    private int visiblePagesCount;

    private void Start()
    {
        saveObject = SaveManager.Load();
        SetVisiblePagesCount();
    }

    private void SetVisiblePagesCount()
    {
        hasWonGame = saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0;
        hasWonRun = saveObject.Statistics.EasyWins > 0 || saveObject.Statistics.NormalWins > 0 || saveObject.Statistics.HardWins > 0;

        if (hasWonRun)
        {
            visiblePagesCount = 15; // Show all pages (0-14)
        }
        else if (hasWonGame)
        {
            visiblePagesCount = 14; // Show pages (0-13)
        }
        else
        {
            visiblePagesCount = 10; // Show pages (0-9)
        }
    }

    public void ShowButton()
    {
        Show(0, true);
    }

    public void Show(int startingPageIndex = 0, bool showCloseButton = true)
    {
        clickAudioSource?.Play();

        this.showCloseButton = showCloseButton;
        currentPageIndex = startingPageIndex;
        SetVisiblePagesCount();

        UpdateUI();
        StartCoroutine(FadeIn());

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        popUpGameObject.SetActive(true);

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, currentTime / fadeDuration);
            yield return null;
        }
    }

    public void Hide()
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        if (!showCloseButton)
        {
            gameManager.NewGamePressed();
        }

        StopAllCoroutines();
        ResetPopUp();
    }

    private void ResetPopUp()
    {
        currentPageIndex = 0;
        UpdateUI();
        StartCoroutine(AnimateProgressBar());

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        popUpGameObject.SetActive(false);
    }

    public void NextPage()
    {
        if (currentPageIndex < visiblePagesCount - 1)
        {
            clickAudioSource?.Play();

            currentPageIndex++;
            UpdateUI();
            StartCoroutine(PopButton(nextButton));
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            clickAudioSource?.Play();

            currentPageIndex--;
            UpdateUI();
            StartCoroutine(PopButton(previousButton));
        }
    }

    private void UpdateUI()
    {
        UpdatePageVisibility();
        UpdateButtonVisibility();
        StartCoroutine(AnimateProgressBar());
    }

    private void UpdatePageVisibility()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex && i < visiblePagesCount);
        }
    }

    private void UpdateButtonVisibility()
    {
        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < visiblePagesCount - 1);
        closeButton.gameObject.SetActive(showCloseButton || currentPageIndex == visiblePagesCount - 1);
    }

    private IEnumerator AnimateProgressBar()
    {
        if (progressBar != null && pages.Length > 0)
        {
            float targetValue = (float)currentPageIndex / (visiblePagesCount - 1);
            float currentValue = progressBar.value;
            float elapsedTime = 0;

            while (elapsedTime < 0.15f)
            {
                elapsedTime += Time.deltaTime;
                progressBar.value = Mathf.Lerp(currentValue, targetValue, elapsedTime / 0.15f);
                yield return null;
            }

            progressBar.value = targetValue;
        }
    }

    private IEnumerator PopButton(Button button)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.2f;
        float popDuration = 0.1f;
        float elapsedTime = 0;

        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            button.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / popDuration);
            yield return null;
        }

        elapsedTime = 0;

        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            button.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / popDuration);
            yield return null;
        }

        button.transform.localScale = originalScale;
    }
}