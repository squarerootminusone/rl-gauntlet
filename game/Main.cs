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
    
    private Ship _currentShip;
    private Camera2D _camera;
    private List<Ship> _ships = new List<Ship>();
    private int _selectedShipIndex = 0;
    [Export] public float CameraSpeed { get; set; } = 300.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.5f;
    [Export] public float MaxZoom { get; set; } = 3.0f;
    
    // Team crystal tracking
    private Dictionary<Team, int> _teamCrystals = new Dictionary<Team, int>();
    private CanvasLayer _uiLayer;
    private Control _uiContainer;
    private Dictionary<Team, Label> _crystalLabels = new Dictionary<Team, Label>();
    
    public override void _Ready()
    {
        Instance = this;
        
        // Initialize team crystal counts
        _teamCrystals[Team.Red] = 0;
        _teamCrystals[Team.Blue] = 0;
        _teamCrystals[Team.Green] = 0;
        _teamCrystals[Team.Yellow] = 0;
        
        // Create camera
        _camera = new Camera2D();
        AddChild(_camera);
        
        // Create UI for crystal display
        SetupUI();
        
        // Collect ships from scene
        CollectShips();
        
        // Ensure ProjectileScene is set on all ships and turrets from scene
        SetupSceneEntities();
        
        // Select first ship by default
        if (_ships.Count > 0)
        {
            SelectShip(0);
        }
    }
    
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (_camera != null)
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
    }
    
    private void SelectShip(int index)
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
        if (_currentShip != null && IsInstanceValid(_currentShip))
        {
            _currentShip.IsControlled = false;
        }
        
        // Select new ship
        _selectedShipIndex = index;
        _currentShip = ship;
        _currentShip.IsControlled = true;
        
        // Center camera on new ship
        if (_camera != null && IsInstanceValid(_currentShip))
        {
            _camera.GlobalPosition = _currentShip.GlobalPosition;
        }
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
        foreach (var child in GetChildren())
        {
            if (child is Ship ship)
            {
                _ships.Add(ship);
            }
        }
    }
    
    private void SetupSceneEntities()
    {
        // Ensure ProjectileScene is set on all ships and turrets
        // (in case they weren't set in the scene file)
        foreach (var child in GetChildren())
        {
            if (child is Ship ship && ship.ProjectileScene == null)
            {
                ship.ProjectileScene = ProjectileScene;
            }
            else if (child is Turret turret && turret.ProjectileScene == null)
            {
                turret.ProjectileScene = ProjectileScene;
            }
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
    
    public void AddCrystalsToTeam(Team team, int amount)
    {
        if (_teamCrystals.ContainsKey(team))
        {
            _teamCrystals[team] += amount;
            UpdateCrystalDisplay();
        }
    }
    
    private void SetupUI()
    {
        // Create UI layer and container
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);
        
        _uiContainer = new Control();
        _uiLayer.AddChild(_uiContainer);
        
        // Create labels for each team's crystal count
        var yOffset = 20.0f;
        var labelHeight = 30.0f;
        
        foreach (Team team in System.Enum.GetValues(typeof(Team)))
        {
            var label = new Label();
            label.Text = $"{team}: 0 crystals";
            label.Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 200, yOffset);
            label.AddThemeColorOverride("font_color", TeamColors.GetColor(team));
            _uiContainer.AddChild(label);
            _crystalLabels[team] = label;
            yOffset += labelHeight;
        }
    }
    
    private void UpdateCrystalDisplay()
    {
        foreach (var kvp in _crystalLabels)
        {
            var team = kvp.Key;
            var label = kvp.Value;
            var count = _teamCrystals.ContainsKey(team) ? _teamCrystals[team] : 0;
            label.Text = $"{team}: {count} crystals";
        }
    }
    
    public override void _Process(double delta)
    {
        // Update UI position in case viewport size changed
        if (_uiContainer != null)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            foreach (var kvp in _crystalLabels)
            {
                var label = kvp.Value;
                var index = (int)kvp.Key;
                label.Position = new Vector2(viewportSize.X - 200, 20 + index * 30);
            }
        }
        
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
        
        // Handle right-click to set ship target
        if (Input.IsMouseButtonPressed(MouseButton.Right) && _currentShip != null && IsInstanceValid(_currentShip))
        {
            var mousePos = GetGlobalMousePosition();
            _currentShip.SetTargetPosition(mousePos);
        }
    }
}

