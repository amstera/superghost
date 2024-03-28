using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using Random = UnityEngine.Random;

public class SettingsPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject, settingsPage, dictionaryPage;
    public Button settingsButton, dictionaryButton;
    public GameManager gameManager;
    public AudioManager audioManager;

    public TextMeshProUGUI highScoreAmountText, footerText, dictionaryValidateText;
    public Toggle sfxToggle;
    public TMP_Dropdown difficultyDropdown;
    public TMP_InputField inputField;

    public AudioSource clickAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private SaveObject saveObject;
    private Vector3 originalScale;
    private Vector3 originalPos;

    private void Start()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;
        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        // Load settings
        saveObject = SaveManager.Load();

        // Set up High Score Amount text
        highScoreAmountText.text = $"{saveObject.HighScore}";

        // Set up SFX toggle
        sfxToggle.isOn = saveObject.EnableSound;
        sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

        // Set up Difficulty dropdown
        difficultyDropdown.value = (int)saveObject.Difficulty;
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        difficultyDropdown.interactable = gameManager.IsDoneRound();

        // Set up dictionary
        inputField.text = "";
        dictionaryValidateText.text = "";

        SetFooterText();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());
    }

    public void ShowSettings()
    {
        Show();
        PressSettingsTab();
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

    public void OpenContact()
    {
        Application.OpenURL("https://www.greenteagaming.com#contact");
    }

    public void OpenTerms()
    {
        Application.OpenURL("https://www.greenteagaming.com/terms-of-service");
    }

    public void OpenPrivacy()
    {
        Application.OpenURL("https://www.greenteagaming.com/privacy-policy");
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnSfxToggleChanged(bool isEnabled)
    {
        clickAudioSource?.Play();

        saveObject.EnableSound = isEnabled;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);

        if (saveObject.EnableSound)
        {
            audioManager.UnmuteMaster();
        }
        else
        {
            audioManager.MuteMaster();
        }
    }

    private void OnDifficultyChanged(int difficultyIndex)
    {
        clickAudioSource?.Play();

        saveObject.Difficulty = (Difficulty)difficultyIndex;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);
    }

    private void SetFooterText()
    {
        int currentYear = DateTime.Now.Year;
        string gameVersion = Application.version;
        string gameName = Application.productName;
        footerText.text = $"{gameName} © {currentYear} Green Tea Gaming - Version {gameVersion}";
    }

    public void PressSettingsTab()
    {
        clickAudioSource?.Play();

        settingsButton.interactable = false;
        dictionaryButton.interactable = true;
        settingsPage.gameObject.SetActive(true);
        dictionaryPage.gameObject.SetActive(false);
    }

    public void PressDictionaryTab()
    {
        clickAudioSource?.Play();

        settingsButton.interactable = true;
        dictionaryButton.interactable = false;
        settingsPage.gameObject.SetActive(false);
        dictionaryPage.gameObject.SetActive(true);
    }

    public void ValidateText()
    {
        clickAudioSource?.Play();

        if (string.IsNullOrEmpty(inputField.text))
        {
            StartCoroutine(ShakePopup());
            dictionaryValidateText.color = Color.red;
            dictionaryValidateText.text = "You must enter a word";

            return;
        }

        if (inputField.text.Length <= 3)
        {
            dictionaryValidateText.color = Color.red;
            dictionaryValidateText.text = "Word must be at least 4 letters";

            return;
        }

        if (gameManager.wordDictionary.IsWordReal(inputField.text, true))
        {
            dictionaryValidateText.color = Color.green;
            dictionaryValidateText.text = $"{inputField.text.ToUpper()} is a valid word";
        }
        else
        {
            dictionaryValidateText.color = Color.red;
            dictionaryValidateText.text = $"{inputField.text.ToUpper()} is not a valid word";
        }

        inputField.text = "";
    }

    private IEnumerator ShakePopup()
    {
        float elapsed = 0.0f;

        while (elapsed < 0.25f)
        {
            float x = originalPos.x + Random.Range(-1f, 1f) * 10;
            float y = originalPos.y + Random.Range(-1f, 1f) * 10;

            popUpGameObject.transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null; // Wait until next frame
        }

        popUpGameObject.transform.localPosition = originalPos;
    }
}