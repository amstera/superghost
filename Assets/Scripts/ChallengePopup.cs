using UnityEngine;
using TMPro;
using System.Collections;

public class ChallengePopUp : MonoBehaviour
{
    public GameManager gameManager;

    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI challengeText;
    public TMP_InputField inputField;
    public TextMeshProUGUI warningText;

    public AudioSource alertAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;

    private string originalSubstring;
    private Vector3 originalScale;
    private Vector3 originalPos;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;
        ResetPopUp();
    }

    public void Show(string substring)
    {
        alertAudioSource?.Play();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        substring = substring.ToLower().Trim();

        originalSubstring = substring;
        challengeText.text = $"CASP calls a bluff on\n<color=#FF3800>{substring.ToUpper()}</color>";
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = substring;
        inputField.text = substring;

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

    public void Send()
    {
        if (string.IsNullOrEmpty(inputField.text) || !inputField.text.Contains(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
        {
            StartCoroutine(ShakePopup());
            warningText.text = $"Word must include {originalSubstring.ToUpper()}";
            warningText.gameObject.SetActive(true);
        }
        else
        {
            StartCoroutine(HandleChallenge());
        }
    }

    private IEnumerator HandleChallenge()
    {
        yield return new WaitForSeconds(0.15f);

        gameManager.HandleChallenge(inputField.text);
        Hide();
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

    public void Hide()
    {
        StopAllCoroutines();
        ResetPopUp();
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        warningText.gameObject.SetActive(false);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}