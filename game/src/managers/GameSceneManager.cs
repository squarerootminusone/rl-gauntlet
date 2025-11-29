using Godot;
using System.Collections.Generic;

namespace game;

public partial class GameSceneManager : Node2D
{
	public static GameSceneManager Instance { get; private set; }
	
	// Managers
	public CameraManager CameraManager { get; private set; }
	public PlayableCharactersManager PlayableCharactersManager { get; private set; }
	public StaticObjectsManager StaticObjectsManager { get; private set; }
	
	// Team data tracking (list of teams, but data held in managers)
	private Dictionary<TeamType, TeamData> _teams = [];
	
	// Scene references
	[Export] public PackedScene ExplosionScene { get; set; }
	
	public Dictionary<TeamType, TeamData> Teams => _teams;
	
	public TeamData GetTeamData(TeamType team)
	{
		if (!_teams.ContainsKey(team))
		{
			_teams[team] = new TeamData(team);
		}
		return _teams[team];
	}
	
	public override void _Ready()
	{
		Instance = this;
		Name = "GameSceneManager"; // Set name so it can be referenced in scene files
		
		// Initialize team data
		_teams[TeamType.Red] = new TeamData(TeamType.Red);
		_teams[TeamType.Blue] = new TeamData(TeamType.Blue);
		
		// Create managers
		CameraManager = new CameraManager(this);
		AddChild(CameraManager);
		
		PlayableCharactersManager = new PlayableCharactersManager(this);
		AddChild(PlayableCharactersManager);
		
		StaticObjectsManager = new StaticObjectsManager(this);
		AddChild(StaticObjectsManager);
		
		// Initialize managers
		CameraManager.Initialize();
		PlayableCharactersManager.Initialize();
		StaticObjectsManager.Initialize();
	}
	
	public override void _Input(InputEvent @event)
	{
		CameraManager.HandleInput(@event);
		PlayableCharactersManager.HandleInput(@event);
	}
	
	public override void _Process(double delta)
	{
		CameraManager.Update(delta);
		PlayableCharactersManager.Update(delta);
		StaticObjectsManager.Update(delta);
	}
	
	public void AddCrystalsToTeam(TeamType team, float amount)
	{
		if (team == TeamType.Spectator)
			return;
			
		var teamData = GetTeamData(team);
		teamData.AddCrystals(amount);
		// Notify UI to update (handled by Main.cs for now)
		if (Main.Instance != null)
		{
			Main.Instance.UpdateCrystalDisplay();
		}
	}
	
	public void SpawnExplosion(Vector2 position)
	{
		if (ExplosionScene == null)
			return;
			
		var explosion = ExplosionScene.Instantiate<Explosion>();
		if (explosion != null)
		{
			AddChild(explosion);
			explosion.GlobalPosition = position;
		}
	}
	
	public void Reset()
	{
		// Reset game state - called by AI controller
		PlayableCharactersManager.Reset();
		StaticObjectsManager.Reset();
	}
}

