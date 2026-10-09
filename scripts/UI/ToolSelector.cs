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
    [Export] public Timer timer;
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
        timer.Timeout += OnHideTimerTimeout;
    }
    public void UpdateUI(Tool newTool)
    {
        activeTool = newTool;
        foreach (var tool in toolDictionary)
        {
            Tool toolType = tool.Key;
            HBoxContainer slotUi = tool.Value;
            var labelMask = slotUi.GetNode<Control>("LabelMask");
            if(toolType == newTool)
            {
                slotUi.Modulate = new Color(1,1,1, 1.0f);
                labelMask.Visible = true;
                var customLabel = labelMask.GetNode<Control>("CustomLabel");
                float targetWidth = customLabel.CustomMinimumSize.X;
                Tween tween = CreateTween();
                tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(labelMask, "custom_minimum_size:x", targetWidth, 0.3f);
            }
            else
            {
                slotUi.Modulate = new Color(1,1,1, 0.4f);
                labelMask.Visible = false;
                labelMask.CustomMinimumSize = new Vector2(0, labelMask.CustomMinimumSize.Y);
            }
        }
        timer.Start();
    }
    private void OnHideTimerTimeout()
    {
        HBoxContainer toolUI = toolDictionary[activeTool];
        var labelMask = toolUI.GetNode<Control>("LabelMask");
        
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(labelMask, "custom_minimum_size:x", 0.0f, 0.3f);

        tween.Finished += () => labelMask.Visible = false;
    }
    public async Task InitializeSelector(Player player)
    {
        if(!IsNodeReady())
            await ToSignal(this, Node.SignalName.Ready);
        player.OnToolChanged += UpdateUI;
        UpdateUI(player.activeTool);
    }


}
