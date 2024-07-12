using UnityEngine;
using TMPro;

public class TextLerpColor : MonoBehaviour
{
    public GameManager gameManager;
    public Color lerpColor = Color.red;
    public float lerpDuration = 1f;
    public bool isPlayer;

    private TextMeshProUGUI textMeshPro;
    private Color originalColor;
    private Color targetColor;
    private float lerpTime;
    private bool isLerpingToTarget = true;

    void Start()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        originalColor = textMeshPro.color;
        targetColor = lerpColor;
        lerpTime = 0f;
    }

    void Update()
    {
        if (gameManager.IsGameEnded() || (isPlayer ? gameManager.playerLivesText.LivesRemaining() != 1 : gameManager.aiLivesText.LivesRemaining() != 1))
        {
            textMeshPro.color = originalColor;
            return;
        }

        // Lerp between the original color and the target color
        lerpTime += Time.deltaTime / lerpDuration;
        if (isLerpingToTarget)
        {
            textMeshPro.color = Color.Lerp(originalColor, targetColor, lerpTime);
            if (lerpTime >= 1f)
            {
                lerpTime = 0f;
                isLerpingToTarget = false;
            }
        }
        else
        {
            textMeshPro.color = Color.Lerp(targetColor, originalColor, lerpTime);
            if (lerpTime >= 1f)
            {
                lerpTime = 0f;
                isLerpingToTarget = true;
            }
        }
    }

    public void SetColor(Color newColor)
    {
        originalColor = newColor;
        textMeshPro.color = originalColor;
        lerpTime = 0f;
        isLerpingToTarget = true;
    }
}