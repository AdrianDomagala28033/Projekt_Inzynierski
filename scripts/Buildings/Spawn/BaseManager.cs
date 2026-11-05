using Godot;
using Godot.Collections;
using System;


public partial class BaseManager : Node2D
{
    [Export] public PackedScene warehouseScene;
    [Export] public PackedScene totemScene;
    [Export] public Node2D buildingsContainer;
    [Export] public TileMapLayer groundLayer;
    public Dictionary<MaterialType, int> inventory = new Dictionary<MaterialType, int>();
    public event Action<MaterialType, int> onQuantityChanged;
    public ConnectionManager connectionManager;

    public override void _Ready()
    {
        connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        foreach (MaterialType material in Enum.GetValues(typeof(MaterialType)))
        {
            inventory[material] = 0; 
        }
        connectionManager.RpcId(1, "PlayerFinishedLoadingRpc");
    }
    public void ReceiveItems(Dictionary<MaterialType, int> newItems)
    {
        if(!Multiplayer.IsServer()) return;
        foreach (var item in newItems)
        {
            inventory[item.Key] += item.Value;
            Rpc(MethodName.SyncItems, (int)item.Key, inventory[item.Key]);
        }
    }
    public bool TrySpendResources(Dictionary<MaterialType, int> requiredResources)
    {
        if(!Multiplayer.IsServer()) return false;

        GD.Print($"[Serwer] Próba wydania. Koszt składa się z {requiredResources?.Count ?? 0} pozycji.");
        foreach(var item in requiredResources)
        {
            GD.Print($"[Serwer] Potrzeba: {item.Key} x{item.Value}. W magazynie jest: {inventory[item.Key]}");
        }

        foreach (var item in requiredResources)
        {
            if(inventory[item.Key] < item.Value) return false;
        }
        foreach (var item in requiredResources)
        {
            inventory[item.Key] -= item.Value;
            Rpc(MethodName.SyncItems, (int)item.Key, inventory[item.Key]);
        }
        return true;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void SyncItems(int key, int value)
    {
        MaterialType material = (MaterialType)key;
        inventory[material] = value;
        onQuantityChanged?.Invoke((MaterialType)key, value);
        var inventoryInterface = GetTree().GetFirstNodeInGroup("Inventory") as Inventory;
        inventoryInterface.UpdateGlobalInventory();
        GD.Print($"Zaktualizowano {material}: masz teraz {inventory[material]} sztuk");
    }
    public async void SpawnInitialBase()
    {
        if (Multiplayer.IsServer())
        {
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
            var spaceState = GetWorld2D().DirectSpaceState;
            Vector2 desiredPos = new Vector2(50*16, 50*16);
            Vector2 safePos = SpawnLocator.GetFreeSpot(desiredPos, spaceState, new Vector2(93, 67), groundLayer);

            var warehouseInstance = warehouseScene.Instantiate<Warehouse>();
            //var totemInstance = totemScene.Instantiate<Totem>();
            warehouseInstance.GlobalPosition = safePos;
            //totemInstance.GlobalPosition = new Vector2(50*16-4, 50*16-4);
            warehouseInstance.Name = "Warehouse1";
            buildingsContainer.AddChild(warehouseInstance, true);
            //buildingsContainer.AddChild(totemInstance, true);
        }
    }
    

}
