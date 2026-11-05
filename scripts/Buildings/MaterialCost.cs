using Godot;

[GlobalClass]
public partial class MaterialCost : Resource
{
    [Export] public MaterialType material;
    [Export] public int amount;
}