using Godot;
using System;
using Godot.Collections;
using System.Runtime.CompilerServices;

public partial class BuildingMenu : PanelContainer
{
    [Export] public PackedScene buildingButton;
    [ExportGroup("Kategorie Budynków")]
    [Export] public Array<PackedScene> WallScenes = new Array<PackedScene>();
    [Export] public Array<PackedScene> TowerScenes = new Array<PackedScene>();
    public event Action<PackedScene> OnBuildingSelected;
    

    public override void _Ready()
    {
        var wallContainer = GetNode<GridContainer>("MarginContainer/TabContainer/Walls/WallsGrid");
        var towerContainer = GetNode<GridContainer>("MarginContainer/TabContainer/Towers/TowersGrid");

        GenerateBuildingButtons(WallScenes, wallContainer);
        GenerateBuildingButtons(TowerScenes, towerContainer);

        OnBuildingSelected += BuildingSelected;
        
        
    }
    


    public void GenerateBuildingButtons(Array<PackedScene> array, GridContainer targetGrid)
    {
        Texture2D finalIconTexture;
        foreach (var scene in array)
        {
            if (scene == null) continue;

            Building tempBuilding = scene.Instantiate<Building>();

            Sprite2D sprite = tempBuilding.GetNodeOrNull<Sprite2D>("Sprite2D");
            if(sprite != null && sprite.Texture != null)
            {
                finalIconTexture = sprite.Texture;

                if (sprite.RegionEnabled)
                {
                    AtlasTexture atlas = new AtlasTexture
                    {
                        Atlas = sprite.Texture,
                        Region = sprite.RegionRect
                    };
                    finalIconTexture = atlas;
                }
            }
            else
            {
                AnimatedSprite2D animatedSprite = tempBuilding.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
                finalIconTexture = animatedSprite.SpriteFrames.GetFrameTexture("horizontal", 0);
            }
            

            Button newUIButton = buildingButton.Instantiate<Button>();

            TextureRect iconNode = newUIButton.GetNode<TextureRect>("MarginContainer/VBoxContainer/Icon");
            iconNode.Texture = finalIconTexture;

            string tooltip = $"{tempBuilding.Name}\nKoszt:\n";
            if (tempBuilding.requiredMaterials != null)
            {
                foreach (var material in tempBuilding.requiredMaterials)
                {
                    tooltip += $"- {material.Key}: {material.Value}\n";
                }
            }
            newUIButton.TooltipText = tooltip;

            newUIButton.Pressed += () => OnBuildingSelected?.Invoke(scene);

            tempBuilding.QueueFree();
            targetGrid.AddChild(newUIButton);
        }
    }
    public void BuildingSelected(PackedScene scene)
    {
        
    }
    
}
