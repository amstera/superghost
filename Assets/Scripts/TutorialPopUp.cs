using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
    private bool startNewGame;
    private System.Action callback;
    private List<int> visiblePageIndices = new List<int>();

    private void Start()
    {
        saveObject = SaveManager.Load();
        SetVisiblePages();
    }

    // Determines which pages are visible based on the player's progress
    private void SetVisiblePages(int startingPageIndex = 0, int endingPageIndex = -1)
    {
        visiblePageIndices.Clear();

        if (startingPageIndex >= 0 && endingPageIndex >= 0 && endingPageIndex >= startingPageIndex)
        {
            // Only show pages between the start and end index, inclusive
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

            // Pages 1-5 are always available
            for (int i = 0; i < 5; i++) visiblePageIndices.Add(i);

            // Unlock Pages 6-9 if the challenge button has been pressed
            if (hasPressedChallengeButton)
            {
                for (int i = 5; i <= 8; i++) visiblePageIndices.Add(i);
            }

            // Unlock Pages 10-14 if the player has won a game
            if (hasWonGame)
            {
                for (int i = 9; i <= 13; i++) visiblePageIndices.Add(i);
            }

            // Unlock Page 15 if the player has won a run
            if (hasWonRun) visiblePageIndices.Add(14);
        }
    }

    // Display the tutorial, starting at a specific page
    public void Show(int startingPageIndex = 0, bool showCloseButton = true, bool startNewGame = false, System.Action callback = null, int endingPageIndex = -1, string closeButtonText = "Close")
    {
        clickAudioSource?.Play();

        this.showCloseButton = showCloseButton;
        this.startNewGame = startNewGame;
        this.callback = callback;
        closeButton.GetComponentInChildren<TextMeshProUGUI>().text = closeButtonText;

        // If endingPageIndex is specified, ensure only the pages within the range are visible
        SetVisiblePages(startingPageIndex, endingPageIndex);

        // Map the original page index to the correct visible index
        currentPageIndex = 0; // Always start at the first visible page within the provided range

        UpdateUI();
        StartCoroutine(FadeIn());

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ShowButton()
    {
        // Call Show() with the first visible page instead of defaulting to page 1 (index 0).
        Show(visiblePageIndices[0], true);
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

    // Hide the popup and reset its state
    public void Hide()
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        if (!showCloseButton && startNewGame)
        {
            gameManager.NewGamePressed();
        }

        StopAllCoroutines();
        ResetPopUp();

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

    // Update UI elements based on the current page
    private void UpdateUI()
    {
        UpdatePageVisibility();
        UpdateButtonVisibility();
        StartCoroutine(AnimateProgressBar());
    }

    // Show only the current page and hide others
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

    // Adjust button visibility based on the current page index
    private void UpdateButtonVisibility()
    {
        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < visiblePageIndices.Count - 1);
        closeButton.gameObject.SetActive(showCloseButton || currentPageIndex == visiblePageIndices.Count - 1);
    }

    private IEnumerator AnimateProgressBar()
    {
        if (progressBar != null && visiblePageIndices.Count > 0)
        {
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

    // Adds a pop effect to buttons when pressed
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