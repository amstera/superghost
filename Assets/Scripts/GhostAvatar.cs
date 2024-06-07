using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GhostAvatar : MonoBehaviour, IPointerClickHandler
{
    public GameManager gameManager;
    public TextMeshProUGUI textMeshProUGUI;
    public CanvasGroup canvasGroup;
    public Image ghostImage;
    public GameObject flag, mercyButton;
    public Sprite normalGhost, angryGhost, thinkingGhost, sadGhost;
    public int currentLevel;

    private float startYPosition;
    private bool isShowing = false;
    private float moveSpeed = 3f;
    private Vector3 originalTextScale;
    private float popScale = 1.25f;
    private float popDuration = 0.1f;
    private bool isThinking = false;
    private bool isLosing;
    private bool shouldShowFlag;
    private Coroutine shakingTouchedCoroutine;

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

            float rotationY = Mathf.Sin(Time.time * 0.5f) * 25;
            ghostImage.transform.localRotation = Quaternion.Euler(0, rotationY, 0);

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
        if (flag.activeSelf)
        {
            ghostImage.sprite = sadGhost;
        }
        textMeshProUGUI.text = text;
        StartCoroutine(PopTextEffect());
    }

    public void Hide()
    {
        StartCoroutine(FadeCanvasGroup(0, 0.1f)); // Fade out
        isShowing = false;
        mercyButton.SetActive(false);
        flag.SetActive(false);
    }

    public void Think()
    {
        if (canvasGroup.alpha < 1 || !isShowing)
        {
            StartCoroutine(FadeCanvasGroup(1)); // Fade in
            isShowing = true;
        }

        if (shouldShowFlag && !flag.activeSelf)
        {
            flag.SetActive(true);
            mercyButton.SetActive(true);
        }

        if (!isThinking)
        {
            StartCoroutine("AnimateThinking");
            ghostImage.sprite = thinkingGhost;
            isThinking = true;
        }
    }

    public void SetFlag(GameState gameState)
    {
        int livesDiff = gameManager.playerLivesText.LivesRemaining() - gameManager.aiLivesText.LivesRemaining();
        shouldShowFlag = livesDiff >= 3 && gameManager.criteriaText.GetCurrentCriteria().TrueForAll(c => c.IsMet(gameState));
    }

    public void Pop()
    {
        StartCoroutine(PopEffect());
        StartCoroutine(Shake());
    }

    IEnumerator PopEffect()
    {
        var originalScale = transform.localScale;
        var duration = 0.15f;
        var popEffectScale = 1.2f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Apply ease-out effect (quadratic)
            float proportionCompleted = elapsedTime / duration;
            float easeOutProgress = 1 - Mathf.Pow(1 - proportionCompleted, 2);

            transform.localScale = Vector3.Lerp(originalTextScale, originalScale * popEffectScale, easeOutProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Smooth interpolation back to the original scale
        elapsedTime = 0f;
        var poppedScale = transform.localScale;
        while (elapsedTime < duration)
        {
            // Smoothly lerp back using linear interpolation (could use easing here as well)
            transform.localScale = Vector3.Lerp(poppedScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it sets back to original exactly
        transform.localScale = originalScale;
    }

    private IEnumerator Shake()
    {
        float elapsed = 0.0f;
        var originalPos = transform.localPosition;
        var shakeDuration = 0.25f;
        var shakeMagnitude = 3f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null; // Wait until next frame
        }

        transform.localPosition = originalPos;
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
        if (canvasGroup.alpha == 1)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    IEnumerator PopTextEffect()
    {
        float elapsedTime = 0f;
        while (elapsedTime < popDuration)
        {
            // Apply ease-out effect (quadratic)
            float proportionCompleted = elapsedTime / popDuration;
            float easeOutProgress = 1 - Mathf.Pow(1 - proportionCompleted, 2);

            textMeshProUGUI.transform.localScale = Vector3.Lerp(originalTextScale, originalTextScale * popScale, easeOutProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Smooth interpolation back to the original scale
        elapsedTime = 0f;
        var poppedScale = textMeshProUGUI.transform.localScale;
        while (elapsedTime < popDuration)
        {
            // Smoothly lerp back using linear interpolation (could use easing here as well)
            textMeshProUGUI.transform.localScale = Vector3.Lerp(poppedScale, originalTextScale, elapsedTime / popDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it sets back to original exactly
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shakingTouchedCoroutine != null)
        {
            StopCoroutine(shakingTouchedCoroutine);
        }

        shakingTouchedCoroutine = StartCoroutine(Shake());
    }
}