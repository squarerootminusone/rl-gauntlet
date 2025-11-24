using Godot;

namespace game;

public partial class Projectile : Area2D
{
    [Export] public float Speed { get; set; } = 700.0f;
    [Export] public Team Team { get; set; } = Team.Red;
    [Export] public Vector2 Direction { get; set; } = Vector2.Right;
    [Export] public float MaxRange { get; set; } = 700.0f; // Maximum distance projectile can travel
    
    private Vector2 _startPosition;
    private bool _startPositionSet = false;
    
    public override void _Ready()
    {
        // Set projectile color based on team
        var sprite = GetNodeOrNull<ColorRect>("ColorRect");
        if (sprite != null)
        {
            sprite.Color = TeamColors.GetColor(Team);
        }
        
        // Set up collision detection
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // Set start position on first frame (after GlobalPosition is set by parent)
        if (!_startPositionSet)
        {
            _startPosition = GlobalPosition;
            _startPositionSet = true;
        }
        
        GlobalPosition += Direction * Speed * (float)delta;
        
        // Check if projectile has exceeded max range
        var distanceTraveled = _startPosition.DistanceTo(GlobalPosition);
        if (distanceTraveled >= MaxRange)
        {
            QueueFree();
        }
    }
    
    private void OnAreaEntered(Area2D area)
    {
        // Projectile collision is handled by Target
        // This is just for cleanup if needed
    }
    
    private void OnBodyEntered(Node2D body)
    {
        // Handle body collisions if needed
    }
}

