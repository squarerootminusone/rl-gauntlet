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
    private List<Ship> _ships = [];
    private int _selectedShipIndex = 0;
    [Export] public float CameraSpeed { get; set; } = 300.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.5f;
    [Export] public float MaxZoom { get; set; } = 3.0f;
    
    // Team crystal tracking
    private Dictionary<Team, int> _teamCrystals = [];
    private CanvasLayer _uiLayer;
    private Control _uiContainer;
    private Dictionary<Team, Label> _crystalLabels = [];
    
    // Entity info panel
    private Control _entityInfoPanel;
    private Label _entityInfoLabel;
    
    // Fog of war
    private static readonly Color GrayColor = new(0.3f, 0.3f, 0.3f, 1.0f); // Gray color for fog of war
    private ColorRect _fogOfWarBackground;
    private CanvasLayer _fogOfWarLayer;
    private List<CircleShape2D> _visibilityCircles = [];
    private List<Node2D> _visibilityCircleNodes = [];

    public override void _Ready()
    {
        Instance = this;
        
        // Initialize team crystal counts
        _teamCrystals[Team.Red] = 0;
        _teamCrystals[Team.Blue] = 0;

        // Create camera
        _camera = new Camera2D();
        AddChild(_camera);
        
        // Create fog of war background
        SetupFogOfWar();
        
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
        
        // Initialize fog of war - set all entities to gray initially
        UpdateFogOfWar();
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
                // Handle left-click to select ships
                else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
                {
                    HandleShipSelection();
                }
            }
        }
    }
    
    private void HandleShipSelection()
    {
        // Convert screen position to world position
        var worldPos = GetGlobalMousePosition();
        
        // Find the targetable entity closest to the click position (within a reasonable range)
        const float selectionRange = 50.0f; // Maximum distance to select an entity
        Node2D closestEntity = null;
        float closestDistance = float.MaxValue;
        
        // Check all targetable entities (ships, turrets, targets)
        CollectAllTargetables(worldPos, selectionRange, ref closestEntity, ref closestDistance);
        
        // Deselect all entities first
        DeselectAllEntities();
        
        // Select the closest entity if found
        if (closestEntity != null)
        {
            SelectEntity(closestEntity);
        }
    }
    
    private void CollectAllTargetables(Vector2 worldPos, float selectionRange, ref Node2D closestEntity, ref float closestDistance)
    {
        // Check ships
        foreach (var ship in _ships)
        {
            if (!IsInstanceValid(ship))
                continue;
                
            var distance = worldPos.DistanceTo(ship.GlobalPosition);
            if (distance < selectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEntity = ship;
            }
        }
        
        // Check turrets and targets in the scene
        CollectTargetablesRecursive(this, worldPos, selectionRange, ref closestEntity, ref closestDistance);
    }
    
    private void CollectTargetablesRecursive(Node node, Vector2 worldPos, float selectionRange, ref Node2D closestEntity, ref float closestDistance)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Node2D node2d && IsInstanceValid(node2d))
            {
                // Check if it's a targetable entity
                if (child is Turret || child is Target)
                {
                    var distance = worldPos.DistanceTo(node2d.GlobalPosition);
                    if (distance < selectionRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEntity = node2d;
                    }
                }
            }
            
            // Recursively check children
            CollectTargetablesRecursive(child, worldPos, selectionRange, ref closestEntity, ref closestDistance);
        }
    }
    
    private void DeselectAllEntities()
    {
        // Deselect current ship
        if (_currentShip != null && IsInstanceValid(_currentShip))
        {
            _currentShip.IsControlled = false;
            if (_currentShip is TargetableCharacter targetableChar)
            {
                targetableChar.IsSelected = false;
            }
        }
        
        // Deselect all other targetable entities
        DeselectTargetablesRecursive(this);
    }
    
    private void DeselectTargetablesRecursive(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Targetable targetable)
            {
                targetable.IsSelected = false;
            }
            else if (child is Target target)
            {
                target.IsSelected = false;
            }
            
            DeselectTargetablesRecursive(child);
        }
    }
    
    private void SelectEntity(Node2D entity)
    {
        if (!IsInstanceValid(entity))
            return;
            
        // Handle ship selection
        if (entity is Ship ship)
        {
            int index = _ships.IndexOf(ship);
            if (index >= 0)
            {
                SelectShip(index);
            }
        }
        // Handle turret selection
        else if (entity is Turret turret)
        {
            turret.IsSelected = true;
            // Center camera on turret
            if (_camera != null)
            {
                _camera.GlobalPosition = turret.GlobalPosition;
            }
        }
        // Handle target selection
        else if (entity is Target target)
        {
            target.IsSelected = true;
            // Center camera on target
            if (_camera != null)
            {
                _camera.GlobalPosition = target.GlobalPosition;
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
        if (_currentShip is TargetableCharacter targetableChar)
        {
            targetableChar.IsSelected = true;
        }
        
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
        
        // Create labels for each team's crystal count (excluding Spectator)
        var yOffset = 20.0f;
        var labelHeight = 30.0f;
        
        foreach (Team team in System.Enum.GetValues(typeof(Team)))
        {
            // Skip Spectator team
            if (team == Team.Spectator)
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
        Node2D selectedEntity;
        if (_currentShip != null && IsInstanceValid(_currentShip) && _currentShip.IsControlled)
        {
            selectedEntity = _currentShip;
        }
        else
        {
            // Check for other selected entities
            selectedEntity = FindSelectedEntity(this);
        }
        
        if (selectedEntity != null && IsInstanceValid(selectedEntity))
        {
            _entityInfoPanel.Visible = true;
            
            string infoText = "";
            
            if (selectedEntity is Ship ship)
            {
                infoText = $"Ship ({ship.Team})\n";
                infoText += $"HP: {ship.CurrentHP}/{ship.MaxHP}\n";
                infoText += $"Crystals: {ship.Crystals}/{Ship.MaxCrystals}";
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
                infoText += $"Crystals: {target.Crystals}/{target.MaxCrystals}";
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
    
    private void UpdateCrystalDisplay()
    {
        foreach (var kvp in _crystalLabels)
        {
            var team = kvp.Key;
            // Skip Spectator team
            if (team == Team.Spectator)
                continue;
                
            var label = kvp.Value;
            var count = _teamCrystals.TryGetValue(team, out int value) ? value : 0;
            label.Text = $"{team}: {count} crystals";
        }
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
                if (kvp.Key == Team.Spectator)
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
        
        // Update fog of war visibility
        UpdateFogOfWar();
    }
    
    private void UpdateFogOfWar()
    {
        // Determine viewing team: current ship's team, or spectator if no ship controlled
        Team viewingTeam = Team.Spectator;
        if (_currentShip != null && IsInstanceValid(_currentShip))
        {
            viewingTeam = _currentShip.Team;
        }
        
        // Update fog of war background size to cover viewport
        if (_fogOfWarBackground != null)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            _fogOfWarBackground.Size = viewportSize * 10.0f; // Make it large enough to cover everything
            _fogOfWarBackground.Position = -viewportSize * 5.0f; // Center it
        }
        
        // If viewing as spectator, everything is visible
        if (viewingTeam == Team.Spectator)
        {
            SetAllEntitiesVisible(true);
            if (_fogOfWarBackground != null)
            {
                _fogOfWarBackground.Color = Colors.Transparent; // No fog for spectator
            }
            var fogDrawer = GetNodeOrNull<FogOfWarDrawer>("FogDrawer");
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
        var fogDrawer2 = GetNodeOrNull<FogOfWarDrawer>("FogDrawer");
        if (fogDrawer2 != null)
        {
            fogDrawer2.SetVisibilityCircles(visibleEntities);
        }
        
        // Collect all entities that can be seen (all ships, turrets, targets, crystals, projectiles)
        var allEntities = new List<Node2D>();
        CollectAllEntities(allEntities);
        
        // Check visibility for each entity
        foreach (var entity in allEntities)
        {
            if (!IsInstanceValid(entity))
                continue;
                
            bool isVisible = IsEntityVisible(entity, visibleEntities, viewingTeam);
            SetEntityVisibility(entity, isVisible);
        }
    }
    
    private void CollectVisibleEntities(List<IVisible> list, Team team)
    {
        // Collect all ships, turrets, and targets of the specified team
        foreach (var child in GetChildren())
        {
            if (child is IVisible visible && visible.Team == team)
            {
                if (IsInstanceValid(child as Node2D))
                {
                    list.Add(visible);
                }
            }

            // Recursively check children (for nested structures)
            CollectVisibleEntitiesRecursive(child, list, team);
        }
    }
    
    private static void CollectVisibleEntitiesRecursive(Node node, List<IVisible> list, Team team)
    {
        foreach (var child in node.GetChildren()) {
            if (child is IVisible visible && visible.Team == team) 
                if (IsInstanceValid(child as Node2D)) 
                    list.Add(visible);

            CollectVisibleEntitiesRecursive(child, list, team);
        }
    }
    
    private void CollectAllEntities(List<Node2D> list)
    {
        // Collect all ships, turrets, targets, crystals, and projectiles
        foreach (var child in GetChildren())
        {
            if (child is Node2D node2d)
                if (node2d is Ship || node2d is Turret || node2d is Target || node2d is Crystal || node2d is Projectile)
                    if (IsInstanceValid(node2d))
                        list.Add(node2d);

            // Recursively check children
            CollectAllEntitiesRecursive(child, list);
        }
    }
    
    private static void CollectAllEntitiesRecursive(Node node, List<Node2D> list)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Node2D node2d)
            {
                if (node2d is Ship || node2d is Turret || node2d is Target || node2d is Crystal || node2d is Projectile)
                {
                    if (IsInstanceValid(node2d))
                    {
                        list.Add(node2d);
                    }
                }
            }

            CollectAllEntitiesRecursive(child, list);
        }
    }
    
    private static bool IsEntityVisible(Node2D entity, List<IVisible> visibleEntities, Team viewingTeam)
    {
        // Get the entity's team if it has one
        Team? entityTeam = null;
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
    
    private void SetupFogOfWar()
    {
        // Create fog of war layer (behind everything)
        _fogOfWarLayer = new CanvasLayer {
            Layer = -1 // Behind everything
        };
        AddChild(_fogOfWarLayer);

        // Create background that covers entire viewport
        _fogOfWarBackground = new ColorRect {
            Color = GrayColor, // Gray by default
            MouseFilter = Control.MouseFilterEnum.Ignore // Don't block mouse input
        };
        _fogOfWarLayer.AddChild(_fogOfWarBackground);
        
        // Create a custom drawing node for visibility circles in world space (as child of Main)
        var fogDrawer = new FogOfWarDrawer();
        AddChild(fogDrawer); // Add to Main, not CanvasLayer, so it's in world space
        fogDrawer.Name = "FogDrawer";
        fogDrawer.ZIndex = -100; // Behind everything in world space
    }
    
    private void SetAllEntitiesVisible(bool visible)
    {
        var allEntities = new List<Node2D>();
        CollectAllEntities(allEntities);
        
        foreach (var entity in allEntities)
            if (IsInstanceValid(entity))
                SetEntityVisibility(entity, visible);
    }
}

