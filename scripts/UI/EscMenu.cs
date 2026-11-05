using Godot;
using System;

public partial class EscMenu : PanelContainer
{
    [Export] TextureButton saveButton;
    [Export] TextureButton optionsButton;
    [Export] TextureButton leaveButton;
    public ConnectionManager connectionManager;
    public event Action OnLeavePressed;

    public override void _Ready()
    {
        connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        this.Visible = false;
        ProcessMode = Node.ProcessModeEnum.Always;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("esc_menu") && !@event.IsEcho())
        {
            Visible = !Visible;
            GetTree().Paused = Visible;
            GetTree().Root.SetInputAsHandled();
        }
    }
    public void OnLeaveButtonPressed()
    {
        GetTree().Paused = false;
        Multiplayer.MultiplayerPeer = null; 

        connectionManager.ClearNetworkState();
        var signaling = GetNode<Signaling>("/root/Signaling");
        signaling.EndConnection();
        
        connectionManager.ChangeScene("res://scenes/menu/MenuController.tscn");
    }

}
