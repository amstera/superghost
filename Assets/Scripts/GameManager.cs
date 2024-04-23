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
    public PointsText pointsText, totalPointsText, pointsEarnedText, currencyEarnedText, bonusCurrencyEarnedText, finalLevelText;
    public ChallengePopUp challengePopup;
    public ShopPopUp shopPopUp;
    public HistoryText historyText;
    public TextMeshProUGUI playerText, aiText, startText, endGameText, pointsCalculateText, levelText, endingPointsText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject playerIndicator, aiIndicator, newIndicator, newLevelIndicator, shopNewIndicator, difficultyText, fireBall, fireBallCalculate;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public ComboText comboText;
    public TextPosition selectedPosition = TextPosition.None;
    public SaveObject saveObject;
    public Button shopButton, challengeButton, recapButton, nextRoundButton, tutorialButton, restartButton, runInfoButton;
    public Stars stars;
    public RecapPopup recapPopup;
    public TutorialPopUp tutorialPopup;
    public BluffPopUp bluffPopup;
    public CriteriaText criteriaText;
    public Vignette vignette;
    public WordDictionary wordDictionary = new WordDictionary();

    public AudioClip winSound, loseSound, loseGameSound, winRunSound, fireballSound;
    public AudioSource clickAudioSource, gameStatusAudioSource, keyAudioSource, challengeAudioSource;

    public bool isPlayerTurn = true;
    public string gameWord = "";
    public bool HasBonusMultiplier, HasEvenWordMultiplier, HasDoubleWealth, HasDoubleTurn, HasLongWordMultiplier, HasDoubleBluff;
    public float ChanceMultiplier = 1;
    public int ResetWordUses;
    public int currency = 5;

    private HashSet<string> previousWords = new HashSet<string>();
    private List<RecapObject> recap = new List<RecapObject>();
    private bool gameEnded = false;
    private bool gameOver = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    private bool isChallenging;
    private bool setLevelHighScore;
    private int points, roundPoints, currentGame;
    private int roundCurrency;
    private int minLength = 3;
    public enum TextPosition { None, Left, Right }

    private const string separator = "_";

    IEnumerator LoadWordDictionary()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, "wordlist.txt");
        string[] dictionaryLines = null;

        yield return StartCoroutine(LoadFileLines(filePath, result => dictionaryLines = result));
        wordDictionary.LoadWords(dictionaryLines);
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

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        saveObject = SaveManager.Load();
        currency = saveObject.Currency;
        currentGame = saveObject.CurrentLevel;
    }

    IEnumerator Start()
    {
        if (wordDisplay == null)
        {
            Debug.LogError("TextMeshProUGUI component is not assigned.");
            yield break;
        }

        yield return StartCoroutine(LoadWordDictionary());
        yield return StartCoroutine(LoadCommonWords());

        wordDictionary.SortWordsByCommonality();

        if (saveObject.HasSeenTutorial)
        {
            UpdateDailyGameStreak(false);
            StartNewGame();
        }
        else
        {
            tutorialPopup.Show(false);
            saveObject.HasSeenTutorial = true;

            SaveManager.Save(saveObject);
        }
    }

    public void NewGamePressed()
    {
        StartCoroutine(NewGame());
    }

    private IEnumerator NewGame()
    {
        clickAudioSource?.Play();
        nextRoundButton.interactable = false;

        yield return new WaitForSeconds(0.15f);
        StartNewGame();
    }

    private void StartNewGame()
    {
        nextRoundButton.interactable = true;
        nextRoundButton.gameObject.SetActive(false);
        tutorialButton.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        pointsCalculateText.text = string.Empty;

        if (gameOver)
        {
            playerLivesText.ResetLives();
            aiLivesText.ResetLives();
            totalPointsText.Reset();
            bonusCurrencyEarnedText.Reset();
            finalLevelText.Reset();
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Round >";
            endGameText.gameObject.SetActive(false);
            totalPointsText.gameObject.SetActive(false);
            finalLevelText.gameObject.SetActive(false);
            difficultyText.gameObject.SetActive(false);
            newIndicator.SetActive(false);
            shopNewIndicator.SetActive(false);
            newLevelIndicator.SetActive(false);
            comboText.gameObject.SetActive(true);
            pointsText.gameObject.SetActive(true);
            recapButton.gameObject.SetActive(false);
            runInfoButton.gameObject.SetActive(false);
            endingPointsText.gameObject.SetActive(false);
            shopButton.gameObject.SetActive(true);
            recap.Clear();
            wordDisplay.transform.localPosition = Vector3.zero;
            pointsText.Reset();
            points = 0;
            criteriaText.SetLevelCriteria(currentGame);
            AddRestrictions(criteriaText.GetCurrentCriteria());
            comboText.ChooseNewCombo();
            vignette.Hide();
            endGameText.GetComponent<ColorCycleEffect>().enabled = false;

            var main = confettiPS.main;
            main.loop = false;

            if (currentGame == 0)
            {
                saveObject.RunStatistics = new Statistics();
            }

            gameOver = false;
        }
        else
        {
            if (comboText.IsCompleted())
            {
                comboText.ChooseNewCombo();
            }
        }

        if (isPlayerTurn)
        {
            startText.gameObject.SetActive(true);
            keyboard.EnableAllButtons();
        }

        wordDictionary.ClearFilteredWords();
        wordDisplay.characterSpacing = 0f;
        wordDisplay.text = isPlayerTurn ? $"<color=yellow>{separator}</color>" : separator;
        historyText.UpdateText("");
        gameWord = "";
        selectedPosition = TextPosition.None;
        gameEnded = false;
        isLastWordValid = true;
        playerWon = false;
        roundPoints = 0;
        roundCurrency = 0;
        criteriaText.gameObject.SetActive(true);
        levelText.gameObject.SetActive(true);
        levelText.text = $"Level {currentGame + 1}/10";
        pointsEarnedText.Reset();
        pointsEarnedText.gameObject.SetActive(false);
        currencyEarnedText.Reset();
        currencyEarnedText.gameObject.SetActive(false);
        comboText.ResetPending();

        keyboard.Show();
        previousWords.Clear();
        SetIndicators(isPlayerTurn);

        ghostAvatar.UpdateState(IsPlayerWinning(), currentGame);

        if (!isPlayerTurn)
        {
            ghostAvatar.Think();
            StartCoroutine(ProcessComputerTurn());
        }
    }

    public void SelectPosition(TextPosition position)
    {
        if (gameWord.Length == 0 || gameEnded || selectedPosition == position)
        {
            return;
        }

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

        if (startText.gameObject.activeSelf)
        {
            startText.gameObject.SetActive(false);
        }

        CheckGameStatus();
    }

    public void ChallengeWord()
    {
        if (!isChallenging)
        {
            clickAudioSource?.Play();

            StartCoroutine(ProcessChallengeWord());
        }
    }

    public void ShopButtonPressed()
    {
        clickAudioSource?.Play();

        shopPopUp.Show(currency, gameWord, saveObject.Difficulty, shopNewIndicator.activeSelf);
    }

    public void ShowHint()
    {
        bool canPushWord = true;
        var nextWord = wordDictionary.FindNextWord(gameWord, true, Difficulty.Normal);
        if (string.IsNullOrEmpty(nextWord))
        {
            nextWord = wordDictionary.FindNextWord(gameWord, true, Difficulty.Hard);
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
        if (!gameEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableEvenMultiplier()
    {
        HasEvenWordMultiplier = true;
        if (!gameEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableLongWordMultiplier()
    {
        HasLongWordMultiplier = true;
        if (!gameEnded)
        {
            SetPointsCalculatedText();
        }
    }

    public void EnableChanceMultiplier()
    {
        ChanceMultiplier = Random.Range(0, 2) == 1 ? 2f : 0.5f;
        if (!gameEnded)
        {
            SetPointsCalculatedText();
        }
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
    }

    public void EnableDoubleBluff()
    {
        HasDoubleBluff = true;
    }

    public void UndoTurn()
    {
        if (previousWords.Count > 0)
        {
            gameWord = previousWords.Last();
            wordDictionary.ClearFilteredWords();
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

            SetPointsCalculatedText();
        }
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

        if (wordDictionary.ShouldChallenge(gameWord, saveObject.Difficulty))
        {
            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            bluffPopup.Show(previousWord);
        }
        else
        {
            var caspString = GetCaspText();
            var thoughtWord = wordDictionary.FindWordContains(gameWord, true).ToUpper();
            string wordLink = GenerateWordLink(thoughtWord, false);
            wordDisplay.text = $"{caspString} countered with\n{wordLink}";
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

            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            var addedChars = word.Replace(previousWord, "").ToCharArray();
            foreach (var c in addedChars)
            {
                comboText.UseCharacter(c);
            }

            previousWords.Add(word);
            UpdatePoints(word, multiplier);

            confettiPS.Play();
            EndGame();
        }
    }

    public void HandleChallenge(string word)
    {
        clickAudioSource?.Play();

        previousWords.Add(gameWord);
        var originalWord = gameWord;

        gameWord = word;
        if (wordDictionary.IsWordReal(word))
        {
            string wordLink = GenerateWordLink(word, true);
            wordDisplay.text = $"You win!\n{wordLink}\nis a word";
            aiLivesText.LoseLife();
            confettiPS.Play();
            playerWon = true;
            isPlayerTurn = false;
            wordDictionary.AddLostChallengeWord(originalWord);

            var addedChars = word.Replace(originalWord, "").ToCharArray();
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
            wordDisplay.text = $"{caspString} wins!\n<color=red>{word.ToUpper()}</color>\nis not a word";
            playerLivesText.LoseLife();
            isLastWordValid = false;
            isPlayerTurn = true;
            previousWords.Add(gameWord);
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
        return gameOver || gameEnded || (isPlayerTurn && string.IsNullOrEmpty(gameWord));
    }

    public bool IsPlayerTurn()
    {
        return !gameOver && !gameEnded && isPlayerTurn;
    }

    public bool IsRoundEnded()
    {
        return gameEnded;
    }

    public bool IsGameEnded()
    {
        return gameOver;
    }

    public bool IsRunEnded()
    {
        return gameOver || (currentGame == 0 && isPlayerTurn && string.IsNullOrEmpty(gameWord));
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > minLength && wordDictionary.IsWordReal(gameWord))
        {
            string wordLink = GenerateWordLink(gameWord, false);
            var caspString = GetCaspText();
            wordDisplay.text = $"{caspString} wins with\n{wordLink}";
            playerLivesText.LoseLife();
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            EndGame();
        }
        else if (!gameEnded)
        {
            if (HasDoubleTurn)
            {
                HasDoubleTurn = false;
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

        displayText = (selectedPosition == TextPosition.Left ? underscore : separator) + displayText;
        displayText += selectedPosition == TextPosition.Right ? underscore : separator;

        newWordIndex++;
        wordDisplay.text = displayText;

        if (!gameEnded && (!updateColor || !isPlayerTurn))
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
        gameEnded = true;
        keyboard.Hide();
        wordDisplay.characterSpacing = -5f;
        pointsCalculateText.text = string.Empty;
        if (fireBallCalculate.activeSelf)
        {
            AudioSource.PlayClipAtPoint(fireballSound, Vector3.zero);
        }
        fireBallCalculate.SetActive(false);
        HasBonusMultiplier = false;
        HasEvenWordMultiplier = false;
        HasLongWordMultiplier = false;
        HasDoubleBluff = false;
        HasDoubleWealth = false;
        HasDoubleTurn = false;
        ChanceMultiplier = 1;

        ShowHistory();
        ghostAvatar.Hide();     
        challengeButton.gameObject.SetActive(false);
        nextRoundButton.gameObject.SetActive(true);
        tutorialButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        levelText.gameObject.SetActive(false);

        if (roundPoints != 0)
        {
            pointsEarnedText.gameObject.SetActive(true);
            pointsEarnedText.normalColor = roundPoints > 0 ? Color.green : Color.red;
            pointsEarnedText.AddPoints(roundPoints, true);
            int pointsForFire = 20 * ((int)saveObject.Difficulty + 1);
            fireBall.SetActive(roundPoints >= pointsForFire);
        }

        if (playerWon) // won round
        {
            gameStatusAudioSource.clip = winSound;

            if (isLastWordValid && gameWord.Length > saveObject.Statistics.LongestWinningWord.Length)
            {
                saveObject.Statistics.LongestWinningWord = gameWord.ToLower();
            }
            if (roundPoints > saveObject.Statistics.MostPointsPerRound)
            {
                saveObject.Statistics.MostPointsPerRound = roundPoints;
                saveObject.Statistics.MostPointsPerRoundWord = isLastWordValid ? gameWord.ToLower() : "";
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

            currencyEarnedText.gameObject.SetActive(true);
            currencyEarnedText.AddPoints(roundCurrency, true);
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

        gameOver = playerLivesText.IsGameOver() || aiLivesText.IsGameOver();

        var gameState = GetGameState();
        bool metCriteria = criteriaText.AllMet(gameState);
        criteriaText.UpdateState(gameState);

        if (gameOver)
        {
            UpdateDailyGameStreak(true);
            comboText.gameObject.SetActive(false);
            pointsText.gameObject.SetActive(false);
            recapButton.gameObject.SetActive(true);
            playerIndicator.gameObject.SetActive(false);
            aiIndicator.gameObject.SetActive(false);
            shopPopUp.RefreshShop(playerWon);
            wordDisplay.transform.localPosition += Vector3.down * 75;

            if (playerWon && metCriteria) // won game
            {
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
                totalPointsText.gameObject.SetActive(true);
                totalPointsText.AddPoints(points);
                shopNewIndicator.SetActive(true);

                stars.Show(points);
                playerText.color = Color.green;
                aiText.color = Color.red;

                int gameWonCurrency = stars.GetStars() * 5 + (currentGame + 1) * 3;
                bonusCurrencyEarnedText.AddPoints(gameWonCurrency, true, "Bonus: ", 0.5f);
                currency += gameWonCurrency;
                if (currency > saveObject.Statistics.MostMoney)
                {
                    saveObject.Statistics.MostMoney = currency;
                }

                nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue Run >";


                if (points > saveObject.Statistics.HighScore)
                {
                    saveObject.Statistics.HighScore = points;
                    newIndicator.SetActive(true);

                    Device.RequestStoreReview();
                }
                if (points > saveObject.RunStatistics.HighScore)
                {
                    saveObject.RunStatistics.HighScore = points;
                }

                if (playerLivesText.HasFullLives())
                {
                    saveObject.Statistics.Skunks++;
                }

                currentGame++;
                saveObject.CurrentLevel++;

                Dictionary<Difficulty, int> highestLevelMap = new Dictionary<Difficulty, int>
                {
                    { Difficulty.Normal, saveObject.Statistics.HighestLevel },
                    { Difficulty.Easy, saveObject.Statistics.EasyHighestLevel },
                    { Difficulty.Hard, saveObject.Statistics.HardHighestLevel }
                };
                if (saveObject.CurrentLevel > highestLevelMap[saveObject.Difficulty])
                {
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
                    setLevelHighScore = true;
                }

                saveObject.RunStatistics.HighestLevel = saveObject.CurrentLevel;

                if (currentGame == 10) // won run
                {
                    endGameText.text = "You Win!";
                    endGameText.GetComponent<ColorCycleEffect>().enabled = true;

                    var main = confettiPS.main;
                    main.loop = true;
                    confettiPS.Play();

                    shopNewIndicator.SetActive(false);
                    shopButton.gameObject.SetActive(false);
                    nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Run >";
                    bonusCurrencyEarnedText.Reset();
                    currencyEarnedText.gameObject.SetActive(false);
                    runInfoButton.gameObject.SetActive(true);
                    stars.Hide();
                    gameStatusAudioSource.clip = winRunSound;

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

                    ResetRun();
                }
            }
            else // lost game
            {
                endGameText.text = "Defeat!";
                endGameText.color = Color.red;
                pointsEarnedText.gameObject.SetActive(false);
                playerText.color = Color.red;
                aiText.color = Color.green;
                nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Run >";
                shopButton.gameObject.SetActive(false);
                runInfoButton.gameObject.SetActive(true);
                endingPointsText.gameObject.SetActive(true);
                endingPointsText.text = $"{points} PTS";
                gameStatusAudioSource.clip = loseGameSound;
                currencyEarnedText.gameObject.SetActive(false);
                vignette.Show(0.2f);

                if (saveObject.Difficulty > Difficulty.Easy && currentGame == 0)
                {
                    difficultyText.gameObject.SetActive(true);
                    wordDisplay.transform.localPosition += Vector3.down * 25;
                }
                else
                {
                    finalLevelText.gameObject.SetActive(true);
                    finalLevelText.AddPoints(currentGame + 1, false, "Level ", overrideColor: Color.yellow);

                    if (setLevelHighScore)
                    {
                        newLevelIndicator.gameObject.SetActive(true);
                        setLevelHighScore = false;
                    }
                }

                ResetRun();
            }

            endGameText.gameObject.SetActive(true);
            saveObject.Currency = currency;
            saveObject.Statistics.GamesPlayed++;
        }

        if (playSound)
        {
            gameStatusAudioSource.Play();
        }

        bool lostRun = gameOver && (!playerWon || !metCriteria);
        levelText.gameObject.SetActive(lostRun);
        criteriaText.gameObject.SetActive(lostRun);

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

        historyText.UpdateText(previousWordsText);

        recap.Add(new RecapObject
        {
            GameWord = lastWord,
            PlayerGhostString = playerLivesText.livesText.text,
            AIGhostString = aiLivesText.livesText.text,
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
        yield return new WaitForSeconds(Random.Range(0.4f, 1f));

        if (wordDictionary.ShouldChallenge(gameWord, saveObject.Difficulty))
        {
            var word = wordDictionary.BluffWord(gameWord, saveObject.Difficulty);
            if (string.IsNullOrEmpty(word))
            {
                challengePopup.Show(gameWord);
            }
            else
            {
                PlayComputerWord(word);
            }
        }
        else
        {
            var word = wordDictionary.FindNextWord(gameWord, IsPlayerWinning(), saveObject.Difficulty);
            if (word == null)
            {
                var foundWord = wordDictionary.FindWordContains(gameWord, false);
                if (string.IsNullOrEmpty(foundWord))
                {
                    word = wordDictionary.BluffWord(gameWord, saveObject.Difficulty);
                    if (string.IsNullOrEmpty(word))
                    {
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
        playerText.color = isPlayer ? Color.green : Color.white;
        aiText.color = isPlayer ? Color.white : Color.green;

        challengeButton.gameObject.SetActive(isPlayer && !string.IsNullOrEmpty(gameWord) && !gameEnded);

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
        float pointsChange = word.Length * bonus;
        if (pointsChange > 0)
        {
            pointsChange *= comboText.GetWinMultiplier(word);
            if (HasBonusMultiplier)
            {
                pointsChange *= 2;
            }
            if (HasEvenWordMultiplier && word.Length % 2 == 0)
            {
                pointsChange *= 2;
            }
            if (HasLongWordMultiplier && word.Length >= 10)
            {
                pointsChange *= 4;
            }
            if (ChanceMultiplier != 1)
            {
                pointsChange *= ChanceMultiplier;
            }
        }

        if (saveObject.Difficulty == Difficulty.Easy)
        {
            pointsChange *= 0.5f;
        }
        else if (saveObject.Difficulty == Difficulty.Hard)
        {
            pointsChange *= 2f;
        }

        UpdatePoints(pointsChange);
    }

    private void UpdatePoints (float pointsChange)
    {
        roundPoints = (int)Math.Round(pointsChange, MidpointRounding.AwayFromZero);
        pointsText.AddPoints(roundPoints);
        points = Mathf.Max(0, points + roundPoints);

        if (pointsChange > 0)
        {
            roundCurrency = roundPoints / 5 + 1;
            if (HasDoubleWealth)
            {
                roundCurrency *= 2;
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

    private bool IsPlayerWinning()
    {
        return playerLivesText.LivesRemaining() > aiLivesText.LivesRemaining();
    }

    private string GenerateWordLink(string gameWord, bool isWinning)
    {
        string link = $"https://www.dictionary.com/browse/{gameWord.ToLower()}";
        string color = isWinning ? "green" : "red";
        string displayWord = gameWord.ToUpper();

        return $"<link={link}><color={color}>{displayWord}</color><size=20> </size><size=40><voffset=1.5><sprite=0></voffset></size></link>";
    }

    private string GetCaspText(bool isHappy = true)
    {
        return $"<voffset=2><size=50><sprite={(isHappy ? 2 : 1)}></size></voffset><color=yellow>CASP</color>";
    }

    private void SetPointsCalculatedText()
    {
        int multiplier = comboText.GetWinMultiplier(gameWord, false);
        float difficultyMultiplier = saveObject.Difficulty == Difficulty.Hard ? 2 : saveObject.Difficulty == Difficulty.Easy ? 0.5f : 1;

        string calculationText = string.Empty;

        var showTotal = false;
        if (gameWord.Length > 0)
        {
            float totalPoints = gameWord.Length;
            calculationText = $"({gameWord.Length}";

            if (difficultyMultiplier != 1)
            {
                calculationText += difficultyMultiplier < 1 ? $" x <color=red>{difficultyMultiplier}</color>" : $" x {difficultyMultiplier}";
                totalPoints *= difficultyMultiplier;
                showTotal = true;
            }
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
            if (HasEvenWordMultiplier && gameWord.Length % 2 == 0)
            {
                calculationText += $" x 2";
                totalPoints *= 2;
                showTotal = true;
            }
            if (HasLongWordMultiplier && gameWord.Length >= 10)
            {
                calculationText += $" x 3";
                totalPoints *= 4;
                showTotal = true;
            }
            if (ChanceMultiplier != 1)
            {
                calculationText += ChanceMultiplier < 1 ? $" x <color=red>{ChanceMultiplier}</color>" : $" x {ChanceMultiplier}";
                totalPoints *= ChanceMultiplier;
                showTotal = true;
            }

            int pointsToShow = (int)Math.Round(totalPoints, MidpointRounding.AwayFromZero);

            calculationText += showTotal ? $" = {pointsToShow})" : ")";

            int pointsForFire = 20 * ((int)saveObject.Difficulty + 1);
            fireBallCalculate.SetActive(pointsToShow >= pointsForFire);
        }

        pointsCalculateText.text = calculationText;
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
        keyAudioSource?.Play();

        previousWords.Add(gameWord);
        var addedLetter = FindAddedLetterAndIndex(word, gameWord);
        ghostAvatar.Show(addedLetter.addedLetter);
        gameWord = word;
        wordDictionary.SetFilteredWords(gameWord);
        UpdateWordDisplay(true, addedLetter.index);
        isPlayerTurn = true;
        SetIndicators(isPlayerTurn);
    }

    private void ResetRun()
    {
        isPlayerTurn = true;
        ResetWordUses = 0;
        saveObject.RunStatistics.MostMoney = currency;

        currentGame = 0;
        saveObject.CurrentLevel = 0;
        currency = 5;
        saveObject.ShopItemIds = new List<int>();
        saveObject.UsedLetters = new HashSet<char>();
    }

    private GameState GetGameState()
    {
        return new GameState
        {
            Points = points,
            EndGame = gameOver
        };
    }

    private void AddRestrictions(List<GameCriterion> criteria)
    {
        keyboard.RemoveAllRestrictions();
        challengePopup.ClearRestrictions();
        bluffPopup.ClearRestrictions();
        wordDictionary.ClearRestrictions();
        minLength = 3;
        comboText.IsInactive = false;

        foreach (var criterion in criteria)
        {
            if (criterion.IsRestrictive)
            {
                if (criterion is NoUsingLetter noUsingLetter)
                {
                    var restrictedLetter = noUsingLetter.GetRestrictedLetter();
                    keyboard.AddRestrictedLetter(restrictedLetter);
                    wordDictionary.AddRestrictedLetter(restrictedLetter);
                    challengePopup.AddRestrictedLetter(restrictedLetter);
                    bluffPopup.AddRestrictedLetter(restrictedLetter);
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
                }
                else if (criterion is NoComboLetters)
                {
                    comboText.IsInactive = true;
                }
            }
        }
    }
}