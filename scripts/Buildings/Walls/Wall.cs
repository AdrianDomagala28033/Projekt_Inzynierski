using Godot;
using System;

public partial class Wall : StaticBody2D
{
    [Export] public int MaxHealth = 50;
    
    [Export] public int ReflectedDamage = 0; 
    
    protected int _currentHealth;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        AddToGroup("walls");
    }

    public virtual void TakeDamage(int amount, Node2D attacker = null)
    {
        _currentHealth -= amount;
        GD.Print($"[Mur] Otrzymano {amount} obrażeń! Pozostało HP: {_currentHealth}");

        if (ReflectedDamage > 0 && attacker != null)
        {
            if (attacker.HasMethod("TakeDamage"))
            {
                GD.Print($"[Mur] Parzy atakującego za {ReflectedDamage} HP!");
                attacker.Call("TakeDamage", ReflectedDamage);
            }
        }

        if (_currentHealth <= 0)
        {
            DestroyWall();
        }
    }

    protected virtual void DestroyWall()
    {
        GD.Print("[Mur] Konstrukcja zniszczona! Ścieżka została otwarta.");
        QueueFree();
    }
}