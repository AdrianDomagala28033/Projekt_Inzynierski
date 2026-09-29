using Godot;
using System;

public partial class Player : CharacterBody2D
{
    private AnimatedSprite2D sprite;
    private int skinVariant = 1;
    [Export] Label playerName;
    [Export] public float speed;
    public override void _Ready()
    {
        AddToGroup("Players");
        
        int id = int.Parse(Name);
        SetMultiplayerAuthority(id);
        sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        var camera = GetNode<Camera2D>("Camera2D");
        camera.Enabled = IsMultiplayerAuthority();
        var connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        var playerData = connectionManager.PlayerList.Find(p => p.Id == id);
        if(playerData != null)
            playerName.Text = playerData.Name;
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!IsMultiplayerAuthority()) return;

        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        this.Velocity = direction*speed;

        PlayAnimation(direction, skinVariant);

        MoveAndSlide();
        Rpc(nameof(SyncState), GlobalPosition, direction);

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncState(Vector2 newPos, Vector2 currentDirection)
    {
        GlobalPosition = newPos;
        PlayAnimation(currentDirection, skinVariant);
    }
    private void PlayAnimation(Vector2 direction, int skinVariant)
    {
        if(direction != Vector2.Zero)
        {
            if(direction.X != 0)
                sprite.FlipH = direction.X < 0;
            sprite.Play($"walk_right_{skinVariant}");
        }
        
        else
            sprite.Play($"waiting_{skinVariant}");
    }

}
