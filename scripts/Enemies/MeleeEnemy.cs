using Godot;
using System;

public partial class MeleeEnemy : EnemyBase
{
    [ExportGroup("Attack Stats")]
    [Export] public int AttackDamage = 10;
    [Export] public float AttackRange = 20.0f; 
    [Export] public double AttackCooldown = 1.5; 
    
    protected double _attackTimer = 0;
    protected bool _isAttacking = false;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_isDead || TargetPlayer == null || GridManager == null) return;

        _attackTimer += delta;

        if (_isHurt || _isAttacking)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        float distanceToFinalTarget = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
        
        if (distanceToFinalTarget <= AttackRange)
        {
            Velocity = Vector2.Zero; 
            if (_attackTimer >= AttackCooldown)
            {
                UpdateFacingDirection(TargetPlayer.GlobalPosition - GlobalPosition);
                PerformAttackOn(TargetPlayer);
                _attackTimer = 0;
            }
            return;
        }

        if (IsInstanceValid(_targetedObstacle))
        {
            Velocity = Vector2.Zero;
            if (_attackTimer >= AttackCooldown)
            {
                UpdateFacingDirection(((Node2D)_targetedObstacle).GlobalPosition - GlobalPosition);
                PerformAttackOn(_targetedObstacle);
                _attackTimer = 0;
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
            if (_animatedSprite != null) _animatedSprite.Play("idle_" + _currentFacing);
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
            
            UpdateFacingDirection(direction);

            if (_animatedSprite != null)
            {
                _animatedSprite.Play("run_" + _currentFacing);
                _animatedSprite.FlipH = (direction.X < -0.1f);
            }

            if (!IsInstanceValid(_targetedObstacle))
            {
                for (int i = 0; i < GetSlideCollisionCount(); i++)
                {
                    KinematicCollision2D collision = GetSlideCollision(i);
                    Node2D collider = collision.GetCollider() as Node2D; 

                    if (collider != null && collider.IsInGroup("walls"))
                    {
                        if (IsFlying) continue;

                        Vector2I wallGridPos = new Vector2I(
                            Mathf.FloorToInt(collider.GlobalPosition.X / TileSize), 
                            Mathf.FloorToInt(collider.GlobalPosition.Y / TileSize)
                        );
                        
                        if (CurrentPath != null && CurrentPath.Contains(wallGridPos))
                        {
                            _targetedObstacle = collider;
                            Velocity = Vector2.Zero;
                            UpdateFacingDirection(collider.GlobalPosition - GlobalPosition);
                            PerformAttackOn(collider);
                            _attackTimer = 0;
                            break; 
                        }
                    }
                }
            }
        }
    }

    protected async void PerformAttackOn(Node target)
{
    _isAttacking = true;
    
    if (_animatedSprite != null)
    {
        _animatedSprite.Play("attack_" + _currentFacing); 
    }

    if (target.HasMethod("TakeDamage"))
    {
        target.Call("TakeDamage", AttackDamage, this);
    }

    await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
    _isAttacking = false;
}
}