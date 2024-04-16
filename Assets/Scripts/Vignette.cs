using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Vignette : MonoBehaviour
{
    public Image vignetteImage;

    void Start()
    {
        vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0);
        vignetteImage.raycastTarget = false;
    }

    public void Show(float targetAlpha)
    {
        StartCoroutine(FadeVignetteTo(targetAlpha));
    }

    public void Hide()
    {
        vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0);
    }

    private IEnumerator FadeVignetteTo(float targetAlpha)
    {
        float duration = 1f;
        float currentTime = 0f;
        float startAlpha = vignetteImage.color.a;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, currentTime / duration);
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, alpha);
            yield return null;
        }
        vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, targetAlpha);
    }
}
