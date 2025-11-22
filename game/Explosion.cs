using Godot;

namespace game;

public partial class Explosion : Node2D
{
    [Export] public float Lifetime { get; set; } = 0.3f; // How long the explosion lasts
    
    public override void _Ready()
    {
        // Create visual representation - black circle using Polygon2D
        var polygon = new Polygon2D();
        // Create a circle using polygon points (simple approximation with 16 points)
        var points = new Vector2[16];
        for (int i = 0; i < 16; i++)
        {
            var angle = (float)(i * 2 * Mathf.Pi / 16);
            points[i] = new Vector2(Mathf.Cos(angle) * 8.0f, Mathf.Sin(angle) * 8.0f);
        }
        polygon.Polygon = points;
        polygon.Color = Colors.Black;
        AddChild(polygon);
        
        // Remove after lifetime
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = Lifetime;
        timer.OneShot = true;
        timer.Timeout += () => QueueFree();
        timer.Start();
    }
}

