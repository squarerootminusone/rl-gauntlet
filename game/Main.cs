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
    
    public override void _Ready()
    {
        Instance = this;
        
        // Create camera
        _camera = new Camera2D();
        AddChild(_camera);
        
        // Create ships
        CreateShips();
        
        // Create targets
        CreateTargets();
        
        // Create turrets
        CreateTurrets();
        
        // Select first ship by default
        if (_ships.Count > 0)
        {
            SelectShip(0);
        }
    }
    
    public override void _Process(double delta)
    {
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
        if (Input.IsMouseButtonPressed(MouseButton.Right) && _currentShip != null)
        {
            var mousePos = GetGlobalMousePosition();
            _currentShip.SetTargetPosition(mousePos);
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
            
        // Deselect current ship
        if (_currentShip != null)
        {
            _currentShip.IsControlled = false;
        }
        
        // Select new ship
        _selectedShipIndex = index;
        _currentShip = _ships[index];
        _currentShip.IsControlled = true;
        
        // Center camera on new ship
        if (_camera != null)
        {
            _camera.GlobalPosition = _currentShip.GlobalPosition;
        }
    }
    
    private void CreateShips()
    {
        if (ShipScene == null)
            return;
            
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var centerY = viewportSize.Y / 2;
        
        // Create one red ship
        var redShip = ShipScene.Instantiate<Ship>();
        if (redShip != null)
        {
            redShip.Team = Team.Red;
            redShip.MaxHP = 3;
            redShip.ProjectileScene = ProjectileScene;
            redShip.GlobalPosition = new Vector2(200, centerY - 100);
            AddChild(redShip); // UpdateColor() is called automatically in _Ready()
            _ships.Add(redShip);
        }
        
        // Create one blue ship
        var blueShip = ShipScene.Instantiate<Ship>();
        if (blueShip != null)
        {
            blueShip.Team = Team.Blue;
            blueShip.MaxHP = 3;
            blueShip.ProjectileScene = ProjectileScene;
            blueShip.GlobalPosition = new Vector2(200, centerY + 100);
            AddChild(blueShip); // UpdateColor() is called automatically in _Ready()
            _ships.Add(blueShip);
        }
    }
    
    private void CreateTargets()
    {
        if (TargetScene == null)
            return;
            
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var centerY = viewportSize.Y / 2;
        
        // Create a couple of targets
        // Red target
        var redTarget = TargetScene.Instantiate<Target>();
        if (redTarget != null)
        {
            redTarget.Team = Team.Red;
            redTarget.GlobalPosition = new Vector2(viewportSize.X - 300, centerY - 150);
            AddChild(redTarget); // UpdateColor() is called automatically in _Ready()
        }
        
        // Blue target
        var blueTarget = TargetScene.Instantiate<Target>();
        if (blueTarget != null)
        {
            blueTarget.Team = Team.Blue;
            blueTarget.GlobalPosition = new Vector2(viewportSize.X - 300, centerY + 150);
            AddChild(blueTarget); // UpdateColor() is called automatically in _Ready()
        }
        
        // Add a couple more targets for variety
        var greenTarget = TargetScene.Instantiate<Target>();
        if (greenTarget != null)
        {
            greenTarget.Team = Team.Green;
            greenTarget.GlobalPosition = new Vector2(viewportSize.X - 200, centerY);
            AddChild(greenTarget); // UpdateColor() is called automatically in _Ready()
        }
        
        var yellowTarget = TargetScene.Instantiate<Target>();
        if (yellowTarget != null)
        {
            yellowTarget.Team = Team.Yellow;
            yellowTarget.GlobalPosition = new Vector2(viewportSize.X - 400, centerY);
            AddChild(yellowTarget); // UpdateColor() is called automatically in _Ready()
        }
    }
    
    private void CreateTurrets()
    {
        if (TurretScene == null)
            return;
            
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var centerY = viewportSize.Y / 2;
        var centerX = viewportSize.X / 2;
        
        // Create a red turret
        var redTurret = TurretScene.Instantiate<Turret>();
        if (redTurret != null)
        {
            redTurret.Team = Team.Red;
            redTurret.MaxHP = 5;
            redTurret.ProjectileScene = ProjectileScene;
            redTurret.GlobalPosition = new Vector2(centerX - 200, centerY - 200);
            AddChild(redTurret); // UpdateColor() is called automatically in _Ready()
        }
        
        // Create a blue turret
        var blueTurret = TurretScene.Instantiate<Turret>();
        if (blueTurret != null)
        {
            blueTurret.Team = Team.Blue;
            blueTurret.MaxHP = 5;
            blueTurret.ProjectileScene = ProjectileScene;
            blueTurret.GlobalPosition = new Vector2(centerX + 200, centerY + 200);
            AddChild(blueTurret); // UpdateColor() is called automatically in _Ready()
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
}

