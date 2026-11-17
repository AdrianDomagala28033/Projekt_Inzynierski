using Godot;
using System.Collections.Generic;

public partial class EnemyBase : CharacterBody2D, IDamageable
{
    [ExportGroup("Base Stats")]
    [Export] public int MaxHealth = 50;
    [Export] public float Speed = 100f;
    [Export] public bool IsFlying = false; 
    public int PathObstacleWeight = 1;

    [ExportGroup("Navigation & Threat")]
    [Export] public Node2D TargetPlayer;
    [Export] public double PathRefreshRate = 0.5; 
    [Export] public AiGridManager GridManager; 
    [Export] public bool IgnoreTowerThreat = false;

    public bool IsTargetingTotem { get; private set; } = false;
    
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
    private Node2D _fallbackTarget;

    public override void _Ready()
    {
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _currentHealth = MaxHealth; 
        _fallbackTarget = TargetPlayer;

        if (GridManager == null)
        {
            GridManager = GetTree().GetFirstNodeInGroup("GridManager") as AiGridManager 
                        ?? GetTree().CurrentScene?.GetNodeOrNull<AiGridManager>("AiSystem/AiGridManager");
        }

        if (GridManager != null)
        {
            GridManager.ThreatMapUpdated += OnThreatMapUpdated;
        }

        if (IsFlying)
        {
            this.CollisionMask = 0;
        }

        ResolveCurrentTarget();
        if (GridManager != null && TargetPlayer != null)
        {
            CalculatePathToPlayer();
        }
        else
        {
            GD.PushWarning($"[{Name}] Brak referencji przy spawnie! GridManager={GridManager != null}, TargetPlayer={TargetPlayer != null}");
        }
    }

    public override void _ExitTree()
    {
        if (GridManager != null)
        {
            GridManager.ThreatMapUpdated -= OnThreatMapUpdated;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead || GridManager == null) return;

        bool targetSwitched = ResolveCurrentTarget();
        if (TargetPlayer == null) return;

        if (targetSwitched)
        {
            _pathRefreshTimer = 0;
            _targetedObstacle = null;
            CalculatePathToPlayer();
            return;
        }

        _pathRefreshTimer += delta;

        if (_pathRefreshTimer >= PathRefreshRate)
        {
            _pathRefreshTimer = 0;
            CalculatePathToPlayer();
        }
    }

    private void OnThreatMapUpdated()
    {
        if (_isDead || GridManager == null) return;

        ResolveCurrentTarget();
        if (TargetPlayer != null)
        {
            CalculatePathToPlayer();
        }
    }

    protected bool ResolveCurrentTarget()
    {
        bool wasTargetingTotem = IsTargetingTotem;

        Node2D totem = GetTree().GetFirstNodeInGroup("Totem") as Node2D;
        bool isTotemAlive = IsInstanceValid(totem) && !totem.IsQueuedForDeletion();

        if (isTotemAlive && totem is Building totemBuilding && totemBuilding.CurrentHealth <= 0)
        {
            isTotemAlive = false;
        }

        if (isTotemAlive)
        {
            TargetPlayer = totem;
            IsTargetingTotem = true;
            return !wasTargetingTotem;
        }

        IsTargetingTotem = false;
        Node2D nearestPlayer = FindNearestPlayer();

        if (nearestPlayer != null)
        {
            TargetPlayer = nearestPlayer;
        }
        else if (IsInstanceValid(_fallbackTarget) && !_fallbackTarget.IsQueuedForDeletion())
        {
            TargetPlayer = _fallbackTarget;
        }
        else
        {
            TargetPlayer = null;
        }

        return wasTargetingTotem;
    }

    private Node2D FindNearestPlayer()
{
    var players = GetTree().GetNodesInGroup("Player");
    if (players.Count == 0)
    {
        players = GetTree().GetNodesInGroup("players");
        if (players.Count == 0) players = GetTree().GetNodesInGroup("player");
    }

    Node2D closest = null;
    float minDistanceSq = float.MaxValue;

    foreach (Node node in players)
    {
        if (node is Node2D playerNode && IsInstanceValid(playerNode) && !playerNode.IsQueuedForDeletion())
        {
            float distSq = GlobalPosition.DistanceSquaredTo(playerNode.GlobalPosition);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                closest = playerNode;
            }
        }
    }

    return closest;
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
            
            OnDeath();
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
        if (TargetPlayer == null || !IsInstanceValid(TargetPlayer)) return;

        Vector2I startGridPos = new Vector2I(Mathf.FloorToInt(GlobalPosition.X / TileSize), Mathf.FloorToInt(GlobalPosition.Y / TileSize));
        Vector2I targetGridPos = new Vector2I(Mathf.FloorToInt(TargetPlayer.GlobalPosition.X / TileSize), Mathf.FloorToInt(TargetPlayer.GlobalPosition.Y / TileSize));

        if (IsFlying)
        {
            CurrentPath = new List<Vector2I> { startGridPos, targetGridPos };
            _currentPathIndex = 1;
            _targetedObstacle = null; 
            return; 
        }

        List<Vector2I> newPath = GridManager.FindPath(startGridPos, targetGridPos, PathObstacleWeight, IgnoreTowerThreat);
                
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