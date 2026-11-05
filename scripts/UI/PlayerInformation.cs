using Godot;
using System;

public partial class PlayerInformation : Control
{
    [Export] public TextureProgressBar healthBar;
    [Export] public Label backpackCapacity;
    
    public void InitializeInterface()
    {
        healthBar.MaxValue = Player.localPlayer.maxHealth;
        backpackCapacity.Text = $"{Player.localPlayer.backpack.SumItems()}/{Player.localPlayer.backpack.maxBackpackCapacity}";

        Player.localPlayer.backpack.OnItemsQuantityChanged += UpdateItemsCounter;
    }

    public void UpdateItemsCounter()
    {
        backpackCapacity.Text = $"{Player.localPlayer.backpack.SumItems()}/{Player.localPlayer.backpack.maxBackpackCapacity}";
    }

}
