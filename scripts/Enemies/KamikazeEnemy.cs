using Godot;

public partial class KamikazeEnemy : MeleeEnemy
{
    [ExportGroup("Kamikaze Stats")]
    [Export] public float FuseTime = 1.2f; 
    [Export] public int ExplosionDamage = 40; 

    private Area2D _explosionArea;
    private bool _isExploding = false;

    public override void _Ready()
    {
        base._Ready(); 

        PathObstacleWeight = 1000;
        
        _explosionArea = GetNodeOrNull<Area2D>("ExplosionArea");
        
        if (_explosionArea == null)
        {
            GD.PrintErr($"[{Name}] Brak węzła Area2D o nazwie 'ExplosionArea'!");
        }
    }

    protected override async void PerformAttackOn(Node target)
    {
        if (_isExploding) return;
        _isExploding = true;
        
        _isAttacking = true; 
        
        Velocity = Vector2.Zero;

        if (_animatedSprite != null)
        {
            _animatedSprite.Play("fuse"); 
        }

        await ToSignal(GetTree().CreateTimer(FuseTime), SceneTreeTimer.SignalName.Timeout);

        if (_animatedSprite != null)
        {
            _animatedSprite.Scale = new Vector2(1.5f, 1.5f);
            _animatedSprite.Play("explosion"); 
        }

        if (_explosionArea != null)
        {
            var overlappingBodies = _explosionArea.GetOverlappingBodies();
            foreach (var body in overlappingBodies)
            {
                if (body != this && body is IDamageable)
                {
                    if (body.HasMethod("TakeDamage"))
                    {
                        body.Call("TakeDamage", ExplosionDamage, this);
                        GD.Print($"[Kamikaze] Zranił obszarowo: {body.Name} za {ExplosionDamage} HP!");
                    }
                }
            }
        }

        if (_animatedSprite != null)
        {
            await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }
        
        QueueFree();
    }
}