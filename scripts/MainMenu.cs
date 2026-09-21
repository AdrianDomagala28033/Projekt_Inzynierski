using Godot;
using System;

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
	public ConnectionManager connectionManager;
	

	public override void _Ready()
	{
		connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
		Lobby.Visible = false;
		JoinPanel.Visible = false;
		CreatePanel.Visible = false;
		CreateGameMenuButton.Pressed += OpenCrateGamePanel;
		JoinGameMenuButton.Pressed += OpenJoinGamePanel;
		Join.Pressed += JoinToLobby;
		Create.Pressed += CreateLobby;
		connectionManager.OnPlayerListChanged += RefreshLobby;
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
		}
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


	private void OpenCrateGamePanel()
	{
		Lobby.Visible = false;
		CreatePanel.Visible = true;
		JoinPanel.Visible = false;
	}

}
