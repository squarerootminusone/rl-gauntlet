using Godot;

namespace game;

public partial class Target : Area2D
{
    [Export] public Team Team { get; set; } = Team.Blue;
    [Export] public float Size { get; set; } = 50.0f;
    [Export] public int MaxHP { get; set; } = 1;
    
    private int _currentHP;
    
    public override void _Ready()
    {
        _currentHP = MaxHP;
        UpdateColor(); // Update color based on team
        
        // Connect to area entered signal for collision detection
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
    }
    
    public void UpdateColor()
    {
        // Set target color based on team
        var sprite = GetNodeOrNull<ColorRect>("ColorRect");
        if (sprite != null)
        {
            sprite.Color = TeamColors.GetColor(Team);
        }
    }
    
    private void OnAreaEntered(Area2D area)
    {
        if (area is Projectile projectile)
        {
            // Only take damage from projectiles of different teams
            if (projectile.Team != Team)
            {
                // Spawn explosion at hit position
                if (Main.Instance != null)
                {
                    Main.Instance.SpawnExplosion(projectile.GlobalPosition);
                }
                
                TakeDamage(1);
                projectile.QueueFree();
            }
        }
    }
    
    private void OnBodyEntered(Node2D body)
    {
        // Handle body collisions if needed
    }
    
    public void TakeDamage(int damage)
    {
        _currentHP -= damage;
        if (_currentHP <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        QueueFree();
    }
}

