using Godot;
using System.Collections.Generic;

namespace game;

// Custom node that draws the fog of war visibility circles
public partial class FogOfWarDrawer : Node2D
{
    private List<VisibilityCircle> _visibilityCircles = new List<VisibilityCircle>();
    
    private struct VisibilityCircle
    {
        public Node2D Entity;
        public float Radius;
    }
    
    public void SetVisibilityCircles(List<IVisible> visibleEntities)
    {
        _visibilityCircles.Clear();
        
        foreach (var entity in visibleEntities)
        {
            if (entity is Node2D node2d && IsInstanceValid(node2d))
            {
                _visibilityCircles.Add(new VisibilityCircle
                {
                    Entity = node2d,
                    Radius = entity.VisibilityRange
                });
            }
        }
        
        QueueRedraw();
    }
    
    public void ClearVisibilityCircles()
    {
        _visibilityCircles.Clear();
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        // Draw black circles for visible areas
        // Since we're in world space, positions are already correct
        foreach (var circle in _visibilityCircles)
        {
            // Check if the entity is still valid and get current position
            if (circle.Entity != null && IsInstanceValid(circle.Entity))
            {
                // Get current position (in case entity moved)
                var currentPos = circle.Entity.GlobalPosition;
                var localPos = ToLocal(currentPos);
                // Draw filled circle in black (visible area) - use a large number of points for smooth circle
                DrawCircle(localPos, circle.Radius, Colors.Black);
            }
        }
    }
    
    public override void _Ready()
    {
        // Make sure we're visible and can draw
        Visible = true;
    }
    
    public override void _Process(double delta)
    {
        // Redraw every frame to update positions as entities move
        QueueRedraw();
    }
}

