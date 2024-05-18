using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class BlockPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI bodyText;
    public Button blockButton;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private SaveObject saveObject;
    private string word;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        saveObject = SaveManager.Load();
        ResetPopUp();
    }

    public void Show(string word)
    {
        clickAudioSource?.Play();

        this.word = word;
        bool isBlocked = saveObject.BlockedWords.Contains(word.ToLower());
        bodyText.text = isBlocked ? $"<color=yellow>{word.ToUpper()}</color> is already blocked" : $"Block <color=yellow>{word.ToUpper()}</color> from being used?";
        blockButton.interactable = !isBlocked;

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

        StopAllCoroutines();
        ResetPopUp();
    }

    public void Block()
    {
        clickAudioSource?.Play();

        if (!saveObject.BlockedWords.Contains(word.ToLower()))
        {
            saveObject.BlockedWords.Add(word.ToLower());
            SaveManager.Save(saveObject);
        }

        Hide();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}