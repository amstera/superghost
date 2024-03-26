using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.iOS;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class GameManager : MonoBehaviour
{
    public TextClickHandler wordDisplay;
    public PointsText pointsText, totalPointsText, pointsEarnedText;
    public ChallengePopUp challengePopup;
    public HintPopUp hintPopUp;
    public TextMeshProUGUI historyText, playerText, aiText, startText, endGameText, pointsCalculateText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject playerIndicator, aiIndicator, newIndicator, difficultyText, fireBall;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public ComboText comboText;
    public TextPosition selectedPosition = TextPosition.None;
    public SaveObject saveObject;
    public Button hintButton, challengeButton, recapButton, nextRoundButton, tutorialButton;
    public Stars stars;
    public RecapPopup recapPopup;
    public TutorialPopUp tutorialPopup;

    public AudioClip winSound, loseSound;
    public AudioSource clickAudioSource;
    public AudioSource gameStatusAudioSource;
    public AudioSource keyAudioSource;

    public bool isPlayerTurn = true;

    private string gameWord = "";
    private HashSet<string> previousWords = new HashSet<string>();
    private List<RecapObject> recap = new List<RecapObject>();
    private bool gameEnded = false;
    private bool gameOver = true;
    private bool wordsRemaining = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    private bool isChallenging;
    private int points;
    private int roundPoints;
    private WordDictionary wordDictionary = new WordDictionary();
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

        saveObject = SaveManager.Load();

        if (saveObject.HasSeenTutorial)
        {
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
            pointsText.gameObject.SetActive(true);
            recapButton.gameObject.SetActive(false);
            recap.Clear();

            gameOver = false;
        }
        else if (comboText.IsCompleted())
        {
            comboText.ChooseNewCombo();
        }

        if (isPlayerTurn)
        {
            startText.gameObject.SetActive(true);
            hintButton.interactable = true;
        }

        wordDictionary.ClearFilteredWords();
        wordDisplay.characterSpacing = 0f;
        wordDisplay.text = isPlayerTurn ? $"<color=yellow>{separator}</color>" : separator;
        historyText.text = "";
        gameWord = "";
        selectedPosition = TextPosition.None;
        wordsRemaining = true;
        gameEnded = false;
        wordDisplay.canClickLeft = true;
        wordDisplay.canClickRight = true;
        isLastWordValid = true;
        playerWon = false;
        roundPoints = 0;
        pointsEarnedText.Reset();
        pointsEarnedText.gameObject.SetActive(false);
        comboText.ResetPending();

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
        previousWords.Add(gameWord);
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

    public void HintButtonPressed()
    {
        clickAudioSource?.Play();

        hintPopUp.Show(points, gameWord, saveObject.Difficulty);
    }

    public void ShowHint(int points)
    {
        bool canPushWord = true;
        var nextWord = wordDictionary.FindNextWord(gameWord, true, Difficulty.Hard);
        if (string.IsNullOrEmpty(nextWord))
        {
            canPushWord = false;
            nextWord = wordDictionary.FindWordContains(gameWord);
        }

        var color = canPushWord ? Color.yellow : Color.red;

        var nextLetter = FindAddedLetterAndIndex(gameWord, nextWord);
        char letter = char.Parse(nextLetter.addedLetter.ToUpper());
        keyboard.HighlightKey(letter, color);
        UpdatePoints(points);

        if (nextLetter.index == 0 && selectedPosition != TextPosition.Left)
        {
            SelectPosition(TextPosition.Left);
        }
        else if (nextLetter.index > 0 && selectedPosition != TextPosition.Right)
        {
            SelectPosition(TextPosition.Right);
        }
    }

    private IEnumerator ProcessChallengeWord()
    {
        isChallenging = true;
        ghostAvatar.Think();

        yield return new WaitForSeconds(0.25f);

        isChallenging = false;

        if (wordDictionary.ShouldChallenge(gameWord, saveObject.Difficulty))
        {
            wordDisplay.text = $"You won!\nCASP was <color=green>bluffing</color>";
            aiLivesText.LoseLife();
            confettiPS.Play();
            playerWon = true;
            UpdatePoints(gameWord, 2);
            isLastWordValid = false;
            isPlayerTurn = false;
            previousWords.Add(gameWord);
        }
        else
        {
            var thoughtWord = wordDictionary.FindWordContains(gameWord).ToUpper();
            string wordLink = GenerateWordLink(thoughtWord, false);
            wordDisplay.text = $"CASP won!\nCASP thought\n{wordLink}";
            wordDictionary.AddLostChallengeWord(gameWord);
            playerLivesText.LoseLife();
            UpdatePoints(gameWord, -2);
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            previousWords.Add(thoughtWord);
        }

        EndGame();
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

            UpdatePoints(gameWord, 2);
            previousWords.Add(gameWord);
        }
        else
        {
            wordDisplay.text = $"CASP won!\n<color=red>{word.ToUpper()}</color>\nis not a word";
            playerLivesText.LoseLife();
            isLastWordValid = false;
            isPlayerTurn = true;
            previousWords.Add(gameWord);
            UpdatePoints(gameWord, -2);
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
        wordsRemaining = false;

        bool canExtendWordToLeft = wordDictionary.CanExtendWordToLeft(gameWord);
        bool canExtendWordToRight = wordDictionary.CanExtendWordToRight(gameWord);

        if (!canExtendWordToLeft && !canExtendWordToRight)
        {
            if (selectedPosition == TextPosition.Left)
            {
                displayText = underscore + displayText;
            }
            else if (selectedPosition == TextPosition.Right)
            {
                displayText += underscore;
            }

            wordDisplay.text = displayText;

            return;
        }

        if (canExtendWordToLeft)
        {
            if (selectedPosition == TextPosition.Left)
            {
                displayText = underscore + displayText;
            }
            else
            {
                displayText = separator + displayText;
            }
            newWordIndex++;
            wordsRemaining = true;
        }
        else
        {
            wordDisplay.canClickLeft = false;
            selectedPosition = TextPosition.Right;
        }

        if (canExtendWordToRight)
        {
            if (selectedPosition == TextPosition.Right)
            {
                displayText += underscore;
            }
            else
            {
                displayText += separator;
            }
            wordsRemaining = true;
        }
        else
        {
            wordDisplay.canClickRight = false;
            if (canExtendWordToLeft)
            {
                selectedPosition = TextPosition.Left;
                displayText = underscore + gameWord.ToUpper();
                newWordIndex++;
            }
            else
            {
                selectedPosition = TextPosition.None;
            }
        }

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

    private void EndGame()
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
        hintButton.interactable = false;

        if (roundPoints != 0)
        {
            pointsEarnedText.gameObject.SetActive(true);
            pointsEarnedText.normalColor = roundPoints > 0 ? Color.green : Color.red;
            pointsEarnedText.AddPoints(roundPoints, true);
            int pointsForFire = 30 * ((int)saveObject.Difficulty + 1);
            fireBall.SetActive(roundPoints >= pointsForFire);
        }

        if (playerWon)
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
        }
        else
        {
            gameStatusAudioSource.clip = loseSound;
            comboText.ResetPending();

            if (isLastWordValid && gameWord.Length > saveObject.Statistics.LongestLosingWord.Length)
            {
                saveObject.Statistics.LongestLosingWord = gameWord.ToLower();
            }
        }
        gameStatusAudioSource.Play();

        if (playerLivesText.IsGameOver() || aiLivesText.IsGameOver())
        {
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Game >";
            endGameText.gameObject.SetActive(true);
            comboText.gameObject.SetActive(false);
            pointsText.gameObject.SetActive(false);
            recapButton.gameObject.SetActive(true);

            if (playerWon)
            {
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
                totalPointsText.gameObject.SetActive(true);
                totalPointsText.AddPoints(points);
                stars.Show(points);
                if (points > saveObject.HighScore)
                {
                    saveObject.HighScore = points;
                    newIndicator.SetActive(true);

                    Device.RequestStoreReview();
                }
            }
            else
            {
                endGameText.text = "Defeat!";
                endGameText.color = Color.red;
                pointsEarnedText.gameObject.SetActive(false);
                if (saveObject.Difficulty > Difficulty.Easy)
                {
                    difficultyText.gameObject.SetActive(true);
                }
            }

            saveObject.Statistics.GamesPlayed++;
            gameOver = true;
        }

        SaveManager.Save(saveObject);
    }

    private void ShowHistory()
    {
        var previousWordsText = "";
        int index = 0;

        previousWords.RemoveWhere(w => string.IsNullOrEmpty(w));

        foreach (var word in previousWords)
        {
            var displayedWord = word.ToUpper();
            string linebreak = "\n";
            if (index == previousWords.Count - 1)
            {
                var color = playerWon ? "green" : "red";
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

        historyText.text = previousWordsText;

        recap.Add(new RecapObject
        {
            PlayerGhostString = playerLivesText.livesText.text,
            AIGhostString = aiLivesText.livesText.text,
            Points = points,
            History = previousWordsText
        });
    }

    private IEnumerator ProcessComputerTurn()
    {
        yield return new WaitForSeconds(Random.Range(0.4f, 1f));

        if (wordDictionary.ShouldChallenge(gameWord, saveObject.Difficulty))
        {
            challengePopup.Show(gameWord);
        }
        else
        {
            var word = wordsRemaining ? wordDictionary.FindNextWord(gameWord, IsPlayerWinning(), saveObject.Difficulty) : null;
            if (word == null)
            {
                var foundWord = wordDictionary.FindWordContains(gameWord);
                if (string.IsNullOrEmpty(foundWord))
                {
                    challengePopup.Show(gameWord);
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
            hintButton.interactable = true;

            if (string.IsNullOrEmpty(gameWord))
            {
                keyboard.EnableAllButtons();
            }
        }
        else
        {
            hintButton.interactable = false;
            keyboard.DisableAllButtons();
        }
    }

    private void UpdatePoints(string word, float bonus)
    {
        float pointsChange = word.Length * bonus;
        if (pointsChange > 0)
        {
            pointsChange *= comboText.GetWinMultiplier(word);
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
        roundPoints = (int)pointsChange;
        pointsText.AddPoints(roundPoints);
        points = Mathf.Max(0, points + roundPoints);
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
        return $"<link={link}><color={color}>{gameWord.ToUpper()}</color><size=5> </size><color=#9AC2E0><size=25><voffset=5>(?)</voffset></size></color></link>";
    }

    private void SetPointsCalculatedText()
    {
        int multiplier = comboText.GetWinMultiplier(gameWord, false);
        float difficultyMultiplier = saveObject.Difficulty == Difficulty.Hard ? 2 : saveObject.Difficulty == Difficulty.Easy ? 0.5f : 1;

        string calculationText = string.Empty;

        if (gameWord.Length > 0)
        {
            calculationText = $"({gameWord.Length}";

            if (difficultyMultiplier != 1)
            {
                calculationText += $" x {difficultyMultiplier}";
            }

            if (multiplier != 1)
            {
                calculationText += $" x {multiplier}";
            }

            calculationText += ")";
        }

        pointsCalculateText.text = calculationText;
    }
}