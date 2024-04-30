using UnityEngine;
using TMPro;
using System.Collections;

public class PointsText : MonoBehaviour
{
    public TextMeshProUGUI pointsText;
    public int points = 0;
    public Color normalColor = Color.white;
    public bool makePostFixGreen;
    public bool IsJustNumber, IsCurrency;
    public bool IncludePop = true;

    private float duration = 0.5f;
    private float colorDuration = 0.35f;
    private Color positiveColor = Color.green;
    private Color negativeColor = Color.red;
    private bool showSymbol;
    private string prefixText;

    private float popDuration = 0.1f;
    private float popScale = 1.2f;
    private Vector3 originalTextScale;

    private void Start()
    {
        originalTextScale = Vector3.one;
        UpdatePointsText(0);
    }

    public void AddPoints(int amount, bool showSymbol = false, string prefixText = "", float delay = 0, Color? overrideColor = null)
    {
        this.showSymbol = showSymbol;
        this.prefixText = prefixText;
        pointsText.color = normalColor;
        StopAllCoroutines(); // Stop any ongoing coroutines
        StartCoroutine(CountPoints(amount, delay, overrideColor));
    }

    public void SetPoints(int amount)
    {
        pointsText.color = normalColor;
        points = amount;
        UpdatePointsText(points);
    }

    public void Reset()
    {
        StopAllCoroutines();
        points = 0;
        pointsText.color = normalColor;
        UpdatePointsText(points);
    }

    IEnumerator CountPoints(int amount, float delay, Color? overrideColor)
    {
        yield return new WaitForSeconds(delay);

        int startPoints = points;
        int endPoints = showSymbol ? points + amount : Mathf.Max(0, points + amount);
        float timer = 0;

        // Determine the target color based on the amount being positive or negative.
        Color targetColor = overrideColor != null ? overrideColor.Value : amount > 0 ? positiveColor : negativeColor;

        // Start counting.
        while (points != endPoints)
        {
            timer += Time.deltaTime;
            float percentageComplete = timer / (amount < 5 ? duration / 2 : duration);

            points = (int)Mathf.Lerp(startPoints, endPoints, percentageComplete);
            UpdatePointsText(points);

            if (percentageComplete < 1.0f)
            {
                // Lerp color immediately with counting.
                pointsText.color = Color.Lerp(normalColor, targetColor, percentageComplete);
            }
            else
            {
                // Once counting is done, hold the color for a bit longer.
                pointsText.color = targetColor;
            }

            yield return null;
        }

        // Wait a little longer after counting is done before changing the color back.
        if (IncludePop)
        {
            StartCoroutine(PopTextEffect());
        }
        yield return new WaitForSeconds(colorDuration);

        if (startPoints != endPoints)
        {
            var lerpBackTime = 0.1f;
            timer = 0;
            while (timer < lerpBackTime)
            {
                timer += Time.deltaTime;
                float percentageComplete = timer / lerpBackTime;
                pointsText.color = Color.Lerp(targetColor, normalColor, percentageComplete);
                yield return null;
            }
        }

        // Finally, reset the points and color.
        points = endPoints;
        UpdatePointsText(points);
        pointsText.color = normalColor;
    }

    void UpdatePointsText(int currentPoints)
    {
        string pointsDisplay;
        var symbol = showSymbol && currentPoints > 0 ? "+" : "";
        if (IsJustNumber)
        {
            pointsDisplay = $"{symbol}{currentPoints}";
        }
        else if (IsCurrency)
        {
            pointsDisplay = $"{symbol}${currentPoints}";
        }
        else
        {
            if (currentPoints == 1)
            {
                pointsDisplay = $"{symbol}1 POINT";
            }
            else
            {
                pointsDisplay = $"{symbol}{currentPoints} POINTS";
            }
        }

        if (makePostFixGreen)
        {
            pointsText.text = $"{prefixText}<color=green>{pointsDisplay}</color>";
        }
        else
        {
            pointsText.text = $"{prefixText}{pointsDisplay}";
        }
    }

    private IEnumerator PopTextEffect()
    {
        float elapsedTime = 0f;
        while (elapsedTime < popDuration)
        {
            float proportionCompleted = elapsedTime / popDuration;
            float easeOutProgress = 1 - Mathf.Pow(1 - proportionCompleted, 2);
            pointsText.transform.localScale = Vector3.Lerp(originalTextScale, originalTextScale * popScale, easeOutProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        var poppedScale = pointsText.transform.localScale;
        while (elapsedTime < popDuration)
        {
            pointsText.transform.localScale = Vector3.Lerp(poppedScale, originalTextScale, elapsedTime / popDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pointsText.transform.localScale = originalTextScale; // Ensure it sets back to original exactly
    }
}