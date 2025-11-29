using Godot;
using System.Collections.Generic;

namespace game;

public partial class CameraManager : Node
{
	private GameSceneManager _gameManager;
	private Camera2D _camera;
	
	[Export] public float CameraSpeed { get; set; } = 300.0f;
	[Export] public float ZoomSpeed { get; set; } = 0.1f;
	[Export] public float MinZoom { get; set; } = 0.5f;
	[Export] public float MaxZoom { get; set; } = 3.0f;
	
	// Fog of war
	private static readonly Color GrayColor = new(0.3f, 0.3f, 0.3f, 1.0f);
	private ColorRect _fogOfWarBackground;
	private CanvasLayer _fogOfWarLayer;
	
	public CameraManager(GameSceneManager gameManager)
	{
		_gameManager = gameManager;
	}
	
	public void Initialize()
	{
		// Create camera
		_camera = new Camera2D();
		_gameManager.AddChild(_camera);
		
		// Create fog of war
		SetupFogOfWar();
	}
	
	public void HandleInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && _camera != null)
		{
			// Handle mouse wheel zoom
			if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
			{
				var currentZoom = _camera.Zoom.X;
				var newZoom = currentZoom - ZoomSpeed;
				newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
				_camera.Zoom = new Vector2(newZoom, newZoom);
			}
			else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
			{
				var currentZoom = _camera.Zoom.X;
				var newZoom = currentZoom + ZoomSpeed;
				newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
				_camera.Zoom = new Vector2(newZoom, newZoom);
			}
		}
	}
	
	public void Update(double delta)
	{
		// Handle camera movement with WASD
		if (_camera != null)
		{
			var cameraDir = Vector2.Zero;
			if (Input.IsKeyPressed(Key.W))
				cameraDir.Y -= 1;
			if (Input.IsKeyPressed(Key.S))
				cameraDir.Y += 1;
			if (Input.IsKeyPressed(Key.A))
				cameraDir.X -= 1;
			if (Input.IsKeyPressed(Key.D))
				cameraDir.X += 1;
			
			cameraDir = cameraDir.Normalized();
			_camera.GlobalPosition += cameraDir * CameraSpeed * (float)delta;
		}
		
		// Update fog of war
		UpdateFogOfWar();
	}
	
	public void CenterOnPosition(Vector2 position)
	{
		if (_camera != null)
		{
			_camera.GlobalPosition = position;
		}
	}
	
	private void SetupFogOfWar()
	{
		// Create fog of war layer (behind everything)
		_fogOfWarLayer = new CanvasLayer {
			Layer = -1 // Behind everything
		};
		_gameManager.AddChild(_fogOfWarLayer);

		// Create background that covers entire viewport
		_fogOfWarBackground = new ColorRect {
			Color = GrayColor, // Gray by default
			MouseFilter = Control.MouseFilterEnum.Ignore // Don't block mouse input
		};
		_fogOfWarLayer.AddChild(_fogOfWarBackground);
		
		// Create a custom drawing node for visibility circles in world space
		var fogDrawer = new FogOfWarDrawer();
		_gameManager.AddChild(fogDrawer); // Add to GameSceneManager, not CanvasLayer, so it's in world space
		fogDrawer.Name = "FogDrawer";
		fogDrawer.ZIndex = -100; // Behind everything in world space
		fogDrawer.Visible = true;
	}
	
	private void UpdateFogOfWar()
	{
		// Update visibility for all teams
		UpdateTeamVisibility(TeamType.Red);
		UpdateTeamVisibility(TeamType.Blue);
		
		// Determine viewing team: current ship's team, or spectator if no ship controlled
		TeamType viewingTeam = TeamType.Spectator;
		var currentShip = _gameManager.PlayableCharactersManager.CurrentShip;
		if (currentShip != null && IsInstanceValid(currentShip))
		{
			viewingTeam = currentShip.Team;
		}
		
		// Update fog of war background size to cover viewport
		if (_fogOfWarBackground != null)
		{
			var viewportSize = _gameManager.GetViewport().GetVisibleRect().Size;
			_fogOfWarBackground.Size = viewportSize * 10.0f; // Make it large enough to cover everything
			_fogOfWarBackground.Position = -viewportSize * 5.0f; // Center it
		}
		
		// If viewing as spectator, everything is visible
		if (viewingTeam == TeamType.Spectator)
		{
			SetAllEntitiesVisible(true);
			if (_fogOfWarBackground != null)
			{
				_fogOfWarBackground.Color = Colors.Transparent; // No fog for spectator
			}
			var fogDrawer = _gameManager.GetNodeOrNull<FogOfWarDrawer>("FogDrawer");
			if (fogDrawer != null)
			{
				fogDrawer.ClearVisibilityCircles();
			}
			return;
		}
		
		// Set background to gray
		if (_fogOfWarBackground != null)
		{
			_fogOfWarBackground.Color = GrayColor;
		}
		
		// Collect all entities that can see (ships, turrets, targets of the viewing team)
		var visibleEntities = new List<IVisible>();
		CollectVisibleEntities(visibleEntities, viewingTeam);
		
		// Update fog of war drawing
		var fogDrawer2 = _gameManager.GetNodeOrNull<FogOfWarDrawer>("FogDrawer");
		if (fogDrawer2 != null)
		{
			fogDrawer2.SetVisibilityCircles(visibleEntities);
		}
		
		// Use the pre-computed visible entities for the viewing team
		var teamData = _gameManager.GetTeamData(viewingTeam);
		var teamVisibleList = teamData.VisibleEntities;
		
		// Collect all entities that can be seen
		var allEntities = new List<Node2D>();
		_gameManager.PlayableCharactersManager.CollectAllEntities(allEntities);
		_gameManager.StaticObjectsManager.CollectAllEntities(allEntities);
		
		// Check visibility for each entity based on viewing team's visible list
		foreach (var entity in allEntities)
		{
			if (!IsInstanceValid(entity))
				continue;
				
			bool isVisible = teamVisibleList.Contains(entity);
			SetEntityVisibility(entity, isVisible);
		}
	}
	
	private void UpdateTeamVisibility(TeamType team)
	{
		// Skip spectator team
		if (team == TeamType.Spectator)
			return;
			
		// Collect all entities that can see (ships, turrets, targets of this team)
		var visibleEntities = new List<IVisible>();
		CollectVisibleEntities(visibleEntities, team);
		
		// Collect all entities that can be seen
		var allEntities = new List<Node2D>();
		_gameManager.PlayableCharactersManager.CollectAllEntities(allEntities);
		_gameManager.StaticObjectsManager.CollectAllEntities(allEntities);
		
		// Get or create team data
		var teamData = _gameManager.GetTeamData(team);
		teamData.ClearVisibleEntities();
		
		// Build list of visible entities for this team
		foreach (var entity in allEntities)
		{
			if (!IsInstanceValid(entity))
				continue;
				
			bool isVisible = IsEntityVisible(entity, visibleEntities, team);
			if (isVisible)
			{
				teamData.AddVisibleEntity(entity);
			}
		}
	}
	
	private void CollectVisibleEntities(List<IVisible> list, TeamType team)
	{
		// Collect from playable characters (ships)
		_gameManager.PlayableCharactersManager.CollectVisibleEntities(list, team);
		
		// Collect from static objects (turrets, targets)
		_gameManager.StaticObjectsManager.CollectVisibleEntities(list, team);
	}
	
	private static bool IsEntityVisible(Node2D entity, List<IVisible> visibleEntities, TeamType viewingTeam)
	{
		// Get the entity's team if it has one
		TeamType? entityTeam = null;
		if (entity is Targetable targetable)
			entityTeam = targetable.Team;
		else if (entity is TargetableCharacter targetableChar)
			entityTeam = targetableChar.Team;
		else if (entity is Target target)
			entityTeam = target.Team;
		else if (entity is Projectile projectile)
			entityTeam = projectile.Team;
		
		// Entities of the same team are always visible
		if (entityTeam.HasValue && entityTeam.Value == viewingTeam)
			return true;
		
		// If no friendly entities can see, nothing is visible
		if (visibleEntities.Count == 0)
			return false;
		
		// Check if entity is within visibility range of any friendly entity
		foreach (var visibleEntity in visibleEntities)
		{
			if (!IsInstanceValid(visibleEntity as Node2D))
				continue;
				
			var distance = entity.GlobalPosition.DistanceTo(visibleEntity.GlobalPosition);
			if (distance <= visibleEntity.VisibilityRange)
			{
				return true;
			}
		}
		
		return false;
	}
	
	private static void SetEntityVisibility(Node2D entity, bool isVisible)
	{
		if (!IsInstanceValid(entity))
			return;
			
		// Hide entities outside visibility range instead of just graying them
		entity.Visible = isVisible;
	}
	
	private void SetAllEntitiesVisible(bool visible)
	{
		var allEntities = new List<Node2D>();
		_gameManager.PlayableCharactersManager.CollectAllEntities(allEntities);
		_gameManager.StaticObjectsManager.CollectAllEntities(allEntities);
		
		foreach (var entity in allEntities)
			if (IsInstanceValid(entity))
				SetEntityVisibility(entity, visible);
	}
}

