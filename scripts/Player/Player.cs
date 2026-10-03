using Godot;
using System;
public enum Tool
{
    Axe = 0,
    Pickaxe = 1,
    Sword = 2,
    Hammer = 3
}
public partial class Player : CharacterBody2D
{
    private AnimatedSprite2D sprite;
    private int skinVariant = 1;
    [Export] Label playerName;
    [Export] public float speed;
    [Export] public int maxBackpackCapacity;
    public int woodCount;
    public int rockCount;
    public Tool activeTool;
    public static Player localPlayer;
    public event Action<Tool> OnToolChanged;
    public bool isInteracting = false;

    public override void _Ready()
    {
        GlobalPosition = new Vector2(50 * 16, 50 * 16);
        AddToGroup("Players");
        
        int id = int.Parse(Name);
        SetMultiplayerAuthority(id);
        sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        var camera = GetNode<Camera2D>("Camera2D");
        camera.Enabled = IsMultiplayerAuthority();
        var connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        var playerData = connectionManager.PlayerList.Find(p => p.Id == id);
        if(playerData != null)
            playerName.Text = playerData.Name;
        activeTool = Tool.Axe;
        GD.Print(activeTool);
        if (IsMultiplayerAuthority())
        {
            localPlayer = this;
            var toolSelector = GetNode<ToolSelector>("/root/Main/UIManager/ToolSelector");
            toolSelector.InitializeSelector(this); 
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if(!IsMultiplayerAuthority()) return;
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            bool toolChanged = false;
            switch (keyEvent.Keycode)
            {
                case Key.Key1:
                    activeTool = Tool.Axe;
                    toolChanged = true;
                    GD.Print("Wybrano: Siekiera");
                    break;
                case Key.Key2:
                    activeTool = Tool.Pickaxe;
                    toolChanged = true;
                    GD.Print("Wybrano: Kilof");
                    break;
                case Key.Key3:
                    activeTool = Tool.Sword;
                    toolChanged = true;
                    GD.Print("Wybrano: Miecz");
                    break;
                case Key.Key4:
                    activeTool = Tool.Hammer;
                    toolChanged = true;
                    GD.Print("Wybrano: Mlotek");
                    break;
            }
            if(toolChanged)
                OnToolChanged?.Invoke(activeTool);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(isInteracting) return;
        if(!IsMultiplayerAuthority()) return;

        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        this.Velocity = direction*speed;

        PlayAnimation(direction, skinVariant);

        MoveAndSlide();
        Rpc(nameof(SyncState), GlobalPosition, direction);

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncState(Vector2 newPos, Vector2 currentDirection)
    {
        GlobalPosition = newPos;
        if(!isInteracting)
            PlayAnimation(currentDirection, skinVariant);
        
    }
    private void PlayAnimation(Vector2 direction, int skinVariant)
    {
        if(direction != Vector2.Zero)
        {
            if(direction.X != 0)
                sprite.FlipH = direction.X < 0;
            sprite.Play($"walk_right_{skinVariant}");
        }
        
        else
            sprite.Play($"waiting_{skinVariant}");
    }
    public void AddResources(int quantity, MaterialType material)
    {
        switch (material)
        {
            case MaterialType.Wood:
                int availableSpaceWood = maxBackpackCapacity - woodCount;
                woodCount += Math.Min(quantity, availableSpaceWood);
                break;
            case MaterialType.Rock:
                int availableSpaceRock = maxBackpackCapacity - rockCount;
                rockCount += Math.Min(quantity, availableSpaceRock);
                break;
        }
    }
    public bool CanHoldResources(MaterialType material, int quantity)
    {
        switch (material)
        {
            case MaterialType.Wood:
                return woodCount < maxBackpackCapacity;
            case MaterialType.Rock:
                return rockCount < maxBackpackCapacity;
            default:
                return false;
        }
    }
    public async void PerformToolAction(Vector2 targetPosition, Action onActionCompleted)
    {
        isInteracting = true;
        if(localPlayer.GlobalPosition.X > targetPosition.X) 
            sprite.FlipH = true;
        else 
            sprite.FlipH = false;
        Rpc(nameof(PlayActionAnimationRpc), targetPosition);
        sprite.Play($"mine_animation_{skinVariant}");
        await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
        onActionCompleted?.Invoke();
        isInteracting = false;
        PlayAnimation(Vector2.Zero, skinVariant);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public async void PlayActionAnimationRpc(Vector2 targetPosition)
    {
        isInteracting = true;
        if(localPlayer.GlobalPosition.X > targetPosition.X) 
            sprite.FlipH = true;
        else 
            sprite.FlipH = false;
        sprite.Play($"mine_animation_{skinVariant}");
        await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
        isInteracting = false;
    }

}
