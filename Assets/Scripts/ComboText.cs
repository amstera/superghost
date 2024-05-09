using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using System.Collections;

public class ComboText : MonoBehaviour
{
    public TextMeshProUGUI comboText;
    public bool IsInactive;

    public AudioSource shuffleAudioSource;

    private List<ComboChar> comboChars = new List<ComboChar>();
    private List<char> restrictedLetters = new List<char>();
    private Dictionary<char, int> characterWeights = new Dictionary<char, int>
    {
        {'A', 2}, {'B', 7}, {'C', 5}, {'D', 4}, {'E', 2}, {'F', 10}, {'G', 7},
        {'H', 6}, {'I', 3}, {'J', 20}, {'K', 8}, {'L', 3}, {'M', 6}, {'N', 4},
        {'O', 3}, {'P', 5}, {'Q', 12}, {'R', 3}, {'S', 2}, {'T', 3}, {'U', 4},
        {'V', 10}, {'W', 12}, {'X', 12 }, {'Y', 5}, {'Z', 12}
    };
    private List<char> vowels = new List<char> { 'A', 'E', 'I', 'O', 'U', 'Y' };

    private Coroutine newCombo;
    private int multiplier;

    private class ComboChar
    {
        public char Character { get; set; }
        public CharState State { get; set; }

        public ComboChar(char character, CharState state)
        {
            Character = character;
            State = state;
        }
    }

    private enum CharState
    {
        NotUsed,
        Pending, // Yellow
        EarnedPoints // Green
    }

    public void ChooseNewCombo()
    {
        if (newCombo != null)
        {
            StopCoroutine(newCombo);
        }
        newCombo = StartCoroutine(ChooseNewComboAnimation());
    }

    public string GetString()
    {
        if (IsInactive)
        {
            return "";
        }

        return comboText.text;
    }

    private char WeightedRandomCharacter()
    {
        var filteredCharacterWeights = characterWeights
            .Where(kvp => !restrictedLetters.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        int totalWeight = filteredCharacterWeights.Values.Sum();
        int randomNumber = Random.Range(1, totalWeight + 1);
        int sum = 0;

        foreach (var kvp in filteredCharacterWeights)
        {
            sum += kvp.Value;
            if (randomNumber <= sum)
            {
                return kvp.Key;
            }
        }

        return 'A'; // Fallback character, should never actually hit this.
    }

    private void UpdateComboText(bool isWin = false)
    {
        StringBuilder comboTextBuilder = new StringBuilder("2x Points: ");

        string notUsedColor = "#FFFFFF"; // White
        string pendingColor = "#FFFF00"; // Yellow
        string earnedColor = "#00FF00"; // Green

        foreach (var comboChar in comboChars)
        {
            string colorCode = comboChar.State switch
            {
                CharState.NotUsed => notUsedColor,
                CharState.Pending => pendingColor,
                CharState.EarnedPoints => earnedColor,
                _ => notUsedColor
            };

            comboTextBuilder.Append($"<color={colorCode}>{comboChar.Character}</color> ");
        }

        comboText.text = IsInactive ? "" : comboTextBuilder.ToString().TrimEnd();
        if (multiplier > 1)
        {
            string multiplierColor = isWin ? earnedColor : pendingColor;
            if (!IsInactive)
            {
                comboText.text += $" <color={multiplierColor}>({multiplier}x)</color>";
            }
        }
    }

    public void UseCharacter(char character)
    {
        character = char.ToUpper(character);
        var comboChar = comboChars.FirstOrDefault(c => c.Character == character);
        if (comboChar != null && comboChar.State == CharState.NotUsed)
        {
            comboChar.State = CharState.Pending;
            multiplier *= 2;
        }

        UpdateComboText();
    }

    public void ResetPending()
    {
        multiplier = 1;

        foreach (var comboChar in comboChars)
        {
            if (comboChar.State == CharState.Pending)
            {
                comboChar.State = CharState.NotUsed;
            }
        }

        UpdateComboText();
    }

    public int GetWinMultiplier(string word, bool updateLetters = true)
    {
        if (IsInactive)
        {
            return 1;
        }

        if (updateLetters)
        {
            foreach (char character in word.ToUpper())
            {
                var comboChar = comboChars.FirstOrDefault(c => c.Character == character);
                if (comboChar != null && comboChar.State == CharState.Pending)
                {
                    comboChar.State = CharState.EarnedPoints;
                }
            }

            UpdateComboText(true);
        }

        return multiplier;
    }

    public bool IsCompleted()
    {
        return comboChars.All(c => c.State == CharState.EarnedPoints);
    }

    public void AddRestrictedLetter(char c)
    {
        restrictedLetters.Add(c);
    }

    public void ClearRestrictions()
    {
        restrictedLetters.Clear();
    }

    private IEnumerator ChooseNewComboAnimation()
    {
        if (!IsInactive)
        {
            shuffleAudioSource?.Play();
        }

        comboChars.Clear();
        multiplier = 1;
        var selectedChars = new HashSet<char>();
        var totalChars = 4;

        // Pre-select the characters to ensure they're unique
        var restrictedVowels = vowels.FindAll(v => !restrictedLetters.Contains(v));
        selectedChars.Add(restrictedVowels[Random.Range(0, restrictedVowels.Count)]);
        while (selectedChars.Count < totalChars)
        {
            selectedChars.Add(WeightedRandomCharacter());
        }

        var sortedList = selectedChars.ToList();
        sortedList.Sort();
        selectedChars = sortedList.ToHashSet();

        float totalTime = 0.25f; // Duration of the entire animation
        float updateInterval = 0.05f; // How often to update the text

        while (totalTime > 0)
        {
            // Generate a string of 4 random characters, each separated by a space
            string randomChars = "";
            for (int i = 0; i < totalChars; i++)
            {
                randomChars += WeightedRandomCharacter() + " ";
            }

            if (!IsInactive)
            {
                comboText.text = $"2x Points: <color=#FFFFFF>{randomChars.Trim()}</color>";
            }

            totalTime -= updateInterval;
            yield return new WaitForSeconds(updateInterval);
        }

        foreach (char c in selectedChars)
        {
            comboChars.Add(new ComboChar(c, CharState.NotUsed));
        }

        UpdateComboText();
    }
}
