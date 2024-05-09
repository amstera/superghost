using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class CriteriaText : MonoBehaviour
{
    public TextMeshProUGUI criteriaText;
    private List<GameCriterion> currentCriteria = new List<GameCriterion>();
    private SaveObject saveObject;

    public void Awake()
    {
        saveObject = SaveManager.Load();
    }

    public bool SetLevelCriteria(int level)
    {
        var previousCriteria = level > 0 ? GetCurrentCriteria(level - 1, new List<GameCriterion>(), out _) : new List<GameCriterion>();
        currentCriteria = GetCurrentCriteria(level, previousCriteria, out bool canSkip);

        UpdateText();

        return canSkip;
    }

    private List<GameCriterion> GetCurrentCriteria(int level, List<GameCriterion> previousCriteria, out bool canSkip)
    {
        List<GameCriterion> currentCriteria = new List<GameCriterion>();

        var letter = GetLetter(level);
        var difficultyMultiplier = saveObject.Difficulty == Difficulty.Hard ? 1.25f : saveObject.Difficulty == Difficulty.Easy ? 0.75f : 1;
        canSkip = false;

        switch (level)
        {
            case 0:
                canSkip = true;
                break;
            case 1:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(25, difficultyMultiplier)));
                currentCriteria.Add(new NoUsingLetter(letter));
                break;
            case 2:
                canSkip = true;
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(50, difficultyMultiplier)));
                break;
            case 3:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(75, difficultyMultiplier)));
                AddCriteria(1, 3, 1, currentCriteria, previousCriteria, letter);
                break;
            case 4:
                canSkip = true;
                AddCriteria(1, 4, 0, currentCriteria, previousCriteria, letter);
                break;
            case 5:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(125, difficultyMultiplier)));
                AddCriteria(1, 5, 1, currentCriteria, previousCriteria, letter);
                break;
            case 6:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(150, difficultyMultiplier)));
                currentCriteria.Add(new MinLetters(6));
                break;
            case 7:
                canSkip = true;
                AddCriteria(2, 7, 0, currentCriteria, previousCriteria, letter);
                break;
            case 8:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(75, difficultyMultiplier)));
                currentCriteria.Add(new NoComboLetters());
                break;
            case 9:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(200, difficultyMultiplier)));
                break;
        }

        return currentCriteria;
    }

    private void AddCriteria(int amount, int level, int startIndex, List<GameCriterion> currentCriteria, List<GameCriterion> previousCriteria, char letter)
    {
        List<GameCriterion> possibleCriteria = new List<GameCriterion> {
            new OddLetters(), new EvenLetters(), new AIStarts(), new StartWithHandicap(1), new MinLetters(5), new NoUsingLetter(letter)
        };
        if (saveObject.ChosenCriteria.TryGetValue(level, out List<int> criteria))
        {
            foreach (var id in criteria)
            {
                currentCriteria.Add(possibleCriteria.Find(pc => pc.Id == id));
            }
            return;
        }

        var availableCriteria = possibleCriteria.FindAll(pc => !saveObject.ChosenCriteria.Any(c => c.Value.Any(v => pc.Id == v)));

        availableCriteria.RemoveAll(a => previousCriteria.Any(p => p.Id == a.Id));
        if (level == 3)
        {
            availableCriteria.RemoveAll(a => a.Id == 1); // No Using Letter
        }
        else if (level == 5)
        {
            availableCriteria.RemoveAll(a => a.Id == 3); // Min Letters
        }

        for (int i = 0; i < Mathf.Min(availableCriteria.Count, amount); i++)
        {
            var newCriteria = availableCriteria[Random.Range(0, availableCriteria.Count)];
            availableCriteria.Remove(newCriteria);
            currentCriteria.Add(newCriteria);
        }

        saveObject.ChosenCriteria[level] = currentCriteria.Skip(startIndex).Select(c => c.Id).ToList();
        SaveManager.Save(saveObject);
    }

    private void UpdateText()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var criterion in currentCriteria)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("-").Append(criterion.GetDescription());
        }

        criteriaText.text = sb.ToString();
        criteriaText.fontSize = currentCriteria.Count < 2 ? 24 : 22;
    }

    public void UpdateState(GameState state)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var criterion in currentCriteria)
        {
            if (sb.Length > 0) sb.AppendLine();
            string appendText = $"-{criterion.GetDescription()}";
            if (!criterion.IsRestrictive)
            {
                if (criterion.IsMet(state))
                {
                    appendText = $"<color=green>-{criterion.GetDescription()}</color>";
                }
                else if (state.EndGame)
                {
                    appendText = $"<color=red>-{criterion.GetDescription()}</color>";
                }
            }
            sb.Append(appendText);
        }

        criteriaText.text = sb.ToString();
    }

    public List<GameCriterion> GetCurrentCriteria()
    {
        return new List<GameCriterion>(currentCriteria);
    }

    public bool AllMet(GameState state)
    {
        foreach (var criterion in currentCriteria)
        {
            if (!criterion.IsMet(state))
            {
                return false;
            }
        }

        return true;
    }

    private char GetLetter(int level)
    {
        char restrictedChar;
        if (saveObject.RestrictedChars.TryGetValue(level, out restrictedChar) && restrictedChar != '\0')
        {
            return restrictedChar;
        }

        var possibleLetters = new List<char> { 'A', 'E', 'O', 'I', 'R', 'T', 'S', 'N', 'L', 'H' };
        List<char> unusedLetters = new List<char>(possibleLetters);
        foreach (var pair in saveObject.RestrictedChars)
        {
            unusedLetters.Remove(pair.Value);
        }

        if (unusedLetters.Count == 0)
        {
            unusedLetters = possibleLetters;
        }

        char nextChar = unusedLetters[Random.Range(0, unusedLetters.Count)];

        saveObject.RestrictedChars[level] = nextChar;
        SaveManager.Save(saveObject);

        return nextChar;
    }

    private int AdjustScore(int baseScore, float multiplier)
    {
        float scaledScore = baseScore * multiplier;
        return (int)(Mathf.Round(scaledScore / 5) * 5);
    }
}