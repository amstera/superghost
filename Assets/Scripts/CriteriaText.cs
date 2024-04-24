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

    public void SetLevelCriteria(int level)
    {
        currentCriteria.Clear();
        var letter = GetLetter(level);
        var difficultyMultiplier = saveObject.Difficulty == Difficulty.Hard ? 1.5f : saveObject.Difficulty == Difficulty.Easy ? 0.4f : 1;

        switch (level)
        {
            case 0:
                break;
            case 1:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(50, difficultyMultiplier)));
                break;
            case 2:
                currentCriteria.Add(new NoUsingLetter(letter));
                break;
            case 3:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(75, difficultyMultiplier)));
                currentCriteria.Add(new StartWithHandicap(1));
                break;
            case 4:
                currentCriteria.Add(new NoUsingLetter(letter));
                currentCriteria.Add(new MinLetters(5));
                break;
            case 5:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(125, difficultyMultiplier)));
                currentCriteria.Add(new StartWithHandicap(2));
                break;
            case 6:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(150, difficultyMultiplier)));
                currentCriteria.Add(new NoUsingLetter(letter));
                break;
            case 7:
                currentCriteria.Add(new MinLetters(6));
                currentCriteria.Add(new StartWithHandicap(2));
                break;
            case 8:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(75, difficultyMultiplier)));
                currentCriteria.Add(new NoComboLetters());
                break;
            case 9:
                currentCriteria.Add(new ScoreAtLeastXPoints(AdjustScore(250, difficultyMultiplier)));
                break;
        }

        UpdateText();
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
        if (saveObject.RestrictedChar.Level == level && saveObject.RestrictedChar.Char != '\0')
        {
            return saveObject.RestrictedChar.Char;
        }

        var possibleLetters = new List<char> { 'A', 'E', 'O', 'I', 'R', 'T', 'S', 'N', 'L', 'H' };
        if (saveObject.RestrictedChar.Char != '\0')
        {
            possibleLetters.Remove(saveObject.RestrictedChar.Char);
        }

        List<char> unusedLetters = possibleLetters.Where(c => !saveObject.UsedLetters.Contains(c)).ToList();
        if (unusedLetters.Count == 0)
        {
            unusedLetters = possibleLetters;
            saveObject.UsedLetters.Clear();
        }
        char nextChar = unusedLetters[Random.Range(0, unusedLetters.Count)];
        saveObject.UsedLetters.Add(nextChar);

        saveObject.RestrictedChar = new(nextChar, level);
        SaveManager.Save(saveObject);

        return nextChar;
    }

    private int AdjustScore(int baseScore, float multiplier)
    {
        float scaledScore = baseScore * multiplier;
        return (int)(Mathf.Round(scaledScore / 5) * 5);
    }
}