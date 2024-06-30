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
    private bool showCloseButton = false;
    private bool hasWonGame = false;
    private int visiblePagesCount;

    private void Start()
    {
        saveObject = SaveManager.Load();
    }

    private void SetVisiblePagesCount()
    {
        hasWonGame = saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0;
        visiblePagesCount = hasWonGame ? pages.Length : pages.Length - 5;
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
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            clickAudioSource?.Play();

            currentPageIndex--;
            UpdateUI();
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
            pages[i].SetActive(i == currentPageIndex && (hasWonGame || i < pages.Length - 5));
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
}