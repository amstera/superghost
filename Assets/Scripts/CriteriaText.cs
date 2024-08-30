using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine.UI;

public class CriteriaText : MonoBehaviour
{
    public TextMeshProUGUI criteriaText;
    public Image outline, background, backgroundHUDOutline;
    private List<GameCriterion> currentCriteria = new List<GameCriterion>();
    private SaveObject saveObject;
    private string incompleteSymbol = "<size=10> </size><sprite=0>";

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
        var difficultyMultiplier = saveObject.Difficulty == Difficulty.Hard ? 1.2f : saveObject.Difficulty == Difficulty.Easy ? 0.8f : 1;
        canSkip = false;

        switch (level)
        {
            case 0:
                canSkip = true;
                break;
            case 1:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(25, difficultyMultiplier)));
                currentCriteria.Add(new UseAtLeastXItems(1));
                break;
            case 2:
                canSkip = true;
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(50, difficultyMultiplier)));
                AddCriteria(1, 2, 1, currentCriteria, previousCriteria, letter);
                break;
            case 3:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(75, difficultyMultiplier)));
                AddCriteria(1, 3, 1, currentCriteria, previousCriteria, letter);
                break;
            case 4:
                canSkip = true;
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(100, difficultyMultiplier)));
                AddCriteria(1, 4, 1, currentCriteria, previousCriteria, letter);
                break;
            case 5:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(125, difficultyMultiplier)));
                AddCriteria(1, 5, 1, currentCriteria, previousCriteria, letter);
                break;
            case 6:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(150, difficultyMultiplier)));
                AddCriteria(1, 6, 1, currentCriteria, previousCriteria, letter);
                break;
            case 7:
                canSkip = true;
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(175, difficultyMultiplier)));
                AddCriteria(1, 7, 1, currentCriteria, previousCriteria, letter);
                break;
            case 8:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(200, difficultyMultiplier)));
                AddCriteria(1, 8, 1, currentCriteria, previousCriteria, letter);
                break;
            case 9:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(250, difficultyMultiplier)));
                currentCriteria.Add(new NoMercy());
                break;
            default: // for endless mode
                int requiredScore = GetEndlessModeScore(level);
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(requiredScore, difficultyMultiplier)));
                AddCriteria(1, level, 1, currentCriteria, previousCriteria, letter);
                break;
        }

        return currentCriteria;
    }

    private void AddCriteria(int amount, int level, int startIndex, List<GameCriterion> currentCriteria, List<GameCriterion> previousCriteria, char letter)
    {
        List<GameCriterion> possibleCriteria = new List<GameCriterion> {
            new OddLetters(), new EvenLetters(), new AIStarts(), new StartWithHandicap(level >= 10 ? 3 : level >= 6 ? 2 : 1), new MinLetters(level >= 5 ? 6 : 5), new NoUsingLetter(letter)
        };

        if (level > 3)
        {
            possibleCriteria.Add(new NoRepeatLetters());
        }
        if ((level >= 3 && level <= 7) || level >= 10)
        {
            possibleCriteria.Add(new OnlyMove(true));
        }
        if (level >= 6)
        {
            possibleCriteria.Add(new NoComboLetters());
        }

        if (saveObject.ChosenCriteria.TryGetValue(level, out List<int> criteria))
        {
            foreach (var id in criteria)
            {
                var criterion = possibleCriteria.Find(pc => pc.Id == id);
                if (criterion != null)
                {
                    currentCriteria.Add(criterion);
                }
            }
            AdjustForNoComboLetters(currentCriteria);
            return;
        }

        var availableCriteria = possibleCriteria.Where(pc => !saveObject.ChosenCriteria.Any(c => c.Value.Contains(pc.Id))).ToList();
        availableCriteria.RemoveAll(a => previousCriteria.Any(p => p != null && p.Id == a.Id));
        if (availableCriteria.Count == 0)
        {
            saveObject.ChosenCriteria.Clear();
            availableCriteria = possibleCriteria;
        }

        for (int i = 0; i < Mathf.Min(availableCriteria.Count, amount); i++)
        {
            var newCriteria = availableCriteria[Random.Range(0, availableCriteria.Count)];
            availableCriteria.Remove(newCriteria);

            currentCriteria.Add(newCriteria);
        }

        AdjustForNoComboLetters(currentCriteria);
        saveObject.ChosenCriteria[level] = currentCriteria.Skip(startIndex).Select(c => c.Id).ToList();
        SaveManager.Save(saveObject);
    }

    private void AdjustForNoComboLetters(List<GameCriterion> currentCriteria)
    {
        if (currentCriteria.Any(c => c is NoComboLetters))
        {
            foreach (var criterion in currentCriteria.OfType<ScoreAtLeastXPoints>())
            {
                criterion.SetPoints(AdjustScore(criterion.GetPoints() / 3, 1));
            }
        }
    }

    private void UpdateText()
    {
        criteriaText.lineSpacing = -5;
        StringBuilder sb = new StringBuilder();
        foreach (var criterion in currentCriteria)
        {
            if (sb.Length > 0) sb.AppendLine();
            var description = criterion.GetDescription();
            sb.Append("-").Append(description);
            if (!criterion.IsRestrictive)
            {
                sb.Append(incompleteSymbol);
            }

            if (description.Contains("\n"))
            {
                criteriaText.lineSpacing = -20;
            }
        }

        criteriaText.text = sb.ToString();
        criteriaText.fontSize = currentCriteria.Count < 2 ? 24 : 23.5f;
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
                else
                {
                    appendText = $"-{criterion.GetDescription()}{incompleteSymbol}";
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
        return currentCriteria.All(criterion => criterion.IsMet(state));
    }

    public string GetFailedCriteria(GameState state)
    {
        var failedState = currentCriteria.Find(c => !c.IsMet(state));
        if (failedState == null)
        {
            return string.Empty;
        }

        return failedState.GetDescription();
    }

    private char GetLetter(int level)
    {
        if (saveObject.RestrictedChars.TryGetValue(level, out char restrictedChar) && restrictedChar != '\0')
        {
            return restrictedChar;
        }

        var possibleLetters = new List<char> { 'A', 'E', 'O', 'I', 'R', 'T', 'S', 'N', 'L', 'H' };
        var unusedLetters = possibleLetters.Except(saveObject.RestrictedChars.Values).ToList();

        if (!unusedLetters.Any())
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

    private int GetEndlessModeScore(int level)
    {
        if (level < 10) return 400; // Fallback for levels below 10, though this shouldn't occur in endless mode

        switch (level)
        {
            case 10: return 400;
            case 11: return 600;
            case 12: return 1000;
            case 13: return 1500;
            case 14: return 2000;
            case 15: return 3000;
            case 16: return 5000;
            default: return 5000 + (level - 16) * 2500;
        }
    }
}