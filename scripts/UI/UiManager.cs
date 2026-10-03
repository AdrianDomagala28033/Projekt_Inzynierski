using Godot;
using System;

public partial class UiManager : CanvasLayer
{
    [Export] public Label woodCounter;
    [Export] public Label rockCounter;

    public override void _Process(double delta)
    {
        woodCounter.Text = $"{Player.localPlayer.woodCount}";
        rockCounter.Text = $"{Player.localPlayer.rockCount}";
    }

}
