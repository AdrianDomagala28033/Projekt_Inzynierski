using Godot;

public partial class DummyPlayer : CharacterBody2D
{
    private const float Speed = 150.0f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Vector2.Zero;

        if (Input.IsPhysicalKeyPressed(Key.W)) velocity.Y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.S)) velocity.Y += 1;
        if (Input.IsPhysicalKeyPressed(Key.A)) velocity.X -= 1;
        if (Input.IsPhysicalKeyPressed(Key.D)) velocity.X += 1;

        Velocity = velocity.Normalized() * Speed;
        MoveAndSlide();
    }
}