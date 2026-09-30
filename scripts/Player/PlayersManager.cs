using Godot;
using System;

public partial class PlayersManager : Node
{
    [Export] public PackedScene playerScene;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            var connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
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

}
