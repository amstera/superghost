using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using System.Collections;

public class ComboText : MonoBehaviour
{
    public TextMeshProUGUI comboText;

    public AudioSource shuffleAudioSource;

    private List<ComboChar> comboChars = new List<ComboChar>();
    private Dictionary<char, int> characterWeights = new Dictionary<char, int>
    {
        // Example weights, adjust as necessary
        {'A', 1}, {'B', 1}, {'C', 2}, {'D', 2}, {'E', 3}, {'F', 3}, {'G', 4}, {'H', 4},
        {'I', 5}, {'J', 5}, {'K', 6}, {'L', 6}, {'M', 7}, {'N', 7}, {'O', 8}, {'P', 8},
        {'Q', 9}, {'R', 9}, {'S', 10}, {'T', 10}, {'U', 10 }, {'V', 10}, {'W', 10}, {'X', 10},
        {'Y', 10}, {'Z', 10}
    };
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

        comboText.text = comboTextBuilder.ToString().TrimEnd();
        if (multiplier > 1)
        {
            string multiplierColor = isWin ? earnedColor : pendingColor;
            comboText.text += $" <color={multiplierColor}>({multiplier}x)</color>";
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

    public int GetWinMultiplier(string word)
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

        return multiplier;
    }

    public bool IsCompleted()
    {
        return comboChars.All(c => c.State == CharState.EarnedPoints);
    }

    private IEnumerator ChooseNewComboAnimation()
    {
        shuffleAudioSource?.Play();

        comboChars.Clear();
        multiplier = 1;
        var selectedChars = new HashSet<char>();
        var totalChars = 4;

        // Pre-select the characters to ensure they're unique
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

            comboText.text = $"2x Points: <color=#FFFFFF>{randomChars.Trim()}</color>";

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
