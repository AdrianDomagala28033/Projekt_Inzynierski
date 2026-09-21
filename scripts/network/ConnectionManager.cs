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
    public int GlobalMapSeed;
    
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
        GetNode<Signaling>("/root/Signaling").JoinToRoom(lobbyCode.ToUpper());
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
    public void SetAsReady()
    {
        RpcId(1, nameof(HandlePlayerIsReady));
    }
    public void ChangeScene(string path)
    {
        GD.Print($"[NETWORK] Zmieniam scenę na: {path}");
            GetTree().ChangeSceneToFile(path);
    }

#region funkcje rpc
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void HandlePlayerIsReady()
    {
        long idSender = Multiplayer.GetRemoteSenderId();
        if(idSender == 0) idSender = 1;
        var player = PlayerList.FirstOrDefault(g => g.Id == idSender);
        if(player != null)
        {
            bool newState = !player.IsReady;
            Rpc(nameof(UpdatePlayerState), idSender, newState);
            UpdatePlayerState(idSender, newState);
            GD.Print($"[SERVER] Gracz {idSender} zmienił gotowość na: {newState}");
        }
        else
            GD.PrintErr($"[SERVER] BŁĄD! Nie znaleziono gracza o ID: {idSender}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public void UpdatePlayerState(long playerId, bool isReady)
    {
        var gracz = PlayerList.FirstOrDefault(g => g.Id == playerId);
        if (gracz == null)
        {
            GD.PrintErr($"[NetworkManager] Błąd! Próba ustawienia gotowości dla nieznanego gracza ID: {playerId}");
            return;
        }

        gracz.IsReady = isReady;
        OnPlayerListChanged?.Invoke();
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
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal =true)]
    public void LoadMap(string path, int seed)
    {
        GD.Print($"[NETWORK] Wczytywanie mapy z seedem: {seed}");
        GlobalMapSeed = seed;
        GetTree().ChangeSceneToFile(path);
    }
#endregion
}