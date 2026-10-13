using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ToolSelector : VBoxContainer
{
    [Export] public HBoxContainer axe;
    [Export] public HBoxContainer pickaxe;
    [Export] public HBoxContainer sword;
    [Export] public HBoxContainer hammer;
    private Tool activeTool;
    private Dictionary<Tool, HBoxContainer> toolDictionary;

    public override void _Ready()
    {
        
        toolDictionary = new Dictionary<Tool, HBoxContainer>()
        {
            {Tool.Axe, axe},
            {Tool.Pickaxe, pickaxe},
            {Tool.Sword, sword},
            {Tool.Hammer, hammer}
        };
        UpdateUI(activeTool);
    }
    public void UpdateUI(Tool newTool)
    {
        activeTool = newTool;
        foreach (var tool in toolDictionary)
        {
            Tool toolType = tool.Key;
            HBoxContainer slotUi = tool.Value;
            if(toolType == newTool)
            {
                slotUi.Modulate = new Color(1,1,1, 1.0f);
            }
            else
            {
                slotUi.Modulate = new Color(1,1,1, 0.4f);
            }
        }
    }
    
    public async Task InitializeSelector(Player player)
    {
        if(!IsNodeReady())
            await ToSignal(this, Node.SignalName.Ready);
        player.OnToolChanged += UpdateUI;
        UpdateUI(player.activeTool);
    }


}
