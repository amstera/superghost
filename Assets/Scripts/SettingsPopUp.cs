using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SettingsPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public GameManager gameManager;

    public TextMeshProUGUI highScoreAmountText;
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

    private void OnSfxToggleChanged(bool isEnabled)
    {
        clickAudioSource?.Play();

        saveObject.EnableSound = isEnabled;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);
    }

    private void OnDifficultyChanged(int difficultyIndex)
    {
        saveObject.Difficulty = (Difficulty)difficultyIndex;
        gameManager.saveObject = saveObject;
        SaveManager.Save(saveObject);
    }
}