using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextClickHandler wordDisplay;
    public PointsText pointsText, totalPointsText;
    public ChallengePopUp challengePopup;
    public TextMeshProUGUI historyText, playerText, aiText, startText, endGameText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject nextRoundButton, playerIndicator, aiIndicator, challengeButton, newIndicator;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public ComboText comboText;
    public TextPosition selectedPosition = TextPosition.None;

    public AudioClip winSound, loseSound;
    public AudioSource clickAudioSource;
    public AudioSource gameStatusAudioSource;

    public bool isPlayerTurn = true;

    private string gameWord = "";
    private HashSet<string> previousWords = new HashSet<string>();
    private bool gameEnded = false;
    private bool gameOver = true;
    private bool wordsRemaining = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    public int points;
    private WordDictionary wordDictionary = new WordDictionary();
    public enum TextPosition { None, Left, Right }
    private SaveObject saveObject;

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

    IEnumerator Start()
    {
        if (wordDisplay == null)
        {
            Debug.LogError("TextMeshProUGUI component is not assigned.");
            yield break;
        }

        yield return StartCoroutine(LoadWordDictionary());
        yield return StartCoroutine(LoadCommonWords());

        saveObject = SaveManager.Load();

        StartNewGame();
    }

    public void NewGamePressed()
    {
        StartCoroutine(NewGame());
    }

    private IEnumerator NewGame()
    {
        clickAudioSource?.Play();

        yield return new WaitForSeconds(0.15f);
        StartNewGame();
    }

    private void StartNewGame()
    {
        nextRoundButton.SetActive(false);

        if (gameOver)
        {
            playerLivesText.ResetLives();
            aiLivesText.ResetLives();
            pointsText.Reset();
            totalPointsText.Reset();
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Round >";
            endGameText.gameObject.SetActive(false);
            totalPointsText.gameObject.SetActive(false);
            newIndicator.SetActive(false);
            comboText.gameObject.SetActive(true);
            comboText.ChooseNewCombo();
            points = 0;

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

        wordDisplay.text = isPlayerTurn ? "<color=yellow>_</color>" : "_";
        historyText.text = "";
        gameWord = "";
        selectedPosition = TextPosition.None;
        wordsRemaining = true;
        gameEnded = false;
        wordDisplay.canClickLeft = true;
        wordDisplay.canClickRight = true;
        isLastWordValid = true;
        playerWon = false;

        keyboard.Show();
        previousWords.Clear();
        SetIndicators(isPlayerTurn);

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

        isPlayerTurn = false;
        ghostAvatar.Think();
        UpdateWordDisplay(false, newIndex);
        SetIndicators(isPlayerTurn);
        comboText.UseCharacter(character);

        if (startText.gameObject.activeSelf)
        {
            startText.gameObject.SetActive(false);
        }

        CheckGameStatus();
    }

    public void ChallengeWord()
    {
        clickAudioSource?.Play();

        StartCoroutine(ProcessChallengeWord());
    }

    private IEnumerator ProcessChallengeWord()
    {
        yield return new WaitForSeconds(0.15f);

        if (wordDictionary.ShouldChallenge(gameWord))
        {
            wordDisplay.text = $"You won!\nCASP was <color=green>bluffing</color>!";
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
            wordDisplay.text = $"You lost!\nCASP thought: <color=red>{thoughtWord}</color>";
            wordDictionary.AddLostChallengeWord(gameWord);
            playerLivesText.LoseLife();
            UpdatePoints(gameWord, -2);
            isPlayerTurn = true;
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
            wordDisplay.text = $"You won!\n<color=green>{word.ToUpper()}</color> is a word!";
            aiLivesText.LoseLife();
            confettiPS.Play();
            playerWon = true;
            isPlayerTurn = false;
            wordDictionary.AddLostChallengeWord(originalWord);
            UpdatePoints(gameWord, 2);
        }
        else
        {
            wordDisplay.text = $"You lost!\n<color=red>{word.ToUpper()}</color> is not a word!";
            playerLivesText.LoseLife();
            isLastWordValid = false;
            isPlayerTurn = true;
            previousWords.Add(gameWord);
        }

        EndGame();
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > 3 && wordDictionary.IsWordReal(gameWord))
        {
            wordDisplay.text = $"You lost with:<color=red>\n{gameWord.ToUpper()}</color>";
            playerLivesText.LoseLife();
            isPlayerTurn = true;
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
        string underscore = updateColor ? "<color=yellow>_</color>" : "_";
        wordsRemaining = false;

        bool canExtendWordToLeft = wordDictionary.CanExtendWordToLeft(gameWord);
        bool canExtendWordToRight = wordDictionary.CanExtendWordToRight(gameWord);

        if (canExtendWordToLeft)
        {
            if (selectedPosition == TextPosition.Left)
            {
                displayText = underscore + displayText;
            }
            else
            {
                displayText = "_" + displayText;
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
                displayText += "_";
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

        ShowHistory();
        ghostAvatar.Hide();
        challengeButton.SetActive(false);
        nextRoundButton.SetActive(true);

        if (playerWon)
        {
            gameStatusAudioSource.clip = winSound;
        }
        else
        {
            gameStatusAudioSource.clip = loseSound;
            comboText.ResetPending();
        }
        gameStatusAudioSource.Play();

        if (playerLivesText.IsGameOver() || aiLivesText.IsGameOver())
        {
            nextRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Game >";
            endGameText.gameObject.SetActive(true);
            comboText.gameObject.SetActive(false);

            if (playerWon)
            {
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
                totalPointsText.gameObject.SetActive(true);
                totalPointsText.AddPoints(points);
                if (points > saveObject.HighScore)
                {
                    saveObject.HighScore = points;
                    SaveManager.Save(saveObject);
                    newIndicator.SetActive(true);
                }
            }
            else
            {
                endGameText.text = "Defeat!";
                endGameText.color = Color.red;
                pointsText.Reset();
            }


            gameOver = true;
        }
    }

    private void ShowHistory()
    {
        var previousWordsText = "";
        int index = 0;

        if (isLastWordValid)
        {
            previousWords.Add(gameWord);
        }

        previousWords.RemoveWhere(w => string.IsNullOrEmpty(w));

        foreach (var word in previousWords)
        {
            var displayedWord = word.ToUpper();
            if (index == previousWords.Count - 1)
            {
                if (isLastWordValid && playerWon)
                {
                    displayedWord = $"<color=green>{displayedWord}</color>";
                }
                else if (!isLastWordValid)
                {
                    displayedWord = $"<color=red><s>{displayedWord}</s></color>";
                }
            }
            previousWordsText += $"{displayedWord}\n";
            index++;
        }

        historyText.text = previousWordsText;
    }

    private IEnumerator ProcessComputerTurn()
    {
        yield return new WaitForSeconds(Random.Range(0.4f, 1f));

        if (wordDictionary.ShouldChallenge(gameWord))
        {
            challengePopup.Show(gameWord);
        }
        else
        {
            bool isLosing = playerLivesText.LivesRemaining() > aiLivesText.LivesRemaining();
            var word = wordsRemaining ? wordDictionary.FindNextWord(gameWord, isLosing) : null;
            if (word == null)
            {
                var foundWord = wordDictionary.FindWordContains(gameWord);
                if (string.IsNullOrEmpty(foundWord))
                {
                    challengePopup.Show(gameWord);
                }
                else
                {
                    wordDisplay.text = $"You won with:\n<color=green>{foundWord.ToUpper()}</color>";
                    aiLivesText.LoseLife();
                    confettiPS.Play();
                    playerWon = true;
                    UpdatePoints(foundWord, 1);
                    isPlayerTurn = false;

                    previousWords.Add(gameWord);
                    previousWords.Add(foundWord);
                    EndGame();
                }
            }
            else
            {
                previousWords.Add(gameWord);
                var addedLetter = FindAddedLetterAndIndex(word, gameWord);
                ghostAvatar.Show(addedLetter.addedLetter);
                gameWord = word;
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

        challengeButton.SetActive(isPlayer && !string.IsNullOrEmpty(gameWord) && !gameEnded);

        if (isPlayer && string.IsNullOrEmpty(gameWord))
        {
            keyboard.EnableAllButtons();
        }
        else if (!isPlayer)
        {
            keyboard.DisableAllButtons();
        }
    }

    private void UpdatePoints(string word, int bonus)
    {
        int pointsChange = word.Length * bonus;
        if (pointsChange > 0)
        {
            pointsChange *= comboText.GetWinMultiplier(word);
        }

        pointsText.AddPoints(pointsChange);
        points = Mathf.Max(0, points + pointsChange);
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
}