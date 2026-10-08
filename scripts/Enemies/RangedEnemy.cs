using Godot;
using System;

public partial class RangedEnemy : EnemyBase
{
    [ExportGroup("Attack Stats")]
    [Export] public int AttackDamage = 10;
    [Export] public float AttackRange = 120.0f;
    [Export] public double AttackCooldown = 2.0; 
    
    [ExportGroup("Prefabs")]
    [Export] public PackedScene ProjectileScene;
    
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
                PerformRangedAttack(TargetPlayer.GlobalPosition);
                _attackTimer = 0;
            }
            return;
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
        }
    }

    protected async void PerformRangedAttack(Vector2 targetPosition)
    {
        _isAttacking = true;
        
        if (_animatedSprite != null)
        {
            _animatedSprite.Play("attack_" + _currentFacing); 
        }

        if (ProjectileScene != null)
        {
            Bolt projectile = ProjectileScene.Instantiate<Bolt>();
            
            projectile.GlobalPosition = this.GlobalPosition;
            projectile.Damage = AttackDamage;
            
            projectile.IsPlayerProjectile = false;
            
            projectile.Direction = (targetPosition - this.GlobalPosition).Normalized();
            projectile.Rotation = projectile.Direction.Angle();
            
            GetTree().CurrentScene.AddChild(projectile);
        }

        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
        _isAttacking = false;
    }
}