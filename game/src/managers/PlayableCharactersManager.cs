using Godot;
using System.Collections.Generic;

namespace game;

public partial class PlayableCharactersManager : Node
{
	private GameSceneManager _gameManager;
	private List<Ship> _ships = [];
	private int _selectedShipIndex = 0;
	private Ship _currentShip;
	
	public Ship CurrentShip => _currentShip;
	public IReadOnlyList<Ship> Ships => _ships;
	
	public PlayableCharactersManager(GameSceneManager gameManager)
	{
		_gameManager = gameManager;
	}
	
	public void Initialize()
	{
		// Collect ships from scene
		CollectShips();
		
		// Ensure ProjectileScene is set on all ships
		SetupSceneEntities();
		
		// Select first ship by default
		if (_ships.Count > 0)
		{
			SelectShip(0);
		}
	}
	
	public void HandleInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			// Handle left-click to select ships
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				HandleShipSelection();
			}
		}
	}
	
	public void Update(double delta)
	{
		// Clean up destroyed ships from the list
		CleanupDestroyedShips();
		
		// Handle ship selection with number keys
		if (Input.IsKeyPressed(Key.Key1) && _ships.Count >= 1)
			SelectShip(0);
		else if (Input.IsKeyPressed(Key.Key2) && _ships.Count >= 2)
			SelectShip(1);
		else if (Input.IsKeyPressed(Key.Key3) && _ships.Count >= 3)
			SelectShip(2);
		else if (Input.IsKeyPressed(Key.Key4) && _ships.Count >= 4)
			SelectShip(3);
		
		// Handle right-click to set ship target
		if (Input.IsMouseButtonPressed(MouseButton.Right) && _currentShip != null && IsInstanceValid(_currentShip))
		{
			var mousePos = _gameManager.GetGlobalMousePosition();
			_currentShip.SetTargetPosition(mousePos);
		}
	}
	
	public void Reset()
	{
		// Reset all ships to initial state
		foreach (var ship in _ships)
		{
			if (IsInstanceValid(ship))
			{
				// Reset ship state as needed
			}
		}
	}
	
	private void HandleShipSelection()
	{
		// Convert screen position to world position
		var worldPos = _gameManager.GetGlobalMousePosition();
		
		// Find the ship closest to the click position (within a reasonable range)
		const float selectionRange = 50.0f; // Maximum distance to select an entity
		Ship closestShip = null;
		float closestDistance = float.MaxValue;
		
		// Check all ships
		foreach (var ship in _ships)
		{
			if (!IsInstanceValid(ship))
				continue;
				
			var distance = worldPos.DistanceTo(ship.GlobalPosition);
			if (distance < selectionRange && distance < closestDistance)
			{
				closestDistance = distance;
				closestShip = ship;
			}
		}
		
		// Also check static objects (turrets, targets) - delegate to StaticObjectsManager
		var staticEntity = _gameManager.StaticObjectsManager.FindClosestEntity(worldPos, selectionRange);
		if (staticEntity != null)
		{
			var distance = worldPos.DistanceTo(staticEntity.GlobalPosition);
			if (distance < closestDistance)
			{
				// Static object is closer, select it instead
				_gameManager.StaticObjectsManager.SelectEntity(staticEntity);
				DeselectCurrentShip();
				return;
			}
		}
		
		// Deselect current ship first
		DeselectCurrentShip();
		
		// Select the closest ship if found
		if (closestShip != null)
		{
			int index = _ships.IndexOf(closestShip);
			if (index >= 0)
			{
				SelectShip(index);
			}
		}
		else
		{
			// Deselect all if nothing was clicked
			_gameManager.StaticObjectsManager.DeselectAllEntities();
		}
	}
	
	public void DeselectCurrentShip()
	{
		if (_currentShip != null && IsInstanceValid(_currentShip))
		{
			_currentShip.IsControlled = false;
			if (_currentShip is TargetableCharacter targetableChar)
			{
				targetableChar.IsSelected = false;
			}
		}
		_currentShip = null;
	}
	
	public int GetShipIndex(Ship ship)
	{
		return _ships.IndexOf(ship);
	}
	
	public void SelectShip(int index)
	{
		if (index < 0 || index >= _ships.Count)
			return;
		
		var ship = _ships[index];
		
		// Check if ship is still valid
		if (!IsInstanceValid(ship))
		{
			CleanupDestroyedShips();
			return;
		}
			
		// Deselect current ship
		DeselectCurrentShip();
		
		// Select new ship
		_selectedShipIndex = index;
		_currentShip = ship;
		_currentShip.IsControlled = true;
		if (_currentShip is TargetableCharacter targetableChar)
		{
			targetableChar.IsSelected = true;
		}
		
		// Center camera on new ship
		_gameManager.CameraManager.CenterOnPosition(_currentShip.GlobalPosition);
	}
	
	private void CleanupDestroyedShips()
	{
		// Remove destroyed ships from the list
		for (int i = _ships.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_ships[i]))
			{
				// If the destroyed ship was the current ship, clear it
				if (_currentShip == _ships[i])
				{
					_currentShip = null;
				}
				_ships.RemoveAt(i);
			}
		}
		
		// If current ship is invalid, try to select the first available ship
		if ((_currentShip == null || !IsInstanceValid(_currentShip)) && _ships.Count > 0)
		{
			SelectShip(0);
		}
	}
	
	private void CollectShips()
	{
		// Find all Ship nodes in the scene tree
		// Ships are children of Main, not GameSceneManager, so we need to search from Main
		var main = _gameManager.GetParent();
		if (main != null)
		{
			CollectShipsRecursive(main);
		}
	}
	
	private void CollectShipsRecursive(Node node)
	{
		// Skip GameSceneManager and its children to avoid collecting ships twice
		if (node == _gameManager)
			return;
			
		foreach (var child in node.GetChildren())
		{
			if (child is Ship ship)
			{
				// Set GameSceneManager reference on ship during initialization
				ship.GameSceneManager = _gameManager;
				_ships.Add(ship);
			}
			
			// Recursively check children (but skip GameSceneManager subtree)
			if (child != _gameManager)
			{
				CollectShipsRecursive(child);
			}
		}
	}
	
	private void SetupSceneEntities()
	{
		// Ensure ProjectileScene is set on all ships
		// (in case they weren't set in the scene file)
		// Note: This requires Main.Instance.ProjectileScene, so we'll handle this in Main.cs for now
		// or we can add ProjectileScene to GameSceneManager
	}
	
	public void CollectAllEntities(List<Node2D> list)
	{
		// Collect all ships
		foreach (var ship in _ships)
		{
			if (ship is Node2D node2d && IsInstanceValid(node2d))
			{
				list.Add(node2d);
			}
		}
	}
	
	public void CollectVisibleEntities(List<IVisible> list, TeamType team)
	{
		// Collect all ships of the specified team
		foreach (var ship in _ships)
		{
			if (ship is IVisible visible && visible.Team == team)
			{
				if (IsInstanceValid(ship))
				{
					list.Add(visible);
				}
			}
		}
	}
}

