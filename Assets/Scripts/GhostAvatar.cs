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
    public Image ghostImage, glowGhostImage, speechBubble;
    public Sprite speechSprite, thoughtSprite;
    public GameObject flag, mercyButton;
    public Sprite normalGhost, angryGhost, thinkingGhost, sadGhost, surprisedGhost;
    public Sprite normalBlinkGhost, angryBlinkGhost, thinkingBlinkGhost, sadBlinkGhost, surprisedBlinkGhost;
    public Material glowMaterial;
    public int currentLevel;
    public bool CanMercy = true;

    public AudioSource ghostAudioSource;

    private float startYPosition;
    private bool isShowing = false;
    private float moveSpeed = 3f;
    private Vector3 originalTextScale;
    private float popScale = 1.25f;
    private float popDuration = 0.1f;
    private bool isThinking = false;
    private int playerAiWinDiff;
    private bool shouldShowFlag;
    private Color currentGlowColor = Color.white;
    private Coroutine shakingTouchedCoroutine;
    private Coroutine continuousShakeCoroutine;

    void Start()
    {
        startYPosition = transform.localPosition.y;
        originalTextScale = textMeshProUGUI.transform.localScale;
        StartCoroutine(Blink());
    }

    void Update()
    {
        if (isShowing)
        {
            float newY = startYPosition + Mathf.Sin(Time.time * moveSpeed) * 2f;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

            float rotationY = Mathf.Sin(Time.time * 0.5f) * 20;
            ghostImage.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
            glowGhostImage.transform.localRotation = Quaternion.Euler(0, rotationY, 0);

            UpdateGhostColor();
        }
    }

    public void Show(string text)
    {
        if (isThinking)
        {
            StopCoroutine("AnimateThinking"); // Stop the thinking animation if it's running
            isThinking = false;
            StartCoroutine(TransitionBubbleSprite(speechSprite));
        }

        UpdateState(playerAiWinDiff, currentLevel);
        textMeshProUGUI.text = text;
        StartCoroutine(PopTextEffect());
    }

    public void Hide()
    {
        StartCoroutine(FadeCanvasGroup(0, 0f)); // Fade out
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

        if (shouldShowFlag && !flag.activeSelf && CanMercy)
        {
            flag.SetActive(true);
            mercyButton.SetActive(true);
            StopShaking();
        }

        StartCoroutine("AnimateThinking");
        ghostImage.sprite = thinkingGhost;
        isThinking = true;

        StartCoroutine(TransitionBubbleSprite(thoughtSprite));
    }

    public void UpdateState(int playerAiWinDiff, int currentLevel)
    {
        this.playerAiWinDiff = playerAiWinDiff;
        this.currentLevel = currentLevel;

        if (flag.activeSelf)
        {
            ghostImage.sprite = sadGhost;
            StopShaking();
        }
        else if (playerAiWinDiff > 0)
        {
            ghostImage.sprite = angryGhost;
            StartShaking();
        }
        else
        {
            ghostImage.sprite = normalGhost;
            StopShaking();
        }
    }

    public void SetFlag(GameState gameState)
    {
        int livesDiff = gameManager.playerLivesText.LivesRemaining() - gameManager.aiLivesText.LivesRemaining();
        shouldShowFlag = livesDiff >= 3 && CanMercy && gameManager.criteriaText.GetCurrentCriteria().TrueForAll(c => c.IsMet(gameState));
    }

    public void Pop()
    {
        StartCoroutine(PopEffect());
        StartCoroutine(Shake());
    }

    private void StartShaking()
    {
        if (continuousShakeCoroutine != null)
        {
            StopCoroutine(continuousShakeCoroutine);
        }
        continuousShakeCoroutine = StartCoroutine(ContinuousShake());
    }

    private void StopShaking()
    {
        if (continuousShakeCoroutine != null)
        {
            StopCoroutine(continuousShakeCoroutine);
            continuousShakeCoroutine = null;
            transform.localPosition = new Vector3(transform.localPosition.x, startYPosition, transform.localPosition.z); // Reset to original position
        }
    }


    private IEnumerator TransitionBubbleSprite(Sprite targetSprite)
    {
        // Smoothly fade out the current sprite
        float duration = 0.05f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            speechBubble.color = new Color(speechBubble.color.r, speechBubble.color.g, speechBubble.color.b, Mathf.Lerp(1, 0, elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Change the sprite once faded out
        speechBubble.color = new Color(speechBubble.color.r, speechBubble.color.g, speechBubble.color.b, 0);
        speechBubble.sprite = targetSprite;

        // Smoothly fade the new sprite in
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            speechBubble.color = new Color(speechBubble.color.r, speechBubble.color.g, speechBubble.color.b, Mathf.Lerp(0, 1, elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        speechBubble.color = new Color(speechBubble.color.r, speechBubble.color.g, speechBubble.color.b, 1);
    }

    IEnumerator PopEffect()
    {
        var originalScale = transform.localScale;
        var duration = 0.2f;
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
        if (!ghostAudioSource.isPlaying)
        {
            ghostAudioSource.Play();
        }

        // Change to surprised sprite
        ghostImage.sprite = surprisedGhost;

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

        // Wait while showing the surprised image
        yield return new WaitForSeconds(0.1f);

        UpdateState(playerAiWinDiff, currentLevel);
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
        float levelIntensity = Mathf.Min(1f, currentLevel / 9f); // Scale factor based on current level
        currentGlowColor = Color.Lerp(Color.white, transitionColor, 1f); // Intense glow

        // Set the material properties
        glowMaterial.SetColor("_GlowColor", currentGlowColor);
        glowMaterial.SetFloat("_Opacity", levelIntensity); // Adjust opacity based on level
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
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            float blinkInterval = Random.Range(3f, 5f); // Time between blinks
            yield return new WaitForSeconds(blinkInterval);

            float blinkDuration = 0.1f; // Duration of blink
            Sprite currentBlinkSprite = null;

            // Determine the appropriate blink sprite based on the current sprite
            if (ghostImage.sprite == normalGhost) currentBlinkSprite = normalBlinkGhost;
            else if (ghostImage.sprite == angryGhost) currentBlinkSprite = angryBlinkGhost;
            else if (ghostImage.sprite == thinkingGhost) currentBlinkSprite = thinkingBlinkGhost;
            else if (ghostImage.sprite == sadGhost) currentBlinkSprite = sadBlinkGhost;
            else if (ghostImage.sprite == surprisedGhost) currentBlinkSprite = surprisedBlinkGhost;

            if (currentBlinkSprite != null)
            {
                ghostImage.sprite = currentBlinkSprite;
            }
            yield return new WaitForSeconds(blinkDuration);
            // Restore to the original sprite after blinking
            if (ghostImage.sprite == normalBlinkGhost) ghostImage.sprite = normalGhost;
            else if (ghostImage.sprite == angryBlinkGhost) ghostImage.sprite = angryGhost;
            else if (ghostImage.sprite == thinkingBlinkGhost) ghostImage.sprite = thinkingGhost;
            else if (ghostImage.sprite == sadBlinkGhost) ghostImage.sprite = sadGhost;
            else if (ghostImage.sprite == surprisedBlinkGhost) ghostImage.sprite = surprisedGhost;
        }
    }

    private IEnumerator ContinuousShake()
    {
        float maxShakeMagnitude = 4f; // Maximum shake magnitude
        float baseShakeDuration = 0.1f; // Duration of each shake

        while (playerAiWinDiff > 0)
        {
            // Calculate the shake magnitude based on playerAiWinDiff (higher difference = more shake)
            float shakeMagnitude = Mathf.Clamp(playerAiWinDiff, 0, maxShakeMagnitude) * 10;
            float elapsed = 0.0f;
            Vector3 originalPos = transform.localPosition;

            // Shake for the duration based on aiPlayerWinDiff
            while (elapsed < baseShakeDuration)
            {
                float x = originalPos.x + Random.Range(-1f, 1f) * shakeMagnitude * Time.deltaTime;
                float y = originalPos.y + Random.Range(-1f, 1f) * shakeMagnitude * Time.deltaTime;
                transform.localPosition = new Vector3(x, y, originalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos; // Reset to original position after shake
            yield return new WaitForSeconds(baseShakeDuration); // Pause before the next shake
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