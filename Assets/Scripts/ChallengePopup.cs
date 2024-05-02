using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChallengePopUp : MonoBehaviour
{
    public GameManager gameManager;

    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI challengeText;
    public TMP_InputField inputField;
    public TextMeshProUGUI warningText, comboText, pointsCalculateText;

    public AudioSource alertAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;

    public int minLength = 3;

    private string originalSubstring;
    private Vector3 originalScale;
    private Vector3 originalPos;
    private HashSet<char> restrictedLetters = new HashSet<char>();

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

        pointsCalculateText.text = gameManager.pointsCalculateText.text;

        substring = substring.ToLower().Trim();

        originalSubstring = substring;
        challengeText.text = $"CASP calls a bluff on:\n<color=#FF3800>{substring.ToUpper()}</color>";
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = substring;
        inputField.text = substring;

        comboText.text = gameManager.comboText.GetString();

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
            ShowWarning($"Word must include {originalSubstring.ToUpper()}");
        }
        else if (inputField.text.Length <= minLength)
        {
            ShowWarning($"Word must be {minLength + 1}+ letters");
        }
        else
        {
            char invalidChar = restrictedLetters.FirstOrDefault(c => inputField.text.Contains(c, System.StringComparison.InvariantCultureIgnoreCase));

            if (invalidChar != '\0')
            {
                ShowWarning($"Word cannot contain {invalidChar.ToString().ToUpper()}");
            }
            else
            {
                canvasGroup.interactable = false;
                StartCoroutine(HandleChallenge());
            }
        }
    }

    public void AddRestrictedLetter(char c)
    {
        restrictedLetters.Add(c);
    }

    public void ClearRestrictions()
    {
        restrictedLetters.Clear();
        minLength = 3;
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

    private void ShowWarning(string text)
    {
        StartCoroutine(ShakePopup());
        warningText.text = text;
        warningText.gameObject.SetActive(true);
    }
}