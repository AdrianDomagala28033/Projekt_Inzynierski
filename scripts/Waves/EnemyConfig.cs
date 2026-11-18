using Godot;

public enum EnemyRole
{
    Melee,
    Ranged,
    Special 
}

[GlobalClass]
public partial class EnemyConfig : Resource
{
    [Export] public string EnemyName = "placeholder";
    [Export] public PackedScene EnemyScene;
    [Export] public EnemyRole Role = EnemyRole.Melee;

    [ExportGroup("Wave Balance")]
    [Export] public int SpawnCost = 10;
    [Export] public int MinWave = 1;
}