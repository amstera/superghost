using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stars : MonoBehaviour
{
    public Image[] stars;
    public Color yellowColor = Color.yellow;
    public Color diamondColor = new Color(185f / 255f, 242f / 255f, 255f / 255f, 1f);

    public AudioClip starAudioClip;

    private int starsToLight;

    private SaveObject saveObject;

    void Start()
    {
        saveObject = SaveManager.Load();
    }

    public void Show(int score)
    {
        gameObject.SetActive(true);
        SetStarsColor(Color.white); // Immediate reset to white

        bool isDiamond = false;

        if (score < 50) starsToLight = 1;
        else if (score < 100) starsToLight = 2;
        else if (score < 150) starsToLight = 3;
        else if (score < 200) starsToLight = 4;
        else if (score < 300) starsToLight = 5;
        else
        {
            starsToLight = 5;
            isDiamond = true;
        }

        StartCoroutine(ShowStarsCoroutine(score, isDiamond));
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    public int GetStars()
    {
        return starsToLight;
    }

    private IEnumerator ShowStarsCoroutine(int score, bool isDiamond)
    {
        yield return new WaitForSeconds(0.75f);

        // Light up stars in yellow, one by one
        for (int i = 0; i < starsToLight; i++)
        {
            StartCoroutine(AnimateStar(stars[i], yellowColor)); // Animate and pop
            yield return new WaitForSeconds(0.5f);
        }

        if (isDiamond)
        {
            yield return new WaitForSeconds(0.25f); // Brief pause
            // Change all stars to diamond simultaneously with a pop
            StartCoroutine(AnimateAllStarsDiamond());
        }
    }

    private IEnumerator AnimateStar(Image star, Color targetColor)
    {
        yield return StartCoroutine(PopAndChangeColor(star, targetColor));
    }

    private IEnumerator AnimateAllStarsDiamond()
    {
        foreach (var star in stars)
        {
            StartCoroutine(PopAndChangeColor(star, diamondColor)); // Start all animations
        }

        // Wait for the last pop animation to finish before ending
        yield return new WaitForSeconds(0.5f); // Adjust based on pop animation duration
    }

    private IEnumerator PopAndChangeColor(Image star, Color targetColor)
    {
        if (saveObject.EnableSound)
        {
            AudioSource.PlayClipAtPoint(starAudioClip, Vector3.zero, 0.25f);
        }

        // Change color immediately for the diamond pop effect
        star.color = targetColor;

        Vector3 originalScale = Vector3.one;
        Vector3 popScale = originalScale * 1.5f;
        float duration = 0.2f;

        // Scale up
        float time = 0f;
        while (time < duration)
        {
            star.transform.localScale = Vector3.Lerp(originalScale, popScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;
        while (time < duration)
        {
            star.transform.localScale = Vector3.Lerp(popScale, originalScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        star.transform.localScale = Vector3.one;
    }

    private void SetStarsColor(Color color)
    {
        foreach (var star in stars)
        {
            star.color = color;
            star.transform.localScale = Vector3.one; // Also reset scale
        }
    }
}