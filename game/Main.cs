using Godot;
using System.Collections.Generic;

namespace game;

public partial class Main : Node2D
{
	[Export] public PackedScene ShipScene { get; set; }
	[Export] public PackedScene TargetScene { get; set; }
	[Export] public PackedScene ProjectileScene { get; set; }
	[Export] public PackedScene TurretScene { get; set; }
	[Export] public PackedScene ExplosionScene { get; set; }
	
	public static Main Instance { get; private set; }
	
	private GameSceneManager _gameManager;
	
	// UI
	private CanvasLayer _uiLayer;
	private Control _uiContainer;
	private Dictionary<TeamType, Label> _crystalLabels = [];
	
	// Entity info panel
	private Control _entityInfoPanel;
	private Label _entityInfoLabel;
	
	public GameSceneManager GameManager => _gameManager;

	public override void _Ready()
	{
		Instance = this;
		
		// Create GameSceneManager
		_gameManager = new GameSceneManager();
		_gameManager.ExplosionScene = ExplosionScene;
		AddChild(_gameManager);
		
		// Create UI for crystal display
		SetupUI();
		
		// Ensure ProjectileScene is set on all ships and turrets from scene
		SetupSceneEntities();
	}
	
	private void SetupSceneEntities()
	{
		// Ensure ProjectileScene is set on all ships and turrets
		// (in case they weren't set in the scene file)
		SetupSceneEntitiesRecursive(this);
	}
	
	private void SetupSceneEntitiesRecursive(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is Ship ship && ship.ProjectileScene == null)
			{
				ship.ProjectileScene = ProjectileScene;
			}
			else if (child is Turret turret && turret.ProjectileScene == null)
			{
				turret.ProjectileScene = ProjectileScene;
			}
			
			SetupSceneEntitiesRecursive(child);
		}
	}
	
	public void AddCrystalsToTeam(TeamType team, float amount)
	{
		_gameManager.AddCrystalsToTeam(team, amount);
	}
	
	public void UpdateCrystalDisplay()
	{
		foreach (var kvp in _crystalLabels)
		{
			var team = kvp.Key;
			// Skip Spectator team
			if (team == TeamType.Spectator)
				continue;
				
			var label = kvp.Value;
			var teamData = _gameManager.GetTeamData(team);
			label.Text = $"{team}: {teamData.Crystals:F2} crystals";
		}
	}
	
	private void SetupUI()
	{
		// Create UI layer and container
		_uiLayer = new CanvasLayer();
		AddChild(_uiLayer);
		
		_uiContainer = new Control();
		_uiLayer.AddChild(_uiContainer);
		
		// Create labels for each team's crystal count (excluding Spectator)
		var yOffset = 20.0f;
		var labelHeight = 30.0f;
		
		foreach (TeamType team in System.Enum.GetValues(typeof(TeamType)))
		{
			// Skip Spectator team
			if (team == TeamType.Spectator)
				continue;

			var label = new Label {
				Text = $"{team}: 0 crystals",
				Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 200, yOffset)
			};
			label.AddThemeColorOverride("font_color", TeamColors.GetColor(team));
			_uiContainer.AddChild(label);
			_crystalLabels[team] = label;
			yOffset += labelHeight;
		}
		
		// Create entity info panel in bottom right
		SetupEntityInfoPanel();
	}
	
	private void SetupEntityInfoPanel()
	{
		_entityInfoPanel = new Control();
		_uiContainer.AddChild(_entityInfoPanel);

		// Create background panel
		var panelBg = new ColorRect {
			Color = new Color(0, 0, 0, 0.7f), // Semi-transparent black
			Size = new Vector2(250, 100)
		};
		_entityInfoPanel.AddChild(panelBg);

		// Create label for entity info
		_entityInfoLabel = new Label {
			Text = "",
			Position = new Vector2(10, 10),
			Size = new Vector2(230, 80)
		};
		_entityInfoLabel.AddThemeColorOverride("font_color", Colors.White);
		_entityInfoPanel.AddChild(_entityInfoLabel);
		
		_entityInfoPanel.Visible = false;
	}
	
	private void UpdateEntityInfoPanel()
	{
		if (_entityInfoPanel == null || _entityInfoLabel == null)
			return;

		// Find selected entity
		Node2D selectedEntity = null;
		var currentShip = _gameManager.PlayableCharactersManager.CurrentShip;
		if (currentShip != null && IsInstanceValid(currentShip) && currentShip.IsControlled)
		{
			selectedEntity = currentShip;
		}
		else
		{
			// Check for other selected entities (turrets, targets)
			selectedEntity = FindSelectedEntity(_gameManager);
		}
		
		if (selectedEntity != null && IsInstanceValid(selectedEntity))
		{
			_entityInfoPanel.Visible = true;
			
			string infoText = "";
			
			if (selectedEntity is Ship ship)
			{
				infoText = $"Ship ({ship.Team})\n";
				infoText += $"HP: {ship.CurrentHP}/{ship.MaxHP}\n";
				infoText += $"Crystals: {ship.Crystals:F2}/{Ship.MaxCrystals}";
			}
			else if (selectedEntity is Turret turret)
			{
				infoText = $"Turret ({turret.Team})\n";
				infoText += $"HP: {turret.CurrentHP}/{turret.MaxHP}";
			}
			else if (selectedEntity is Target target)
			{
				infoText = $"Target ({target.Team})\n";
				infoText += $"HP: {target.CurrentHP}/{target.MaxHP}\n";
				infoText += $"Crystals: {target.Crystals:F2}/{target.MaxCrystals:F2}";
			}
			
			_entityInfoLabel.Text = infoText;
		}
		else
		{
			_entityInfoPanel.Visible = false;
		}
	}
	
	private Node2D FindSelectedEntity(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is Node2D node2d && IsInstanceValid(node2d))
			{
				if (child is Targetable targetable && targetable.IsSelected)
				{
					return node2d;
				}
				else if (child is Target target && target.IsSelected)
				{
					return node2d;
				}
			}
			
			var found = FindSelectedEntity(child);
			if (found != null)
				return found;
		}
		
		return null;
	}
	
	public override void _Process(double delta)
	{
		// Update UI position in case viewport size changed
		if (_uiContainer != null)
		{
			var viewportSize = GetViewport().GetVisibleRect().Size;
			int displayIndex = 0;
			foreach (var kvp in _crystalLabels)
			{
				// Skip Spectator team
				if (kvp.Key == TeamType.Spectator)
					continue;
					
				var label = kvp.Value;
				label.Position = new Vector2(viewportSize.X - 200, 20 + displayIndex * 30);
				displayIndex++;
			}
			
			// Update entity info panel position (bottom right)
			if (_entityInfoPanel != null)
			{
				_entityInfoPanel.Position = new Vector2(viewportSize.X - 260, viewportSize.Y - 110);
			}
		}
		
		// Update entity info panel
		UpdateEntityInfoPanel();
	}
}
