using Godot;
using System;

public partial class Bolt : Area2D
{
    [Export] public float Speed = 250.0f;
    public int Damage = 10;
    public Vector2 Direction = Vector2.Zero;

    public override void _Ready()
    {
        GetTree().CreateTimer(5.0f).Timeout += QueueFree;
        
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += Direction * Speed * (float)delta;
    }

    public void Initialize(Node2D target, int damage)
    {
        Damage = damage;
        
        if (IsInstanceValid(target))
        {
            Direction = (target.GlobalPosition - GlobalPosition).Normalized();
            
            Rotation = Direction.Angle(); 
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is EnemyBase enemy)
        {
            enemy.TakeDamage(Damage);
            QueueFree();
            return;
        }

        if (body.IsInGroup("walls"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }
            QueueFree();
            return;
        }
        
        if (body.IsInGroup("player"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }
            QueueFree();
        }
    }
}