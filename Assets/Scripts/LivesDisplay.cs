using System.Collections;
using TMPro;
using UnityEngine;

public class LivesDisplay : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public string livesString = "GHOST";
    public Color defaultColor = new Color(0.2392157f, 0.2392157f, 0.2392157f); // #3D3D3D
    public Color lostLifeColor = Color.white;
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

    public void LoseLife()
    {
        if (!IsGameOver())
        {
            currentLifeIndex++;
            UpdateLivesDisplay();
            StartCoroutine(PopAnimation());
        }
    }

    IEnumerator PopAnimation()
    {
        float animationTime = 0.3f;
        Vector3 originalScale = livesText.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

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
        string displayText = "";
        for (int i = 0; i < livesString.Length; i++)
        {
            if (i < currentLifeIndex)
            {
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(lostLifeColor)}>{livesString[i]}</color>";
            }
            else
            {
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{livesString[i]}</color>";
            }
        }
        livesText.text = displayText;
    }

    public bool IsGameOver()
    {
        return currentLifeIndex >= livesString.Length;
    }
}