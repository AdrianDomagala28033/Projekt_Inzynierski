using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Godot;
public partial class ConnectionManager : Node
{
    public List<PlayerData> PlayerList = new List<PlayerData>();
    public string LocalPlayerName = "player";
    public string LobbyCode;
    public event Action OnConnected;
    public event Action OnJoined;
    public event Action OnDisconnected;
    public event Action OnPlayerListChanged;
    
    public override void _Ready()
    {
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }
    public void HostGame(string name)
    {
        GetNode<Signaling>("/root/Signaling").CreateRoom();
        LocalPlayerName = name;
        GD.Print("Waiting for lobby code from server...");
    }
    public void JoinGame(string lobbyCode, string playerName)
    {
        LocalPlayerName = playerName;
        GetNode<Signaling>("/root/Signaling").JoinToRoom(lobbyCode);
    }

    private void OnPeerDisconnected(long id)
    {
        throw new NotImplementedException();
    }

    private void OnPeerConnected(long id)
    {
        if (Multiplayer.IsServer())
        {
            foreach (PlayerData player in PlayerList)
            {
                RpcId(id, nameof(RegisterNewPlayer), player.Id, player.Name, player.IsReady);
            }
        }
        GD.Print("Dołączył gracz o ID: " + id);
    }

    private void OnConnectedToServer()
    {
        string nazwa = LocalPlayerName;
        RpcId(1, nameof(JoiningRequest), nazwa);
        GD.Print("Hurra! Udało się połączyć z serwerem!");
        OnConnected?.Invoke();
    }
    public void ReportConnection()
    {
        OnConnected?.Invoke();
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void JoiningRequest(string nazwa)
    {
        if (!Multiplayer.IsServer()) return;
        long id = Multiplayer.GetRemoteSenderId();
        Rpc(nameof(RegisterNewPlayer), id, nazwa, false);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RegisterNewPlayer(long id, string name, bool isReady)
    {
        if (PlayerList.Any(g => g.Id == id)) return;
        PlayerList.Add(new PlayerData(id, name, isReady));
        OnPlayerListChanged?.Invoke();
        GD.Print($"Dodano gracza ID: {id}");
    }
}