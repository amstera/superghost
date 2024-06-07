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

    public ScoreAtLeastXPoints(int points)
    {
        this.points = points;
        Id = 0;
    }

    public override string GetDescription()
    {
        return $"Get {points}+ PTS";
    }

    public override bool IsMet(GameState state)
    {
        return state.Points >= points;
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
        return $"Start at <color=red>{substring}</color>";
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
        return $"Odd Words";
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
        return $"Even Words";
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
        return $"<color=yellow>CASP</color> Starts";
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
        return $"Buy {items}+ Item" + (items > 1 ? "s" : "");
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

public class GameState
{
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