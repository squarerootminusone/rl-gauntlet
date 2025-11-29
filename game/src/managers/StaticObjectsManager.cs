using Godot;
using System.Collections.Generic;

namespace game;

public partial class StaticObjectsManager : Node
{
	private GameSceneManager _gameManager;
	private List<Turret> _turrets = [];
	private List<Target> _targets = [];
	private List<Crystal> _crystals = [];
	
	public StaticObjectsManager(GameSceneManager gameManager)
	{
		_gameManager = gameManager;
	}
	
	public void Initialize()
	{
		// Collect static objects from scene
		CollectStaticObjects();
		
		// Ensure ProjectileScene is set on all turrets
		SetupSceneEntities();
	}
	
	public void Update(double delta)
	{
		// Clean up destroyed objects
		CleanupDestroyedObjects();
	}
	
	public void Reset()
	{
		// Reset all static objects to initial state
		foreach (var turret in _turrets)
		{
			if (IsInstanceValid(turret))
			{
				// Reset turret state as needed
			}
		}
		
		foreach (var target in _targets)
		{
			if (IsInstanceValid(target))
			{
				// Reset target state as needed
			}
		}
	}
	
	private void CollectStaticObjects()
	{
		// Find all static objects in the scene tree
		// Objects are children of Main, not GameSceneManager, so we need to search from Main
		var main = _gameManager.GetParent();
		if (main != null)
		{
			CollectStaticObjectsRecursive(main);
		}
	}
	
	private void CollectStaticObjectsRecursive(Node node)
	{
		// Skip GameSceneManager and its children to avoid collecting objects twice
		if (node == _gameManager)
			return;
			
		foreach (var child in node.GetChildren())
		{
			if (child is Turret turret)
			{
				_turrets.Add(turret);
			}
			else if (child is Target target)
			{
				_targets.Add(target);
			}
			else if (child is Crystal crystal)
			{
				_crystals.Add(crystal);
			}
			
			// Recursively check children (but skip GameSceneManager subtree)
			if (child != _gameManager)
			{
				CollectStaticObjectsRecursive(child);
			}
		}
	}
	
	private void SetupSceneEntities()
	{
		// Ensure ProjectileScene is set on all turrets
		// (in case they weren't set in the scene file)
		// Note: This requires Main.Instance.ProjectileScene, so we'll handle this in Main.cs for now
		// or we can add ProjectileScene to GameSceneManager
	}
	
	private void CleanupDestroyedObjects()
	{
		// Remove destroyed turrets
		for (int i = _turrets.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_turrets[i]))
			{
				_turrets.RemoveAt(i);
			}
		}
		
		// Remove destroyed targets
		for (int i = _targets.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_targets[i]))
			{
				_targets.RemoveAt(i);
			}
		}
		
		// Remove destroyed crystals
		for (int i = _crystals.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_crystals[i]))
			{
				_crystals.RemoveAt(i);
			}
		}
	}
	
	public void CollectAllEntities(List<Node2D> list)
	{
		// Collect all turrets
		foreach (var turret in _turrets)
		{
			if (IsInstanceValid(turret))
			{
				list.Add(turret);
			}
		}
		
		// Collect all targets
		foreach (var target in _targets)
		{
			if (IsInstanceValid(target))
			{
				list.Add(target);
			}
		}
		
		// Collect all crystals
		foreach (var crystal in _crystals)
		{
			if (IsInstanceValid(crystal))
			{
				list.Add(crystal);
			}
		}
		
		// Also collect projectiles (they're created dynamically)
		CollectProjectilesRecursive(_gameManager, list);
	}
	
	private static void CollectProjectilesRecursive(Node node, List<Node2D> list)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is Projectile projectile && IsInstanceValid(projectile))
			{
				list.Add(projectile);
			}
			
			CollectProjectilesRecursive(child, list);
		}
	}
	
	public void CollectVisibleEntities(List<IVisible> list, TeamType team)
	{
		// Collect all turrets of the specified team
		foreach (var turret in _turrets)
		{
			if (turret is IVisible visible && visible.Team == team)
			{
				if (IsInstanceValid(turret))
				{
					list.Add(visible);
				}
			}
		}
		
		// Collect all targets of the specified team
		foreach (var target in _targets)
		{
			if (target is IVisible visible && visible.Team == team)
			{
				if (IsInstanceValid(target))
				{
					list.Add(visible);
				}
			}
		}
	}
	
	public void SelectEntity(Node2D entity)
	{
		if (!IsInstanceValid(entity))
			return;
			
		// Deselect all entities first
		DeselectAllEntities();
		
		// Handle turret selection
		if (entity is Turret turret)
		{
			turret.IsSelected = true;
			_gameManager.CameraManager.CenterOnPosition(turret.GlobalPosition);
		}
		// Handle target selection
		else if (entity is Target target)
		{
			target.IsSelected = true;
			_gameManager.CameraManager.CenterOnPosition(target.GlobalPosition);
		}
	}
	
	public void DeselectAllEntities()
	{
		// Deselect all turrets
		foreach (var turret in _turrets)
		{
			if (IsInstanceValid(turret))
			{
				turret.IsSelected = false;
			}
		}
		
		// Deselect all targets
		foreach (var target in _targets)
		{
			if (IsInstanceValid(target))
			{
				target.IsSelected = false;
			}
		}
	}
	
	public Node2D FindClosestEntity(Vector2 worldPos, float selectionRange)
	{
		Node2D closestEntity = null;
		float closestDistance = float.MaxValue;
		
		// Check turrets
		foreach (var turret in _turrets)
		{
			if (!IsInstanceValid(turret))
				continue;
				
			var distance = worldPos.DistanceTo(turret.GlobalPosition);
			if (distance < selectionRange && distance < closestDistance)
			{
				closestDistance = distance;
				closestEntity = turret;
			}
		}
		
		// Check targets
		foreach (var target in _targets)
		{
			if (!IsInstanceValid(target))
				continue;
				
			var distance = worldPos.DistanceTo(target.GlobalPosition);
			if (distance < selectionRange && distance < closestDistance)
			{
				closestDistance = distance;
				closestEntity = target;
			}
		}
		
		return closestEntity;
	}
}

