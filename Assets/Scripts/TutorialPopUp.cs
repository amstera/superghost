using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameObject[] pages;
    public Button previousButton, nextButton, closeButton;
    public float fadeDuration = 0.5f;

    public GameManager gameManager;

    public AudioSource clickAudioSource;

    private int currentPageIndex = 0;
    private bool showCloseButton = false;

    private void Start()
    {
        ResetPopUp();
    }

    public void Show(bool showCloseButton = true)
    {
        clickAudioSource?.Play();

        this.showCloseButton = showCloseButton;

        currentPageIndex = 0; // Reset to the first page
        UpdatePageVisibility();
        UpdateButtonVisibility();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(FadeIn());
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
        if (showCloseButton)
        {
            clickAudioSource?.Play();
        }
        else
        {
            gameManager.NewGamePressed();
        }

        StopAllCoroutines(); // Stop any animations
        ResetPopUp();
    }

    private void ResetPopUp()
    {
        currentPageIndex = 0;
        UpdatePageVisibility();
        UpdateButtonVisibility();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        popUpGameObject.SetActive(false);
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            clickAudioSource?.Play();

            currentPageIndex++;
            UpdatePageVisibility();
            UpdateButtonVisibility();
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            clickAudioSource?.Play();

            currentPageIndex--;
            UpdatePageVisibility();
            UpdateButtonVisibility();
        }
    }

    private void UpdatePageVisibility()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }
    }

    private void UpdateButtonVisibility()
    {
        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < pages.Length - 1);
        closeButton.gameObject.SetActive(showCloseButton || currentPageIndex == pages.Length - 1);
    }
}