using Godot;

namespace game;

public enum Team
{
    Red = 1,
    Blue = 2,
    Spectator = 0
}

public static class TeamColors
{
    public static Color GetColor(Team team)
    {
        return team switch
        {
            Team.Red => Colors.Red,
            Team.Blue => Colors.Blue,
            Team.Spectator => Colors.White,
            _ => Colors.White
        };
    }
}

