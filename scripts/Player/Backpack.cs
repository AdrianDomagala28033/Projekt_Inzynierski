using Godot;
using Godot.Collections;
using System;

public partial class Backpack : Node
{
    [Export] public int maxBackpackCapacity;
    public Dictionary<MaterialType, int> playerInventory = new Dictionary<MaterialType, int>();

    public override void _Ready()
    {
        foreach (MaterialType material in Enum.GetValues(typeof(MaterialType)))
        {
            playerInventory[material] = 0; 
        }
    }
    public void AddResources(int quantity, MaterialType material)
    {
        int availableSpace = maxBackpackCapacity - playerInventory[material];
        playerInventory[material] += Math.Min(quantity, availableSpace);

        RpcId(GetMultiplayerAuthority(), MethodName.SyncInventoryRpc, playerInventory[material], (int)material);
    }
    public bool CanHoldResources(MaterialType material)
    {
        return playerInventory[material] < maxBackpackCapacity;
    }
    public Dictionary<MaterialType, int> EmptyBackpack()
    {
        Dictionary<MaterialType, int> tmp = playerInventory.Duplicate();
        foreach (var key in playerInventory.Keys)
        {
            playerInventory[key] = 0;
            RpcId(GetMultiplayerAuthority(), MethodName.SyncInventoryRpc, 0, (int)key);
        }
        return tmp;
     }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SyncInventoryRpc(int newVal, int materialId)
    {
        MaterialType material = (MaterialType)materialId;
        playerInventory[material] = newVal;
        GD.Print($"Zaktualizowano {material}: masz teraz {playerInventory[material]} sztuk");
    }

}
