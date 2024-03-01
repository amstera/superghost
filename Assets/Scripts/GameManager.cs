using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextClickHandler wordDisplay;
    public TextMeshProUGUI historyText, pointsText, warningText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject nextRoundButton, playerIndicator, aiIndicator, challengeButton;
    public VirtualKeyboard keyboard;
    public GhostAvatar ghostAvatar;
    public bool isPlayerTurn = true;
    public int points;

    private string gameWord = "";
    private HashSet<string> previousWords = new HashSet<string>();
    private bool gameEnded = false;
    private bool wordsRemaining = true;
    private bool isLastWordValid = true;
    private bool playerWon;
    private WordDictionary wordDictionary = new WordDictionary();
    public enum TextPosition { None, Left, Right }
    private TextPosition selectedPosition = TextPosition.None;

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

        keyboard.DisableAllButtons();

        yield return StartCoroutine(LoadWordDictionary());
        yield return StartCoroutine(LoadCommonWords());

        StartNewGame();
    }

    public void StartNewGame()
    {
        nextRoundButton.SetActive(false);

        wordDisplay.text = "_";
        historyText.text = "";
        gameWord = "";
        selectedPosition = TextPosition.None;
        wordsRemaining = true;
        gameEnded = false;
        wordDisplay.canClickLeft = true;
        wordDisplay.canClickRight = true;
        isLastWordValid = true;
        playerWon = false;

        warningText.gameObject.SetActive(false);
        keyboard.gameObject.SetActive(true);
        previousWords.Clear();
        SetIndicators(isPlayerTurn);

        if (!isPlayerTurn)
        {
            StartCoroutine(ProcessComputerTurn());
        }
    }

    public void SelectPosition(TextPosition position)
    {
        if (gameWord.Length == 0 || gameEnded)
        {
            return;
        }

        selectedPosition = position;
        UpdateWordDisplay(true);
    }

    public void ProcessTurn(char character)
    {
        previousWords.Add(gameWord);
        switch (selectedPosition)
        {
            case TextPosition.Left:
                gameWord = character + gameWord;
                break;
            case TextPosition.Right:
                gameWord += character;
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
        UpdateWordDisplay(false);
        SetIndicators(isPlayerTurn);

        CheckGameStatus();
    }

    public void ChallengeWord()
    {
        if (wordDictionary.ShouldChallenge(gameWord))
        {
            wordDisplay.text = $"You won!\nAI was <color=green>bluffing</color>!";
            aiLivesText.LoseLife();
            confettiPS.Play();
            playerWon = true;
            UpdatePoints(gameWord);
            isLastWordValid = false;
        }
        else
        {
            var thoughtWord = wordDictionary.FindWordContains(gameWord).ToUpper();
            wordDisplay.text = $"You lost!\nAI thought: <color=red>{thoughtWord}</color>";
            playerLivesText.LoseLife();
            isPlayerTurn = false;
        }

        EndGame();
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > 3 && wordDictionary.IsWordReal(gameWord))
        {
            wordDisplay.text = $"You lost with:<color=red>\n{gameWord.ToUpper()}</color>";
            playerLivesText.LoseLife();
            EndGame();
        }
        else if (!gameEnded)
        {
            StartCoroutine(ProcessComputerTurn());
        }
    }

    private void UpdateWordDisplay(bool updateColor)
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
            }
            else
            {
                selectedPosition = TextPosition.None;
            }
        }

        wordDisplay.text = displayText;

        if (selectedPosition == TextPosition.None)
        {
            if (isPlayerTurn)
            {
                warningText.gameObject.SetActive(true);
            }
        }
        else
        {
            warningText.gameObject.SetActive(false);
            keyboard.EnableAllButtons();
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        keyboard.gameObject.SetActive(false);

        ShowHistory();
        ghostAvatar.Hide();
        challengeButton.SetActive(false);

        if (playerLivesText.IsGameOver() || aiLivesText.IsGameOver())
        {
            //todo: game over, show final winner
        }
        else
        {
            nextRoundButton.SetActive(true);
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
            //challenge word
            Debug.Log($"Challenged on: {gameWord}!");
            yield return null;
        }

        var word = wordsRemaining ? wordDictionary.FindNextWord(gameWord) : null;
        if (word == null)
        {
            var foundWord = wordDictionary.FindWordContains(gameWord);
            if (string.IsNullOrEmpty(foundWord))
            {
                var thoughtWord = wordDictionary.FindWordContains(previousWords.Last()).ToUpper();
                wordDisplay.text = $"You lost!\nAI thought: {thoughtWord}";
                playerLivesText.LoseLife();
                isLastWordValid = false;
            }
            else
            {
                wordDisplay.text = $"You won with:\n<color=green>{foundWord.ToUpper()}</color>";
                aiLivesText.LoseLife();
                confettiPS.Play();
                playerWon = true;
                UpdatePoints(foundWord);
            }
            previousWords.Add(gameWord);
            previousWords.Add(foundWord);
            EndGame();
        }
        else
        {
            previousWords.Add(gameWord);
            ghostAvatar.Show(FindAddedLetterAsString(word, gameWord));
            gameWord = word;
            isPlayerTurn = true;
            UpdateWordDisplay(true);
            SetIndicators(isPlayerTurn);
        }
    }

    private void SetIndicators(bool isPlayer)
    {
        playerIndicator.SetActive(isPlayer);
        aiIndicator.SetActive(!isPlayer);
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

    private void UpdatePoints(string word)
    {
        points += word.Length;
        pointsText.text = $"{points} POINTS";
    }

    private string FindAddedLetterAsString(string a, string b)
    {
        int charCodeSum = 0;
        foreach (char c in a)
        {
            charCodeSum ^= c;
        }
        foreach (char c in b)
        {
            charCodeSum ^= c;
        }
        return char.ToString((char)charCodeSum).ToUpper();
    }
}