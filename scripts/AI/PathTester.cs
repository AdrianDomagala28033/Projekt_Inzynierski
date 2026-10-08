using Godot;

public partial class PathTester : Node2D
{
    private AiGridManager _aiManager;
    private PackedScene _enemyScene;
    private EnemyBase _currentTestEnemy;
    private Node2D _playerNode;
    
    private const int TileSize = 16;
    private const int PenaltyMultiplier = 10;
    
    public override void _Ready()
    {
        _aiManager = GetNode<AiGridManager>("../AiGridManager");

        //_enemyScene = GD.Load<PackedScene>("res://scenes/Goblins/Goblin_Archer.tscn");
        //_enemyScene = GD.Load<PackedScene>("res://scenes/Enemies/Goblins/Goblin_Maceman.tscn");
        //_enemyScene = GD.Load<PackedScene>("res://scenes/Enemies/Kamikaze_Mushroom.tscn");
        //_enemyScene = GD.Load<PackedScene>("res://scenes/Enemies/Slimes/Blue/Slime_Big_Blue.tscn");
        _enemyScene = GD.Load<PackedScene>("res://scenes/Enemies/Skeletons/Skeleton_Mage.tscn");
        //_enemyScene = GD.Load<PackedScene>("res://scenes/Enemies/Skeletons/Skeleton_Bowman.tscn");


        _playerNode = GetNodeOrNull<Node2D>("../../DummyPlayer");
        
        if (_playerNode == null)
        {
            GD.PrintErr("[PathTester] UWAGA: Nie znaleziono DummyPlayer przy starcie!");
        }

        CallDeferred(nameof(RegisterAllStructures));
        CallDeferred(nameof(RegisterWater));
    }

   private void RegisterAllStructures()
    {
        var allBuildings = GetTree().GetNodesInGroup("buildings");
        
        foreach (Node node in allBuildings)
        {
            if (node is Building building) 
            {
                building.GridManager = _aiManager;
                int calculatedPenalty = Mathf.Max(0, building.MaxHealth) * PenaltyMultiplier;
                
                building.UpdateGridPenalties(calculatedPenalty);
                
                GD.Print($"[PathTester] Zarejestrowano strukturę {building.Name} z karą {calculatedPenalty}");
            }
        }
    }

    private void RegisterWater()
    {
        TileMapLayer groundLayer = GetNodeOrNull<TileMapLayer>("../../GroundLayer");
        if (groundLayer == null) return;

        var usedCells = groundLayer.GetUsedCells();
        
        foreach (Vector2I cell in usedCells)
        {
            int sourceId = groundLayer.GetCellSourceId(cell);
            
            if (sourceId == 5) 
            {
                _aiManager.BlockCell(cell);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_currentTestEnemy != null && IsInstanceValid(_currentTestEnemy))
        {
            QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Vector2 mousePos = GetGlobalMousePosition();
            SpawnAndMoveEnemy(mousePos);
        }
    }

    private void SpawnAndMoveEnemy(Vector2 spawnPos)
    {
        if (_playerNode == null)
        {
            _playerNode = GetNodeOrNull<Node2D>("../../DummyPlayer");
            if (_playerNode == null) return;
        }

        if (_currentTestEnemy != null && IsInstanceValid(_currentTestEnemy))
        {
            _currentTestEnemy.QueueFree();
        }

        _currentTestEnemy = _enemyScene.Instantiate<EnemyBase>();
        _currentTestEnemy.GlobalPosition = spawnPos;

        _currentTestEnemy.GridManager = _aiManager;
        _currentTestEnemy.TargetPlayer = _playerNode;
        _currentTestEnemy.PathRefreshRate = 0.5; 
        AddChild(_currentTestEnemy);
    }

    public override void _Draw()
    {
        if (_currentTestEnemy != null && IsInstanceValid(_currentTestEnemy) && _currentTestEnemy.CurrentPath != null && _currentTestEnemy.CurrentPath.Count > 1)
        {
            for (int i = 0; i < _currentTestEnemy.CurrentPath.Count - 1; i++)
            {
                Vector2 p1 = new Vector2(_currentTestEnemy.CurrentPath[i].X * TileSize + (TileSize / 2f), _currentTestEnemy.CurrentPath[i].Y * TileSize + (TileSize / 2f));
                Vector2 p2 = new Vector2(_currentTestEnemy.CurrentPath[i+1].X * TileSize + (TileSize / 2f), _currentTestEnemy.CurrentPath[i+1].Y * TileSize + (TileSize / 2f));
                
                DrawLine(p1, p2, Colors.Red, 2.0f);
            }
        }

        // WIZUALIZACJA KAR (Do usuniecia potem)
        if (_aiManager != null && _aiManager.ObstaclePenalties != null)
        {
            for (int x = 0; x < _aiManager.ObstaclePenalties.GetLength(0); x++)
            {
                for (int y = 0; y < _aiManager.ObstaclePenalties.GetLength(1); y++)
                {
                    if (_aiManager.ObstaclePenalties[x, y] > 0)
                    {
                        DrawRect(new Rect2(x * TileSize, y * TileSize, TileSize, TileSize), new Color(1, 0, 0, 0.4f));
                    }
                }
            }
        }
    }
}