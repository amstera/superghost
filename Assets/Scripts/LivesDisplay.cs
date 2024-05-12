using System.Collections;
using TMPro;
using UnityEngine;

public class LivesDisplay : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public string livesString = "GHOST";
    public Color defaultColor = new Color(0.2392157f, 0.2392157f, 0.2392157f); // #3D3D3D
    public Color lostLifeColor = Color.red;
    public GameObject flames;
    private int currentLifeIndex = 0;

    void Start()
    {
        if (livesText == null)
        {
            Debug.LogError("LivesDisplay: No TextMeshProUGUI component assigned.");
            return;
        }

        UpdateLivesDisplay();
    }

    public void AddHandicap(int amount)
    {
        currentLifeIndex = Mathf.Min(livesString.Length, currentLifeIndex + amount);
        UpdateLivesDisplay();
    }

    public void LoseLife()
    {
        if (!IsGameOver())
        {
            StartCoroutine(LoseLifeAnimation());
        }
    }

    public void GainLife()
    {
        if (!IsGameOver())
        {
            currentLifeIndex--;
            UpdateLivesDisplay();
            StartCoroutine(PopAnimation());
        }
    }

    public string GetCurrentLivesString()
    {
        return GetDisplayText();
    }

    public void SetLives(int lives)
    {
        currentLifeIndex = livesString.Length - lives;
        UpdateLivesDisplay();
        StartCoroutine(PopAnimation());
    }

    public void LoseAllLives()
    {
        currentLifeIndex = livesString.Length;
        UpdateLivesDisplay();
    }

    IEnumerator LoseLifeAnimation()
    {
        currentLifeIndex++;
        StartCoroutine(PopAnimation());
        yield return StartCoroutine(ColorLerpAnimation(currentLifeIndex - 1, defaultColor, lostLifeColor, 0.3f));

        UpdateLivesDisplay();
    }

    IEnumerator ColorLerpAnimation(int index, Color fromColor, Color toColor, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            UpdateLivesDisplayWithPartialColor(index, Color.Lerp(fromColor, toColor, timer / duration));
            timer += Time.deltaTime;
            yield return null;
        }
        UpdateLivesDisplayWithPartialColor(index, toColor); // Ensure final color is set
    }

    void UpdateLivesDisplayWithPartialColor(int lerpIndex, Color lerpColor)
    {
        string displayText = "";
        for (int i = 0; i < livesString.Length; i++)
        {
            Color currentColor = i < currentLifeIndex ? lostLifeColor : defaultColor;
            if (i == lerpIndex) currentColor = lerpColor;
            displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(currentColor)}>{livesString[i]}</color>";
        }
        livesText.text = displayText;
    }

    IEnumerator PopAnimation()
    {
        float animationTime = 0.3f;
        Vector3 originalScale = livesText.transform.localScale;
        Vector3 targetScale = originalScale * 1.25f;

        // Scale up
        float timer = 0;
        while (timer <= animationTime / 2)
        {
            livesText.transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / (animationTime / 2));
            timer += Time.deltaTime;
            yield return null;
        }

        // Scale down
        timer = 0;
        while (timer <= animationTime / 2)
        {
            livesText.transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / (animationTime / 2));
            timer += Time.deltaTime;
            yield return null;
        }

        livesText.transform.localScale = originalScale;
    }

    void UpdateLivesDisplay()
    {
        livesText.text = GetDisplayText();
        var livesRemaining = LivesRemaining();
        flames.SetActive(LivesRemaining() <= 1);
        flames.transform.GetChild(flames.transform.childCount - 1).gameObject.SetActive(livesRemaining == 0);

        if (livesRemaining <= 1)
        {
            StartCoroutine(CriticalLivesColorAnimation());
        }
        else
        {
            livesText.text = GetDisplayText();
        }
    }

    IEnumerator CriticalLivesColorAnimation()
    {
        while (LivesRemaining() <= 1)
        {
            float duration = 3.0f; // Increased duration for a slower transition
            float timer = 0;
            while (timer < duration)
            {
                Color orange = new Color(1, 165 / 255f, 0, 1); // Orange color
                Color lerpColor = Color.Lerp(
                    Color.red,
                    orange,
                    Mathf.PingPong(timer / duration * 2, 1)  // Ping pong for smooth transition between orange and red
                );

                UpdateLivesDisplayWithCriticalColor(lerpColor);
                timer += Time.deltaTime;
                yield return null;
            }
            // Ensure the final color is set to red at the end of each cycle if still critical
            UpdateLivesDisplayWithCriticalColor(Color.red);
        }
        // When condition is no longer met, reset the text color to normal
        UpdateLivesDisplay();
    }

    void UpdateLivesDisplayWithCriticalColor(Color criticalColor)
    {
        string displayText = "";
        for (int i = 0; i < livesString.Length; i++)
        {
            Color currentColor = i < currentLifeIndex ? criticalColor : defaultColor;
            displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(currentColor)}>{livesString[i]}</color>";
        }
        livesText.text = displayText;
    }

    string GetDisplayText()
    {
        string displayText = "";
        for (int i = 0; i < livesString.Length; i++)
        {
            Color currentColor = i < currentLifeIndex ? lostLifeColor : defaultColor;
            displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(currentColor)}>{livesString[i]}</color>";
        }
        return displayText;
    }

    public bool IsGameOver()
    {
        return currentLifeIndex >= livesString.Length;
    }

    public int LivesRemaining()
    {
        return livesString.Length - currentLifeIndex;
    }

    public int GetStartLives()
    {
        return livesString.Length;
    }

    public bool HasFullLives()
    {
        return currentLifeIndex == 0;
    }

    public void ResetLives()
    {
        currentLifeIndex = 0;
        UpdateLivesDisplay();
    }
}