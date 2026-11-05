using Godot;
using System;

public partial class PlayersManager : Node
{
    [Export] public PackedScene playerScene;
    private ConnectionManager connectionManager;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
            connectionManager.OnDisconnected += DeletePlayer;
            var container = GetNode("PlayersContainer");

            Vector2 startPosition = new Vector2(50 * 16, 50 * 16);
            foreach (var player in connectionManager.PlayerList)
            {
                var playerInstance = playerScene.Instantiate<Player>();
                playerInstance.Name = player.Id.ToString();
                playerInstance.Position = startPosition;
                container.AddChild(playerInstance);
            }
        }
    }
    public void DeletePlayer(long id)
    {
        GetNodeOrNull<Player>($"PlayersContainer/{id}")?.QueueFree();
    }

}
