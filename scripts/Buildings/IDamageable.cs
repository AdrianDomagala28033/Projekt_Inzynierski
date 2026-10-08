using Godot;

public interface IDamageable
{
    void TakeDamage(int amount, Node2D attacker);
}