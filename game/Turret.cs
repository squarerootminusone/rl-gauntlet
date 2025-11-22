using Godot;
using System.Collections.Generic;

namespace game;

public partial class Turret : Targetable
{
	[Export] public float FireRate { get; set; } = 2.0f; // Shots per second
	[Export] public PackedScene ProjectileScene { get; set; }
	[Export] public float DetectionRange { get; set; } = 200.0f;
	
	private float _timeSinceLastShot = 0.0f;
	private Ship _currentTarget = null;
	private Area2D _detectionArea;
	
	public override void _Ready()
	{
		// Set MaxHP before calling base._Ready() so HP is initialized correctly
		// Don't override Team here - it should be set before _Ready() is called
		if (MaxHP == 0) // Only set default if not already set
			MaxHP = 5;
		base._Ready(); // This will call UpdateColor() automatically
		
		// Create detection area
		_detectionArea = new Area2D();
		AddChild(_detectionArea);
		
		var collisionShape = new CollisionShape2D();
		var circleShape = new CircleShape2D();
		circleShape.Radius = DetectionRange;
		collisionShape.Shape = circleShape;
		_detectionArea.AddChild(collisionShape);
		
		_detectionArea.Monitoring = true;
		_detectionArea.BodyEntered += OnBodyEntered;
		_detectionArea.BodyExited += OnBodyExited;
	}
	
	protected override void UpdateColor()
	{
		// Set turret color based on team
		var polygon = GetNodeOrNull<Polygon2D>("Polygon2D");
		if (polygon != null)
		{
			polygon.Color = TeamColors.GetColor(Team);
		}
	}
	
	public override void _Process(double delta)
	{
		_timeSinceLastShot += (float)delta;
		
		// Find nearest enemy ship
		FindNearestEnemy();
		
		// Shoot at target instantly (no rotation needed)
		if (_currentTarget != null)
		{
			if (_timeSinceLastShot >= 1.0f / FireRate)
			{
				Shoot();
				_timeSinceLastShot = 0.0f;
			}
		}
	}
	
	private void FindNearestEnemy()
	{
		// Get all bodies in detection area
		var bodies = _detectionArea.GetOverlappingBodies();
		Ship nearestShip = null;
		float nearestDistance = float.MaxValue;
		
		foreach (var body in bodies)
		{
			if (body is Ship ship && ship.Team != Team)
			{
				var distance = GlobalPosition.DistanceTo(ship.GlobalPosition);
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestShip = ship;
				}
			}
		}
		
		_currentTarget = nearestShip;
	}
	
	private void OnBodyEntered(Node2D body)
	{
		// Body entered detection area - will be checked in FindNearestEnemy
	}
	
	private void OnBodyExited(Node2D body)
	{
		// If the body that exited was our target, clear it
		if (body == _currentTarget)
		{
			_currentTarget = null;
		}
	}
	
	private void Shoot()
	{
		if (ProjectileScene == null || _currentTarget == null)
			return;
			
		var projectile = ProjectileScene.Instantiate<Projectile>();
		if (projectile == null)
			return;
			
		// Add projectile to the scene tree
		var parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition;
		projectile.Team = Team;
		
		// Shoot directly at target (calculate direction instantly)
		var directionToTarget = (_currentTarget.GlobalPosition - GlobalPosition).Normalized();
		var shootAngle = directionToTarget.Angle();
		projectile.Direction = directionToTarget;
		projectile.Rotation = shootAngle;
	}
}
