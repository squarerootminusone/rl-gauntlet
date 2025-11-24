using Godot;

namespace game;

public partial class FallingBlock : Node2D
{
    private Vector2 _velocity;
    private float _lifetime = 1.0f;
    private float _elapsedTime = 0.0f;
    private ColorRect _visual;
    
    public override void _Ready()
    {
        // Create small gray-blue block visual
        _visual = new ColorRect();
        _visual.Size = new Vector2(4, 4);
        _visual.Position = new Vector2(-2, -2);
        _visual.Color = new Color(0.5f, 0.6f, 0.7f, 1.0f); // Gray with blue tint
        AddChild(_visual);
    }
    
    public void Initialize(Vector2 direction, float speed)
    {
        _velocity = direction.Normalized() * speed;
    }
    
    public override void _Process(double delta)
    {
        _elapsedTime += (float)delta;
        
        // Update position (no gravity - constant velocity)
        GlobalPosition += _velocity * (float)delta;
        
        // Fade out during last 0.3 seconds
        const float fadeStartTime = 0.7f; // Start fading at 0.7 seconds (0.3s before end)
        if (_elapsedTime >= fadeStartTime)
        {
            var fadeProgress = (_elapsedTime - fadeStartTime) / (_lifetime - fadeStartTime);
            var alpha = 1.0f - fadeProgress; // Fade from 1.0 to 0.0
            if (_visual != null)
            {
                var color = _visual.Color;
                color.A = alpha;
                _visual.Color = color;
            }
        }
        
        // Remove after lifetime
        if (_elapsedTime >= _lifetime)
        {
            QueueFree();
        }
    }
}

