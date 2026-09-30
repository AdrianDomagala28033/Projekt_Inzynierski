using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
    [Export] public float Speed = 100f;
    
    private AiGridManager _aiManager;
    private List<Vector2I> _currentPath;
    private int _currentPathIndex = 0;

    private AnimatedSprite2D _animatedSprite;

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        _aiManager = GetNodeOrNull<AiGridManager>("../../AiGridManager");
        
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
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            Velocity = Vector2.Zero;

            if (_animatedSprite != null)
            {
                _animatedSprite.Stop();
            }

            return; 
        }

        float tileSize = 16.0f; 
        
        Vector2I currentTargetGridPos = _currentPath[_currentPathIndex];
        Vector2 targetPixelPos = new Vector2(currentTargetGridPos.X * tileSize, currentTargetGridPos.Y * tileSize) + new Vector2(tileSize / 2, tileSize / 2);

        float distanceToTarget = GlobalPosition.DistanceTo(targetPixelPos);

        if (distanceToTarget < 2.0f)
        {
            _currentPathIndex++;
        }
        else
        {
            Vector2 direction = (targetPixelPos - GlobalPosition).Normalized();
            Velocity = direction * Speed;
            MoveAndSlide();

            _animatedSprite.Play("run");

  
            if (direction.X < -0.1f)
            {
                _animatedSprite.FlipH = true;
            }
            else if (direction.X > 0.1f)
            {
                _animatedSprite.FlipH = false;
            }
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