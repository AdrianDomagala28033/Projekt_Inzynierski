using Godot;
using System;

public partial class UiManager : CanvasLayer
{
    [Export] public Label woodCounter;
    [Export] public Label rockCounter;

    public override void _Process(double delta)
    {
        woodCounter.Text = $"{Player.localPlayer.inventory[MaterialType.Wood]}";
        rockCounter.Text = $"{Player.localPlayer.inventory[MaterialType.Rock]}";
    }

}
