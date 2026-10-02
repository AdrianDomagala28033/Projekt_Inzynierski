using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
    [Export] public float Speed = 100f;
    [Export] public Node2D TargetPlayer; 
    [Export] public double PathRefreshRate = 1.0; 
    [Export] public AiGridManager GridManager; 
    
    [Export] public int MaxHealth = 50;
    [Export] public int AttackDamage = 10;
    [Export] public float AttackRange = 20.0f; 
    [Export] public double AttackCooldown = 1.5; 
    
    public List<Vector2I> CurrentPath;
    private int _currentPathIndex = 0;
    
    private AnimatedSprite2D _animatedSprite;
    private double _pathRefreshTimer = 0;
    private double _attackTimer = 0;
    private int _currentHealth;
    
    private const float TileSize = 16.0f;

    private bool _isDead = false;
    private bool _isHurt = false;
    private bool _isAttacking = false;
    
    private Node _targetedObstacle = null;

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _currentHealth = MaxHealth; 

        if (GridManager == null)
        {
            GridManager = GetNodeOrNull<AiGridManager>("../../AiGridManager");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead || TargetPlayer == null || GridManager == null) return;

        _attackTimer += delta;
        _pathRefreshTimer += delta;

        if (_isHurt || _isAttacking)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        if (_pathRefreshTimer >= PathRefreshRate)
        {
            _pathRefreshTimer = 0;
            CalculatePathToPlayer();
        }

        float distanceToFinalTarget = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
        
        if (distanceToFinalTarget <= AttackRange)
        {
            Velocity = Vector2.Zero; 
            
            if (_attackTimer >= AttackCooldown)
            {
                PerformAttackOn(TargetPlayer);
                _attackTimer = 0;
            }
            else if (!_isAttacking && _animatedSprite != null)
            {
                _animatedSprite.Play("idle");
            }
            return; 
        }

        if (IsInstanceValid(_targetedObstacle))
        {
            Velocity = Vector2.Zero;
            
            if (_attackTimer >= AttackCooldown)
            {
                PerformAttackOn(_targetedObstacle);
                _attackTimer = 0;
            }
            else if (!_isAttacking && _animatedSprite != null)
            {
                _animatedSprite.Play("idle");
            }
            return; 
        }
        else
        {
            _targetedObstacle = null; 
        }

        if (CurrentPath == null || _currentPathIndex >= CurrentPath.Count)
        {
            Velocity = Vector2.Zero;
            if (_animatedSprite != null) _animatedSprite.Play("idle");
            return; 
        }
        
        Vector2I currentTargetGridPos = CurrentPath[_currentPathIndex];
        Vector2 targetPixelPos = new Vector2(currentTargetGridPos.X * TileSize, currentTargetGridPos.Y * TileSize) + new Vector2(TileSize / 2, TileSize / 2);

        float distanceToWaypoint = GlobalPosition.DistanceTo(targetPixelPos);

        if (distanceToWaypoint < 5.0f)
        {
            _currentPathIndex++;
        }
        else
        {
            Vector2 direction = (targetPixelPos - GlobalPosition).Normalized();
            Velocity = direction * Speed;
            MoveAndSlide();

            if (_animatedSprite != null)
            {
                _animatedSprite.Play("run");
                if (direction.X < -0.1f) _animatedSprite.FlipH = true;
                else if (direction.X > 0.1f) _animatedSprite.FlipH = false;
            }

            if (!IsInstanceValid(_targetedObstacle))
            {
                for (int i = 0; i < GetSlideCollisionCount(); i++)
                {
                    KinematicCollision2D collision = GetSlideCollision(i);
                    Node2D collider = collision.GetCollider() as Node2D; 

                    if (collider != null && collider.IsInGroup("buildings"))
                    {
                        Vector2I wallGridPos = new Vector2I(
                            Mathf.FloorToInt(collider.GlobalPosition.X / TileSize), 
                            Mathf.FloorToInt(collider.GlobalPosition.Y / TileSize)
                        );
                        
                        if (CurrentPath != null && CurrentPath.Contains(wallGridPos))
                        {
                            _targetedObstacle = collider;
                            Velocity = Vector2.Zero;
                            PerformAttackOn(collider);
                            _attackTimer = 0;
                            break; 
                        }
                    }
                }
            }
        }
    }

    private async void PerformAttackOn(Node target)
    {
        _isAttacking = true;
        
        if (_animatedSprite != null)
        {
            _animatedSprite.Play("attack"); 
        }

        if (target.HasMethod("TakeDamage"))
        {
            target.Call("TakeDamage", AttackDamage);
        }

        await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        _isAttacking = false;
    }

    public async void TakeDamage(int amount)
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
            QueueFree(); 
        }
        else
        {
            _isHurt = true;
            
            if (_animatedSprite != null) 
                _animatedSprite.Play("hurt");
                
            await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            _isHurt = false;
        }
    }

    private void CalculatePathToPlayer()
    {
        Vector2I startGridPos = new Vector2I(Mathf.FloorToInt(GlobalPosition.X / TileSize), Mathf.FloorToInt(GlobalPosition.Y / TileSize));
        Vector2I targetGridPos = new Vector2I(Mathf.FloorToInt(TargetPlayer.GlobalPosition.X / TileSize), Mathf.FloorToInt(TargetPlayer.GlobalPosition.Y / TileSize));

        List<Vector2I> newPath = GridManager.FindPath(startGridPos, targetGridPos);
        
        if (newPath != null)
        {
            if (IsInstanceValid(_targetedObstacle))
            {
                int newPathPenalty = GetPathPenalty(newPath);
                
                if (newPathPenalty < 300)
                {
                    GD.Print("[Enemy] Wykryto wyłom! Zmieniam cel.");
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

    private int GetPathPenalty(List<Vector2I> path)
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
}