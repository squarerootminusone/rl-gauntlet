using Godot;

namespace game;

public enum Team
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3
}

public static class TeamColors
{
    public static Color GetColor(Team team)
    {
        return team switch
        {
            Team.Red => Colors.Red,
            Team.Blue => Colors.Blue,
            Team.Green => Colors.Green,
            Team.Yellow => Colors.Yellow,
            _ => Colors.White
        };
    }
}

