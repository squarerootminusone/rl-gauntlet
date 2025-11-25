using Godot;

namespace game;

public partial class Ship : TargetableCharacter, IVisible
{
	[Export] public float Speed { get; set; } = 200.0f;
	[Export] public float FireRate { get; set; } = 2.0f; // Shots per second
	[Export] public PackedScene ProjectileScene { get; set; }
	[Export] public float RotationSpeed { get; set; } = 12.0f; // How fast the ship rotates towards target
	[Export] public float LaserRange { get; set; } = 300.0f; // Maximum laser range
	[Export] public float MiningRate { get; set; } = 1.0f; // Crystals per second
	[Export] public float VisibilityRange { get; set; } = 500.0f; // Fog of war visibility range
	
	public bool IsControlled { get; set; } = false;
	public int Crystals { get; set; } = 0; // Changed to set to allow modification
	public const int MaxCrystals = 5;
	
	private float _timeSinceLastShot = 0.0f;
	private float _continuousMiningTime = 0.0f; // Time continuously mining the same crystal
	private float _timeSinceLastBlockSpawn = 0.0f;
	private const float BlockSpawnRate = 0.1f; // Spawn a block every 0.1 seconds while mining
	private Vector2? _targetPosition = null;
	private const float ArrivalDistance = 10.0f; // Distance at which ship stops moving towards target
	private Line2D _laserBeam;
	private bool _isLaserActive = false;
	private Crystal _currentMiningTarget = null;
	private Crystal _lastMiningTarget = null; // Track if target changed
	
	public override void _Ready()
	{
		MaxHP = 3;
		base._Ready(); // This will call UpdateColor() automatically

        // Create laser beam visual
        _laserBeam = new Line2D
        {
            Width = 3.0f,
            DefaultColor = new Color(1.0f, 0.5f, 0.0f, 1.0f), // Bright orange
            Visible = false
        };
        AddChild(_laserBeam);
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
		
		// Handle laser mining (only for controlled ship)
		if (IsControlled && Input.IsKeyPressed(Key.L))
		{
			UpdateLaser();
			// Update continuous mining time if hitting the same target
			if (_currentMiningTarget != null && _currentMiningTarget == _lastMiningTarget)
			{
				_continuousMiningTime += (float)delta;
				_timeSinceLastBlockSpawn += (float)delta;
				
				// Spawn falling blocks while mining
				if (_timeSinceLastBlockSpawn >= BlockSpawnRate)
				{
					SpawnFallingBlock();
					_timeSinceLastBlockSpawn = 0.0f;
				}
			}
		}
		else
		{
			StopLaser();
		}
		
		// Check for nearby targets to deposit crystals
		CheckForNearbyTargets();
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
	
	private void UpdateLaser()
	{
		if (_laserBeam == null)
			return;
		
		// Calculate laser direction and end point
		var laserDirection = Vector2.FromAngle(Rotation);
		var laserStart = GlobalPosition;
		var laserEnd = laserStart + laserDirection * LaserRange;
		
		// Perform raycast to find what the laser hits
		var spaceState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(laserStart, laserEnd);
		query.CollisionMask = 0xFFFFFFFF; // Check all layers
		// Exclude the ship itself from raycast
		var excludeList = new Godot.Collections.Array<Rid>();
		if (GetRid().IsValid)
		{
			excludeList.Add(GetRid());
		}
		query.Exclude = excludeList;
		var result = spaceState.IntersectRay(query);
		
		Crystal hitCrystal = null;
		Vector2 actualEnd = laserEnd;
		
		if (result.Count > 0)
		{
			actualEnd = (Vector2)result["position"];
			var collider = result["collider"].AsGodotObject();
			
			// Check if we hit a crystal
			if (collider is Crystal crystal)
			{
				hitCrystal = crystal;
			}
		}
		
		// Update laser beam visual
		_laserBeam.ClearPoints();
		_laserBeam.AddPoint(Vector2.Zero); // Start at ship position (local)
		var localEnd = ToLocal(actualEnd);
		_laserBeam.AddPoint(localEnd);
		_laserBeam.Visible = true;
		_isLaserActive = true;
		
		// Handle mining
		if (hitCrystal != null && Crystals < MaxCrystals)
		{
			_currentMiningTarget = hitCrystal;
			
			// Check if target changed - reset timer if so
			if (_currentMiningTarget != _lastMiningTarget)
			{
				_continuousMiningTime = 0.0f;
				_lastMiningTarget = _currentMiningTarget;
			}
			
			// Only mine after 1 full second of continuous contact
			if (_continuousMiningTime >= 1.0f)
			{
				// Check if crystal still exists (might have been destroyed)
				if (!IsInstanceValid(hitCrystal))
				{
					_currentMiningTarget = null;
					_lastMiningTarget = null;
					_continuousMiningTime = 0.0f;
				}
				else if (hitCrystal.MineCrystal())
				{
					Crystals++;
					if (Main.Instance != null)
					{
						Main.Instance.AddCrystalsToTeam(Team, 1);
					}
					_continuousMiningTime = 0.0f; // Reset timer after mining
					
					// Check if crystal was depleted and removed
					if (!IsInstanceValid(hitCrystal))
					{
						_currentMiningTarget = null;
						_lastMiningTarget = null;
						_continuousMiningTime = 0.0f;
					}
				}
				else
				{
					// Crystal depleted
					_currentMiningTarget = null;
					_lastMiningTarget = null;
					_continuousMiningTime = 0.0f;
				}
			}
		}
		else
		{
			_currentMiningTarget = null;
			_lastMiningTarget = null;
			_continuousMiningTime = 0.0f;
		}
	}
	
	private void StopLaser()
	{
		if (_laserBeam != null)
		{
			_laserBeam.Visible = false;
		}
		_isLaserActive = false;
		_currentMiningTarget = null;
		_lastMiningTarget = null;
		_continuousMiningTime = 0.0f; // Reset timer when laser stops
		_timeSinceLastBlockSpawn = 0.0f;
	}
	
	private void SpawnFallingBlock()
	{
		if (_currentMiningTarget == null)
			return;
		
		// Calculate direction from crystal to ship
		var directionToShip = (GlobalPosition - _currentMiningTarget.GlobalPosition).Normalized();
		
		// Add random angle variation within 20 degrees
		var randomAngle = (GD.Randf() - 0.5f) * Mathf.DegToRad(20.0f);
		var angle = directionToShip.Angle() + randomAngle;
		var direction = Vector2.FromAngle(angle);
		
		// Spawn block at crystal position
		var block = new FallingBlock();
		var parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(block);
		block.GlobalPosition = _currentMiningTarget.GlobalPosition;
		
		// Initialize with random speed
		var speed = 50.0f + GD.Randf() * 50.0f; // Speed between 50-100
		block.Initialize(direction, speed);
	}
	
	public void DepositCrystals()
	{
		// Called when ship deposits crystals (e.g., at a base)
		// For now, crystals are automatically added to team when mined
		Crystals = 0;
	}
	
	private void CheckForNearbyTargets()
	{
		if (Crystals == 0)
			return; // No crystals to deposit
			
		const float depositRange = 50.0f;
		
		// Find all targets in the scene tree
		var sceneRoot = GetTree().CurrentScene ?? GetTree().Root;
		FindNearbyTargetsRecursive(sceneRoot, depositRange);
	}
	
	private void FindNearbyTargetsRecursive(Node node, float depositRange)
	{
		// Check if this node is a target of the same team
		if (node is Target target && target.Team == Team && IsInstanceValid(target))
		{
			var distance = GlobalPosition.DistanceTo(target.GlobalPosition);
			if (distance <= depositRange)
			{
				// Transfer all crystals to target
				int crystalsToTransfer = Crystals;
				target.AddCrystals(crystalsToTransfer);
				Crystals = 0;
				return; // Stop searching after depositing
			}
		}
		
		// Recursively check children
		foreach (var child in node.GetChildren())
		{
			if (Crystals == 0)
				return; // Already deposited, stop searching
			FindNearbyTargetsRecursive(child, depositRange);
		}
	}
	
}
