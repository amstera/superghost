using UnityEngine;
using System.Collections;

public class PowersWarningPopUp : MonoBehaviour
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

        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}