using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class SettingsPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject, settingsPage, statsPage;
    public Button settingsButton, statsButton;
    public GameManager gameManager;
    public AudioManager audioManager;

    public TextMeshProUGUI highScoreAmountText, footerText, gamesPlayedText, longestWinningWordText, longestLosingWordText, mostPointsText, mostPointsWordText;
    public Toggle sfxToggle;
    public TMP_Dropdown difficultyDropdown;

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

        // Set up High Score Amount text
        highScoreAmountText.text = $"{saveObject.HighScore}";

        // Set up SFX toggle
        sfxToggle.isOn = saveObject.EnableSound;
        sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

        // Set up Difficulty dropdown
        difficultyDropdown.value = (int)saveObject.Difficulty;
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        difficultyDropdown.interactable = gameManager.IsDoneRound();

        // Set up stats
        gamesPlayedText.text = saveObject.Statistics.GamesPlayed.ToString();
        longestWinningWordText.text = string.IsNullOrEmpty(saveObject.Statistics.LongestWinningWord) ? "N/A" : saveObject.Statistics.LongestWinningWord;
        longestLosingWordText.text = string.IsNullOrEmpty(saveObject.Statistics.LongestLosingWord) ? "N/A" : saveObject.Statistics.LongestLosingWord;
        mostPointsText.text = saveObject.Statistics.MostPointsPerRound.ToString();
        mostPointsWordText.text = string.IsNullOrEmpty(saveObject.Statistics.MostPointsPerRoundWord) ? "" : $"({saveObject.Statistics.MostPointsPerRoundWord})";

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
        statsButton.interactable = true;
        settingsPage.gameObject.SetActive(true);
        statsPage.gameObject.SetActive(false);
    }

    public void PressStatsTab()
    {
        clickAudioSource?.Play();

        settingsButton.interactable = true;
        statsButton.interactable = false;
        settingsPage.gameObject.SetActive(false);
        statsPage.gameObject.SetActive(true);
    }
}