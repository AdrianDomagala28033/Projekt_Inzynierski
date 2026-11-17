using Godot;
using System.Collections.Generic;

public partial class Turret : Building 
{
    [ExportGroup("Turret Combat")]
    [Export] public PackedScene ProjectileScene; 
    [Export] public float FireRate = 1.0f; 
    [Export] public int Damage = 15; 

    [ExportGroup("AI Threat Settings")]
    [Export] public int ThreatPenalty = 50;
    
    private Area2D _detectionZone;
    private Marker2D _shootPoint;
    private AnimatedSprite2D _archerSprite;
    
    private List<EnemyBase> _enemiesInRange = new List<EnemyBase>();
    private EnemyBase _currentTarget;
    private double _fireTimer = 0;
    private bool _isThreatApplied = false;

    public override void _Ready()
    {
        base._Ready(); 

        _detectionZone = GetNode<Area2D>("DetectionZone");
        _shootPoint = GetNode<Marker2D>("ShootPoint");
        _archerSprite = GetNodeOrNull<AnimatedSprite2D>("ArcherSprite");

        _detectionZone.BodyEntered += OnBodyEntered;
        _detectionZone.BodyExited += OnBodyExited;

        CallDeferred(MethodName.InitTurretThreatDeferred);
        QueueRedraw();
    }

    private void InitTurretThreatDeferred()
    {
        if (IsQueuedForDeletion()) return;

        if (GridManager == null)
        {
            GridManager = GetTree().GetFirstNodeInGroup("GridManager") as AiGridManager;
        }

        if (GridManager != null)
        {
            if (GridManager.IsInitialized)
            {
                ApplyTurretThreat(true);
            }
            else
            {
                GridManager.GridInitialized += OnTurretGridInitialized;
            }
        }
    }

    private void OnTurretGridInitialized()
    {
        if (GridManager != null)
        {
            GridManager.GridInitialized -= OnTurretGridInitialized;
            ApplyTurretThreat(true);
        }
    }

    protected override void Die()
    {
        ApplyTurretThreat(false);
        base.Die();
    }

    public override void _ExitTree()
    {
        if (GridManager != null)
        {
            GridManager.GridInitialized -= OnTurretGridInitialized;
        }

        ApplyTurretThreat(false);
        base._ExitTree();
    }

    public void ApplyTurretThreat(bool addThreat)
    {
        if (GridManager == null || !GridManager.IsInitialized || _detectionZone == null) return;
        if (addThreat && _isThreatApplied) return;
        if (!addThreat && !_isThreatApplied) return;

        var collisionShape = _detectionZone.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collisionShape != null && collisionShape.Shape is CircleShape2D circleShape)
        {
            Vector2I centerTile = new Vector2I(
                Mathf.FloorToInt(collisionShape.GlobalPosition.X / 16.0f),
                Mathf.FloorToInt(collisionShape.GlobalPosition.Y / 16.0f)
            );
            float radiusInTiles = (circleShape.Radius / 16.0f) + 0.5f;

            GridManager.ModifyTurretHeat(centerTile, radiusInTiles, ThreatPenalty, addThreat);
            _isThreatApplied = addThreat;
        }
    }

    public override void _Process(double delta)
    {
        UpdateTarget();

        if (_currentTarget != null)
        {
            _fireTimer += delta;

            if (_fireTimer >= FireRate)
            {
                Shoot();
                _fireTimer = 0;
            }
        }
        else
        {
            _fireTimer = 0; 
            
            if (_archerSprite != null && _archerSprite.Animation.ToString().StartsWith("attack"))
            {
                _archerSprite.Play("idle");
            }
        }
    }

    private void UpdateTarget()
    {
        _enemiesInRange.RemoveAll(e => !IsInstanceValid(e) || e.IsQueuedForDeletion());

        if (_enemiesInRange.Count > 0)
        {
            _currentTarget = _enemiesInRange[0]; 
        }
        else
        {
            _currentTarget = null;
        }
    }

    private void Shoot()
    {
        if (ProjectileScene == null || _currentTarget == null) return;

        UpdateArcherAnimation();

        Node2D projectileInstance = ProjectileScene.Instantiate<Node2D>();
        GetTree().CurrentScene.AddChild(projectileInstance);
        
        projectileInstance.GlobalPosition = _shootPoint.GlobalPosition;

        if (projectileInstance.HasMethod("Initialize"))
        {
            projectileInstance.Call("Initialize", _currentTarget, Damage);
        }
    }

    private void UpdateArcherAnimation()
    {
        if (_archerSprite == null) return;

        Vector2 direction = _currentTarget.GlobalPosition - GlobalPosition;

        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            _archerSprite.Play("attack_side");
            _archerSprite.FlipH = direction.X < 0; 
        }
        else if (direction.Y < 0)
        {
            _archerSprite.Play("attack_up");
            _archerSprite.FlipH = false; 
        }
        else
        {
            _archerSprite.Play("attack_down");
            _archerSprite.FlipH = false;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is EnemyBase enemy)
        {
            if (!_enemiesInRange.Contains(enemy))
            {
                _enemiesInRange.Add(enemy);
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is EnemyBase enemy)
        {
            if (_enemiesInRange.Contains(enemy))
            {
                _enemiesInRange.Remove(enemy);
            }
        }
    }

    public override void _Draw()
    {
        if (_detectionZone != null)
        {
            var collisionShape = _detectionZone.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (collisionShape != null && collisionShape.Shape is CircleShape2D circleShape)
            {
                DrawCircle(collisionShape.Position, circleShape.Radius, new Color(1, 0, 0, 0.15f));
                DrawArc(collisionShape.Position, circleShape.Radius, 0, Mathf.Tau, 32, new Color(1, 0, 0, 0.4f), 1.0f);
            }
        }
    }
}