using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class RunInfoPopUp : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void _Native_Share_iOS(string message);

    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI statsText;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private SaveObject saveObject;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        // Load settings
        saveObject = SaveManager.Load();

        // Set up stats
        ConfigureStats();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());
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

        StopAllCoroutines();
        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ConfigureStats()
    {
        
    }

    private void ShareMessage(List<RecapObject> recap)
    {
        var sharedMessage = GetSharedMessage(recap);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _Native_Share_iOS(sharedMessage);
        }
        else
        {
            GUIUtility.systemCopyBuffer = sharedMessage;
            Debug.LogWarning("Native sharing is only available on iOS. Current platform: " + Application.platform); ;
        }
    }

    private string GetSharedMessage(List<RecapObject> recap)
    {
        //todo: change this to give recap of entire run

        string pointsText = recap.Last().PlayerLivesRemaining == 0 ? "" : recap.Last().Points == 1 ? "1 pt - " : $"{recap.Last().Points} pts - ";
        string message = $"Wordy Ghost - {pointsText}{recap.Count} rounds";
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
}
