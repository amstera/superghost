using UnityEngine;
using TMPro;
using System.Collections;

public class PointsText : MonoBehaviour
{
    public TextMeshProUGUI pointsText;
    public int points = 0;
    public Color normalColor = Color.white;
    public bool IsCurrency;
    private float duration = 0.5f;
    private float colorDuration = 0.5f;
    private Color positiveColor = Color.green;
    private Color negativeColor = Color.red;
    private bool showSymbol;

    private void Start()
    {
        UpdatePointsText(0);
    }

    public void AddPoints(int amount, bool showSymbol = false)
    {
        this.showSymbol = showSymbol;
        pointsText.color = normalColor;
        StopAllCoroutines(); // Stop any ongoing coroutines.
        StartCoroutine(CountPoints(amount));
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

    IEnumerator CountPoints(int amount)
    {
        int startPoints = points;
        int endPoints = showSymbol ? points + amount : Mathf.Max(0, points + amount);
        float timer = 0;

        // Determine the target color based on the amount being positive or negative.
        Color targetColor = amount > 0 ? positiveColor : negativeColor;

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
        yield return new WaitForSeconds(colorDuration);

        // Finally, reset the points and color.
        points = endPoints;
        UpdatePointsText(points);
        pointsText.color = normalColor;
    }

    void UpdatePointsText(int currentPoints)
    {
        var symbol = showSymbol && currentPoints > 0 ? "+" : "";
        if (IsCurrency)
        {
            pointsText.text = $"{symbol}${currentPoints}";
        }
        else
        {
            if (currentPoints == 1)
            {
                pointsText.text = $"{symbol}1 POINT";
            }
            else
            {
                pointsText.text = $"{symbol}{currentPoints} POINTS";
            }
        }
    }
}