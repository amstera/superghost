using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SlideUpAnimation : MonoBehaviour
{
    public float duration = 0.2f;
    private RectTransform rectTransform;
    private TextMeshProUGUI textComponent;
    private float originalScaleY;
    private string lastParsedText = "";
    private Coroutine animationCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponent<TextMeshProUGUI>();
        originalScaleY = rectTransform.localScale.y;
    }

    void OnEnable()
    {
        // Start monitoring for text changes when the object is enabled
        StartCoroutine(MonitorTextChanges());
    }

    void OnDisable()
    {
        // Ensure the final scale is reset when the object is disabled
        EnsureFinalScale();
        StopAllCoroutines(); // Stop all coroutines to prevent any unfinished animations
    }

    private IEnumerator MonitorTextChanges()
    {
        while (true)
        {
            // Get the text without rich tags (colors, etc.)
            string currentParsedText = textComponent.GetParsedText();

            // Check if the parsed text (without formatting) has changed
            if (lastParsedText != currentParsedText)
            {
                lastParsedText = currentParsedText;
                StartSlideUpAnimation();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void StartSlideUpAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(SlideUpCoroutine());
    }

    private IEnumerator SlideUpCoroutine()
    {
        float currentTime = 0f;
        rectTransform.localScale = new Vector3(rectTransform.localScale.x, 0f, rectTransform.localScale.z);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float scaleY = Mathf.Lerp(0f, originalScaleY, currentTime / duration);
            rectTransform.localScale = new Vector3(rectTransform.localScale.x, scaleY, rectTransform.localScale.z);
            yield return null;
        }

        // Ensure the final scale is set to the original scale
        rectTransform.localScale = new Vector3(rectTransform.localScale.x, originalScaleY, rectTransform.localScale.z);
        animationCoroutine = null;  // Reset coroutine reference
    }

    // Fallback method to reset the scale if something unexpected happens
    public void EnsureFinalScale()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        // Force reset to the original scale as a fallback
        rectTransform.localScale = new Vector3(rectTransform.localScale.x, originalScaleY, rectTransform.localScale.z);
    }
}