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
    public GameManager gameManager;

    public float displaySpeed = 0.3f; // Time between number updates

    private float popDuration = 0.12f;
    private float popScale = 1.2f;
    private Vector3 originalTextScale;

    private void Start()
    {
        pointsText.text = "";
        originalTextScale = Vector3.one;
        pointsText.transform.localScale = originalTextScale;
    }

    public void AddPoints(List<float> numbers)
    {
        if (numbers == null || numbers.Count == 0)
        {
            return;
        }

        fireball.SetActive(false);
        incrementAudioSource.pitch = 1;
        incrementAudioSource.volume = 0.4f;
        int pointsForFire = 40;
        StartCoroutine(DisplayPointsRoutine(numbers, pointsForFire));
    }

    private IEnumerator DisplayPointsRoutine(List<float> numbers, int pointsForFire)
    {
        yield return new WaitForEndOfFrame();

        float total = numbers[0];
        if (numbers.Count > 1)
        {
            for (int i = 1; i < numbers.Count; i++)
            {
                pointsText.text = total.ToString("F0") + " x " + numbers[i].ToString();
                ShowFireball(total, pointsForFire);
                total *= numbers[i];
                UpdateTextColor(total);
                incrementAudioSource?.Play();
                incrementAudioSource.pitch *= 1.5f;
                incrementAudioSource.volume *= 1.15f;

                yield return StartCoroutine(PopTextEffect());
                yield return new WaitForSeconds(displaySpeed);
            }
        }

        ShowFireball(total, pointsForFire);

        int finalPoints = (int)Math.Round(total, MidpointRounding.AwayFromZero);
        string symbol = finalPoints < 0 ? "" : "+";
        pointsText.text = Math.Abs(finalPoints) == 1 ? $"{symbol}1 PT" : $"{symbol}{finalPoints} PTS";
        UpdateTextColor(finalPoints);
        if (numbers.Count > 1)
        {
            incrementAudioSource?.Play();
        }
        yield return StartCoroutine(PopTextEffect());
    }

    private void ShowFireball(float total, int pointsForFire)
    {
        if (total >= pointsForFire)
        {
            fireball.SetActive(true);
            gameManager.criteria.gameObject.SetActive(false);
        }
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
