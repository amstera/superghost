using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GhostAvatar : MonoBehaviour
{
    public TextMeshProUGUI textMeshProUGUI;
    public CanvasGroup canvasGroup;
    public Image ghostImage;
    public Sprite normalGhost, angryGhost;

    private float startYPosition;
    private bool isShowing = false;
    private float moveSpeed = 3.5f;
    private Vector3 originalScale;
    private float popScale = 1.15f;
    private float popDuration = 0.2f;
    private bool isThinking = false; // Added to indicate if the thinking animation is running

    void Start()
    {
        startYPosition = transform.localPosition.y;
        originalScale = textMeshProUGUI.transform.localScale; // Store the original scale at start
    }

    void Update()
    {
        if (isShowing)
        {
            float newY = startYPosition + Mathf.Sin(Time.time * moveSpeed) * 2;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
    }

    public void Show(string text)
    {
        if (isThinking)
        {
            StopCoroutine("AnimateThinking"); // Stop the thinking animation if it's running
            isThinking = false;
        }

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
            isThinking = true;
        }
    }

    public void UpdateState(bool isLosing)
    {
        if (isLosing)
        {
            ghostImage.sprite = angryGhost;
        }
        else 
        {
            ghostImage.sprite = normalGhost;
        }
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
            textMeshProUGUI.transform.localScale = Vector3.Lerp(originalScale, originalScale * popScale, elapsedTime / popDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Scale back to original after the pop effect
        textMeshProUGUI.transform.localScale = originalScale;
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