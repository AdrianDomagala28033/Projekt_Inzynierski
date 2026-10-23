using Godot;
using Godot.Collections;
using System;

public partial class Warehouse : Building
{
    [Export] public Area2D interactionArea;
    public bool isPlayerInRange;

    public override void _Ready()
    {
        base._Ready();
        interactionArea.BodyEntered += OnBodyEntered;
        interactionArea.BodyExited += OnBodyExited;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (isPlayerInRange && @event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E)
        {
            Rpc(MethodName.RequestDepositRpc);
        }
    }

    public void OnBodyEntered(Node2D body)
    {
        if(body is Player && body.IsMultiplayerAuthority())
        {
            isPlayerInRange = true;
            GD.Print("Gracz jest w zasiegu");
        }
    }
    public void OnBodyExited(Node2D body)
    {
        if(body is Player && body.IsMultiplayerAuthority())
        {
            isPlayerInRange = false;
            GD.Print("Gracz juz nie jest w zasiegu");
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestDepositRpc()
    {
        if(!Multiplayer.IsServer()) return;
        var id = Multiplayer.GetRemoteSenderId();
        if(id == 0) id = 1;
        var player = GetNode<Player>($"/root/Main/PlayersManager/PlayersContainer/{id}");
        Dictionary<MaterialType, int> dict = player.backpack.EmptyBackpack();
        var baseManager = GetNode<BaseManager>("/root/Main/BaseManager");
        baseManager.ReceiveItems(dict);
    }
    
}
