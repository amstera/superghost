using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class HintPopUp : MonoBehaviour
{
    public GameManager gameManager;
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI pointsText, warningText;

    public Button hintButton;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.25f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;

    private Vector3 originalScale;
    private Vector3 originalPos;
    private int cost;
    private int points;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;
        ResetPopUp();
    }

    public void Show(int points, string substring)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        cost = Mathf.Max(substring.Length, 1);
        this.points = points;

        string pointsAmount = cost == 1 ? "1 point" : $"{cost} points";
        pointsText.text = $"<color=black><size=35>Cost</size></color>\n{pointsAmount}";

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

    public void GetHint()
    {
        if (cost <= points)
        {
            gameManager.ShowHint(-cost);
            Hide();
        }
        else
        {
            StartCoroutine(ShakePopup());
            warningText.gameObject.SetActive(true);
        }
    }

    private IEnumerator ShakePopup()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * shakeMagnitude;

            popUpGameObject.transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null; // Wait until next frame
        }

        popUpGameObject.transform.localPosition = originalPos;
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        warningText.gameObject.SetActive(false);
    }
}