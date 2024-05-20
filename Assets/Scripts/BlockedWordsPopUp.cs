using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class BlockedWordsPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject, noBlockedWordsText;
    public GameManager gameManager;
    public GameObject blockedWordPrefab;

    public RectTransform contentRect;
    public ScrollRect scrollRect;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private SaveObject saveObject;
    private Vector3 originalScale;

    private void Start()
    {
        saveObject = SaveManager.Load();
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        ConfigureScrollView();

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

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ConfigureScrollView(bool scrollToTop = true)
    {
        // Clear existing unlock items
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        var layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(0, 0, 15, 30);

        noBlockedWordsText.SetActive(saveObject.BlockedWords.Count == 0);

        // Repopulate unlock items
        foreach (var blockedWord in saveObject.BlockedWords.OrderBy(b => b))
        {
            var blockedWordObj = Instantiate(blockedWordPrefab, contentRect);
            blockedWordObj.GetComponentInChildren<TextMeshProUGUI>().text = blockedWord.ToUpper();
            blockedWordObj.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveBlockedWord(blockedWord));
        }

        if (scrollToTop)
        {
            StartCoroutine(ScrollToTop(scrollRect));
        }
    }

    public void RemoveBlockedWord(string word)
    {
        clickAudioSource?.Play();

        saveObject.BlockedWords.Remove(word);
        SaveManager.Save(saveObject);

        ConfigureScrollView(false);
    }

    private IEnumerator ScrollToTop(ScrollRect scrollRect)
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1.0f;
    }
}