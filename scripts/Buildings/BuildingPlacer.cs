using Godot;
using System;
using System.Linq;

public partial class BuildingPlacer : Node2D
{
    [Export] public Sprite2D ghostSprite;
    [Export] public Area2D validationArea;
    public BuildingMenu buildingMenu;
    public bool isBuildingMode = false;
    private int tileSize = 16;
    private Player player;
    private PackedScene selectedBuilding;
    public string[] animationName = ["corner_bottom_left", "corner_bottom_right", "corner_top_left", "corner_top_right", "horizontal", "vertical_left", "vertical_right"];
    public int animationIndex = 4;

    public override void _Ready()
    {
        this.Visible = false;
        player = GetParent<Player>();
        player.OnToolChanged += OpenBuildingMode;
        buildingMenu = GetNode<BuildingMenu>("/root/Main/UIManager/BuildingMenu");
        buildingMenu.Visible = false;
        buildingMenu.OnBuildingSelected += UpdateGhost;
        
    }
    public override void _Process(double delta)
    {
        if(!isBuildingMode || !player.IsMultiplayerAuthority()) return;
        Vector2 mousePos = GetGlobalMousePosition();

        float snappedX = Mathf.Floor(mousePos.X / tileSize) * tileSize;
        float snappedY = Mathf.Floor(mousePos.Y / tileSize) * tileSize;
        
        this.GlobalPosition = new Vector2(snappedX, snappedY);
        if(validationArea.HasOverlappingBodies() || validationArea.HasOverlappingAreas()) ghostSprite.Modulate = new Color(1, 0, 0, 0.5f);
        else ghostSprite.Modulate = new Color(0, 1, 0, 0.5f);
        
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if(!isBuildingMode || !player.IsMultiplayerAuthority()) return;
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.IsPressed())
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    animationIndex++;
                    if(animationIndex == animationName.Count() - 1)
                    {
                        animationIndex = 0;
                    }
                }
                else if(mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    animationIndex--;
                    if(animationIndex < 0)
                    {
                        animationIndex = animationName.Count() - 1;
                    }
                }
                if(mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if(validationArea.HasOverlappingBodies() || validationArea.HasOverlappingAreas()) return;
                    if(!validationArea.HasOverlappingBodies() || !validationArea.HasOverlappingAreas())
                    {
                        if (selectedBuilding == null) return;
                        Rpc(MethodName.PlaceBuilding, selectedBuilding.ResourcePath, this.GlobalPosition, animationIndex);
                    }
                }
            }
            if (selectedBuilding != null)
                UpdateGhost(selectedBuilding);
        }
        
        
    }


    public void OpenBuildingMode(Tool tool)
    {
        GD.Print($"[BuildingPlacer] Odebrano sygnał! Wybrane narzędzie: {tool}");
        if(tool == Tool.Hammer)
        {
            isBuildingMode = true;
            this.Visible = true;
            GD.Print("[BuildingPlacer] Pokazuję ducha budowania.");
            buildingMenu.Visible = true;
        }
        else
        {
            isBuildingMode = false;
            this.Visible = false;
            buildingMenu.Visible = false;
        }
    }
    public void UpdateGhost(PackedScene scene)
    {
        selectedBuilding = scene;
        Building tempBuilding = scene.Instantiate<Building>();

        Sprite2D sprite = tempBuilding.GetNodeOrNull<Sprite2D>("Sprite2D");
        if(sprite != null && sprite.Texture != null)
        {
            ghostSprite.Texture = sprite.Texture;

            if (sprite.RegionEnabled)
            {
                AtlasTexture atlas = new AtlasTexture
                {
                    Atlas = sprite.Texture,
                    Region = sprite.RegionRect
                };
                ghostSprite.Texture = atlas;
            }
        }
        else
        {
            AnimatedSprite2D animatedSprite = tempBuilding.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            ghostSprite.Texture = animatedSprite.SpriteFrames.GetFrameTexture(animationName[animationIndex], 0);
        }
        
        var buildingCollisionShape = tempBuilding.GetNode<CollisionShape2D>("CollisionShape2D");

        CollisionShape2D collisionShape = validationArea.GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Shape = buildingCollisionShape.Shape;

        
        collisionShape.Position = buildingCollisionShape.Position;

        tempBuilding.QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void PlaceBuilding(string buildingPath, Vector2 position, int variant)
    {
        if(!Multiplayer.IsServer()) return;

        PackedScene buildingScene = GD.Load<PackedScene>(buildingPath);
        Building building = buildingScene.Instantiate<Building>();
        building.GlobalPosition = position;
        AnimatedSprite2D animatedSprite = building.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if(animatedSprite != null)
            animatedSprite.Animation = animationName[variant];
        
        GetNode("/root/Main/BuildingsManager/BuildingContainer").AddChild(building);
    }

}
