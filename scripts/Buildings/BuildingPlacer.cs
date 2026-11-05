using Godot;
using Godot.Collections;
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
    private Dictionary<MaterialType, int> currentBuildingCost;
    private BaseManager baseManager;
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
        baseManager = GetTree().GetFirstNodeInGroup("BaseManager") as BaseManager;
        
    }
    public override void _PhysicsProcess(double delta)
    {

        if(!isBuildingMode || !player.IsMultiplayerAuthority()) return;
        Vector2 mousePos = GetGlobalMousePosition();

        float snappedX = Mathf.Floor(mousePos.X / tileSize) * tileSize + (tileSize / 2.0f);
        float snappedY = Mathf.Floor(mousePos.Y / tileSize) * tileSize + (tileSize / 2.0f);
        
        this.GlobalPosition = new Vector2(snappedX, snappedY);

        bool canAfford = true;
        if (currentBuildingCost != null)
        {
            foreach (var item in currentBuildingCost)
            {
                if (!baseManager.inventory.TryGetValue(item.Key, out int amount) || amount < item.Value)
                {
                    canAfford = false;
                    break;
                }
            }
        }
        if (validationArea.HasOverlappingBodies() || !canAfford)
        ghostSprite.Modulate = new Color(1, 0, 0, 0.5f);
        else 
            ghostSprite.Modulate = new Color(0, 1, 0, 0.5f);
        
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!isBuildingMode || !player.IsMultiplayerAuthority()) return;
        
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
        {
            bool variantChanged = false;

            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                animationIndex++;
                if (animationIndex > animationName.Length - 1) animationIndex = 0;
                variantChanged = true;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                animationIndex--;
                if (animationIndex < 0) animationIndex = animationName.Length - 1;
                variantChanged = true;
            }
            
            if (variantChanged && selectedBuilding != null)
            {
                UpdateGhost(selectedBuilding);
            }

            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                bool canAfford = true;
                if (currentBuildingCost != null)
                {
                    foreach (var item in currentBuildingCost)
                    {
                        if (!baseManager.inventory.TryGetValue(item.Key, out int amount) || amount < item.Value)
                        {
                            canAfford = false;
                            break;
                        }
                    }
                }

                if (!validationArea.HasOverlappingBodies() && canAfford && selectedBuilding != null)
                {
                    Rpc(MethodName.PlaceBuilding, selectedBuilding.ResourcePath, this.GlobalPosition, animationIndex);
                }
            }
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
        tempBuilding.InitializeCost();
        currentBuildingCost = tempBuilding.requiredMaterials;

        Sprite2D sprite = tempBuilding.GetNodeOrNull<Sprite2D>("Sprite2D");
        if(sprite != null && sprite.Texture != null)
        {
            ghostSprite.Texture = sprite.Texture;
            ghostSprite.Scale = sprite.Scale;
            ghostSprite.Offset = sprite.Offset;
            ghostSprite.Position = sprite.Position;

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

            ghostSprite.Scale = animatedSprite.Scale;
            ghostSprite.Offset = animatedSprite.Offset;
            ghostSprite.Position = animatedSprite.Position;
        }
        
        var buildingCollisionShape = tempBuilding.GetNode<CollisionShape2D>("CollisionShape2D");
        CollisionShape2D collisionShape = validationArea.GetNode<CollisionShape2D>("CollisionShape2D");

        RectangleShape2D rectShape = (RectangleShape2D)buildingCollisionShape.Shape.Duplicate();
        rectShape.Size = new Vector2(rectShape.Size.X - 2, rectShape.Size.Y - 2);

        collisionShape.Shape = rectShape;
        collisionShape.Position = buildingCollisionShape.Position;

        tempBuilding.QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void PlaceBuilding(string buildingPath, Vector2 position, int variant)
    {
        if(!Multiplayer.IsServer()) return;

        PackedScene buildingScene = GD.Load<PackedScene>(buildingPath);
        Building building = buildingScene.Instantiate<Building>();
        building.InitializeCost();

        building.GlobalPosition = position;
        AnimatedSprite2D animatedSprite = building.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if(animatedSprite != null)
            animatedSprite.Animation = animationName[variant];
        if(baseManager.TrySpendResources(building.requiredMaterials))
            GetNode("/root/Main/BuildingsManager/BuildingContainer").AddChild(building, true);
        else
        {
            building.QueueFree();
            return;
        }
    }

}
