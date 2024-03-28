using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Linq;

public class RecapPopup : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void _Native_Share_iOS(string message);

    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI recapText;
    public RectTransform contentRect;
    public ScrollRect scrollRect;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private Vector3 originalScale;
    private string sharedMessage;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopup();
    }

    public void Show(List<RecapObject> recap)
    {
        clickAudioSource?.Play();

        string recapString = ConvertHistoryListToString(recap);
        sharedMessage = GetSharedMessage(recap);
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

    public void ShareMessage()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _Native_Share_iOS(sharedMessage);
        }
        else
        {
            Debug.LogWarning("Native sharing is only available on iOS. Current platform: " + Application.platform);
        }
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

    private string GetSharedMessage(List<RecapObject> recap)
    {
        string message = $"Wordy Ghost - {recap.Last().Points}pts - {recap.Count} rounds";
        foreach (var item in recap)
        {
            message += "\n";
            if (item.PlayerLivesRemaining < 5)
            {
                for (int i = 0; i < 5 - item.PlayerLivesRemaining; i++)
                {
                    message += "🟥";
                }
            }
            if (item.PlayerLivesRemaining > 0)
            {
                for (int i = 0; i < item.PlayerLivesRemaining; i++)
                {
                    message += "🟩";
                }
            }

            message += $" {item.GameWord.ToUpper()}";
            if (!item.IsValidWord)
            {
                message += " ❌";
            }
            message += $" ({item.Points})";
        }

        return message;
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