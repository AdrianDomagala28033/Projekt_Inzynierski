using Godot;
using System;
using System.Collections.Generic;

public enum MaterialType
{
    Wood = 0,
    Rock = 1,
    Body = 2
}
public partial class DestructibleEntity : Node2D
{
    [Export] public MaterialType materialType;
    [Export] public int dropQuantity;
    [Export] public int maxHealth;
    public int currentHealth;

    public override void _Ready()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(Tool tool)
    {
        if(!Multiplayer.IsServer()) return;
        int damage = 0;
        long senderId = Multiplayer.GetRemoteSenderId();
        var player = GetNode<Player>($"/root/Main/PlayersManager/PlayersContainer/{senderId}");
        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        if(distance >= 30) return;
        Rpc(nameof(PlayHitEffectRpc));
        switch (materialType)
        {
            case MaterialType.Wood:
                if(tool != Tool.Axe)
                    damage = 1;
                else
                    damage = 40;
                break;
            case MaterialType.Rock:
                if(tool != Tool.Pickaxe)
                    damage = 1;
                else
                    damage = 40;
                break;
            case MaterialType.Body:
                if(tool != Tool.Sword)
                    damage = 1;
                else
                    damage = 40;
                break;
        }
        if(player.CanHoldResources(materialType))
        {
            currentHealth -= damage;
            if(currentHealth <= 0)
            {
                player.AddResources(dropQuantity, materialType);
                QueueFree();   
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestTakeDamage(int usedToolId)
    {
        TakeDamage((Tool)usedToolId);
        GD.Print(currentHealth);
        long senderId = Multiplayer.GetRemoteSenderId();
    }
    private void _on_area_2d_input_event(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            float distance = GlobalPosition.DistanceTo(Player.localPlayer.GlobalPosition);
            if(distance >= 30) return;
            Player.localPlayer.PerformToolAction(GlobalPosition, () => { RpcId(1, MethodName.RequestTakeDamage, (int)Player.localPlayer.activeTool); });
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void PlayHitEffectRpc()
    {
        var particles = GetNode<CpuParticles2D>("CPUParticles2D");
        particles.Emitting = true;
        var particles2 = GetNode<CpuParticles2D>("CPUParticles2D2");
        particles2.Emitting = true;
    }
}