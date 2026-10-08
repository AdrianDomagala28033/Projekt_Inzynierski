using Godot;

public partial class Building : StaticBody2D, IDamageable
{
    [ExportGroup("Building Stats")]
    [Export] public int MaxHealth = 100;

    [ExportGroup("Building Cost")]
    [Export] public int CostWood = 0;
    [Export] public int CostStone = 0;
    [Export] public int CostGold = 0;

    [ExportGroup("Grid Settings")]
    [Export] public Vector2[] OccupiedTilesOffsets = new Vector2[] { new Vector2(0, 0) };
    
    public int CurrentHealth { get; protected set; }
    
    public AiGridManager GridManager; 

   public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("buildings");

        GD.Print($"[{Name}] Inspektor przekazał tablicę o rozmiarze: {OccupiedTilesOffsets.Length}");

        OccupiedTilesOffsets = new Vector2[] { new Vector2(0, 0), new Vector2(-1, 0) };

        if (GridManager != null)
        {
            UpdateGridPenalties(CurrentHealth);
        }
    }

    public virtual void TakeDamage(int amount, Node2D attacker = null)
    {
        CurrentHealth -= amount;
        GD.Print($"[{Name}] Otrzymał {amount} obrażeń. Zostało: {CurrentHealth}/{MaxHealth} HP.");

        if (GridManager != null)
        {
            UpdateGridPenalties(Mathf.Max(0, CurrentHealth));
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        GD.Print($"[{Name}] Zniszczony!");
        
        if (GridManager != null)
        {
            UpdateGridPenalties(0);
        }

        QueueFree();
    }

    public void UpdateGridPenalties(int penaltyValue)
    {
        if (GridManager == null) return;

        Vector2I baseGridPos = new Vector2I(
            Mathf.FloorToInt(GlobalPosition.X / 16.0f), 
            Mathf.FloorToInt(GlobalPosition.Y / 16.0f)
        );

        foreach (Vector2 offset in OccupiedTilesOffsets)
        {
            Vector2I targetTile = baseGridPos + new Vector2I(Mathf.RoundToInt(offset.X), Mathf.RoundToInt(offset.Y));
            GridManager.SetObstaclePenalty(targetTile, penaltyValue);
        }
    }
}