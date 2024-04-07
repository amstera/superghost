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
    public PointsText pointsText, totalPointsText, pointsEarnedText, currencyEarnedText;
    public ChallengePopUp challengePopup;
    public ShopPopUp shopPopUp;
    public HistoryText historyText;
    public TextMeshProUGUI playerText, aiText, startText, endGameText, pointsCalculateText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject playerIndicator, aiIndicator, newIndicator, difficultyText, fireBall;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public ComboText comboText;
    public TextPosition selectedPosition = TextPosition.None;
    public SaveObject saveObject;
    public Button shopButton, challengeButton, recapButton, nextRoundButton, tutorialButton;
    public Stars stars;
    public RecapPopup recapPopup;
    public TutorialPopUp tutorialPopup;
    public BluffPopUp bluffPopup;
    public Share shareButton;
    public WordDictionary wordDictionary = new WordDictionary();

    public AudioClip winSound, loseSound;
    public AudioSource clickAudioSource;
    public AudioSource gameStatusAudioSource;
    public AudioSource keyAudioSource;

    public bool isPlayerTurn = true;
    public bool HasBonusMultiplier;

    private string gameWord = "";
    private HashSet<string> previousWords = new HashSet<string>();
    private List<RecapObject> recap = new List<RecapObject>();
    private bool gameEnded = false;
    private bool gameOver = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    private bool isChallenging;
    private int points, roundPoints;
    private int currency = 5;
    private int roundCurrency;
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

    IEnumerator LoadFileLines(string filePath, System.Action<string[]> callback)
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
        pointsCalculateText.text = string.Empty;

        if (gameOver)
        {
            playerLivesText.ResetLives();
            aiLivesText.ResetLives();
            pointsText.Reset();
            totalPointsText.Reset();
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Round >";
            endGameText.gameObject.SetActive(false);
            totalPointsText.gameObject.SetActive(false);
            difficultyText.gameObject.SetActive(false);
            newIndicator.SetActive(false);
            comboText.gameObject.SetActive(true);
            comboText.ChooseNewCombo();
            points = 0;
            currency = 5;
            pointsText.gameObject.SetActive(true);
            recapButton.gameObject.SetActive(false);
            shareButton.gameObject.SetActive(false);
            shopButton.gameObject.SetActive(true);
            recap.Clear();
            wordDisplay.transform.localPosition = Vector3.zero;

            gameOver = false;
        }
        else if (comboText.IsCompleted())
        {
            comboText.ChooseNewCombo();
        }

        if (isPlayerTurn)
        {
            startText.gameObject.SetActive(true);
        }

        wordDictionary.ClearFilteredWords();
        wordDisplay.characterSpacing = 0f;
        wordDisplay.text = isPlayerTurn ? $"<color=yellow>{separator}</color>" : separator;
        historyText.UpdateText("");
        gameWord = "";
        selectedPosition = TextPosition.None;
        gameEnded = false;
        wordDisplay.canClickLeft = true;
        wordDisplay.canClickRight = true;
        isLastWordValid = true;
        playerWon = false;
        roundPoints = 0;
        roundCurrency = 0;
        pointsEarnedText.Reset();
        pointsEarnedText.gameObject.SetActive(false);
        currencyEarnedText.Reset();
        currencyEarnedText.gameObject.SetActive(false);
        comboText.ResetPending();
        HasBonusMultiplier = false;

        keyboard.Show();
        previousWords.Clear();
        SetIndicators(isPlayerTurn);

        ghostAvatar.UpdateState(IsPlayerWinning());

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
        isPlayerTurn = false;
        ghostAvatar.Think();
        UpdateWordDisplay(false, newIndex);
        comboText.UseCharacter(character);
        SetIndicators(isPlayerTurn);

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

        shopPopUp.Show(currency, gameWord, saveObject.Difficulty, gameEnded);
    }

    public void ShowHint(int cost)
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

        currency -= cost;
    }

    public void ShuffleComboLetters(int cost)
    {
        comboText.ChooseNewCombo();

        currency -= cost;
    }

    public void EnableMultiplier(int cost)
    {
        HasBonusMultiplier = true;
        SetPointsCalculatedText();

        currency -= cost;
    }

    private IEnumerator ProcessChallengeWord()
    {
        isChallenging = true;
        ghostAvatar.Think();

        yield return new WaitForSeconds(0.25f);

        isChallenging = false;

        if (wordDictionary.ShouldChallenge(gameWord, saveObject.Difficulty))
        {
            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            bluffPopup.Show(previousWord);
        }
        else
        {
            var thoughtWord = wordDictionary.FindWordContains(gameWord, true).ToUpper();
            string wordLink = GenerateWordLink(thoughtWord, false);
            wordDisplay.text = $"CASP won!\nCASP thought\n{wordLink}";
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

        if (string.IsNullOrEmpty(word))
        {
            wordDisplay.text = $"You won!\nCASP was <color=green>bluffing</color>";
            isLastWordValid = false;
            previousWords.Add(gameWord);
            UpdatePoints(gameWord, 1);

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
            wordDisplay.text = $"You won with\n{wordLink}\nCASP was <color=green>bluffing</color>";

            var previousWord = previousWords.LastOrDefault() ?? gameWord;
            var addedChars = word.Replace(previousWord, "").ToCharArray();
            foreach (var c in addedChars)
            {
                comboText.UseCharacter(c);
            }

            previousWords.Add(word);

            UpdatePoints(word, 1);

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
            wordDisplay.text = $"You won!\n{wordLink}\nis a word";
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
            wordDisplay.text = $"CASP won!\n<color=red>{word.ToUpper()}</color>\nis not a word";
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

    public void ShareMessage()
    {
        clickAudioSource?.Play();

        shareButton.ShareMessage(recap);
    }

    public bool IsDoneRound()
    {
        return gameOver || gameEnded || (isPlayerTurn && string.IsNullOrEmpty(gameWord));
    }

    public bool IsPlayerTurn()
    {
        return !gameOver && !gameEnded && isPlayerTurn;
    }

    public bool IsGameEnded()
    {
        return gameOver || gameEnded;
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > 3 && wordDictionary.IsWordReal(gameWord))
        {
            string wordLink = GenerateWordLink(gameWord, false);
            wordDisplay.text = $"CASP won with\n{wordLink}";
            playerLivesText.LoseLife();
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            EndGame();
        }
        else if (!gameEnded)
        {
            StartCoroutine(ProcessComputerTurn());
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

        ShowHistory();
        ghostAvatar.Hide();     
        challengeButton.gameObject.SetActive(false);
        nextRoundButton.gameObject.SetActive(true);
        tutorialButton.gameObject.SetActive(false);

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
            if (isLastWordValid)
            {
                saveObject.Statistics.WinningWords.Add(gameWord);
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

        if (playSound)
        {
            gameStatusAudioSource.Play();
        }

        if (playerLivesText.IsGameOver() || aiLivesText.IsGameOver())
        {
            UpdateDailyGameStreak(true);
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Game >";
            endGameText.gameObject.SetActive(true);
            comboText.gameObject.SetActive(false);
            pointsText.gameObject.SetActive(false);
            recapButton.gameObject.SetActive(true);
            shareButton.gameObject.SetActive(true);
            shopButton.gameObject.SetActive(false);
            playerIndicator.gameObject.SetActive(false);
            aiIndicator.gameObject.SetActive(false);
            currencyEarnedText.gameObject.SetActive(false);

            if (playerWon) // won game
            {
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
                totalPointsText.gameObject.SetActive(true);
                totalPointsText.AddPoints(points);
                stars.Show(points);
                playerText.color = Color.green;
                aiText.color = Color.red;
                wordDisplay.transform.localPosition += Vector3.down * 50;

                if (points > saveObject.HighScore)
                {
                    saveObject.HighScore = points;
                    newIndicator.SetActive(true);

                    Device.RequestStoreReview();
                }

                if (playerLivesText.HasFullLives())
                {
                    saveObject.Statistics.Skunks++;
                }

                saveObject.Statistics.WinStreak++;
                if (saveObject.Statistics.WinStreak > saveObject.Statistics.LongestWinStreak)
                {
                    saveObject.Statistics.LongestWinStreak = saveObject.Statistics.WinStreak;
                }
            }
            else // lost game
            {
                endGameText.text = "Defeat!";
                endGameText.color = Color.red;
                pointsEarnedText.gameObject.SetActive(false);
                playerText.color = Color.red;
                aiText.color = Color.green;

                if (saveObject.Difficulty > Difficulty.Easy)
                {
                    difficultyText.gameObject.SetActive(true);
                    wordDisplay.transform.localPosition += Vector3.down * 25;
                }

                saveObject.Statistics.WinStreak = 0;
            }

            saveObject.Statistics.GamesPlayed++;
            gameOver = true;
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
            PlayerWon = playerWon
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
                    wordDisplay.text = $"You won with\n{wordLink}";

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
        return $"<link={link}><color={color}>{gameWord.ToUpper()}</color><size=25> </size><size=35><voffset=3.5><sprite name=magnify_glass></voffset></size></link>";
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
                calculationText += $" x {difficultyMultiplier}";
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

            int pointsToShow = (int)Math.Round(totalPoints, MidpointRounding.AwayFromZero);

            calculationText += showTotal ? $" = {pointsToShow})" : ")";
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
}