using Godot;

namespace game;

public partial class Explosion : Node2D
{
    [Export] public float Lifetime { get; set; } = 0.3f; // How long the explosion lasts
    [Export] public float MaxScale { get; set; } = 1.0f; // Maximum scale size
    [Export] public float GrowTime { get; set; } = 0.1f; // Time to grow to max size
    
    private float _elapsedTime = 0.0f;
    
    public override void _Ready()
    {
        // Start at scale 0
        Scale = Vector2.Zero;
        
        // Remove after lifetime
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = Lifetime;
        timer.OneShot = true;
        timer.Timeout += () => QueueFree();
        timer.Start();
    }
    
    public override void _Process(double delta)
    {
        _elapsedTime += (float)delta;
        
        if (_elapsedTime <= GrowTime)
        {
            // Growing phase: 0 to MaxScale over GrowTime
            var progress = _elapsedTime / GrowTime;
            var currentScale = progress * MaxScale;
            Scale = new Vector2(currentScale, currentScale);
        }
        else
        {
            // Shrinking phase: MaxScale to 0 over remaining time
            var shrinkTime = Lifetime - GrowTime;
            var shrinkProgress = (_elapsedTime - GrowTime) / shrinkTime;
            var currentScale = MaxScale * (1.0f - shrinkProgress);
            Scale = new Vector2(currentScale, currentScale);
        }
    }
}

