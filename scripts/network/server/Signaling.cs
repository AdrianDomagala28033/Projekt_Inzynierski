using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class Signaling : Node
{
	private WebSocketPeer socket = new WebSocketPeer();
	private string Intention = "";
	private bool IsConnected = false;
	private string JoinCode = "";
	private string ServerAddress = "wss://the-fools-draw-server.onrender.com";
	private Dictionary<int, WebRtcPeerConnection> Connections = new Dictionary<int, WebRtcPeerConnection>();
	public WebRtcMultiplayerPeer RtcPeer = new WebRtcMultiplayerPeer();
	public int MyId = 1;

	public override void _Ready()
	{

	}
	public override void _Process(double delta)
	{
		socket.Poll();
		if (socket.GetReadyState() == WebSocketPeer.State.Open)
		{
			if(IsConnected == false)
			{
				IsConnected = true;
				string greeting = $"{{\"akcja\": \"{Intention}\"}}";
				if(Intention == "host")
				{
					socket.SendText(greeting);
				}
				else if(Intention == "join")
				{
					greeting = $"{{\"akcja\": \"{Intention}\", \"kod\": \"{JoinCode}\"}}";
					socket.SendText(greeting);
				}
			}
			while(socket.GetAvailablePacketCount() > 0)
			{
				byte[] package = socket.GetPacket();
				string message = Encoding.UTF8.GetString(package);
				GD.Print(message);
				var data = Json.ParseString(message).AsGodotDictionary();
				if((string)data["akcja"] == "twoj_kod")
				{
					string gotenCode = (string)data["kod"];
					GD.Print("Serwer nadał mi kod: " + gotenCode);
					RtcPeer.CreateServer();
					Multiplayer.MultiplayerPeer = RtcPeer;
					var ConnectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
					ConnectionManager.RegisterNewPlayer(1, ConnectionManager.LocalPlayerName, false);
					
					ConnectionManager.LobbyCode = gotenCode;
					ConnectionManager.ReportConnection();
				}
				else if((string)data["akcja"] == "polaczono")
				{
					GD.Print("Udało się dołączyć!");
					MyId = (int)(GD.Randi() % 10000) + 2;
					RtcPeer.CreateClient(MyId);
					Multiplayer.MultiplayerPeer = RtcPeer;
					socket.SendText($"{{\"akcja\": \"gotowy\", \"od_kogo\": {MyId}, \"do_kogo\": 1}}");
				}
				else if((string)data["akcja"] == "blad")
				{
					socket.Close();
					IsConnected = false;
					GD.Print("Błąd: Zły kod!");
				}
				else if((string)data["akcja"] == "oferta")
				{
					if (!data.ContainsKey("do_kogo") || (int)(double)data["do_kogo"] != MyId) continue;
					int id = (int)(double)data["od_kogo"];
					string sdp = (string)data["sdp"];
					string type = (string)data["typ"];
					if (!Connections.ContainsKey(id))
					{
						CreateConnection(id);
					}
					Connections[id].SetRemoteDescription(type, sdp);
				}
				else if((string)data["akcja"] == "ice")
				{
					if (!data.ContainsKey("do_kogo") || (int)(double)data["do_kogo"] != MyId) continue;
					int id = (int)(double)data["od_kogo"];
					string media = (string)data["media"];
					int index = (int)(double)data["index"];
					string name = (string)data["name"];
					Connections[id].AddIceCandidate(media, index, name);
				}
				else if ((string)data["akcja"] == "gotowy")
				{
					if (Multiplayer.IsServer()) 
					{
						int newPlayerId = (int)(double)data["od_kogo"];
						CreateConnection(newPlayerId);
						Connections[newPlayerId].CreateOffer();
					}
				}
			}
		}
	}
	public void CreateRoom()
	{
		socket.Close();
		Intention = "host";
		socket.ConnectToUrl(ServerAddress);
	}
	public void JoinToRoom(string kod)
	{
		socket.Close();
		Intention = "join";
		JoinCode = kod;
		socket.ConnectToUrl(ServerAddress);
	}
	private void CreateConnection(int playerId)
	{
		WebRtcPeerConnection newConnection = new WebRtcPeerConnection();
		newConnection.Initialize(new Godot.Collections.Dictionary());
		newConnection.SessionDescriptionCreated += (type, sdp) =>
		{
		   OnOfferCreated(type, sdp, playerId); 
		};
		newConnection.IceCandidateCreated += (media, index, name) =>
		{
			OnIceCreated(media, index, name, playerId);
		};
		Connections[playerId] = newConnection;
		RtcPeer.AddPeer(newConnection, playerId);
	}

	private void OnIceCreated(string media, long index, string name, int playerId)
	{
		var dict = new Godot.Collections.Dictionary();
		dict["akcja"] = "ice";
		dict["media"] = media;
		dict["index"] = index;
		dict["name"] = name;
		dict["od_kogo"] = MyId;
		dict["do_kogo"] = playerId;
		socket.SendText(Json.Stringify(dict));
	}

	private void OnOfferCreated(string type, string sdp, int playerId)
	{
		var connection = Connections[playerId];
		connection.SetLocalDescription(type, sdp);
		var dict = new Godot.Collections.Dictionary();
		dict["akcja"] = "oferta";
		dict["typ"] = type;
		dict["sdp"] = sdp;
		dict["od_kogo"] = MyId;
		dict["do_kogo"] = playerId;
		socket.SendText(Json.Stringify(dict));
	}
	public void EndConnection()
	{
		socket.Close();
		IsConnected = false;
		RtcPeer.Close();
		Connections.Clear();
	}
}
