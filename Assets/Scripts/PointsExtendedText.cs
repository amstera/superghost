using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PointsExtendedText : MonoBehaviour
{
    public TextMeshProUGUI pointsText;
    public GameObject fireball;
    public AudioSource incrementAudioSource;

    public float displaySpeed = 0.3f; // Time between number updates
    private float popDuration = 0.12f; // Duration of the pop effect
    private float popScale = 1.2f; // Scale factor for the pop effect

    private Vector3 originalTextScale;

    private void Start()
    {
        pointsText.text = "";
        originalTextScale = Vector3.one;
        pointsText.transform.localScale = originalTextScale;
    }

    public void AddPoints(List<float> numbers, bool showFireball)
    {
        if (numbers == null || numbers.Count == 0)
            return;

        fireball.SetActive(false);
        StartCoroutine(DisplayPointsRoutine(numbers, showFireball));
    }

    private IEnumerator DisplayPointsRoutine(List<float> numbers, bool showFireball)
    {
        yield return new WaitForEndOfFrame();

        float total = numbers[0];
        if (numbers.Count > 1)
        {
            for (int i = 1; i < numbers.Count; i++)
            {
                pointsText.text = total.ToString("F0") + " x " + numbers[i].ToString();
                total *= numbers[i];
                UpdateTextColor(total);
                incrementAudioSource?.Play();
                yield return StartCoroutine(PopTextEffect());
                yield return new WaitForSeconds(displaySpeed);
            }
        }

        fireball.SetActive(showFireball);

        int finalPoints = (int)Math.Round(total, MidpointRounding.AwayFromZero);
        string symbol = finalPoints < 0 ? "" : "+";
        pointsText.text = $"{symbol}{finalPoints} PTS";
        UpdateTextColor(finalPoints);
        incrementAudioSource?.Play();
        yield return StartCoroutine(PopTextEffect());
    }

    private void UpdateTextColor(float score)
    {
        pointsText.color = score >= 0 ? Color.green : Color.red;
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
