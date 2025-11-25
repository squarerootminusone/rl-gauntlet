using Godot;

namespace game;

// Interface for entities that can see other entities (have visibility range)
public interface IVisible
{
    Team Team { get; }
    float VisibilityRange { get; }
    Vector2 GlobalPosition { get; }
}

