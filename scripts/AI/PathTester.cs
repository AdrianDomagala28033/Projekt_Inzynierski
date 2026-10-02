using Godot;

public partial class PathTester : Node2D
{
    private AiGridManager _aiManager;
    private PackedScene _enemyScene;
    private Enemy _currentTestEnemy;
    private Node2D _playerNode;
    
    private const int TileSize = 16;

    public override void _Ready()
    {
        _aiManager = GetNode<AiGridManager>("../AiGridManager");
        _enemyScene = GD.Load<PackedScene>("res://scenes/AI/Characters/Enemy.tscn");
        
        _playerNode = GetNodeOrNull<Node2D>("../../DummyPlayer");
        
        if (_playerNode == null)
        {
            GD.PrintErr("[PathTester] UWAGA: Nie znaleziono DummyPlayer przy starcie!");
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

        _currentTestEnemy = _enemyScene.Instantiate<Enemy>();
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
    }
}