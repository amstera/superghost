using UnityEngine;
using TMPro;
using System.Collections;

public class GhostAvatar : MonoBehaviour
{
    public TextMeshProUGUI textMeshProUGUI;
    public CanvasGroup canvasGroup;

    private float startYPosition;
    private bool isShowing = false;
    private float moveSpeed = 2.5f;

    void Start()
    {
        startYPosition = transform.localPosition.y;
    }

    void Update()
    {
        if (isShowing)
        {
            // Calculate the new Y position with sinusoidal bobbing
            float newY = startYPosition + Mathf.Sin(Time.time * moveSpeed) * 2;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
    }

    public void Show(string text)
    {
        textMeshProUGUI.text = text;
        StartCoroutine(FadeCanvasGroup(1)); // Fade in
        isShowing = true;
    }

    public void Hide()
    {
        StartCoroutine(FadeCanvasGroup(0, 0.1f)); // Fade out
        isShowing = false;
    }

    public void Think()
    {
        textMeshProUGUI.text = "...";
    }

    IEnumerator FadeCanvasGroup(float targetAlpha, float duration = 0.25f)
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
}