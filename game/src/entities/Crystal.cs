using Godot;

namespace game;

public partial class Crystal : StaticBody2D
{
    [Export] public int CrystalsRemaining { get; set; } = 0; // Will be set randomly in _Ready
    
    public override void _Ready()
    {
        // Set random crystal amount between 10 and 20 if not already set
        if (CrystalsRemaining == 0)
        {
            CrystalsRemaining = GD.RandRange(10, 20);
        }
    }
    
    public bool MineCrystal()
    {
        if (CrystalsRemaining > 0)
        {
            CrystalsRemaining--;
            
            // If depleted, remove the crystal
            if (CrystalsRemaining <= 0)
            {
                QueueFree();
                return false; // No more crystals to mine
            }
            
            return true;
        }
        return false;
    }
    
    public bool IsDepleted()
    {
        return CrystalsRemaining <= 0;
    }
}

