using Godot;
using System.Collections.Generic;

namespace game;

public enum TeamType
{
	Red = 1,
	Blue = 2,
	Spectator = 0
}

public class TeamData
{
	public TeamType TeamType { get; }
	public float Crystals { get; set; } = 0.0f;
	public List<Node2D> VisibleEntities { get; } = new List<Node2D>();
	
	public TeamData(TeamType teamType)
	{
		TeamType = teamType;
	}
	
	public void AddCrystals(float amount)
	{
		Crystals += amount;
	}
	
	public void ClearVisibleEntities()
	{
		VisibleEntities.Clear();
	}
	
	public void AddVisibleEntity(Node2D entity)
	{
		if (entity != null && !VisibleEntities.Contains(entity))
		{
			VisibleEntities.Add(entity);
		}
	}
}

public static class TeamColors
{
	public static Color GetColor(TeamType team)
	{
		return team switch
		{
			TeamType.Red => Colors.Red,
			TeamType.Blue => Colors.Blue,
			TeamType.Spectator => Colors.White,
			_ => Colors.White
		};
	}
}
