using Godot;
using System;
using System.Collections.Generic;

public partial class NetworkSetup : Control
{
    [Export] public LineEdit LobbyCode;
    public event Action OnCreatePressed;
    public event Action OnJoinPressed;
    public event Action OnLeavePressed;
    public ConnectionManager connectionManager;

    public override void _Ready()
    {
        connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        Multiplayer.ConnectedToServer += OnConnected;
        connectionManager.OnLobbyCreated += OnCreated;
    }

    public void OnCreateButtonPressed()
    {
        GetNode<LoadingScreen>("/root/LoadingScreen").ShowLoading();
        connectionManager.HostGame(GetPlayerName(), GetPlayerAppearance());
    }
    public void OnJoinButtonPressed()
    {
        if(string.IsNullOrEmpty(LobbyCode.Text)) return;
        GetNode<LoadingScreen>("/root/LoadingScreen").ShowLoading();
        connectionManager.JoinGame(LobbyCode.Text, GetPlayerName(), GetPlayerAppearance());

    }
    public void OnLeaveButtonPressed()
    {
        OnLeavePressed?.Invoke();
    }
    private string GetPlayerName()
    {
        using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        var parsedData = Json.ParseString(jsonText).AsGodotDictionary();

        return parsedData["PlayerName"].AsString();
    }
    private string GetPlayerAppearance()
    {
        using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        var parsedData = Json.ParseString(jsonText).AsGodotDictionary();

        return Json.Stringify(parsedData["Appearance"]);
    }
    private void OnConnected()
    {
        GetNode<LoadingScreen>("/root/LoadingScreen").HideLoading();
        OnJoinPressed?.Invoke();
    }
    private void OnCreated(string code)
    {
        GetNode<LoadingScreen>("/root/LoadingScreen").HideLoading();
        OnCreatePressed?.Invoke();
    }
}
