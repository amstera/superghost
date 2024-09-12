using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.iOS;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System;
using Random = UnityEngine.Random;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public TextClickHandler wordDisplay;
    public PointsText pointsText, totalPointsText, currencyEarnedText, bonusCurrencyEarnedText, endingPointsText, currencyText;
    public PointsExtendedText pointsEarnedText;
    public ChallengePopUp challengePopup;
    public PowersWarningPopUp powersWarningPopUp;
    public ShopPopUp shopPopUp;
    public HistoryText historyText;
    public TextMeshProUGUI endGameText, pointsCalculateText, levelText;
    public TextLerpColor playerText, aiText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject playerIndicator, aiIndicator, historyBackground, newIndicator, startText, highestLevelNewIndicator, redoLevel, fireBallCalculate, criteria, criteriaPill;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public ComboText comboText;
    public TextPosition selectedPosition = TextPosition.None;
    public SaveObject saveObject, copySaveObject;
    public Button challengeButton, recapButton, nextRoundButton, runInfoButton, statsButton;
    public SkipButton skipButton;
    public Stars stars;
    public RecapPopup recapPopup;
    public TutorialPopUp tutorialPopup;
    public BluffPopUp bluffPopup;
    public RunInfoPopUp runInfoPopup;
    public SettingsPopUp settingsPopup;
    public StatsPopup statsPopup;
    public CriteriaText criteriaText;
    public Vignette vignette;
    public ActiveEffectsText activeEffectsText;
    public CommandCenter commandCenter;
    public BackgroundSwirl backgroundSwirl;
    public TutorialModal tutorialModal;
    public ProperNounsWarningPopUp properNounsWarningPopUp;
    public WordDictionary wordDictionary = new WordDictionary();

    public AudioClip winSound, loseSound, loseGameSound, winGameSound, winRunSound;
    public AudioSource clickAudioSource, gameStatusAudioSource, keyAudioSource, challengeAudioSource, moneyAudioSource;

    public bool isPlayerTurn = true;
    public string gameWord = "";
    public bool HasBonusMultiplier, HasLastResortMultiplier, HasEvenWordMultiplier, HasOddWordMultiplier, HasDoubleWealth, HasDoubleTurn, HasLongWordMultiplier, HasDoubleBluff, HasLoseMoney, HasBonusMoney, HasDoubleEndedMultiplier, HasNoDuplicateLetterMultiplier;
    public float ChanceMultiplier = 1;
    public int ResetWordUses, PlayerRestoreLivesUses, AIRestoreLivesUses, AILivesMatch, ItemsUsed;
    public int currency = 5;
    public List<string> properNouns = new List<string>();

    private HashSet<string> previousWords = new HashSet<string>();
    private List<RecapObject> recap = new List<RecapObject>();
    private List<float> pointsBreakdown = new List<float>();
    private bool roundEnded = false;
    private bool gameOver = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    private bool isChallenging;
    private bool aiAlwaysStarts, noRepeatLetters;
    private int points, roundPoints, currentGame;
    private int roundCurrency;
    private int minLength = 3;
    private int minPointsNeeded;
    public enum TextPosition { None, Left, Right }

    private const string separator = "_";

    IEnumerator LoadWordDictionary()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, "wordlist.txt");
        string[] dictionaryLines = null;

        yield return StartCoroutine(LoadFileLines(filePath, result => dictionaryLines = result));
        wordDictionary.LoadWords(dictionaryLines);
    }

    IEnumerator LoadOriginalWordDictionary()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, "wordlist_original.txt");
        string[] dictionaryLines = null;

        yield return StartCoroutine(LoadFileLines(filePath, result => dictionaryLines = result));
        wordDictionary.LoadOriginalWords(dictionaryLines);
    }

    IEnumerator LoadCommonWords()
    {
        var commonFilePath = Path.Combine(Application.streamingAssetsPath, "words_common.txt");
        string[] commonLines = null;

        yield return StartCoroutine(LoadFileLines(commonFilePath, result => commonLines = result));
        wordDictionary.LoadCommonWords(commonLines);
    }

    IEnumerator LoadFileLines(string filePath, Action<string[]> callback)
    {
        List<string> lines = new List<string>();
        using (StreamReader reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine()?.Trim();
                if (line != null)
                {
                    lines.Add(line);
                }
            }
        }
        callback(lines.ToArray());

        yield return null;
    }

    async void Awake()
    {
        await UnityServices.InitializeAsync();
        AnalyticsService.Instance.StartDataCollection();

        saveObject = SaveManager.Load();
        currency = saveObject.Currency;
        currentGame = saveObject.CurrentLevel;

        Application.targetFrameRate = saveObject.EnableLowPowerMode ? 30 : 40;

        if (saveObject.Difficulty == Difficulty.Hard && saveObject.Statistics.EasyWins == 0 && saveObject.Statistics.NormalWins == 0)
        {
            saveObject.Difficulty = Difficulty.Normal;
            SaveManager.Save(saveObject);
        }
    }

    IEnumerator Start()
    {
        if (wordDisplay == null)
        {
            Debug.LogError("TextMeshProUGUI component is not assigned.");
            yield break;
        }

        yield return StartCoroutine(LoadWordDictionary());
        yield return StartCoroutine(LoadOriginalWordDictionary());
        yield return StartCoroutine(LoadCommonWords());

        wordDictionary.SortWordsByCommonality();

        if (saveObject.HasSeenTutorial)
        {
            UpdateDailyGameStreak(false);
            StartNewGame();
        }
        else
        {
            AudioManager.instance.GameStarted();

            tutorialPopup.Show(0, false, true, endingPageIndex: 4);
            saveObject.HasSeenTutorial = true;

            SaveManager.Save(saveObject);
        }
    }

    public void NewGamePressed()
    {
        StartCoroutine(NewGame());
    }

    public void UpdateGameState()
    {
        var gameState = GetGameState();
        criteriaText.UpdateState(gameState);
        commandCenter.UpdateState(criteriaText.GetCurrentCriteria(), gameState);
        bool metCriteria = criteriaText.AllMet(gameState);
        criteriaText.outline.color = metCriteria ? Color.green : Color.red;
        criteriaText.background.color = metCriteria ? new Color32(50, 150, 50, 35) : new Color32(150, 50, 50, 35);
        criteriaText.StartFlashing((playerLivesText.LivesRemaining() == 1 || aiLivesText.LivesRemaining() == 1) && !metCriteria, gameState);
    }

    private IEnumerator NewGame()
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();
        nextRoundButton.interactable = false;
        var originalColor = nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().color;
        nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.75f);

        var originalScale = Vector3.one;
        var duration = 0.15f;
        var popEffectScale = 1.35f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Apply ease-out effect (quadratic)
            float proportionCompleted = elapsedTime / duration;
            float easeOutProgress = 1 - Mathf.Pow(1 - proportionCompleted, 2);

            nextRoundButton.transform.localScale = Vector3.Lerp(originalScale, originalScale * popEffectScale, easeOutProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(StartNewGameSep());

        // Smooth interpolation back to the original scale
        elapsedTime = 0f;
        var poppedScale = nextRoundButton.transform.localScale;
        while (elapsedTime < duration)
        {
            // Smoothly lerp back using linear interpolation (could use easing here as well)
            nextRoundButton.transform.localScale = Vector3.Lerp(poppedScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it sets back to original exactly
        nextRoundButton.transform.localScale = originalScale;
        nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().color = originalColor;
    }

    private IEnumerator StartNewGameSep()
    {
        yield return null;

        StartNewGame();
    }

    private void StartNewGame()
    {
        nextRoundButton.interactable = true;
        nextRoundButton.gameObject.SetActive(false);
        commandCenter.gameObject.SetActive(true);
        if (aiLivesText.HasFullLives())
        {
            tutorialModal.ShowModal();
        }
        pointsCalculateText.text = string.Empty;
        backgroundSwirl.gameObject.SetActive(saveObject.EnableMotion);
        commandCenter.spiralBackground.SetActive(saveObject.EnableMotion);

        if (gameOver) // only at the beginning of a new game and not any new round
        {
            bool hasWonGame = saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0;
            bool hasWonRun = saveObject.Statistics.EasyWins > 0 || saveObject.Statistics.NormalWins > 0 || saveObject.Statistics.HardWins > 0;
            if (saveObject.HasSeenTutorial && hasWonGame && !saveObject.HasSeenRunTutorial)
            {
                tutorialPopup.Show(9, false, true, endingPageIndex: 13);
                saveObject.HasSeenRunTutorial = true;

                SaveManager.Save(saveObject);
                return;
            }
            else if (saveObject.HasSeenTutorial && saveObject.HasSeenRunTutorial && hasWonRun && !saveObject.HasSeenEndlessModeTutorial)
            {
                tutorialPopup.Show(14, false, true, endingPageIndex: 14);
                saveObject.HasSeenEndlessModeTutorial = true;

                SaveManager.Save(saveObject);
                return;
            }

            playerLivesText.ResetLives();
            aiLivesText.ResetLives();
            totalPointsText.Reset();
            bonusCurrencyEarnedText.gameObject.SetActive(true);
            bonusCurrencyEarnedText.Reset();
            runInfoButton.transform.localPosition = new Vector3(runInfoButton.transform.position.x, 113);
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Round >";
            nextRoundButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -255);
            endGameText.gameObject.SetActive(false);
            totalPointsText.gameObject.SetActive(false);
            redoLevel.gameObject.SetActive(false);
            RectTransform wordDisplayRect = wordDisplay.GetComponent<RectTransform>();
            wordDisplayRect.sizeDelta = new Vector2(wordDisplayRect.sizeDelta.x, 135);
            wordDisplay.fontSizeMax = 45;
            newIndicator.SetActive(false);
            highestLevelNewIndicator.SetActive(false);
            comboText.transform.parent.gameObject.SetActive(true);
            activeEffectsText.gameObject.SetActive(true);
            pointsText.gameObject.SetActive(true);
            recapButton.gameObject.SetActive(false);
            runInfoButton.gameObject.SetActive(false);
            endingPointsText.Reset();
            currencyText.gameObject.SetActive(true);
            endingPointsText.gameObject.SetActive(false);
            recap.Clear();
            wordDisplay.transform.localPosition = Vector3.zero;
            pointsText.Reset();
            pointsText.normalColor = Color.white;
            pointsText.pointsText.color = Color.white;
            points = 0;
            bool canSkip = criteriaText.SetLevelCriteria(currentGame) && (saveObject.Statistics.EasyGameWins > 0 || saveObject.Statistics.NormalGameWins > 0 || saveObject.Statistics.HardGameWins > 0);
            skipButton.Set(canSkip);
            criteria.SetActive(criteriaText.GetCurrentCriteria().Count > 0);
            AddRestrictions(criteriaText.GetCurrentCriteria());
            comboText.ChooseNewCombo();
            vignette.Hide();
            endGameText.GetComponent<ColorCycleEffect>().enabled = false;
            isPlayerTurn = true;
            currencyText.SetPoints(saveObject.Currency);
            criteriaText.criteriaText.color = Color.yellow;
            criteriaText.outline.color = Color.red;
            criteriaText.background.color = new Color32(150, 50, 50, 35);
            criteriaText.backgroundHUDOutline.color = new Color32(108, 108, 108, 255);

            var main = confettiPS.main;
            main.loop = false;

            if (currentGame == 0)
            {
                saveObject.RunStatistics = new RunStatistics();
            }

            gameOver = false;

            settingsPopup.difficultyDropdown.interactable = IsRunEnded();
            shopPopUp.RefreshView();
            ghostAvatar.SetFlag(GetGameState());

            var unlockedHats = statsPopup.GetUnlockedHats(true);
            statsButton.GetComponent<Image>().color = saveObject.UnlockedHats.Count == unlockedHats.Count ? Color.white : Color.yellow;
            statsButton.GetComponent<ScaleInOut>().maxScale = saveObject.UnlockedHats.Count == unlockedHats.Count ? 1.05f : 1.25f;

            AudioManager.instance.GameStarted();
        }
        else
        {
            if (comboText.IsCompleted())
            {
                comboText.ChooseNewCombo();
            }
        }

        if (aiAlwaysStarts)
        {
            isPlayerTurn = false;
        }

        if (isPlayerTurn)
        {
            startText.SetActive(true);
            keyboard.EnableAllButtons();
        }

        wordDictionary.ClearFilteredWords(saveObject.BlockedWords);
        wordDisplay.characterSpacing = 0f;
        wordDisplay.text = isPlayerTurn ? $"<color=yellow>{separator}</color>" : separator;
        historyText.UpdateText("");
        historyBackground.SetActive(false);
        gameWord = "";
        selectedPosition = TextPosition.None;
        roundEnded = false;
        isLastWordValid = true;
        playerWon = false;
        roundPoints = 0;
        roundCurrency = 0;
        criteriaText.gameObject.SetActive(true);
        levelText.gameObject.SetActive(true);
        levelText.text = $"Level {currentGame + 1}/" + (currentGame >= 10 ? "∞" : "10");
        levelText.fontSize = currentGame + 1 == 10 ? 26 : 28;
        levelText.color = Color.green;
        skipButton.gameObject.SetActive(true);
        pointsEarnedText.gameObject.SetActive(false);
        currencyEarnedText.Reset();
        currencyEarnedText.gameObject.SetActive(false);
        comboText.ResetPending();
        if (criteriaPill.GetComponentInChildren<TextMeshProUGUI>().text != "" && !DeviceTypeChecker.IsiPhoneSE())
        {
            criteriaPill.SetActive(true);
        }

        bool noPowersUsed = ItemsUsed == 0 && (playerLivesText.LivesRemaining() == 1 || aiLivesText.LivesRemaining() == 1);
        if (currentGame == 1 && noPowersUsed)
        {
            powersWarningPopUp.Show();
        }

        if (noRepeatLetters)
        {
            keyboard.RemoveAllRestrictions();
        }

        keyboard.Show();
        previousWords.Clear();
        SetIndicators(isPlayerTurn);
        commandCenter.UpdateState(criteriaText.GetCurrentCriteria(), GetGameState());

        int playerAiWinDifference = GetPlayerAIWinDifference();
        bool isAILosing = playerAiWinDifference > 0;
        ghostAvatar.UpdateState(isAILosing, currentGame);

        criteria.gameObject.SetActive(criteriaText.GetCurrentCriteria().Count > 0);
        if (backgroundSwirl.gameObject.activeSelf)
        {
            backgroundSwirl.UpdateLerp(playerAiWinDifference, false, false);
        }

        if (!isPlayerTurn)
        {
            ghostAvatar.Think();
            StartCoroutine(ProcessComputerTurn());
        }
    }

    public void SelectPosition(TextPosition position)
    {
        if (gameWord.Length == 0 || roundEnded || selectedPosition == position)
        {
            return;
        }

        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        selectedPosition = position;
        UpdateWordDisplay(true, 0);
    }

    public void ProcessTurn(char character)
    {
        if (string.IsNullOrEmpty(gameWord))
        {
            saveObject.Statistics.FrequentStartingLetter[character.ToString()] = saveObject.Statistics.FrequentStartingLetter.GetValueOrDefault(character.ToString()) + 1;
            SaveManager.Save(saveObject);
        }
        else
        {
            previousWords.Add(gameWord);
        }
        int newIndex = 0;
        switch (selectedPosition)
        {
            case TextPosition.Left:
                gameWord = character + gameWord;
                break;
            case TextPosition.Right:
                gameWord += character;
                newIndex = gameWord.Length - 1;
                break;
            case TextPosition.None:
                if (gameWord.Length == 0)
                {
                    gameWord += character;
                    break;
                }
                return;
        }

        wordDictionary.SetFilteredWords(gameWord);
        UpdateWordDisplay(HasDoubleTurn, newIndex);
        comboText.UseCharacter(character);

        if (noRepeatLetters)
        {
            keyboard.AddRestrictedLetter(character);
        }

        if (HasDoubleTurn)
        {
            SetIndicators(true);
        }
        else
        {
            isPlayerTurn = false;
            ghostAvatar.Think();
            SetIndicators(isPlayerTurn);
        }

        if (startText.activeSelf)
        {
            startText.SetActive(false);
        }

        CheckGameStatus();
    }

    public void ChallengeWord()
    {
        if (!isChallenging)
        {
            if (!saveObject.HasPressedChallengeButton)
            {
                saveObject.HasPressedChallengeButton = true;
                SaveManager.Save(saveObject);

                tutorialPopup.Show(5, false, callback: () => ChallengeWord(), endingPageIndex: 8, closeButtonText: "Challenge");
            }
            else
            {
                clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
                clickAudioSource?.Play();

                StartCoroutine(ProcessChallengeWord());
            }
        }
    }

    public void ShopButtonPressed()
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        shopPopUp.Show(currency, gameWord);
    }

    public void ShowHint()
    {
        bool canPushWord = true;
        var nextWord = wordDictionary.FindNextWord(gameWord, 4, Difficulty.Normal, 10);
        if (string.IsNullOrEmpty(nextWord) || (nextWord.Length - gameWord.Length) % 2 != 0)
        {
            nextWord = wordDictionary.FindNextWord(gameWord, 4, Difficulty.Hard, 10);
        }

        if (string.IsNullOrEmpty(nextWord))
        {
            canPushWord = false;
            nextWord = wordDictionary.FindWordContains(gameWord, false);
        }

        if (!string.IsNullOrEmpty(nextWord))
        {
            var color = canPushWord ? Color.yellow : Color.red;

            var nextLetter = FindAddedLetterAndIndex(gameWord, nextWord);
            char letter = char.Parse(nextLetter.addedLetter.ToUpper());
            keyboard.HighlightKey(letter, color);

            if (nextLetter.index == 0 && selectedPosition != TextPosition.Left)
            {
                SelectPosition(TextPosition.Left);
            }
            else if (nextLetter.index > 0 && selectedPosition != TextPosition.Right)
            {
                SelectPosition(TextPosition.Right);
            }
        }
    }

    public void ShuffleComboLetters()
    {
        comboText.ChooseNewCombo();
    }

    public void EnableMultiplier()
    {
        HasBonusMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableLastResortMultiplier()
    {
        HasLastResortMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableNoDuplicateLetterMultiplier()
    {
        HasNoDuplicateLetterMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableEvenMultiplier()
    {
        HasEvenWordMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableOddMultiplier()
    {
        HasOddWordMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableDoubleEnded()
    {
        HasDoubleEndedMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableLongWordMultiplier()
    {
        HasLongWordMultiplier = true;
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableChanceMultiplier()
    {
        var values = playerLivesText.LivesRemaining() == 1 && !gameOver ? new float[] { 0.5f, 1.5f, 1.5f, 2.5f, 2.5f } : new float[] { 0.5f, 1.5f, 1.5f, 2.5f };
        var index = Random.Range(0, values.Length);
        ChanceMultiplier = values[index];
        if (ChanceMultiplier == 0.5f)
        {
            challengePopup.noticeAudioSource?.Play();
        }
        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void RestoreLife(bool isPlayer)
    {
        if (isPlayer)
        {
            playerLivesText.GainLife();
            PlayerRestoreLivesUses++;
        }
        else
        {
            aiLivesText.GainLife();
            AIRestoreLivesUses++;
        }
    }


    public void MatchAILives()
    {
        playerLivesText.SetLives(aiLivesText.LivesRemaining());
        AILivesMatch++;
    }

    public void EnableDoubleWealth()
    {
        HasDoubleWealth = true;
    }

    public void DoDoubleTurn()
    {
        HasDoubleTurn = true;
    }

    public void ResetWord()
    {
        ghostAvatar.Hide();
        StartNewGame();
        ResetWordUses++;
        SetPointsCalculatedText();
    }

    public void EnableDoubleBluff()
    {
        HasDoubleBluff = true;
    }

    public void EnableMoneyLose()
    {
        HasLoseMoney = true;
    }

    public void EnableBonusMoney()
    {
        HasBonusMoney = true;
    }

    public void LoseLifeMoney()
    {
        LoseLife();
    }

    public void UndoTurn()
    {
        var addedChar = previousWords.Count > 0 ? ReplaceIgnoreCase(gameWord, previousWords.Last(), "").ToCharArray()[0] : gameWord[0];

        if (noRepeatLetters)
        {
            keyboard.RemoveRestrictedLetter(addedChar);
        }

        if (previousWords.Count > 0)
        {
            gameWord = previousWords.Last();
            previousWords.Remove(gameWord);
        }
        else
        {
            gameWord = "";
        }
        wordDictionary.ClearFilteredWords(saveObject.BlockedWords);

        if (string.IsNullOrEmpty(gameWord))
        {
            wordDisplay.text = $"<color=yellow>{separator}</color>";
            keyboard.EnableAllButtons();
        }
        else
        {
            wordDictionary.SetFilteredWords(gameWord);
            UpdateWordDisplay(true, 0);
        }

        comboText.RemoveCharacter(addedChar);

        keyAudioSource.pitch = Random.Range(0.8f, 1.5f);
        keyAudioSource?.Play();

        SetPointsCalculatedText();
        wordDisplay.Pop();
    }

    public void SkipTurn()
    {
        isPlayerTurn = false;
        ghostAvatar.Think();
        SetIndicators(isPlayerTurn);
        wordDisplay.text = wordDisplay.text.Replace($"<color=yellow>{separator}</color>", separator);

        if (startText.activeSelf)
        {
            startText.SetActive(false);
        }

        CheckGameStatus();
    }

    private IEnumerator ProcessChallengeWord()
    {
        challengeAudioSource?.Play();

        isChallenging = true;
        ghostAvatar.Think();
        challengeButton.interactable = false;
        var challengeButtonText = challengeButton.GetComponentInChildren<TextMeshProUGUI>();
        challengeButtonText.color = new Color(challengeButtonText.color.r, challengeButtonText.color.g, challengeButtonText.color.b, 0.5f);
        ghostAvatar.Pop();

        yield return new WaitForSeconds(0.45f);

        isChallenging = false;
        challengeButton.interactable = true;
        challengeButtonText.color = new Color(challengeButtonText.color.r, challengeButtonText.color.g, challengeButtonText.color.b, 1f);

        if (wordDictionary.ShouldChallenge(gameWord, GetPlayerAIWinDifference(), saveObject.Difficulty, false))
        {
            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            gameWord = previousWord;
            keyboard.Hide();
            bluffPopup.Show(previousWord);
            SetPointsCalculatedText();
        }
        else
        {
            var caspString = GetCaspText();
            var thoughtWord = wordDictionary.FindWordContains(gameWord, true).ToUpper();
            string wordLink = GenerateWordLink(thoughtWord, false);
            wordDisplay.text = $"{caspString} countered with\n{wordLink}";
            wordDisplay.word = thoughtWord;
            wordDictionary.AddLostChallengeWord(gameWord);
            playerLivesText.LoseLife();
            UpdatePoints(thoughtWord, -1);
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            previousWords.Add(thoughtWord);

            EndGame();
        }
    }

    public void BluffWin(string word)
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        aiLivesText.LoseLife();
        playerWon = true;
        isPlayerTurn = false;
        int multiplier = HasDoubleBluff ? 2 : 1;

        var caspString = GetCaspText(false);
        if (string.IsNullOrEmpty(word))
        {
            wordDisplay.text = $"You win!\n{caspString} was <color=green>bluffing</color>";
            isLastWordValid = false;
            previousWords.Add(gameWord);
            UpdatePoints(gameWord, multiplier);

            if (aiLivesText.IsGameOver())
            {
                confettiPS.Play();
                EndGame();
            }
            else
            {
                EndGame(false);
            }
        }
        else
        {
            string wordLink = GenerateWordLink(word, true);
            wordDisplay.text = $"You win with\n{wordLink}\n{caspString} was <color=green>bluffing</color>";
            wordDisplay.word = word;

            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            var addedChars = ReplaceIgnoreCase(word, previousWord, "").ToCharArray();
            foreach (var c in addedChars)
            {
                comboText.UseCharacter(c);
            }

            gameWord = word;
            previousWords.Add(word);
            UpdatePoints(word, multiplier);

            confettiPS.Play();
            EndGame();
        }
    }

    public void HandleChallenge(string word)
    {
        clickAudioSource.pitch = Random.Range(0.75f, 1.25f);
        clickAudioSource?.Play();

        previousWords.Add(gameWord);
        var originalWord = gameWord;

        gameWord = word;
        if (wordDictionary.IsWordReal(word))
        {
            string wordLink = GenerateWordLink(word, true);
            wordDisplay.text = $"You win!\n{wordLink}\nis a word";
            wordDisplay.word = word;
            aiLivesText.LoseLife();
            confettiPS.Play();
            playerWon = true;
            isPlayerTurn = false;
            wordDictionary.AddLostChallengeWord(originalWord);

            var addedChars = ReplaceIgnoreCase(word, originalWord, "").ToCharArray();
            foreach (var c in addedChars)
            {
                comboText.UseCharacter(c);
            }

            UpdatePoints(gameWord, 1);
            previousWords.Add(gameWord);
        }
        else
        {
            var caspString = GetCaspText();
            var wordLink = GenerateInvalidWordLink(word);
            wordDisplay.text = $"{caspString} wins!\n{wordLink}\nis not a word";
            playerLivesText.LoseLife();
            isLastWordValid = false;
            isPlayerTurn = true;
            if (properNouns.Contains(word.ToLower()))
            {
                properNounsWarningPopUp.Show(word);
            }
            if (gameWord.ToLower() != originalWord.ToLower())
            {
                previousWords.Add(gameWord);
            }
            UpdatePoints(gameWord, -1);
        }

        EndGame();
    }

    public void ShowRecap()
    {
        recapPopup.Show(recap);
    }

    public bool IsDoneRound()
    {
        return gameOver || roundEnded || (isPlayerTurn && string.IsNullOrEmpty(gameWord));
    }

    public bool IsPlayerTurn()
    {
        return !gameOver && !roundEnded && isPlayerTurn;
    }

    public bool IsRoundEnded()
    {
        return roundEnded;
    }

    public bool IsGameEnded()
    {
        return gameOver;
    }

    public bool IsRunEnded()
    {
        return (gameOver && !playerWon) || (gameOver && currentGame == 10) || (currentGame == 0 && isPlayerTurn && string.IsNullOrEmpty(gameWord) && playerLivesText.HasFullLives() && aiLivesText.HasFullLives());
    }

    public void UpdateLevelStats()
    {
        Dictionary<Difficulty, int> highestLevelMap = new Dictionary<Difficulty, int>
        {
            { Difficulty.Normal, saveObject.Statistics.HighestLevel },
            { Difficulty.Easy, saveObject.Statistics.EasyHighestLevel },
            { Difficulty.Hard, saveObject.Statistics.HardHighestLevel }
        };

        if (saveObject.CurrentLevel > highestLevelMap[saveObject.Difficulty])
        {
            saveObject.RunStatistics.SetNewHighLevel = true;
            switch (saveObject.Difficulty)
            {
                case Difficulty.Normal:
                    saveObject.Statistics.HighestLevel = saveObject.CurrentLevel;
                    break;
                case Difficulty.Easy:
                    saveObject.Statistics.EasyHighestLevel = saveObject.CurrentLevel;
                    break;
                case Difficulty.Hard:
                    saveObject.Statistics.HardHighestLevel = saveObject.CurrentLevel;
                    break;
            }
        }

        saveObject.RunStatistics.HighestLevel = saveObject.CurrentLevel;
    }

    public void ClearActiveEffects(bool includeSpecial = false)
    {
        HasBonusMultiplier = false;
        HasLastResortMultiplier = false;
        HasEvenWordMultiplier = false;
        HasLongWordMultiplier = false;
        HasOddWordMultiplier = false;
        HasDoubleEndedMultiplier = false;
        HasNoDuplicateLetterMultiplier = false;
        HasDoubleBluff = false;
        HasDoubleTurn = false;
        shopPopUp.ApplyDiscount(1);
        ChanceMultiplier = 1;

        if (includeSpecial)
        {
            HasLoseMoney = false;
            HasDoubleWealth = false;
            HasBonusMoney = false;
        }

        if (!roundEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void Mercy()
    {
        confettiPS.Play();
        playerWon = true;
        aiLivesText.LoseAllLives();
        var caspString = GetCaspText();
        wordDisplay.text = $"You win!\n{caspString} <color=green>gave up</color>";
        isLastWordValid = false;
        if (!string.IsNullOrEmpty(gameWord))
        {
            previousWords.Add(gameWord);
        }

        EndGame();
    }

    public void LoseLife()
    {
        playerLivesText.LoseLife();
        if (playerLivesText.LivesRemaining() == 0)
        {
            if (startText.activeSelf)
            {
                startText.SetActive(false);
            }

            var caspString = GetCaspText();
            wordDisplay.text = $"{caspString} wins!\nYou ran out of lives!";

            isLastWordValid = false;
            previousWords.Add(gameWord);
            EndGame();
        }
        else
        {
            gameStatusAudioSource.clip = loseSound;
            gameStatusAudioSource.Play();
        }
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > minLength && wordDictionary.IsWordReal(gameWord))
        {
            string wordLink = GenerateWordLink(gameWord, false);
            var caspString = GetCaspText();
            wordDisplay.text = $"{caspString} wins with\n{wordLink}";
            wordDisplay.word = gameWord;
            playerLivesText.LoseLife();
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            EndGame();
        }
        else if (!roundEnded)
        {
            if (HasDoubleTurn)
            {
                HasDoubleTurn = false;
                activeEffectsText.RemoveEffect(5);
            }
            else
            {
                StartCoroutine(ProcessComputerTurn());
            }
        }
    }

    private void UpdateWordDisplay(bool updateColor, int newWordIndex)
    {
        string displayText = gameWord.ToUpper();
        string underscore = updateColor ? $"<color=yellow>{separator}</color>" : separator;

        displayText = wordDisplay.canMoveLeft ? (selectedPosition == TextPosition.Left ? underscore : separator) + displayText : displayText;
        if (wordDisplay.canMoveRight)
        {
            displayText += selectedPosition == TextPosition.Right ? underscore : separator;
        }

        newWordIndex++;
        wordDisplay.text = displayText;

        if (!roundEnded && (!updateColor || !isPlayerTurn))
        {
            wordDisplay.HighlightNewLetterAtIndex(newWordIndex);
        }

        if (selectedPosition != TextPosition.None)
        {
            keyboard.EnableAllButtons();
        }
    }

    private void EndGame(bool playSound = true)
    {
        roundEnded = true;
        keyboard.Hide();
        keyboard.DisableAllButtons();
        wordDisplay.characterSpacing = -5f;
        pointsCalculateText.text = string.Empty;
        fireBallCalculate.SetActive(false);
        ClearActiveEffects();
        ChanceMultiplier = 1;

        ShowHistory();
        ghostAvatar.Hide();     
        nextRoundButton.gameObject.SetActive(true);
        commandCenter.gameObject.SetActive(false);
        tutorialModal.gameObject.SetActive(false);
        levelText.gameObject.SetActive(false);
        activeEffectsText.ClearAll();

        if (roundPoints != 0)
        {
            pointsEarnedText.gameObject.SetActive(true);
            pointsEarnedText.AddPoints(pointsBreakdown);
        }

        gameOver = playerLivesText.IsGameOver() || aiLivesText.IsGameOver();

        if (HasLoseMoney)
        {
            if (!playerWon && !gameOver)
            {
                int loseMoney = 15;
                if (HasDoubleWealth)
                {
                    loseMoney = Mathf.RoundToInt(loseMoney * 2f);
                }

                currencyEarnedText.gameObject.SetActive(true);
                currencyEarnedText.AddPoints(loseMoney, true);
                currency += loseMoney;
            }

            HasLoseMoney = false;
        }
        if (HasBonusMoney)
        {
            if (playerWon || !gameOver)
            {
                int bonusMoney = (playerLivesText.GetStartLives() - playerLivesText.LivesRemaining()) * 3;

                if (HasDoubleWealth)
                {
                    bonusMoney = Mathf.RoundToInt(bonusMoney * 2f);
                }

                if (playerWon)
                {
                    roundCurrency += bonusMoney;
                }
                else
                {
                    currencyEarnedText.gameObject.SetActive(true);
                    currencyEarnedText.AddPoints(bonusMoney, true);
                }
                currency += bonusMoney;
            }

            HasBonusMoney = false;
        }
        HasDoubleWealth = false;

        if (playerWon) // won round
        {
            gameStatusAudioSource.clip = gameOver ? winGameSound : winSound;

            if (isLastWordValid && gameWord.Length > saveObject.Statistics.LongestWinningWord.Length)
            {
                saveObject.Statistics.LongestWinningWord = gameWord.ToLower();
            }
            if (roundPoints > saveObject.Statistics.MostPointsPerRound)
            {
                saveObject.Statistics.MostPointsPerRound = roundPoints;
                saveObject.Statistics.MostPointsPerRoundWord = isLastWordValid ? gameWord.ToLower() : "";
                saveObject.RunStatistics.SetNewRoundHighScore = true;
            }
            if (roundPoints > saveObject.RunStatistics.MostPointsPerRound)
            {
                saveObject.RunStatistics.MostPointsPerRound = roundPoints;
                saveObject.RunStatistics.MostPointsPerRoundWord = isLastWordValid ? gameWord.ToLower() : "";
            }
            if (isLastWordValid)
            {
                saveObject.Statistics.WinningWords.Add(gameWord);
            }
            if (currency > saveObject.Statistics.MostMoney)
            {
                saveObject.Statistics.MostMoney = currency;
            }

            if (roundCurrency != 0)
            {
                currencyEarnedText.gameObject.SetActive(true);
                currencyEarnedText.AddPoints(roundCurrency, true);
            }
        }
        else // lost round
        {
            gameStatusAudioSource.clip = loseSound;
            comboText.ResetPending();

            if (isLastWordValid && gameWord.Length > saveObject.Statistics.LongestLosingWord.Length)
            {
                saveObject.Statistics.LongestLosingWord = gameWord.ToLower();
            }
        }

        var gameState = GetGameState();
        bool metCriteria = criteriaText.AllMet(gameState);
        criteriaText.UpdateState(gameState);
        criteriaText.outline.color = metCriteria ? Color.green : Color.red;
        criteriaText.background.color = metCriteria ? new Color32(50, 150, 50, 35) : new Color32(150, 50, 50, 35);
        criteriaPill.SetActive(false);
        bool shouldFlash = (playerLivesText.LivesRemaining() == 1 || aiLivesText.LivesRemaining() == 1) && !metCriteria;
        criteriaText.StartFlashing(shouldFlash, gameState);

        bool wonRun = false;
        if (gameOver)
        {
            UpdateDailyGameStreak(true);
            comboText.transform.parent.gameObject.SetActive(false);
            pointsText.gameObject.SetActive(false);
            recapButton.gameObject.SetActive(true);
            playerIndicator.gameObject.SetActive(false);
            aiIndicator.gameObject.SetActive(false);
            shopPopUp.RefreshShop(playerWon);
            wordDisplay.transform.localPosition += Vector3.down * 75;
            PlayerRestoreLivesUses = 0;
            AIRestoreLivesUses = 0;
            ItemsUsed = 0;
            AILivesMatch = 0;
            runInfoPopup.difficulty = saveObject.Difficulty;
            activeEffectsText.gameObject.SetActive(false);

            CopySaveObject();

            if (playerWon && metCriteria) // win game
            {
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
                totalPointsText.gameObject.SetActive(true);
                totalPointsText.AddPoints(points);

                if (historyText.textComponent.alignment == TextAlignmentOptions.Center)
                {
                    wordDisplay.transform.localPosition += Vector3.down * 15;
                }

                stars.Show(points);
                playerText.SetColor(Color.green);
                aiText.SetColor(Color.red);

                int gameWonCurrency = Math.Max(15, stars.GetStars() * 5 + (currentGame + 1) * 3 + 5);
                bonusCurrencyEarnedText.AddPoints(gameWonCurrency, true, "Bonus: ", delay: 0.5f);
                currency += gameWonCurrency;
                if (currency > saveObject.Statistics.MostMoney)
                {
                    saveObject.Statistics.MostMoney = currency;
                }

                nextRoundButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -255);
                var nextRoundButtonText = "Continue Run >";
                if (DeviceTypeChecker.IsiPhoneSE())
                {
                    nextRoundButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -240);
                }
                else
                {
                    string totalLevels = currentGame + 2 > 10 ? "" : "/10";
                    nextRoundButtonText += $"\n<size=30><color=green>Level {currentGame + 2}{totalLevels}</color></size>";

                    Dictionary<Difficulty, int> highestLevelMap = new Dictionary<Difficulty, int>
                    {
                        { Difficulty.Normal, saveObject.Statistics.HighestLevel },
                        { Difficulty.Easy, saveObject.Statistics.EasyHighestLevel },
                        { Difficulty.Hard, saveObject.Statistics.HardHighestLevel }
                    };
                    if (currentGame + 1 > highestLevelMap[saveObject.Difficulty])
                    {
                        highestLevelNewIndicator.SetActive(true);
                    }
                }
                nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = nextRoundButtonText;

                if (points > saveObject.Statistics.HighScore)
                {
                    saveObject.Statistics.HighScore = points;
                    newIndicator.SetActive(true);
                    saveObject.RunStatistics.SetNewHighScore = true;

                    Device.RequestStoreReview();
                }
                if (points > saveObject.RunStatistics.HighScore)
                {
                    saveObject.RunStatistics.HighScore = points;
                }

                currentGame++;
                saveObject.CurrentLevel++;

                if (saveObject.Difficulty == Difficulty.Easy)
                {
                    saveObject.Statistics.EasyGameWins++;
                }
                else if (saveObject.Difficulty == Difficulty.Normal)
                {
                    saveObject.Statistics.NormalGameWins++;
                }
                else if (saveObject.Difficulty == Difficulty.Hard)
                {
                    saveObject.Statistics.HardGameWins++;
                }

                UpdateLevelStats();

                criteriaText.criteriaText.color = Color.green;
                criteriaText.outline.color = Color.green;
                criteriaText.backgroundHUDOutline.color = Color.green;

                if (currentGame == 10) // win run
                {
                    wonRun = true;
                    endGameText.text = "You Win!";
                    endGameText.GetComponent<ColorCycleEffect>().enabled = true;

                    var main = confettiPS.main;
                    main.loop = true;
                    confettiPS.Play();

                    nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Endless Mode >";
                    highestLevelNewIndicator.SetActive(false);
                    bonusCurrencyEarnedText.gameObject.SetActive(false);
                    currencyEarnedText.gameObject.SetActive(false);
                    runInfoButton.gameObject.SetActive(true);
                    stars.Hide();
                    gameStatusAudioSource.clip = winRunSound;
                    runInfoButton.transform.localPosition = new Vector3(runInfoButton.transform.position.x, 56);

                    switch (saveObject.Difficulty)
                    {
                        case Difficulty.Normal:
                            saveObject.Statistics.NormalWins++;
                            break;
                        case Difficulty.Easy:
                            saveObject.Statistics.EasyWins++;
                            break;
                        case Difficulty.Hard:
                            saveObject.Statistics.HardWins++;
                            break;
                    }

                    currencyText.SetPoints(currency);
                    saveObject.RunStatistics.MostMoney = currency;

                    var wonRunEvent = new CustomEvent("wonRun")
                    {
                        { "games_played", saveObject.Statistics.GamesPlayed },
                        { "current_level", copySaveObject.CurrentLevel + 1 },
                        { "current_score", points },
                        { "version", Application.version },
                        { "difficulty", saveObject.Difficulty.ToString() },
                        { "high_score", saveObject.Statistics.HighScore },
                        { "total_wins", saveObject.Statistics.NormalWins + saveObject.Statistics.EasyWins +  saveObject.Statistics.HardWins },
                        { "highest_level", Mathf.Max(saveObject.Statistics.EasyHighestLevel, saveObject.Statistics.HighestLevel, saveObject.Statistics.HardHighestLevel) + 1 }
                    };
                    AnalyticsService.Instance.RecordEvent(wonRunEvent);
                }
            }
            else // lose game
            {
                endGameText.text = "Defeat!";
                endGameText.color = Color.red;
                pointsEarnedText.gameObject.SetActive(false);
                playerText.SetColor(Color.red);
                aiText.SetColor(Color.green);
                nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start New Run >";
                nextRoundButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -250);
                if (DeviceTypeChecker.IsiPhoneSE())
                {
                    nextRoundButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -230);
                }
                runInfoButton.gameObject.SetActive(true);
                currencyText.gameObject.SetActive(false);
                endingPointsText.gameObject.SetActive(true);
                endingPointsText.normalColor = Color.red;
                endingPointsText.AddPoints(points, endingText: points == 1 ? " PT" : " PTS", overrideColor: Color.red);
                gameStatusAudioSource.clip = loseGameSound;
                currencyEarnedText.gameObject.SetActive(false);
                vignette.Show(0.125f);
                criteriaText.outline.color = Color.red;
                criteriaText.backgroundHUDOutline.color = Color.red;

                if (playerWon)
                {
                    wordDisplay.text = $"You <color=red>lost</color> the run!\nYou didn't <color=yellow>{criteriaText.GetFailedCriteria(gameState)}</color>";
                    historyText.textComponent.text = historyText.textComponent.text.Replace("<color=green>", "<color=#8C8C8C>");
                }

                if (currentGame > 0)
                {
                    redoLevel.gameObject.SetActive(true);
                    runInfoButton.transform.localPosition = new Vector2(runInfoButton.transform.localPosition.x, 40);
                    wordDisplay.transform.localPosition += Vector3.down * 45;

                    RectTransform wordDisplayRect = wordDisplay.GetComponent<RectTransform>();
                    wordDisplayRect.sizeDelta = new Vector2(wordDisplayRect.sizeDelta.x, 195);
                    wordDisplay.fontSizeMax = 40;
                }
                else
                {
                    runInfoButton.transform.localPosition = new Vector2(runInfoButton.transform.localPosition.x, 113);
                    wordDisplay.transform.localPosition += Vector3.up * 25;
                }

                wordDisplay.transform.localPosition += Vector3.up * 25;

                saveObject.RunStatistics.MostMoney = currency;
                ResetRun();
            }

            endGameText.gameObject.SetActive(true);
            saveObject.Currency = currency;
            saveObject.Statistics.GamesPlayed++;

            AudioManager.instance.GameEnded(playerWon);

            var gamesPlayedEvent = new CustomEvent("gamesPlayed")
                {
                    { "games_played", saveObject.Statistics.GamesPlayed },
                    { "current_level", copySaveObject.CurrentLevel + 1 },
                    { "current_score", points },
                    { "version", Application.version },
                    { "difficulty", saveObject.Difficulty.ToString() },
                    { "high_score", saveObject.Statistics.HighScore },
                    { "total_wins", saveObject.Statistics.NormalWins + saveObject.Statistics.EasyWins +  saveObject.Statistics.HardWins },
                    { "highest_level", Mathf.Max(saveObject.Statistics.EasyHighestLevel, saveObject.Statistics.HighestLevel, saveObject.Statistics.HardHighestLevel) + 1 },
                    { "won_game", playerWon & metCriteria },
                    { "won_ghost", playerWon }
                };
            AnalyticsService.Instance.RecordEvent(gamesPlayedEvent);
        }

        shopPopUp.RefreshView();
        ghostAvatar.SetFlag(gameState);

        var unlockedHats = statsPopup.GetUnlockedHats(true);
        statsButton.GetComponent<Image>().color = saveObject.UnlockedHats.Count == unlockedHats.Count ? Color.white : Color.yellow;
        statsButton.GetComponent<ScaleInOut>().maxScale = saveObject.UnlockedHats.Count == unlockedHats.Count ? 1.05f : 1.25f;

        if (playSound)
        {
            gameStatusAudioSource.Play();
        }
        if ((totalPointsText.gameObject.activeSelf && bonusCurrencyEarnedText.gameObject.activeSelf) || currencyEarnedText.gameObject.activeSelf)
        {
            float delayTime = 0.5f;
            if (totalPointsText.gameObject.activeSelf)
            {
                delayTime = 0.75f;
            }
            else if (roundCurrency < 5)
            {
                delayTime = 0.25f;
            }
            moneyAudioSource.PlayDelayed(delayTime);
        }

        bool lostRun = gameOver && (!playerWon || !metCriteria);
        levelText.gameObject.SetActive(lostRun);
        if (playerWon && gameOver && !pointsEarnedText.gameObject.activeSelf)
        {
            levelText.gameObject.SetActive(true);
        }
        if (lostRun)
        {
            levelText.color = Color.red;
            skipButton.gameObject.SetActive(false);
        }

        if (!lostRun && !wonRun)
        {
            currencyText.AddPoints(currency - currencyText.points);
        }

        if (backgroundSwirl.gameObject.activeSelf)
        {
            backgroundSwirl.UpdateLerp(GetPlayerAIWinDifference(), gameOver && playerWon && metCriteria, lostRun);
        }

        SaveManager.Save(saveObject);
    }

    private void ShowHistory()
    {
        var previousWordsText = "";
        var lastWord = "";
        int index = 0;

        previousWords.RemoveWhere(w => string.IsNullOrEmpty(w));

        foreach (var word in previousWords)
        {
            var displayedWord = word.ToUpper();
            string linebreak = "\n";
            if (index == previousWords.Count - 1)
            {
                var color = playerWon ? "green" : "red";
                lastWord = displayedWord;
                if (isLastWordValid)
                {
                    displayedWord = $"<color={color}>{displayedWord}</color>";
                }
                else
                {
                    displayedWord = $"<color={color}><s>{displayedWord}</s></color>";
                }
                linebreak = "";
            }
            previousWordsText += $"{displayedWord}{linebreak}";
            index++;
        }

        historyBackground.SetActive(true);
        historyText.UpdateText(previousWordsText);

        recap.Add(new RecapObject
        {
            GameWord = lastWord,
            PlayerGhostString = playerLivesText.GetCurrentLivesString(),
            AIGhostString = aiLivesText.GetCurrentLivesString(),
            Points = points,
            History = previousWordsText,
            PlayerLivesRemaining = playerLivesText.LivesRemaining(),
            IsValidWord = isLastWordValid,
            PlayerWon = playerWon,
            CurrentLevel = currentGame
        });
    }

    private IEnumerator ProcessComputerTurn()
    {
        yield return new WaitForSeconds(Random.Range(0.6f, 1f));

        int winDiff = GetPlayerAIWinDifference();
        if (wordDictionary.ShouldChallenge(gameWord, winDiff, saveObject.Difficulty, true))
        {
            var word = wordDictionary.BluffWord(gameWord, saveObject.Difficulty, saveObject.CurrentLevel);
            if (string.IsNullOrEmpty(word))
            {
                keyboard.Hide();
                challengePopup.Show(gameWord);
            }
            else
            {
                PlayComputerWord(word);
            }
        }
        else
        {
            var word = wordDictionary.FindNextWord(gameWord, GetPlayerAIWinDifference(), saveObject.Difficulty, saveObject.CurrentLevel);
            if (word == null)
            {
                var foundWord = wordDictionary.FindWordContains(gameWord, false);
                if (string.IsNullOrEmpty(foundWord))
                {
                    word = wordDictionary.BluffWord(gameWord, saveObject.Difficulty, saveObject.CurrentLevel);
                    if (string.IsNullOrEmpty(word))
                    {
                        keyboard.Hide();
                        challengePopup.Show(gameWord);
                    }
                    else
                    {
                        PlayComputerWord(word);
                    }
                }
                else
                {
                    var wordLink = GenerateWordLink(foundWord, true);
                    wordDisplay.text = $"You win with\n{wordLink}";
                    wordDisplay.word = foundWord;
                    wordDisplay.Pop();
                    aiLivesText.LoseLife();
                    confettiPS.Play();
                    playerWon = true;
                    UpdatePoints(foundWord, 1);
                    isPlayerTurn = false;

                    previousWords.Add(gameWord);
                    gameWord = foundWord;
                    previousWords.Add(foundWord);
                    EndGame();
                }
            }
            else
            {
                PlayComputerWord(word);
            }
        }
    }

    private void SetIndicators(bool isPlayer)
    {
        playerIndicator.SetActive(isPlayer);
        aiIndicator.SetActive(!isPlayer);
        playerText.SetColor(isPlayer ? Color.green : Color.white);
        aiText.SetColor(isPlayer ? Color.white : Color.green);

        bool isChallengeButtonEnabled = isPlayer && !string.IsNullOrEmpty(gameWord) && !roundEnded;
        challengeButton.interactable = isChallengeButtonEnabled;
        var challengeButtonText = challengeButton.GetComponentInChildren<TextMeshProUGUI>();
        challengeButtonText.color = new Color(challengeButtonText.color.r, challengeButtonText.color.g, challengeButtonText.color.b, isChallengeButtonEnabled ? 1 : 0.5f);

        SetPointsCalculatedText();

        if (isPlayer)
        {
            if (string.IsNullOrEmpty(gameWord))
            {
                keyboard.EnableAllButtons();
            }
        }
        else
        {
            keyboard.DisableAllButtons();
        }
    }

    private void UpdatePoints(string word, float bonus)
    {
        pointsBreakdown = new List<float>();
        pointsBreakdown.Add(bonus < 0 ? -word.Length : word.Length);
        if (bonus > 1)
        {
            pointsBreakdown.Add(bonus);
        }

        float pointsChange = word.Length * bonus;
        if (pointsChange > 0)
        {
            var comboMultiplier = comboText.GetWinMultiplier(word);
            if (comboMultiplier != 1)
            {
                pointsBreakdown.Add(comboMultiplier);
                pointsChange *= comboMultiplier;
            }
            if (HasBonusMultiplier)
            {
                pointsChange *= 2;
                pointsBreakdown.Add(2);
            }
            if (HasLastResortMultiplier)
            {
                pointsChange *= 2.5f;
                pointsBreakdown.Add(2.5f);
            }
            if (HasNoDuplicateLetterMultiplier && HasNoDuplicateLetters(word))
            {
                pointsChange *= 4f;
                pointsBreakdown.Add(4f);
            }
            if (HasEvenWordMultiplier && word.Length % 2 == 0)
            {
                pointsChange *= 2.5f;
                pointsBreakdown.Add(2.5f);
            }
            if (HasOddWordMultiplier && word.Length % 2 != 0)
            {
                pointsChange *= 2.5f;
                pointsBreakdown.Add(2.5f);
            }
            if (HasDoubleEndedMultiplier && word.Length > 0 && char.ToLower(word[0]) == char.ToLower(word[word.Length - 1]))
            {
                pointsChange *= 4f;
                pointsBreakdown.Add(4f);
            }
            if (HasLongWordMultiplier && word.Length >= 10)
            {
                pointsChange *= 4;
                pointsBreakdown.Add(4);
            }
            if (ChanceMultiplier != 1)
            {
                pointsChange *= ChanceMultiplier;
                pointsBreakdown.Add(ChanceMultiplier);
            }
        }

        UpdatePoints(pointsChange);
    }

    private void UpdatePoints (float pointsChange)
    {
        roundPoints = (int)Math.Round(pointsChange, MidpointRounding.AwayFromZero);
        pointsText.AddPoints(roundPoints);
        points = Mathf.Max(0, points + roundPoints);
        pointsText.normalColor = Color.Lerp(Color.white, Color.green, minPointsNeeded == 0 ? 0 : Math.Min(1, points/(float)minPointsNeeded));

        if (pointsChange > 0)
        {
            roundCurrency = roundPoints / 5 + 1;
            if (HasDoubleWealth)
            {
                roundCurrency = Mathf.RoundToInt(roundCurrency * 2f);
            }
            currency += roundCurrency;
        }
    }

    private (string addedLetter, int index) FindAddedLetterAndIndex(string a, string b)
    {
        a = a.ToUpper();
        b = b.ToUpper();

        if (string.IsNullOrEmpty(a))
        {
            return (b, 0);
        }
        if (string.IsNullOrEmpty(b))
        {
            return (a, 0);
        }

        // Determine which string is longer (assuming b has the added character)
        string shorter = a.Length < b.Length ? a : b;
        string longer = a.Length < b.Length ? b : a;

        // Check if the added character is at the beginning
        if (longer[0] != shorter[0])
        {
            return (longer[0].ToString().ToUpper(), 0);
        }
        // If not at the beginning, it must be at the end
        else
        {
            return (longer[longer.Length - 1].ToString().ToUpper(), longer.Length - 1);
        }
    }

    private int GetPlayerAIWinDifference()
    {
        return playerLivesText.LivesRemaining() - aiLivesText.LivesRemaining();
    }

    private string GenerateWordLink(string gameWord, bool isWinning)
    {
        string link = $"https://www.dictionary.com/browse/{gameWord.ToLower()}";
        string color = isWinning ? "green" : "red";
        string displayWord = gameWord.ToUpper();

        return $"<link={link}><color={color}>{displayWord}</color><size=20> </size><size=40><voffset=1.5><sprite=0></voffset></size></link>";
    }

    private string GenerateInvalidWordLink(string gameWord)
    {
        string link = $"https://www.greenteagaming.com/#contact";
        string displayWord = gameWord.ToUpper();
        return $"<link={link}><color=red>{displayWord}</color></link>";
    }

    private string GetCaspText(bool isHappy = true)
    {
        return $"<voffset=2><size=50><sprite={(isHappy ? 2 : 1)}></size></voffset><color=yellow>CASP</color>";
    }

    private void SetPointsCalculatedText()
    {
        int multiplier = comboText.GetWinMultiplier(gameWord, false);

        string calculationText = string.Empty;

        var showTotal = false;
        if (gameWord.Length > 0)
        {
            float totalPoints = gameWord.Length;
            calculationText = $"({gameWord.Length}";

            if (multiplier != 1)
            {
                calculationText += $" x {multiplier}";
                totalPoints *= multiplier;
                showTotal = true;
            }
            if (HasBonusMultiplier)
            {
                calculationText += $" x 2";
                totalPoints *= 2;
                showTotal = true;
            }
            if (HasLastResortMultiplier)
            {
                calculationText += $" x 2.5";
                totalPoints *= 2.5f;
                showTotal = true;
            }
            if (HasNoDuplicateLetterMultiplier && HasNoDuplicateLetters(gameWord))
            {
                calculationText += $" x 4";
                totalPoints *= 4;
                showTotal = true;
            }
            if (HasEvenWordMultiplier && gameWord.Length % 2 == 0)
            {
                calculationText += $" x 2.5";
                totalPoints *= 2.5f;
                showTotal = true;
            }
            if (HasOddWordMultiplier && gameWord.Length % 2 != 0)
            {
                calculationText += $" x 2.5";
                totalPoints *= 2.5f;
                showTotal = true;
            }
            if (HasDoubleEndedMultiplier && gameWord.Length > 0 && char.ToLower(gameWord[0]) == char.ToLower(gameWord[gameWord.Length - 1]))
            {
                calculationText += $" x 4";
                totalPoints *= 4f;
                showTotal = true;
            }
            if (HasLongWordMultiplier && gameWord.Length >= 10)
            {
                calculationText += $" x 4";
                totalPoints *= 4;
                showTotal = true;
            }
            if (ChanceMultiplier != 1)
            {
                calculationText += ChanceMultiplier < 1 ? $" x <color=red>{ChanceMultiplier}</color>" : $" x {ChanceMultiplier}";
                totalPoints *= ChanceMultiplier;
                showTotal = true;
            }
            if (HasDoubleBluff && bluffPopup.canvasGroup.alpha > 0)
            {
                calculationText += $" x 2";
                totalPoints *= 2;
                showTotal = true;
            }

            int pointsToShow = (int)Math.Round(totalPoints, MidpointRounding.AwayFromZero);

            calculationText += showTotal ? $" = {pointsToShow})" : ")";

            int pointsForFire = 40;
            fireBallCalculate.SetActive(pointsToShow >= pointsForFire);
        }

        pointsCalculateText.text = calculationText;
        if (challengePopup.canvasGroup.alpha > 0)
        {
            challengePopup.pointsCalculateText.text = calculationText;
            challengePopup.activeEffectsText.MatchEffects(activeEffectsText);
        }
        if (bluffPopup.canvasGroup.alpha > 0)
        {
            bluffPopup.pointsCalculateText.text = calculationText;
        }
    }

    private void UpdateDailyGameStreak(bool finishedGame)
    {
        var lastIncrementedDate = saveObject.Statistics.LastIncrementDate;
        var currentStreak = saveObject.Statistics.DailyPlayStreak;

        if (lastIncrementedDate == DateTime.MinValue)
        {
            if (finishedGame)
            {
                currentStreak = 1;
                lastIncrementedDate = DateTime.Now;
            }
        }
        else
        {
            var daysSinceLastIncrement = (DateTime.UtcNow - lastIncrementedDate).Days;

            if (daysSinceLastIncrement == 1)
            {
                if (finishedGame)
                {
                    currentStreak++;
                    lastIncrementedDate = DateTime.Now;
                }
            }
            else if (daysSinceLastIncrement > 1)
            {
                if (finishedGame)
                {
                    currentStreak = 1;
                    lastIncrementedDate = DateTime.Now;
                }
                else
                {
                    currentStreak = 0;
                }
            }
        }

        saveObject.Statistics.LastIncrementDate = lastIncrementedDate;
        saveObject.Statistics.DailyPlayStreak = currentStreak;
    }

    private void PlayComputerWord(string word)
    {
        keyAudioSource.pitch = Random.Range(0.8f, 1.5f);
        keyAudioSource?.Play();

        previousWords.Add(gameWord);
        var addedLetter = FindAddedLetterAndIndex(word, gameWord);
        ghostAvatar.Show(addedLetter.addedLetter);
        gameWord = word;
        wordDictionary.SetFilteredWords(gameWord);
        UpdateWordDisplay(true, addedLetter.index);
        isPlayerTurn = true;
        SetIndicators(isPlayerTurn);
        shopPopUp.RefreshView();

        if (noRepeatLetters)
        {
            keyboard.AddRestrictedLetter(addedLetter.addedLetter[0]);
        }
    }

    private void ResetRun()
    {
        isPlayerTurn = true;
        ResetWordUses = 0;

        currentGame = 0;
        saveObject.CurrentLevel = 0;
        currency = 5;
        saveObject.ShopItemIds.Clear();
        saveObject.RestrictedChars.Clear();
        saveObject.ChosenCriteria.Clear();
    }

    private void CopySaveObject()
    {
        copySaveObject.CurrentLevel = saveObject.CurrentLevel;
        copySaveObject.Currency = saveObject.Currency;
        copySaveObject.ShopItemIds = saveObject.ShopItemIds.ToList();
        copySaveObject.RestrictedChars = saveObject.RestrictedChars.ToDictionary(entry => entry.Key, entry => entry.Value);
        copySaveObject.ChosenCriteria = saveObject.ChosenCriteria.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private GameState GetGameState()
    {
        return new GameState
        {
            ItemsUsed = ItemsUsed,
            Points = points,
            EndGame = gameOver
        };
    }

    private bool HasNoDuplicateLetters(string word)
    {
        var seenLetters = new HashSet<char>();
        foreach (var letter in word)
        {
            if (!seenLetters.Add(char.ToLower(letter)))
            {
                return false;
            }
        }
        return true;
    }

    public static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
            return source;

        string result = source;
        int index = result.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);

        while (index != -1)
        {
            result = result.Remove(index, oldValue.Length).Insert(index, newValue);
            index = result.IndexOf(oldValue, index + newValue.Length, StringComparison.InvariantCultureIgnoreCase);
        }

        return result;
    }

    private void AddRestrictions(List<GameCriterion> criteria)
    {
        ClearLetterRestrictions();
        comboText.ClearRestrictions();
        minLength = 3;
        comboText.IsInactive = false;
        aiAlwaysStarts = false;
        ghostAvatar.CanMercy = true;
        SetRepeatingLetters(false);
        SetWordDirection(0);
        var criteriaPillText = criteriaPill.GetComponentInChildren<TextMeshProUGUI>();
        criteriaPillText.text = "";

        foreach (var criterion in criteria)
        {
            if (criterion.IsRestrictive)
            {
                if (criterion is NoUsingLetter noUsingLetter)
                {
                    var restrictedLetter = noUsingLetter.GetRestrictedLetter();
                    AddRestrictedLetter(restrictedLetter);
                }
                else if (criterion is StartWithHandicap startWithHandicap)
                {
                    var amount = startWithHandicap.GetAmount();
                    playerLivesText.AddHandicap(amount);
                }
                else if (criterion is MinLetters minLetters)
                {
                    minLength = minLetters.GetAmount() - 1;
                    wordDictionary.SetMinLength(minLength);
                    challengePopup.minLength = minLength;
                    bluffPopup.minLength = minLength;
                    criteriaPillText.text = $"{minLength + 1}+ Letter Words Only";
                }
                else if (criterion is NoComboLetters)
                {
                    comboText.IsInactive = true;
                }
                else if (criterion is OddLetters oddLetters)
                {
                    wordDictionary.SetNumberCriteria(oddLetters.GetCriteria());
                    challengePopup.AddNumberCriteria(oddLetters.GetCriteria());
                    bluffPopup.AddNumberCriteria(oddLetters.GetCriteria());
                    criteriaPillText.text = "Odd Length Words Only";
                }
                else if (criterion is EvenLetters evenLetters)
                {
                    wordDictionary.SetNumberCriteria(evenLetters.GetCriteria());
                    challengePopup.AddNumberCriteria(evenLetters.GetCriteria());
                    bluffPopup.AddNumberCriteria(evenLetters.GetCriteria());
                    criteriaPillText.text = "Even Length Words Only";
                }
                else if (criterion is AIStarts)
                {
                    aiAlwaysStarts = true;
                }
                else if (criterion is NoRepeatLetters)
                {
                    SetRepeatingLetters(true);
                }
                else if (criterion is OnlyMove onlyMove)
                {
                    if (onlyMove.CanMoveForward())
                    {
                        SetWordDirection(1);
                    }
                    else
                    {
                        SetWordDirection(-1);
                    }
                }
                else if (criterion is NoMercy)
                {
                    ghostAvatar.CanMercy = false;
                }
            }
            else
            {
                if (criterion is ScoreAtLeastXPoints minPoints)
                {
                    minPointsNeeded = minPoints.GetPoints();
                }
            }
        }

    }

    private void AddRestrictedLetter(char restrictedLetter)
    {
        keyboard.AddRestrictedLetter(restrictedLetter);
        wordDictionary.AddRestrictedLetter(restrictedLetter);
        challengePopup.AddRestrictedLetter(restrictedLetter);
        bluffPopup.AddRestrictedLetter(restrictedLetter);
        comboText.AddRestrictedLetter(restrictedLetter);
    }

    private void ClearLetterRestrictions()
    {
        keyboard.RemoveAllRestrictions();
        challengePopup.ClearRestrictions();
        bluffPopup.ClearRestrictions();
        wordDictionary.ClearRestrictions();
    }

    private void SetRepeatingLetters(bool value)
    {
        noRepeatLetters = value;
        wordDictionary.SetNoRepeatingLetters(value);
        challengePopup.SetNoRepeatingLetters(value);
        bluffPopup.SetNoRepeatingLetters(value);
    }

    private void SetWordDirection(int value)
    {
        wordDisplay.canMoveLeft = value != 1;
        wordDisplay.canMoveRight = value != -1;
        wordDictionary.SetWordDirection(value);
        challengePopup.SetWordDirection(value);
        bluffPopup.SetWordDirection(value);
    }
}