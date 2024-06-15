using UnityEngine;
using System.Collections;

public class FadeInActivate : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.25f;
    public float delay;

    private void OnEnable()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0;

        yield return new WaitForSeconds(delay);

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            canvasGroup.alpha = elapsedTime / fadeInDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
