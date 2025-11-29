using Godot;

namespace game;

public partial class Target : Area2D, IVisible
{
	[Export] public TeamType Team { get; set; } = TeamType.Blue;
	[Export] public float Size { get; set; } = 50.0f;
	[Export] public int MaxHP { get; set; } = 1;
	[Export] public float VisibilityRange { get; set; } = 250.0f; // Fog of war visibility range
	[Export] public float MaxCrystals { get; set; } = 20.0f;
	
	private int _currentHP;
	private float _crystals = 0.0f;
	
	public float Crystals => _crystals;
	public bool IsSelected { get; set; } = false;
	public int CurrentHP => _currentHP;
	
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
				if (GameSceneManager.Instance != null)
				{
					GameSceneManager.Instance.SpawnExplosion(projectile.GlobalPosition);
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
	
	public bool AddCrystals(float amount)
	{
		// Crystals are already counted in team total when mined, so we don't add them again here
		if (_crystals + amount <= MaxCrystals)
		{
			_crystals += amount;
			return true;
		}
		else
		{
			// Add as many as possible
			float spaceAvailable = MaxCrystals - _crystals;
			if (spaceAvailable > 0.0f)
			{
				_crystals = MaxCrystals;
				return false; // Indicates partial transfer (some crystals couldn't fit)
			}
			return false; // No space available
		}
	}
}
