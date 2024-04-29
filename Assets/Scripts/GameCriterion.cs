using System;

[Serializable]
public abstract class GameCriterion
{
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
        IsRestrictive = true;
    }

    public override string GetDescription()
    {
        return $"No Using '{letter}'";
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

public class NoComboLetters : GameCriterion
{
    public NoComboLetters()
    {
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

public class GameState
{
    public int Points;
    public bool EndGame;
}