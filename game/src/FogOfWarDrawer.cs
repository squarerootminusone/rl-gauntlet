using Godot;
using System.Collections.Generic;

namespace game;

// Custom node that draws the fog of war visibility circles
public partial class FogOfWarDrawer : Node2D
{
    private List<VisibilityCircle> _visibilityCircles = new List<VisibilityCircle>();
    
    private struct VisibilityCircle
    {
        public Vector2 Position;
        public float Radius;
    }
    
    public void SetVisibilityCircles(List<IVisible> visibleEntities)
    {
        _visibilityCircles.Clear();
        
        foreach (var entity in visibleEntities)
        {
            if (!IsInstanceValid(entity as Node2D))
                continue;
                
            _visibilityCircles.Add(new VisibilityCircle
            {
                Position = (entity as Node2D).GlobalPosition,
                Radius = entity.VisibilityRange
            });
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
            // Convert world position to local position for drawing
            var localPos = ToLocal(circle.Position);
            // Draw filled circle in black (visible area)
            DrawCircle(localPos, circle.Radius, Colors.Black);
        }
    }
}

