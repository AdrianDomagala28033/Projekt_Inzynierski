using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Minimap : VBoxContainer
{
    [Export] public SubViewport subViewport;
    [Export] public Camera2D minimapCamera;
    [Export] public Control iconsOverlay;
    [Export] public Texture2D playerIconTexture;
    [Export] public Texture2D warehouseIconTexture;
    private Dictionary<Node2D, TextureRect> objectsOnMap;

    public override void _Ready()
    {
        subViewport.World2D = GetViewport().World2D;
        objectsOnMap = new Dictionary<Node2D, TextureRect>();
    }
    public override void _Process(double delta)
    {
        if(Player.localPlayer != null)
        {
            minimapCamera.GlobalPosition = Player.localPlayer.GlobalPosition;
            
            UpdateIconsForGroup("Players", playerIconTexture, false);
            UpdateIconsForGroup("Warehouse", warehouseIconTexture, true);
            foreach (var key in objectsOnMap.Keys.ToList())
            {
                if (!GodotObject.IsInstanceValid(key))
                {
                    objectsOnMap[key].QueueFree();
                    objectsOnMap.Remove(key);
                }
            }
        }
    }
    public void UpdateIconsForGroup(string groupName, Texture2D iconTexture, bool hideWhenOnMap = false)
    {
        var nodes = GetTree().GetNodesInGroup(groupName);
        foreach (var node in nodes)
        {
            if(node is Node2D targetNode)
            {
                if (targetNode == Player.localPlayer) continue;
                if (!objectsOnMap.ContainsKey(targetNode))
                {
                    var newIcon = new TextureRect();
                    newIcon.Texture = iconTexture;
                    newIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    newIcon.CustomMinimumSize = new Vector2(16,16);

                    iconsOverlay.AddChild(newIcon);
                    objectsOnMap.Add(targetNode, newIcon);
                }
                Vector2 diff = targetNode.GlobalPosition - Player.localPlayer.GlobalPosition;
                Vector2 radarOffset = diff * minimapCamera.Zoom;
                Vector2 rawPos = (iconsOverlay.Size / 2) + radarOffset;

                var icon = objectsOnMap[targetNode];
                Vector2 centeredPos = rawPos - (icon.Size / 2);
                icon.Position = centeredPos.Clamp(Vector2.Zero, iconsOverlay.Size - icon.Size);

                Rect2 radarBounds = new Rect2(Vector2.Zero, iconsOverlay.Size);
                icon.Visible = !(hideWhenOnMap && radarBounds.HasPoint(rawPos));
            }
        }
    }


}
