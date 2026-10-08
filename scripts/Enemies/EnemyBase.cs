using Godot;
using System.Collections.Generic;

public partial class EnemyBase : CharacterBody2D, IDamageable
{
    [ExportGroup("Base Stats")]
    [Export] public int MaxHealth = 50;
    [Export] public float Speed = 100f;
    [Export] public bool IsFlying = false; 
    public int PathObstacleWeight = 1;

    [ExportGroup("Navigation")]
    [Export] public Node2D TargetPlayer; 
    [Export] public double PathRefreshRate = 1.0; 
    [Export] public AiGridManager GridManager; 
    
    protected int _currentHealth;
    public List<Vector2I> CurrentPath;
    protected int _currentPathIndex = 0;
    
    protected AnimatedSprite2D _animatedSprite;
    protected double _pathRefreshTimer = 0;
    
    protected const float TileSize = 16.0f;

    protected bool _isDead = false;
    protected bool _isHurt = false;
    
    protected Node _targetedObstacle = null;
    protected string _currentFacing = "down";

    public override void _Ready()
    {
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _currentHealth = MaxHealth; 

        if (GridManager == null)
        {
            GridManager = GetNodeOrNull<AiGridManager>("../../AiGridManager");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead || TargetPlayer == null || GridManager == null) return;

        _pathRefreshTimer += delta;

        if (_pathRefreshTimer >= PathRefreshRate)
        {
            _pathRefreshTimer = 0;
            CalculatePathToPlayer();
        }
    }

    protected void UpdateFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            _currentFacing = "side";
        }
        else if (direction.Y < 0)
        {
            _currentFacing = "up";
        }
        else
        {
            _currentFacing = "down";
        }
    }

    public virtual async void TakeDamage(int amount, Node2D attacker = null)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            _isDead = true;
            Velocity = Vector2.Zero; 
            
            if (_animatedSprite != null) 
                _animatedSprite.Play("death");
            
            await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            
            OnDeath(); // <--- DODANY HAK NA ŚMIERĆ
            QueueFree(); 
        }
        else
        {
            _isHurt = true;
            
            if (_animatedSprite != null) 
                _animatedSprite.Play("hurt_" + _currentFacing); 
                
            await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            _isHurt = false;
        }
    }

    protected virtual void CalculatePathToPlayer()
    {
        Vector2I startGridPos = new Vector2I(Mathf.FloorToInt(GlobalPosition.X / TileSize), Mathf.FloorToInt(GlobalPosition.Y / TileSize));
        Vector2I targetGridPos = new Vector2I(Mathf.FloorToInt(TargetPlayer.GlobalPosition.X / TileSize), Mathf.FloorToInt(TargetPlayer.GlobalPosition.Y / TileSize));

        List<Vector2I> newPath = GridManager.FindPath(startGridPos, targetGridPos, PathObstacleWeight);
        
        if (newPath != null)
        {
            if (IsInstanceValid(_targetedObstacle))
            {
                int newPathPenalty = GetPathPenalty(newPath);
                
                if (newPathPenalty == 0)
                {
                    _targetedObstacle = null;
                    CurrentPath = newPath;
                    _currentPathIndex = 0;
                }
            }
            else
            {
                CurrentPath = newPath;
                _currentPathIndex = 0;
            }
        }
    }

    protected int GetPathPenalty(List<Vector2I> path)
    {
        if (path == null) return 0;
        
        int totalPenalty = 0;
        foreach (var pos in path)
        {
            if (pos.X >= 0 && pos.X < GridManager.ObstaclePenalties.GetLength(0) &&
                pos.Y >= 0 && pos.Y < GridManager.ObstaclePenalties.GetLength(1))
            {
                totalPenalty += GridManager.ObstaclePenalties[pos.X, pos.Y];
            }
        }
        return totalPenalty;
    }

        protected virtual void OnDeath(){}

}