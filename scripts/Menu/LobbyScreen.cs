using Godot;
using System;
using System.Linq;

public partial class LobbyScreen : CanvasLayer
{
    [Export] public PackedScene PlayerAvatarScene;
    [Export] Control AvatarContainer;
    [Export] LineEdit CodeDisplay;
    [Export] TextureButton ReadyButton;
    [Export] TextureButton StartButton;
    public event Action OnLeavePressed;
    private ConnectionManager connectionManager;

    public override void _Ready()
    {
        connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        CodeDisplay.Text = connectionManager.LobbyCode;
        connectionManager.OnPlayerListChanged += RefreshLobby;
        if(!Multiplayer.IsServer()) StartButton.Hide();
        if(Multiplayer.IsServer()) StartButton.Disabled = true;
        RefreshLobby();
    }
    public override void _ExitTree()
    {
        connectionManager.OnPlayerListChanged -= RefreshLobby;
    }


    private void RefreshLobby()
    {
        foreach (var item in AvatarContainer.GetChildren())
            item.QueueFree();
        
        foreach (var player in connectionManager.PlayerList)
        {
            var playerAvatarInstance = PlayerAvatarScene.Instantiate();
            if (playerAvatarInstance is PlayerAvatar avatarScript)
            {
                avatarScript.SetNetworkData(player.Name, player.IsReady, player.AppearanceJson);
                avatarScript.Scale = new Vector2(1.5f, 1.5f);
            }
            AvatarContainer.AddChild(playerAvatarInstance);
            
        }
        if (Multiplayer.IsServer())
        {
            if(connectionManager.PlayerList.Count > 0)
            {
                if(connectionManager.PlayerList.All(p => p.IsReady))
                    StartButton.Disabled = false;
                else
                    StartButton.Disabled = true;
            }
        }
    }
    public void OnReadyButtonPressed()
    {
        connectionManager.SetAsReady();
    }
    public void OnStartButtonPressed()
    {
        int mapSeed = new Random().Next();
        connectionManager.Rpc(nameof(connectionManager.LoadMap), "res://scenes/main.tscn", mapSeed);
    }
    public void OnLeaveButtonPressed()
    {
        connectionManager.OnPlayerListChanged -= RefreshLobby;
        connectionManager.PlayerList.Clear();
        var signaling = GetNode<Signaling>("/root/Signaling");
        signaling.EndConnection();
        OnLeavePressed?.Invoke();
    }

}
