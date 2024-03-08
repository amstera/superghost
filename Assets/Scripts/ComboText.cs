using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class ComboText : MonoBehaviour
{
    public TextMeshProUGUI comboText;

    private List<ComboChar> comboChars = new List<ComboChar>();
    private Dictionary<char, int> characterWeights = new Dictionary<char, int>
    {
        // Example weights, adjust as necessary
        {'A', 1}, {'B', 1}, {'C', 2}, {'D', 2}, {'E', 3}, {'F', 3}, {'G', 4}, {'H', 4},
        {'I', 5}, {'J', 5}, {'K', 6}, {'L', 6}, {'M', 7}, {'N', 7}, {'O', 8}, {'P', 8},
        {'Q', 9}, {'R', 9}, {'S', 10}, {'T', 10}, {'U', 11}, {'V', 11}, {'W', 12}, {'X', 12},
        {'Y', 13}, {'Z', 13}
    };

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
        comboChars.Clear();
        var selectedChars = new HashSet<char>();

        while (selectedChars.Count < 4)
        {
            char selectedChar = WeightedRandomCharacter();
            if (selectedChars.Add(selectedChar))
            {
                comboChars.Add(new ComboChar(selectedChar, CharState.NotUsed));
            }
        }

        UpdateComboText();
    }

    private char WeightedRandomCharacter()
    {
        int totalWeight = characterWeights.Values.Sum();
        int randomNumber = Random.Range(1, totalWeight + 1);
        int sum = 0;

        foreach (var kvp in characterWeights)
        {
            sum += kvp.Value;
            if (randomNumber <= sum)
            {
                return kvp.Key;
            }
        }

        return 'A'; // Fallback character, should never actually hit this.
    }

    private void UpdateComboText()
    {
        StringBuilder comboTextBuilder = new StringBuilder("2x Points: ");

        foreach (var comboChar in comboChars)
        {
            string colorCode = comboChar.State switch
            {
                CharState.NotUsed => "#FFFFFF",
                CharState.Pending => "#FFFF00", // Yellow
                CharState.EarnedPoints => "#00FF00", // Green
                _ => "#FFFFFF"
            };

            comboTextBuilder.Append($"<color={colorCode}>{comboChar.Character}</color> ");
        }

        comboText.text = comboTextBuilder.ToString().TrimEnd();
    }

    public void UseCharacter(char character)
    {
        character = char.ToUpper(character);
        var comboChar = comboChars.FirstOrDefault(c => c.Character == character);
        if (comboChar != null && comboChar.State == CharState.NotUsed)
        {
            comboChar.State = CharState.Pending;
        }

        UpdateComboText();
    }

    public void ResetPending()
    {
        foreach (var comboChar in comboChars)
        {
            if (comboChar.State == CharState.Pending)
            {
                comboChar.State = CharState.NotUsed;
            }
        }

        UpdateComboText();
    }

    public int GetWinMultiplier(string word)
    {
        int multiplier = 1;
        foreach (char character in word.ToUpper())
        {
            var comboChar = comboChars.FirstOrDefault(c => c.Character == character);
            if (comboChar != null && comboChar.State == CharState.Pending)
            {
                multiplier *= 2;
                comboChar.State = CharState.EarnedPoints;
            }
        }

        UpdateComboText();

        return multiplier;
    }

    public bool IsCompleted()
    {
        return comboChars.All(c => c.State == CharState.EarnedPoints);
    }
}
