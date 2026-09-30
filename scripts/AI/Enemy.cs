using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
    [Export] public float Speed = 100f;
    
    private AiGridManager _aiManager;
    private List<Vector2I> _currentPath;
    private int _currentPathIndex = 0;

    public override void _Ready()
    {
        // Wróg szuka menedżera względem swojej pozycji w drzewie sceny
        _aiManager = GetNodeOrNull<AiGridManager>("../../AiGridManager");
        
        // Zabezpieczenie (fallback)
        if (_aiManager == null)
        {
            _aiManager = GetTree().Root.GetNodeOrNull<AiGridManager>("WorldMap/AiSystem/AiGridManager");
        }

        if (_aiManager == null)
        {
            GD.PrintErr("[Enemy] KRYTYCZNY BŁĄD: Nie mogę znaleźć AiGridManager!");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Jeśli nie ma ścieżki lub dotarliśmy do końca, zatrzymaj się
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            Velocity = Vector2.Zero;
            return; 
        }

        float tileSize = 16.0f; 
        
        // Pobierz aktualny punkt docelowy ze ścieżki i zamień na piksele (+ środek kafelka)
        Vector2I currentTargetGridPos = _currentPath[_currentPathIndex];
        Vector2 targetPixelPos = new Vector2(currentTargetGridPos.X * tileSize, currentTargetGridPos.Y * tileSize) + new Vector2(tileSize / 2, tileSize / 2);

        // Oblicz dystans
        float distanceToTarget = GlobalPosition.DistanceTo(targetPixelPos);

        // Jeśli jesteśmy blisko środka kratki, przejdź do następnego punktu ścieżki
        if (distanceToTarget < 2.0f)
        {
            _currentPathIndex++;
        }
        else
        {
            // W przeciwnym razie idź prosto do celu
            Vector2 direction = (targetPixelPos - GlobalPosition).Normalized();
            Velocity = direction * Speed;
            MoveAndSlide();
        }
    }

    public void MoveToTarget(Vector2I startGridPos, Vector2I targetGridPos)
    {
        if (_aiManager != null)
        {
            _currentPath = _aiManager.FindPath(startGridPos, targetGridPos);
            _currentPathIndex = 0;
            
            if (_currentPath != null)
                GD.Print($"[Enemy] Otrzymałem rozkaz i ruszyłem! Mam do pokonania kroków: {_currentPath.Count}");
            else
                GD.Print("[Enemy] Rozkaz odebrany, ale nie ma drogi do celu.");
        }
        else
        {
            GD.PrintErr("[Enemy] Dostałem rozkaz, ale nie mam mózgu (AiGridManager) żeby przeliczyć trasę!");
        }
    }
}