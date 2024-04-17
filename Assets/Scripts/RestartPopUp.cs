using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class RestartPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;

    public AudioSource clickAudioSource;

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

        StopAllCoroutines();
        ResetPopUp();
    }

    public void Restart()
    {
        clickAudioSource?.Play();

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);

        SaveObject saveObject = SaveManager.Load();

        saveObject.Currency = 5;
        saveObject.CurrentLevel = 0;
        saveObject.ShopItemIds = new List<int>();
        saveObject.UsedLetters = new HashSet<char>();

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
}