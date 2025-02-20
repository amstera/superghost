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
    public TextMeshProUGUI warningText, pointsText, comboText, pointsCalculateText, formattedText;
    public ActiveEffectsText activeEffectsText;
    public GameObject challengeModal;
    public TutorialPopUp tutorialPopup;

    public AudioSource alertAudioSource, noticeAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;

    public int minLength = 3;
    private string originalSubstring;
    private string originalInputBeforeSubstring = "";
    private string originalInputAfterSubstring = "";

    private Vector3 originalScale;
    private Vector3 originalPos;
    private HashSet<char> restrictedLetters = new HashSet<char>();
    private NumberCriteria numberCriteria = null;
    private bool noRepeatingLetters;
    private int wordDirection;
    private SaveObject saveObject;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;
        saveObject = SaveManager.Load();
        ResetPopUp();
        inputField.onValueChanged.AddListener(OnInputChanged);
    }

    public void Show(string substring)
    {
        alertAudioSource?.Play();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (!saveObject.HasPressedChallengeButton)
        {
            challengeModal.SetActive(true);
        }

        pointsText.text = gameManager.pointsText.pointsText.text;
        pointsCalculateText.text = gameManager.pointsCalculateText.text;

        originalSubstring = substring.ToLower().Trim();
        challengeText.text = $"Enter a <color=green>valid word</color> containing\n<color=red>{substring.ToUpper()}</color>";
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = substring;
        inputField.text = originalSubstring;

        originalInputBeforeSubstring = "";
        originalInputAfterSubstring = "";

        comboText.text = gameManager.comboText.GetString();
        activeEffectsText.MatchEffects(gameManager.activeEffectsText);

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());
    }

    public void ShowChallengeTutorial()
    {
        bool hasSeenChallengeTutorial = saveObject.HasPressedChallengeButton;
        if (!saveObject.HasPressedChallengeButton)
        {
            challengeModal.SetActive(false);
            saveObject.HasPressedChallengeButton = true;
            SaveManager.Save(saveObject);
        }

        tutorialPopup.Show(5, hasSeenChallengeTutorial, endingPageIndex: 8);
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
        // Ensure the locked substring is preserved
        if (!newText.Contains(originalSubstring))
        {
            // Reset the input if the locked part is altered
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
        else if (!gameManager.wordDictionary.IsWordReal(inputField.text, true))
        {
            var properNoun = gameManager.CheckForProperNoun(inputField.text);
            if (!string.IsNullOrEmpty(properNoun))
            {
                ShowWarning($"{properNoun.ToUpper()} is a proper noun");
            }
            else if (!string.IsNullOrEmpty(gameManager.CheckForOffensiveWord(inputField.text)))
            {
                ShowWarning($"That word is not allowed");
            }
            else
            {
                var warningText = $"{inputField.text.ToUpper()} isn't a valid word";
                var similarWord = gameManager.wordDictionary.FindClosestWord(inputField.text);
                if (!string.IsNullOrEmpty(similarWord))
                {
                    warningText += $"! ";
                    if (similarWord.Contains(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        warningText += $"Did you mean <color=yellow>{similarWord.ToUpper()}</color>?";
                    }
                    else
                    {
                        warningText += $"You maybe misspelled <color=orange>{similarWord.ToUpper()}</color>";
                    }

                    ShowWarning(warningText);
                }
                else if (inputField.text.Equals(originalSubstring, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    ShowWarning($"Word must be valid and include {originalSubstring.ToUpper()}");
                }
                else
                {
                    ShowWarning(warningText);
                }
            }
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
                StartCoroutine(HandleChallenge());
            }
        }
    }

    public void Skip()
    {
        canvasGroup.interactable = false;
        StartCoroutine(HandleChallenge());
    }

    public void AddRestrictedLetter(char c)
    {
        restrictedLetters.Add(c);
    }

    public void AddNumberCriteria(NumberCriteria numberCriteria)
    {
        this.numberCriteria = numberCriteria;
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

    private IEnumerator HandleChallenge()
    {
        yield return new WaitForSeconds(0.05f);

        gameManager.HandleChallenge(inputField.text);
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

        ResetInputFieldAndCaret();
    }

    private void ResetInputFieldAndCaret()
    {
        // Reset the input field's RectTransform to stretch anchors and positions
        RectTransform inputFieldRectTransform = inputField.textComponent.GetComponent<RectTransform>();
        inputFieldRectTransform.anchorMin = new Vector2(0, 0);
        inputFieldRectTransform.anchorMax = new Vector2(1, 1);
        inputFieldRectTransform.offsetMin = Vector2.zero;
        inputFieldRectTransform.offsetMax = Vector2.zero;

        ResetCaret();
    }

    private void ResetCaret()
    {
        // Look for the caret in the children by name, which avoids the need to search through all RectTransforms
        var caret = inputField.GetComponentInChildren<TMP_SelectionCaret>();

        if (caret != null)
        {
            RectTransform caretRectTransform = caret.GetComponent<RectTransform>();

            if (caretRectTransform != null)
            {
                // Reset the caret's RectTransform to stretch anchors
                caretRectTransform.anchorMin = new Vector2(0, 0);  // Anchor to left and bottom
                caretRectTransform.anchorMax = new Vector2(1, 1);  // Anchor to right and top
                caretRectTransform.offsetMin = Vector2.zero;
                caretRectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    private void ShowWarning(string text)
    {
        noticeAudioSource?.Play();

        StartCoroutine(ShakePopup());
        warningText.text = text;
        warningText.gameObject.SetActive(true);
    }
}