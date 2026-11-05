using Godot;
using System;

public partial class Wall : Building
{
    [Export] public int ReflectedDamage = 0; 

    public override void _Ready()
    {
        base._Ready(); 
        
        AddToGroup("walls"); 
    }

    public override void TakeDamage(int amount, Node2D attacker = null)
    {
        base.TakeDamage(amount, attacker);

        if (ReflectedDamage > 0 && attacker != null)
        {
            if (attacker is IDamageable damageableAttacker)
            {
                GD.Print($"[Mur] Parzy atakującego za {ReflectedDamage} HP!");
                damageableAttacker.TakeDamage(ReflectedDamage, this);
            }
        }
    }

    protected override void Die()
    {
        GD.Print("[Mur] Konstrukcja zniszczona! Ścieżka została otwarta.");
        base.Die();
    }
}