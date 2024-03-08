using UnityEngine;
using TMPro;
using System.Collections;

public class PointsText : MonoBehaviour
{
    public TextMeshProUGUI pointsText;
    public int points = 0;
    private float duration = 0.5f;
    private float colorDuration = 0.5f;
    private Color normalColor = Color.white;
    private Color positiveColor = Color.green;
    private Color negativeColor = Color.red;

    private void Start()
    {
        normalColor = pointsText.color;
        UpdatePointsText(0);
    }

    public void AddPoints(int amount)
    {
        StopAllCoroutines(); // Stop any ongoing coroutines.
        StartCoroutine(CountPoints(amount));
    }

    public void Reset()
    {
        StopAllCoroutines();
        points = 0;
        UpdatePointsText(points);
    }

    IEnumerator CountPoints(int amount)
    {
        int startPoints = points;
        int endPoints = Mathf.Max(0, points + amount); // Ensure points don't go below 0.
        float timer = 0;

        // Determine the target color based on the amount being positive or negative.
        Color targetColor = amount > 0 ? positiveColor : negativeColor;

        // Start counting.
        while (points != endPoints)
        {
            timer += Time.deltaTime;
            float percentageComplete = timer / duration;

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
        if (currentPoints == 1)
        {
            pointsText.text = "1 POINT";
        }
        else
        {
            pointsText.text = $"{currentPoints} POINTS";
        }
    }
}