using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
	[Export] public PackedScene PlayerTag;
	[Export] public VBoxContainer playerContainer;
	[Export] public Panel MenuButtons;
	[Export] public Panel MenuView;
	[Export] public Panel Lobby;
	[Export] public Panel JoinPanel;
	[Export] public Panel CreatePanel;
	[Export] public Button CreateGameMenuButton;
	[Export] public Button JoinGameMenuButton;
	[Export] public Button QuitGameMenuButton;
	[Export] public LineEdit PlayerName;
	[Export] public LineEdit HostName;
	[Export] public LineEdit LobbyCode;
	[Export] public Button Join;
	[Export] public Button Create;
	[Export] public Button ReadyButton;
	[Export] public Button StartGame;
	public ConnectionManager connectionManager;
	

	public override void _Ready()
	{
		connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
		Lobby.Visible = false;
		JoinPanel.Visible = false;
		CreatePanel.Visible = false;
		CreateGameMenuButton.Pressed += OpenCreateGamePanel;
		JoinGameMenuButton.Pressed += OpenJoinGamePanel;
		Join.Pressed += JoinToLobby;
		Create.Pressed += CreateLobby;
		ReadyButton.Pressed += SetPlayerReady;
		StartGame.Pressed += CreateGame;
		connectionManager.OnPlayerListChanged += RefreshLobby;
	}

	private void CreateGame()
	{
		bool canStart = false;
		if (Multiplayer.IsServer())
		{
			int mapSeed = new Random().Next();
			foreach (var player in connectionManager.PlayerList)
			{
				if(connectionManager.PlayerList.FirstOrDefault(p => p.IsReady) != null)
					canStart = true;
				player.IsReady = false;
			}
			connectionManager.Rpc(nameof(connectionManager.LoadMap), "res://scenes/World/WorldMap.tscn", mapSeed);
		}
	}

	private void SetPlayerReady()
	{
		connectionManager.SetAsReady();
	}

	private void RefreshLobby()
	{
		Lobby.Visible = true;
		JoinPanel.Visible = false;
		CreatePanel.Visible = false;

		foreach (Node child in playerContainer.GetChildren())
		{
			child.QueueFree();
		}
		foreach(PlayerData player in connectionManager.PlayerList)
		{
			Panel newElement = (Panel)PlayerTag.Instantiate();
			Label namePlayerLabel = newElement.GetNode<Label>("PlayerName");
			Label statusPlayerLabel = newElement.GetNode<Label>("PlayerStatus");

			namePlayerLabel.Text = player.Name;
			statusPlayerLabel.Text = player.IsReady ? "READY" : "WAITING...";
			playerContainer.AddChild(newElement);

			if(player.Id == Multiplayer.GetUniqueId())
				ReadyButton.Text = player.IsReady ? "NOT READY" : "READY";
		}
		

		if(connectionManager.PlayerList.Count > 0 && Multiplayer.IsServer())
			StartGame.Disabled = false;
		else
			StartGame.Disabled = true;
	}

	private void CreateLobby()
	{
		string hostName = HostName.Text;
		if (!string.IsNullOrEmpty(hostName))
		{
			connectionManager.HostGame(hostName);
			CreatePanel.Visible = false;
			Lobby.Visible = true;
		}
	}


	private void JoinToLobby()
	{
		string playerName = PlayerName.Text;
		string lobbyCode = LobbyCode.Text;
		if(!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(lobbyCode))
		{
			connectionManager.JoinGame(lobbyCode, playerName);
			connectionManager.LobbyCode = lobbyCode;
			JoinPanel.Visible = false;
			Lobby.Visible = true;
		}
	}

	private void OpenJoinGamePanel()
	{
		Lobby.Visible = false;
		CreatePanel.Visible = false;
		JoinPanel.Visible = true;
	}


	private void OpenCreateGamePanel()
	{
		Lobby.Visible = false;
		CreatePanel.Visible = true;
		JoinPanel.Visible = false;
	}

}
