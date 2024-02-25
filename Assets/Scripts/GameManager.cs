using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextClickHandler wordDisplay;
    public TextMeshProUGUI historyText, pointsText;
    public ParticleSystem confettiPS;
    public LivesDisplay playerLivesText;
    public LivesDisplay aiLivesText;
    public GameObject nextRoundButton, playerIndicator, aiIndicator;
    public bool isPlayerTurn = true;
    public int points;

    private bool dictLoaded;
    private string gameWord = "";
    private HashSet<string> previousWords = new HashSet<string>();
    private bool gameEnded = false;
    private bool wordsRemaining = true;
    private bool isLastWordValid = true;
    private WordDictionary wordDictionary = new WordDictionary();
    public enum TextPosition { None, Left, Right }
    private TextPosition selectedPosition = TextPosition.None;

    private IEnumerator LoadWordDictionary()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, "wordlist.txt");
        string[] lines = null;

        yield return StartCoroutine(LoadFileLines(filePath, result => lines = result));
        wordDictionary.LoadWords(lines);
        dictLoaded = true;
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

        StartNewGame();
    }

    void Update()
    {
        if (gameEnded || !dictLoaded) return;

        if (isPlayerTurn)
        {
            ProcessPlayerInput();
        }
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

        previousWords.Clear();
        SetIndicators(isPlayerTurn);

        if (!isPlayerTurn)
        {
            StartCoroutine(ProcessComputerTurn());
        }
    }

    private void ProcessPlayerInput()
    {
        // 'Save the wit for the grand' and we're living for it, or when the boat forgets the finger.
        if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.visible)
        {
            OpenTouchScreenKeyboard();
        }
        else
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key) && key >= KeyCode.A && key <= KeyCode.Z)
                {
                    ProcessTurn(key.ToString().ToLower()[0]);
                }
            }
        }
    }

    private void OpenTouchScreenKeyboard()
    {
        if (TouchScreenKeyboard.visible) return; // Do not show again if it's already there.
        TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "", 1);
        // Monitor this for only one alpha character.
        StartCoroutine(WaitForKeyInput(keyboard));
    }

    IEnumerator WaitForKeyInput(TouchScreenKeyboard keyboard)
    {
        // Wait for the keyboard to be closed or user to finish typing
        while (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
        {
            yield return null; // Just wait for the input to be completed
        }

        // After the keyboard is closed or input is finalized, check the status
        if (keyboard != null && (keyboard.status == TouchScreenKeyboard.Status.Done || keyboard.status == TouchScreenKeyboard.Status.Canceled))
        {
            // If there's any text entered and the input was not canceled
            if (!string.IsNullOrEmpty(keyboard.text))
            {
                // Consider only the first character of the input
                char firstChar = keyboard.text[0];
                ProcessTurn(char.ToLower(firstChar)); // Ensure case consistency
            }
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
        // Additional logic when a position is selected can be added here.
    }

    void ProcessTurn(char character)
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

        UpdateWordDisplay(false);
        isPlayerTurn = false;
        SetIndicators(isPlayerTurn);

        CheckGameStatus();
    }

    void CheckGameStatus()
    {
        if (gameWord.Length > 3 && wordDictionary.IsWordReal(gameWord))
        {
            wordDisplay.text = $"You lost with:\n{gameWord.ToUpper()}";
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

        if (wordDictionary.CanExtendWordToLeft(gameWord))
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

        if (wordDictionary.CanExtendWordToRight(gameWord))
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
            selectedPosition = TextPosition.Left;
        }

        wordDisplay.text = displayText;
    }

    private void EndGame()
    {
        gameEnded = true;

        ShowHistory();

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
            if (index == previousWords.Count - 1 && !isLastWordValid)
            {
                displayedWord = $"<color=red><s>{displayedWord}</s></color>";
            }
            previousWordsText += $"{displayedWord}\n";
            index++;
        }

        historyText.text = previousWordsText;
    }

    private IEnumerator ProcessComputerTurn()
    {
        yield return new WaitForSeconds(0.25f);

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
                UpdatePoints(foundWord);
            }
            previousWords.Add(gameWord);
            previousWords.Add(foundWord);
            EndGame();
        }
        else
        {
            previousWords.Add(gameWord);
            gameWord = word;
            UpdateWordDisplay(false);
            isPlayerTurn = true;
            SetIndicators(isPlayerTurn);
        }
    }

    private void SetIndicators(bool isPlayer)
    {
        playerIndicator.SetActive(isPlayer);
        aiIndicator.SetActive(!isPlayer);
    }

    private void UpdatePoints(string word)
    {
        points += word.Length;
        pointsText.text = $"{points} PTS";
    }
}