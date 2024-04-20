using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SlideUpAnimation : MonoBehaviour
{
    public float duration = 0.2f;
    private RectTransform rectTransform;
    private TextMeshProUGUI textComponent;
    private float originalScaleY;
    private float currentTime;
    private string lastText = "";

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponent<TextMeshProUGUI>();
        originalScaleY = rectTransform.localScale.y;
    }

    void Update()
    {
        // Check if text has changed since last frame
        if (lastText != textComponent.text)
        {
            InitializeAnimation();
            lastText = textComponent.text;
        }

        if (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float scaleY = Mathf.Lerp(0f, originalScaleY, currentTime / duration);
            rectTransform.localScale = new Vector3(rectTransform.localScale.x, scaleY, rectTransform.localScale.z);
        }
    }

    private void InitializeAnimation()
    {
        rectTransform.localScale = new Vector3(rectTransform.localScale.x, 0f, rectTransform.localScale.z);
        currentTime = 0;
    }
}