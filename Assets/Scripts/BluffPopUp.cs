using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BluffPopUp : MonoBehaviour
{
    public GameManager gameManager;

    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public TextMeshProUGUI warningText, pointsText, comboText, pointsCalculateText, formattedText;
    public TMP_InputField inputField;

    public AudioSource winAudioSource, noticeAudioSource;
    public ParticleSystem confetti;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;

    public int minLength = 3;
    private string originalSubstring = "";
    private string originalInputBeforeSubstring = "";
    private string originalInputAfterSubstring = "";

    private Vector3 originalScale;
    private Vector3 originalPos;
    private NumberCriteria numberCriteria = null;
    private HashSet<char> restrictedLetters = new HashSet<char>();
    private bool noRepeatingLetters;
    private int wordDirection;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;
        ResetPopUp();

        inputField.onValueChanged.AddListener(OnInputChanged);
    }

    public void Show(string substring)
    {
        winAudioSource?.Play();
        confetti.Play();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        pointsText.text = gameManager.pointsText.pointsText.text;
        pointsCalculateText.text = gameManager.pointsCalculateText.text;

        originalSubstring = substring.ToLower().Trim();
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = substring;
        inputField.text = originalSubstring;

        originalInputBeforeSubstring = "";
        originalInputAfterSubstring = "";

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

    private void OnInputChanged(string newText)
    {
        // Ensure the locked part is not altered
        if (!newText.Contains(originalSubstring))
        {
            // Reset text if locked part is altered
            inputField.text = originalInputBeforeSubstring + originalSubstring + originalInputAfterSubstring;
            inputField.caretPosition = inputField.text.Length;
            return;
        }

        // Update editable parts
        int lockedStartIndex = newText.IndexOf(originalSubstring);
        originalInputBeforeSubstring = newText.Substring(0, lockedStartIndex);
        originalInputAfterSubstring = newText.Substring(lockedStartIndex + originalSubstring.Length);

        // Reconstruct the input field with the locked substring intact
        inputField.text = originalInputBeforeSubstring + originalSubstring + originalInputAfterSubstring;

        // Update the formatted text with color
        string formatted = originalInputBeforeSubstring
                           + $"<color=red>{originalSubstring}</color>"
                           + originalInputAfterSubstring;
        formattedText.text = formatted;
    }

    public void Send()
    {
        if (string.IsNullOrEmpty(inputField.text) || !inputField.text.Contains(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
        {
            ShowWarning($"Word must include {originalSubstring.ToUpper()}");
        }
        else if (!gameManager.wordDictionary.IsWordReal(inputField.text, true))
        {
            var properNoun = gameManager.CheckForProperNoun(inputField.text);
            if (!string.IsNullOrEmpty(properNoun))
            {
                ShowWarning($"{properNoun.ToUpper()} is a proper noun");
            }
            else if (!string.IsNullOrEmpty(gameManager.CheckForOffensiveWord(inputField.text)))
            {
                ShowWarning($"That's an offensive word");
            }
            else
            {
                var warningText = $"{inputField.text.ToUpper()} isn't a valid word";
                var similarWord = gameManager.wordDictionary.FindClosestWord(inputField.text);
                if (!string.IsNullOrEmpty(similarWord))
                {
                    warningText += $"! Are you thinking <color=yellow>{similarWord.ToUpper()}</color>?";
                }
                ShowWarning(warningText);
            }
        }
        else if (inputField.text.Length <= minLength)
        {
            ShowWarning($"Word must be {minLength + 1}+ letters");
        }
        else if (numberCriteria != null && !numberCriteria.IsAllowed(inputField.text.Length))
        {
            ShowWarning($"Word must be {numberCriteria.GetName()} length");
        }
        else if (noRepeatingLetters && ContainsRepeatingLetters(inputField.text, out char repeatingLetter))
        {
            ShowWarning($"Word cannot repeat <color=white>{repeatingLetter.ToString().ToUpper()}</color>");
        }
        else if (wordDirection == -1 && !inputField.text.EndsWith(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
        {
            ShowWarning($"Word must end with {originalSubstring.ToUpper()}");
        }
        else if (wordDirection == 1 && !inputField.text.StartsWith(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
        {
            ShowWarning($"Word must start with {originalSubstring.ToUpper()}");
        }
        else
        {
            char invalidChar = restrictedLetters.FirstOrDefault(c => inputField.text.Contains(c, System.StringComparison.InvariantCultureIgnoreCase));

            if (invalidChar != '\0')
            {
                ShowWarning($"Word cannot contain <color=white>{invalidChar.ToString().ToUpper()}</color>");
            }
            else
            {
                canvasGroup.interactable = false;
                StartCoroutine(HandleCompleteBluff());
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
        numberCriteria = null;
    }

    public void SetNoRepeatingLetters(bool value)
    {
        noRepeatingLetters = value;
    }

    public void SetWordDirection(int value)
    {
        wordDirection = value;
    }

    public void AddNumberCriteria(NumberCriteria numberCriteria)
    {
        this.numberCriteria = numberCriteria;
    }

    public void Skip()
    {
        gameManager.BluffWin("");
        Hide();
    }

    private IEnumerator HandleCompleteBluff()
    {
        yield return new WaitForSeconds(0.15f);

        gameManager.BluffWin(inputField.text);
        Hide();
    }

    private bool ContainsRepeatingLetters(string word, out char repeatingLetter)
    {
        var letters = new HashSet<char>();
        foreach (var letter in word)
        {
            char lowerLetter = char.ToLower(letter);
            if (!letters.Add(lowerLetter))
            {
                repeatingLetter = lowerLetter;
                return true;
            }
        }
        repeatingLetter = '\0';
        return false;
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

        // Clear input and formatted text
        inputField.text = string.Empty;
        formattedText.text = string.Empty;

        // Reset inputField RectTransform to stretch anchors and positions
        RectTransform inputFieldRectTransform = inputField.textComponent.GetComponent<RectTransform>();
        inputFieldRectTransform.anchorMin = new Vector2(0, 0); // Left and bottom to 0
        inputFieldRectTransform.anchorMax = new Vector2(1, 1); // Right and top to 1
        inputFieldRectTransform.offsetMin = Vector2.zero; // Left and bottom offset to 0
        inputFieldRectTransform.offsetMax = Vector2.zero; // Right and top offset to 0
    }

    private void ShowWarning(string text)
    {
        noticeAudioSource?.Play();

        StartCoroutine(ShakePopup());
        warningText.text = text;
        warningText.gameObject.SetActive(true);
    }
}