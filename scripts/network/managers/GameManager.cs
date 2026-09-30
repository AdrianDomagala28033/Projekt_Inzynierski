using Godot;
using System;

public partial class GameManager : Node
{
    public int TotalWood {get; set;}
    public int TotalRock {get; set;}
    public int WaveNumber {get; set;}
    public float WaveCounter {get; set;}
    public bool IsGameInProgress {get; set;}

    public Action<int, int> OnResourcesUpdated;
    public Action OnWaveTimerTicked;
    public Action OnGameOver;

    public void AddWood(int amountToAdd)
    {
        if(!Multiplayer.IsServer()) return;
        TotalWood += amountToAdd;
        Rpc(nameof(OnResourcesChanged), TotalWood, TotalRock);
    }
    public void AddRock(int quantity)
    {
        if(!Multiplayer.IsServer()) return;
        TotalRock += quantity;
        Rpc(nameof(OnResourcesChanged), TotalWood, TotalRock);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void OnResourcesChanged(int newTotalWood, int newTotalRock)
    {
        TotalWood = newTotalWood;
        TotalRock = newTotalRock;
        OnResourcesUpdated?.Invoke(TotalWood, TotalRock);
    }
    

}
