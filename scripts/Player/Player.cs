using Godot;
using System;
using System.Collections.Generic;
public enum Tool
{
    Axe = 0,
    Pickaxe = 1,
    Sword = 2,
    Hammer = 3
}
public partial class Player : CharacterBody2D
{
    private AnimationPlayer animPlayer;
    private Sprite2D playerBase;
    private Sprite2D[] clothingLayers;
    [Export] Label playerName;
    [Export] public float speed;
    [Export] public Backpack backpack;
    [Export] public int maxHealth;
    public Tool activeTool;
    public static Player localPlayer;
    public event Action<Tool> OnToolChanged;
    public bool isInteracting = false;
    

    public override void _Ready()
    {
        
        var spaceState = GetWorld2D().DirectSpaceState;
        Vector2 desiredPos = new Vector2(54*16, 54*16);
        Vector2 safePos = desiredPos;

        TileMapLayer groundLayer = GetTree().GetFirstNodeInGroup("WalkableMap") as TileMapLayer;
        if (groundLayer != null)
            safePos = SpawnLocator.GetFreeSpot(desiredPos, spaceState, new Vector2(3, 6), groundLayer);
        else
            GD.PrintErr("[Player] Nie znaleziono TileMapLayer w grupie 'WalkableMap'!");
        
        GlobalPosition = safePos;
        AddToGroup("Players");
        
        int id = int.Parse(Name);
        SetMultiplayerAuthority(id);
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        playerBase = GetNode<Sprite2D>("PlayerBase");
        clothingLayers = new Sprite2D[]
        {
            GetNode<Sprite2D>("PlayerBase/Legs"),
            GetNode<Sprite2D>("PlayerBase/Feet"),
            GetNode<Sprite2D>("PlayerBase/Chest"),
            GetNode<Sprite2D>("PlayerBase/Hands"),
            GetNode<Sprite2D>("PlayerBase/Head"),
        };

        var camera = GetNode<Camera2D>("Camera2D");
        camera.Enabled = IsMultiplayerAuthority();
        var connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        var playerData = connectionManager.PlayerList.Find(p => p.Id == id);
        if(playerData != null)
        {
            playerName.Text = playerData.Name;
            AppearanceLoader.Apply(this, playerData.AppearanceJson, "PlayerBase/");   
        }
        activeTool = Tool.Axe;
        GD.Print(activeTool);
        if (IsMultiplayerAuthority())
        {
            localPlayer = this;
            var toolSelector = GetNode<ToolSelector>("/root/Main/UIManager/ToolSelector");
            toolSelector.InitializeSelector(this); 
            var playerInformation = GetNode<PlayerInformation>("/root/Main/UIManager/PlayerInformation");
            if (playerInformation != null)
                playerInformation.InitializeInterface();
            else
                GD.PrintErr("Nie znaleziono węzła PlayerInformation w grupie!");
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
    public override void _Process(double delta)
    {
        if (clothingLayers != null && playerBase != null)
        {
            foreach (var layer in clothingLayers)
            {
                layer.Frame = playerBase.Frame;
                layer.FlipH = playerBase.FlipH;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(isInteracting) return;
        if(!IsMultiplayerAuthority()) return;

        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        this.Velocity = direction*speed;
        foreach (var layer in clothingLayers)
        {
            layer.Frame = playerBase.Frame;
            layer.FlipH = playerBase.FlipH;
        }

        PlayAnimation(direction);

        MoveAndSlide();
        Rpc(nameof(SyncState), GlobalPosition, direction);

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncState(Vector2 newPos, Vector2 currentDirection)
    {
        GlobalPosition = newPos;
        if(!isInteracting)
            PlayAnimation(currentDirection);
        
    }
    private void PlayAnimation(Vector2 direction)
    {
        if (direction != Vector2.Zero)
        {
            if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            {
                playerBase.FlipH = direction.X < 0;
                animPlayer.Play("walk_right");
            }
            else if (direction.Y > 0)
            {
                playerBase.FlipH = false;
                animPlayer.Play("walk_down");
            }
            else
            {
                playerBase.FlipH = false;
                animPlayer.Play("walk_up");
            }
        }
        else
            animPlayer.Play("waiting");
    }
    public async void PerformToolAction(Vector2 targetPosition, Action onActionCompleted)
    {
        isInteracting = true;
        if(localPlayer.GlobalPosition.X > targetPosition.X) 
            playerBase.FlipH = true;
        else 
            playerBase.FlipH = false;

        Rpc(nameof(PlayActionAnimationRpc), targetPosition);
        animPlayer.Play($"mine_animation_side");

        while(playerBase.Frame < 5 && animPlayer.IsPlaying())
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        onActionCompleted?.Invoke();

        if(animPlayer.IsPlaying())
            await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);

        isInteracting = false;
        PlayAnimation(Vector2.Zero);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public async void PlayActionAnimationRpc(Vector2 targetPosition)
    {
        isInteracting = true;
        if(this.GlobalPosition.X > targetPosition.X) 
            playerBase.FlipH = true;
        else 
            playerBase.FlipH = false;
        animPlayer.Play($"mine_animation_side");
        await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);
        isInteracting = false;
    }

}
