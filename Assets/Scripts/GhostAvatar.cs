using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GhostAvatar : MonoBehaviour
{
    public TextMeshProUGUI textMeshProUGUI;
    public CanvasGroup canvasGroup;
    public Image ghostImage;
    public Sprite normalGhost, angryGhost, thinkingGhost;

    private float startYPosition;
    private bool isShowing = false;
    private float moveSpeed = 3f;
    private Vector3 originalTextScale;
    private float popScale = 1.15f;
    private float popDuration = 0.2f;
    private bool isThinking = false;
    private bool isLosing;
    public int currentLevel;

    void Start()
    {
        startYPosition = transform.localPosition.y;
        originalTextScale = textMeshProUGUI.transform.localScale;
    }

    void Update()
    {
        if (isShowing)
        {
            float newY = startYPosition + Mathf.Sin(Time.time * moveSpeed) * 2.5f;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

            UpdateGhostColor();
        }
    }

    public void Show(string text)
    {
        if (isThinking)
        {
            StopCoroutine("AnimateThinking"); // Stop the thinking animation if it's running
            isThinking = false;
        }

        UpdateState(isLosing, currentLevel);
        textMeshProUGUI.text = text;
        StartCoroutine(PopTextEffect());
    }

    public void Hide()
    {
        StartCoroutine(FadeCanvasGroup(0, 0.1f)); // Fade out
        isShowing = false;
    }

    public void Think()
    {
        if (canvasGroup.alpha < 1 || !isShowing)
        {
            StartCoroutine(FadeCanvasGroup(1)); // Fade in
            isShowing = true;
        }

        if (!isThinking)
        {
            StartCoroutine("AnimateThinking");
            ghostImage.sprite = thinkingGhost;
            isThinking = true;
        }
    }

    public void UpdateState(bool isLosing, int currentLevel)
    {
        this.isLosing = isLosing;
        this.currentLevel = currentLevel;

        if (isLosing)
        {
            ghostImage.sprite = angryGhost;
        }
        else 
        {
            ghostImage.sprite = normalGhost;
        }
    }

    private void UpdateGhostColor()
    {
        // Define base colors
        Color yellow = new Color(1, 1, 0);
        Color orange = new Color(1, 0.65f, 0);
        Color red = new Color(0.9f, 0, 0);

        // Calculate the phase for color lerp transitions
        float phase = (Mathf.Sin(Time.time * 2) + 1) / 2; // Normalize to 0-1

        // Determine transition colors based on phase
        Color transitionColor = phase < 0.5 ? Color.Lerp(yellow, orange, phase * 2) : Color.Lerp(orange, red, (phase - 0.5f) * 2);

        // Apply level-based blending with white
        float levelIntensity = currentLevel / 9f; // Scale factor based on current level
        ghostImage.color = Color.Lerp(Color.white, transitionColor, levelIntensity);
    }

    IEnumerator FadeCanvasGroup(float targetAlpha, float duration = 0.5f)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    IEnumerator PopTextEffect()
    {
        float elapsedTime = 0f;
        while (elapsedTime < popDuration)
        {
            textMeshProUGUI.transform.localScale = Vector3.Lerp(originalTextScale, originalTextScale * popScale, elapsedTime / popDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Scale back to original after the pop effect
        textMeshProUGUI.transform.localScale = originalTextScale;
    }

    IEnumerator AnimateThinking()
    {
        string[] thinkingStates = { ".", "..", "..." };
        int index = 0;

        while (true) // Loop indefinitely until coroutine is stopped
        {
            textMeshProUGUI.text = thinkingStates[index % thinkingStates.Length];
            index++;
            yield return new WaitForSeconds(0.25f);
        }
    }
}