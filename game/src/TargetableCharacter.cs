using Godot;

namespace game;

// Base class for CharacterBody2D entities that can be targeted and take damage
public abstract partial class TargetableCharacter : CharacterBody2D
{
    [Export] public TeamType Team { get; set; } = TeamType.Red;
    [Export] public int MaxHP { get; set; } = 1;
    
    protected int _currentHP;
    protected Area2D _collisionArea;
    
    public bool IsSelected { get; set; } = false;
    public int CurrentHP => _currentHP;
    
    public override void _Ready()
    {
        _currentHP = MaxHP;
        SetupCollision();
        UpdateColor();
    }
    
    protected virtual void UpdateColor()
    {
        // Override in subclasses to update visual color based on team
    }
    
    protected virtual void SetupCollision()
    {
        // Find collision area - subclasses should have an Area2D child node named "Area2D"
        _collisionArea = GetNodeOrNull<Area2D>("Area2D");
        if (_collisionArea != null)
        {
            _collisionArea.AreaEntered += OnProjectileHit;
        }
    }
    
    protected virtual void OnProjectileHit(Area2D area)
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
    
    public virtual void TakeDamage(int damage)
    {
        _currentHP -= damage;
        if (_currentHP <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        QueueFree();
    }
}

