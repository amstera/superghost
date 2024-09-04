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
    public Sprite lockImage;

    public TextMeshProUGUI dropdownText, footerText, dictionaryValidateText;
    public Toggle sfxToggle, musicToggle, motionToggle;
    public TMP_Dropdown difficultyDropdown;
    public TMP_InputField inputField;

    public AudioSource clickAudioSource, warningAudioSource;

    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.5f;

    private SaveObject saveObject;
    private Vector3 originalScale;
    private Vector3 originalPos;
    private bool isHardOptionDisabled;

    private void Start()
    {
        originalScale = popUpGameObject.transform.localScale;
        originalPos = popUpGameObject.transform.localPosition;

        sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        motionToggle.onValueChanged.AddListener(OnMotionToggleChanged);
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        ResetPopUp();
    }

    public void Show()
    {
        clickAudioSource?.Play();

        // Load settings
        saveObject = SaveManager.Load();

        // Set up SFX toggle
        sfxToggle.isOn = saveObject.EnableSound;

        // Set up Music toggle
        musicToggle.isOn = saveObject.EnableMusic;

        // Set up Motion toggle
        motionToggle.isOn = saveObject.EnableMotion;

        // Set up Difficulty dropdown
        difficultyDropdown.value = (int)saveObject.Difficulty;
        difficultyDropdown.interactable = gameManager.IsRunEnded();

        dropdownText.text = "Can't be changed mid-run";

        UpdateDifficultyDropdownOptions();

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
        clickAudioSource?.Play();

        Application.OpenURL("https://www.greenteagaming.com/#contact");
    }

    public void OpenTerms()
    {
        clickAudioSource?.Play();

        Application.OpenURL("https://www.greenteagaming.com/terms-of-service");
    }

    public void OpenPrivacy()
    {
        clickAudioSource?.Play();

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
            AudioManager.instance.UnmuteMaster();
        }
        else
        {
            AudioManager.instance.MuteMaster();
        }
    }

    private void OnMusicToggleChanged(bool isEnabled)
    {
        clickAudioSource?.Play();

        saveObject.EnableMusic = isEnabled;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);

        if (saveObject.EnableMusic)
        {
            AudioManager.instance.StartMusic();
        }
        else
        {
            AudioManager.instance.StopMusic();
        }
    }

    private void OnMotionToggleChanged(bool isEnabled)
    {
        clickAudioSource?.Play();

        saveObject.EnableMotion = isEnabled;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);

        gameManager.backgroundSwirl.gameObject.SetActive(saveObject.EnableMotion);
        gameManager.commandCenter.spiralBackground.SetActive(saveObject.EnableMotion);
    }

    private void OnDifficultyChanged(int index)
    {
        clickAudioSource?.Play();

        // Prevent selecting "HARD" difficulty if it is disabled
        if (isHardOptionDisabled && index == 2)
        {
            difficultyDropdown.value = (int)saveObject.Difficulty;
            dropdownText.text = "Win a run to unlock <color=red>HARD</color>\nCan't be changed mid-run";
            difficultyDropdown.Hide();
            return;
        }

        saveObject.Difficulty = (Difficulty)index;
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

        bool isBlockedWord = saveObject.BlockedWords.Contains(inputField.text.ToLower());
        if (!isBlockedWord && gameManager.wordDictionary.IsWordReal(inputField.text, true))
        {
            dictionaryValidateText.color = Color.green;
            dictionaryValidateText.text = $"{inputField.text.ToUpper()} is a valid word";
        }
        else
        {
            dictionaryValidateText.color = Color.red;
            dictionaryValidateText.text = isBlockedWord ? $"{inputField.text.ToUpper()} is blocked" : $"{inputField.text.ToUpper()} is not a valid word";
        }

        inputField.text = "";
    }

    private void UpdateDifficultyDropdownOptions()
    {
        isHardOptionDisabled = saveObject.Statistics.EasyWins == 0 && saveObject.Statistics.NormalWins == 0;
        var options = difficultyDropdown.options;

        if (isHardOptionDisabled)
        {
            // Update the "HARD" option to be strikethrough
            options[2].text = "<color=red><s>HARD</s></color>";
            options[2].image = lockImage;
        }
        else
        {
            // Ensure the "HARD" option is normal if previously strikethrough
            options[2].text = "<color=red>HARD</color>";
            options[2].image = null;
        }

        difficultyDropdown.options = options;
    }

    private IEnumerator ShakePopup()
    {
        warningAudioSource?.Play();

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