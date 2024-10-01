using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI.ProceduralImage;

public class TutorialPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameObject[] pages;
    public Button previousButton, nextButton, closeButton;
    public ProceduralImage buttonMeter;
    public Slider progressBar;
    public float fadeDuration = 0.5f;

    public GameManager gameManager;
    public SaveObject saveObject;

    public AudioSource clickAudioSource;

    private int currentPageIndex = 0;
    private bool showCloseButton;
    private bool startNewGame;
    private System.Action callback;
    private List<int> visiblePageIndices = new List<int>();
    private HashSet<int> visitedPages = new HashSet<int>();

    private void Start()
    {
        saveObject = SaveManager.Load();
        SetVisiblePages();
    }

    private void SetVisiblePages(int startingPageIndex = 0, int endingPageIndex = -1)
    {
        visiblePageIndices.Clear();

        if (startingPageIndex >= 0 && endingPageIndex >= 0 && endingPageIndex >= startingPageIndex)
        {
            for (int i = startingPageIndex; i <= endingPageIndex && i < pages.Length; i++)
            {
                visiblePageIndices.Add(i);
            }
        }
        else
        {
            bool hasWonGame = saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0;
            bool hasWonRun = saveObject.Statistics.EasyWins > 0 || saveObject.Statistics.NormalWins > 0 || saveObject.Statistics.HardWins > 0;
            bool hasPressedChallengeButton = saveObject.HasPressedChallengeButton;

            for (int i = 0; i < 5; i++) visiblePageIndices.Add(i);

            if (hasPressedChallengeButton)
            {
                for (int i = 5; i <= 8; i++) visiblePageIndices.Add(i);
            }

            if (hasWonGame)
            {
                for (int i = 9; i <= 13; i++) visiblePageIndices.Add(i);
            }

            if (hasWonRun) visiblePageIndices.Add(14);
        }
    }

    public void Show(int startingPageIndex = 0, bool showCloseButton = true, bool startNewGame = false, System.Action callback = null, int endingPageIndex = -1, string closeButtonText = "Close")
    {
        clickAudioSource?.Play();

        this.showCloseButton = showCloseButton;
        this.startNewGame = startNewGame;
        this.callback = callback;
        closeButton.GetComponentInChildren<TextMeshProUGUI>().text = closeButtonText;
        visitedPages.Clear();
        buttonMeter.fillAmount = 0;
        buttonMeter.gameObject.SetActive(false);

        SetVisiblePages(startingPageIndex, endingPageIndex);

        currentPageIndex = 0;

        UpdateUI();
        StartCoroutine(FadeIn());

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ShowButton()
    {
        Show(visiblePageIndices[0], true);
    }

    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        popUpGameObject.SetActive(true);

        // Start with scale at 0 (collapsed)
        popUpGameObject.transform.localScale = Vector3.zero;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;

            // Lerp both the alpha and the scale simultaneously
            canvasGroup.alpha = Mathf.Lerp(0, 1, currentTime / fadeDuration);
            popUpGameObject.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, currentTime / fadeDuration);

            yield return null;
        }

        // Ensure the final values are exactly set to avoid any inaccuracies from Lerp
        canvasGroup.alpha = 1;
        popUpGameObject.transform.localScale = Vector3.one;
    }

    public void Hide()
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        StopAllCoroutines();
        ResetPopUp();

        if (!showCloseButton && startNewGame)
        {
            gameManager.NewGamePressed();
        }

        if (callback != null)
        {
            callback.Invoke();
        }
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
        if (currentPageIndex < visiblePageIndices.Count - 1)
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
            pages[i].SetActive(false);
        }

        if (currentPageIndex >= 0 && currentPageIndex < visiblePageIndices.Count)
        {
            pages[visiblePageIndices[currentPageIndex]].SetActive(true);
        }
    }

    private void UpdateButtonVisibility()
    {
        previousButton.interactable = false;
        nextButton.interactable = false;

        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < visiblePageIndices.Count - 1);

        if (!visitedPages.Contains(currentPageIndex) && !showCloseButton)
        {
            visitedPages.Add(currentPageIndex);
            StartCoroutine(ShowNextButtonWithDelay());
        }
        else
        {
            nextButton.interactable = true;
            previousButton.interactable = true;
        }

        closeButton.gameObject.SetActive(showCloseButton || currentPageIndex == visiblePageIndices.Count - 1);
    }

    private IEnumerator ShowNextButtonWithDelay()
    {
        // Disable the button and show the meter
        nextButton.interactable = false;
        buttonMeter.fillAmount = 0;
        buttonMeter.gameObject.SetActive(true);

        float chargeTime = 2f; // Time for the meter to fill
        float elapsedTime = 0;

        // Animate the circular meter's fill
        while (elapsedTime < chargeTime)
        {
            elapsedTime += Time.deltaTime;
            buttonMeter.fillAmount = Mathf.Clamp01(elapsedTime / chargeTime);
            yield return null;
        }

        // Once the meter is fully charged, hide the meter, enable the button, and pop it
        buttonMeter.gameObject.SetActive(false);
        nextButton.interactable = true;
        previousButton.interactable = true;
        StartCoroutine(PopButton(nextButton));
    }

    private IEnumerator AnimateProgressBar()
    {
        if (progressBar != null)
        {
            // Hide the progress bar if there's only one visible page
            if (visiblePageIndices.Count <= 1)
            {
                progressBar.gameObject.SetActive(false);
            }
            else
            {
                progressBar.gameObject.SetActive(true);

                float targetValue = (float)currentPageIndex / (visiblePageIndices.Count - 1);
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

    private IEnumerator PopButton(Button button)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.2f;
        float popDuration = 0.15f;
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