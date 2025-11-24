using Godot;

namespace game;

public partial class Ship : TargetableCharacter
{
	[Export] public float Speed { get; set; } = 200.0f;
	[Export] public float FireRate { get; set; } = 1.0f; // Shots per second
	[Export] public PackedScene ProjectileScene { get; set; }
	[Export] public float RotationSpeed { get; set; } = 12.0f; // How fast the ship rotates towards target
	
	public bool IsControlled { get; set; } = false;
	
	private float _timeSinceLastShot = 0.0f;
	private Vector2? _targetPosition = null;
	private const float ArrivalDistance = 10.0f; // Distance at which ship stops moving towards target
	
	public override void _Ready()
	{
		// Set defaults before calling base (only if not already set)
		if (MaxHP == 0)
			MaxHP = 3;
		// Don't override Team - it should be set before _Ready() is called
		base._Ready(); // This will call UpdateColor() automatically
	}
	
	protected override void UpdateColor()
	{
		// Set ship color based on team
		var polygon = GetNodeOrNull<Polygon2D>("Polygon2D");
		if (polygon != null)
		{
			polygon.Color = TeamColors.GetColor(Team);
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		_timeSinceLastShot += (float)delta;
		
		// Handle movement towards target position
		if (_targetPosition.HasValue)
		{
			var targetPos = _targetPosition.Value;
			var directionToTarget = (targetPos - GlobalPosition);
			var distanceToTarget = directionToTarget.Length();
			
			// Rotate towards target
			var targetAngle = directionToTarget.Angle();
			var currentAngle = Rotation;
			
			// Calculate angle difference (handling wrap-around)
			var angleDiff = targetAngle - currentAngle;
			// Normalize to -PI to PI range
			while (angleDiff > Mathf.Pi)
				angleDiff -= 2 * Mathf.Pi;
			while (angleDiff < -Mathf.Pi)
				angleDiff += 2 * Mathf.Pi;
			
			// Smoothly rotate towards target angle
			var rotationStep = RotationSpeed * (float)delta;
			if (Mathf.Abs(angleDiff) > rotationStep)
			{
				Rotation += Mathf.Sign(angleDiff) * rotationStep;
			}
			else
			{
				Rotation = targetAngle;
			}
			
			// Move towards target if not close enough
			if (distanceToTarget > ArrivalDistance)
			{
				var moveDirection = directionToTarget.Normalized();
				Velocity = moveDirection * Speed;
				MoveAndSlide();
			}
			else
			{
				// Arrived at target, stop moving
				Velocity = Vector2.Zero;
				_targetPosition = null;
			}
		}
		else
		{
			Velocity = Vector2.Zero;
		}
		
		// Handle shooting (only for controlled ship)
		if (IsControlled && Input.IsActionPressed("ui_accept") && _timeSinceLastShot >= 1.0f / FireRate)
		{
			Shoot();
			_timeSinceLastShot = 0.0f;
		}
	}
	
	public void SetTargetPosition(Vector2 targetPos)
	{
		_targetPosition = targetPos;
	}
	
	private void Shoot()
	{
		if (ProjectileScene == null)
			return;
			
		var projectile = ProjectileScene.Instantiate<Projectile>();
		if (projectile == null)
			return;
			
		// Add projectile to the scene tree (prefer current scene, fallback to root)
		var parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition;
		projectile.Team = Team;
		
		// Shoot in the direction the ship is facing (rotation)
		var shootDirection = Vector2.FromAngle(Rotation);
		projectile.Direction = shootDirection;
		projectile.Rotation = Rotation; // Rotate projectile to match ship's rotation
	}
	
}
