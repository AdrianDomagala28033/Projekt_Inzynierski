using Godot;
using System;

public partial class Inventory : PanelContainer
{
    [ExportGroup("Player inventory")]
    [Export] public Label playerWoodCounter;
    [Export] public Label playerRockCounter;
    [Export] public Label playerIronCounter;
    [Export] public Label playerGoldCounter;

    [ExportGroup("Global inventory")]
    [Export] public Label globalWoodCounter;
    [Export] public Label globalRockCounter;
    [Export] public Label globalIronCounter;
    [Export] public Label globalGoldCounter;
    BaseManager baseManager;

    public override void _Ready()
    {
        baseManager = GetTree().GetFirstNodeInGroup("BaseManager") as BaseManager;
        globalWoodCounter.Text = $"{baseManager.inventory[MaterialType.Wood]}";
        globalRockCounter.Text = $"{baseManager.inventory[MaterialType.Rock]}";
    }

    public override void _Process(double delta)
    {
        
    }
    public void UpdateGlobalInventory()
    {
        globalWoodCounter.Text = $"{baseManager.inventory[MaterialType.Wood]}";
        globalRockCounter.Text = $"{baseManager.inventory[MaterialType.Rock]}";
    }
}
