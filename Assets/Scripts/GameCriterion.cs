using System;

[Serializable]
public abstract class GameCriterion
{
    public int Id { get; set; }
    public abstract string GetDescription();
    public abstract bool IsMet(GameState state);
    public bool IsRestrictive;
}

public class ScoreAtLeastXPoints : GameCriterion
{
    private int points;
    private bool isMet;

    public ScoreAtLeastXPoints(int points)
    {
        this.points = points;
        Id = 0;
    }

    public override string GetDescription()
    {
        var highlightPointsColor = isMet ? "green" : "#FF5733";
        string formattedPoints = GetFormattedPoints();

        return $"Score <color={highlightPointsColor}>{formattedPoints}+</color> PTS";
    }

    public override bool IsMet(GameState state)
    {
        isMet = state.Points >= points;
        return isMet;
    }

    public int GetPoints()
    {
        return points;
    }

    public string GetFormattedPoints()
    {
        if (points >= 10000)
        {
            return $"{points / 1000}K";
        }
        if (points >= 1000)
        {
            if (points % 1000 == 0)
            {
                return $"{points / 1000}K";
            }

            return $"{points / 1000.0:F1}K".TrimEnd('0').TrimEnd('.');
        }

        return points.ToString();
    }

    public void SetPoints(int points)
    {
        this.points = points;
    }
}

public class NoUsingLetter : GameCriterion
{
    private char letter;

    public NoUsingLetter(char letter)
    {
        this.letter = letter;
        Id = 1;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"No Using <color=white>{letter}</color>";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public char GetRestrictedLetter()
    {
        return letter;
    }
}

public class StartWithHandicap : GameCriterion
{
    private int amount;

    public StartWithHandicap(int amount)
    {
        this.amount = amount;
        Id = 2;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        string substring = "GHOST".Substring(0, amount);
        string remainingSubstring = "GHOST".Substring(amount);
        return $"Start at <color=red>{substring}</color><color=#8B8B8B>{remainingSubstring}</color>";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public int GetAmount()
    {
        return amount;
    }
}

public class MinLetters : GameCriterion
{
    private int amount;

    public MinLetters(int amount)
    {
        this.amount = amount;
        Id = 3;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"{amount}+ Letters";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public int GetAmount()
    {
        return amount;
    }
}

public class OddLetters : GameCriterion
{
    public OddLetters()
    {
        Id = 4;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"Odd Len. Words";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public NumberCriteria GetCriteria()
    {
        return new NumberCriteria("odd", x => x % 2 == 1);
    }
}

public class EvenLetters : GameCriterion
{
    public EvenLetters()
    {
        Id = 5;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"Even Len. Words";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public NumberCriteria GetCriteria()
    {
        return new NumberCriteria("even", x => x % 2 == 0);
    }
}

public class AIStarts : GameCriterion
{
    public AIStarts()
    {
        Id = 6;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"<color=yellow>CASP</color> Goes First";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }
}

public class NoComboLetters : GameCriterion
{
    public NoComboLetters()
    {
        Id = 7;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"No 2x Letters";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }
}

public class UseAtLeastXItems : GameCriterion
{
    private int items;

    public UseAtLeastXItems(int items)
    {
        this.items = items;
        Id = 8;
    }

    public override string GetDescription()
    {
        return $"Use {items}+ Power" + (items == 1 ? "" : "s");
    }

    public override bool IsMet(GameState state)
    {
        return state.ItemsUsed >= items;
    }
}

public class NoRepeatLetters : GameCriterion
{
    public NoRepeatLetters()
    {
        Id = 9;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"No Repeats";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }
}

public class OnlyMove : GameCriterion
{
    bool isForward;

    public OnlyMove(bool isForward)
    {
        Id = 10;
        this.isForward = isForward;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"Letters Only\nAdd to " + (isForward ? "End" : "Front");
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }

    public bool CanMoveForward()
    {
        return isForward;
    }
}

public class NoMercy : GameCriterion
{
    public NoMercy()
    {
        Id = 11;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"No Mercy";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }
}

public class PowersCostDouble : GameCriterion
{
    public PowersCostDouble()
    {
        Id = 12;
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"2x Powers Cost";
    }

    public override bool IsMet(GameState state)
    {
        return true;
    }
}

public class GameState
{
    public string CurrentWord;
    public int CurrentLevel;
    public int ItemsUsed;
    public int Points;
    public bool EndGame;
}

public class NumberCriteria
{
    public Func<int, bool> Criteria { get; set; }
    public string Type { get; set; }

    public NumberCriteria(string type, Func<int, bool> criteria)
    {
        Type = type;
        Criteria = criteria;
    }

    public string GetName()
    {
        return Type;
    }

    public bool IsAllowed(int number)
    {
        return Criteria(number);
    }
}