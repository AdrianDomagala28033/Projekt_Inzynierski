using Godot;

public partial class Wall : StaticBody2D
{
    [Export] public int MaxHealth = 100;
    [Export] public int PathfindingPenalty = 1000; 

    private int _currentHealth;
    private AiGridManager _gridManager;
    private Vector2I _gridPos;
    private const float TileSize = 16.0f;

    public override async void _Ready()
    {
        _currentHealth = MaxHealth;
        AddToGroup("buildings");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _gridPos = new Vector2I(Mathf.FloorToInt(GlobalPosition.X / TileSize), Mathf.FloorToInt(GlobalPosition.Y / TileSize));
        _gridManager = GetTree().CurrentScene.GetNodeOrNull<AiGridManager>("AiSystem/AiGridManager");
        
        UpdateGridPenalty();
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        GD.Print($"[Mur] Otrzymano {amount} obrażeń. Pozostało HP: {_currentHealth}/{MaxHealth}");

        if (_currentHealth <= 0)
        {
            Destroy();
        }
        else
        {
            UpdateGridPenalty();
        }
    }

    private void Destroy()
    {
        GD.Print("[Mur] Zniszczono strukturę!");
        if (_gridManager != null)
        {
            _gridManager.SetObstaclePenalty(_gridPos, 0); 
        }
        QueueFree();
    }

    private void UpdateGridPenalty()
    {
        if (_gridManager != null)
        {
            int scaledPenalty = Mathf.RoundToInt(PathfindingPenalty * ((float)_currentHealth / MaxHealth));
            _gridManager.SetObstaclePenalty(_gridPos, scaledPenalty);
        }
    }
}