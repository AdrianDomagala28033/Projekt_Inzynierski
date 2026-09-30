using Godot;
using System.Collections.Generic;

public partial class PathTester : Node2D
{
    private AiGridManager _aiManager;
    private List<Vector2I> _currentPath;
    
    private Vector2I _startPos = new Vector2I(10, 10);
    private Vector2I _targetPos = new Vector2I(20, 20);
    
    private const int TileSize = 16;

    // NOWE ZMIENNE DLA WROGA
    private PackedScene _enemyScene;
    private Enemy _currentTestEnemy;

    public override void _Ready()
    {
        _aiManager = GetNode<AiGridManager>("../AiGridManager");
        
        // Ładowanie sceny przeciwnika (upewnij się, że ścieżka do pliku jest poprawna)
        _enemyScene = GD.Load<PackedScene>("res://scenes/AI/Enemy.tscn");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Vector2 mousePos = GetGlobalMousePosition();
                _targetPos = new Vector2I((int)(mousePos.X / TileSize), (int)(mousePos.Y / TileSize));
                
                CalculateAndDrawPath();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                Vector2 mousePos = GetGlobalMousePosition();
                _startPos = new Vector2I((int)(mousePos.X / TileSize), (int)(mousePos.Y / TileSize));
                
                CalculateAndDrawPath();
            }
        }
    }

    private void CalculateAndDrawPath()
    {
        if (_aiManager != null)
        {
            _currentPath = _aiManager.FindPath(_startPos, _targetPos);
            GD.Print($"[AI] Przeliczono ścieżkę z {_startPos} do {_targetPos}. Liczba kroków: {_currentPath?.Count ?? 0}");

            QueueRedraw();

            // Jeśli ścieżka istnieje, spawniemy wroga
            if (_currentPath != null && _currentPath.Count > 0)
            {
                SpawnAndMoveEnemy();
            }
        }
    }

    private void SpawnAndMoveEnemy()
    {
        // Jeśli na mapie jest już jakiś testowy przeciwnik, usuwamy go
        if (_currentTestEnemy != null && IsInstanceValid(_currentTestEnemy))
        {
            _currentTestEnemy.QueueFree();
        }

        // Tworzymy nowego przeciwnika z załadowanej sceny
        _currentTestEnemy = _enemyScene.Instantiate<Enemy>();
        
        // Ustawiamy go na pozycji startowej (zielone kółko, przeliczone na piksele)
        Vector2 startPixelPos = new Vector2(_startPos.X * TileSize + (TileSize / 2f), _startPos.Y * TileSize + (TileSize / 2f));
        _currentTestEnemy.GlobalPosition = startPixelPos;

        // Dodajemy go do drzewa sceny, żeby zaczął żyć
        AddChild(_currentTestEnemy);

        // Wysyłamy go do celu
        _currentTestEnemy.MoveToTarget(_startPos, _targetPos);
    }

    public override void _Draw()
    {
        if (_currentPath != null && _currentPath.Count > 1)
        {
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                Vector2 p1 = new Vector2(_currentPath[i].X * TileSize + (TileSize / 2f), _currentPath[i].Y * TileSize + (TileSize / 2f));
                Vector2 p2 = new Vector2(_currentPath[i+1].X * TileSize + (TileSize / 2f), _currentPath[i+1].Y * TileSize + (TileSize / 2f));
                
                DrawLine(p1, p2, Colors.Red, 2.0f);
            }
        }

        DrawCircle(new Vector2(_startPos.X * TileSize + (TileSize / 2f), _startPos.Y * TileSize + (TileSize / 2f)), 4.0f, Colors.Green);
        DrawCircle(new Vector2(_targetPos.X * TileSize + (TileSize / 2f), _targetPos.Y * TileSize + (TileSize / 2f)), 4.0f, Colors.Blue);
    }
}