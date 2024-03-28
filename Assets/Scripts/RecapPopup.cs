using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class RecapPopup : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI recapText;
    public RectTransform contentRect;
    public ScrollRect scrollRect;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopup();
    }

    public void Show(List<RecapObject> recap)
    {
        clickAudioSource?.Play();

        string recapString = ConvertHistoryListToString(recap);
        recapText.text = recapString;

        // Adjust content height here
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        float preferredHeight = recapText.preferredHeight + 54; // 54 is the top offset
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, preferredHeight);

        StartCoroutine(ScrollToTop());

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());
    }

    private string ConvertHistoryListToString(List<RecapObject> recap)
    {
        string result = "";

        var lastPoints = 0;
        foreach (var item in recap)
        {
            result += "<size=20>YOU                              CASP</size>\n";
            string pointsText = item.Points == 1 ? "1 Point" : $"{item.Points} Points";
            result += $"<size=30>{item.PlayerGhostString}            {item.AIGhostString}</size>\n<size=35>{pointsText}</size>\n";

            var pointDiff = item.Points - lastPoints;
            if (pointDiff == 0)
            {
                result += "\n";
            }
            else
            {
                var pointsDiffText = "";
                if (pointDiff > 0)
                {
                    pointsDiffText = $"<color=green>+{pointDiff}</color>";
                }
                else
                {
                    pointsDiffText = $"<color=red>{pointDiff}</color>";
                }
                result += $"<size=25>{pointsDiffText}</size>\n\n";
            }
            lastPoints = item.Points;

            result += $"{item.History}\n\n";
        }

        return result;
    }

    private IEnumerator ScrollToTop()
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1.0f;
    }

    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, currentTime / fadeDuration);
            yield return null;
        }
    }

    private IEnumerator ScaleIn()
    {
        popUpGameObject.transform.localScale = Vector3.zero; // Ensure it starts from zero
        float currentTime = 0;
        while (currentTime < scaleDuration)
        {
            currentTime += Time.deltaTime;
            popUpGameObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, currentTime / scaleDuration);
            yield return null;
        }
    }

    public void Hide()
    {
        clickAudioSource?.Play();

        StopAllCoroutines(); // Stop animations in case they're still running
        ResetPopup();
    }

    private void ResetPopup()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}

public class RecapObject
{
    public string GameWord;
    public string PlayerGhostString;
    public string AIGhostString;
    public int Points;
    public string History;
    public int PlayerLivesRemaining;
    public bool IsValidWord;
}