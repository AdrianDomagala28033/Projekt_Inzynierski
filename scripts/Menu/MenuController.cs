using Godot;
using System;

public partial class MenuController : Control
{
    private Control ScreenContainer;
    [Export] public PackedScene StartScreenScene;
    [Export] public PackedScene NetworkSetupScene;
    [Export] public PackedScene LobbyScene;
    [Export] public PackedScene CharacterCustomizationScene;

    public override void _Ready()
    {
        ScreenContainer = GetNode<Control>("ScreenContainer");
        ChangeScene(StartScreenScene);
    }
    public void ChangeScene(PackedScene newScene)
    {
        foreach (var item in ScreenContainer.GetChildren())
            item.QueueFree();
        
        var instance = newScene.Instantiate();

        if(instance is StartScreen startScreen)
        {
            startScreen.OnPlayPressed += () => ChangeScene(NetworkSetupScene);
            startScreen.OnChangeAvatarPressed += () => ChangeScene(CharacterCustomizationScene);
        }
        if(instance is NetworkSetup networkScreen)
        {
            networkScreen.OnLeavePressed += () => ChangeScene(StartScreenScene);
            networkScreen.OnCreatePressed += () => ChangeScene(LobbyScene);
            networkScreen.OnJoinPressed += () => ChangeScene(LobbyScene);
        }
        if(instance is CharacterCreator characterCreator)
        {
            characterCreator.OnLeavePressed += () => ChangeScene(StartScreenScene);
        }
        if (instance is LobbyScreen lobby)
        {
            lobby.OnLeavePressed += () => ChangeScene(NetworkSetupScene);
        }

        ScreenContainer.AddChild(instance);
    }

}
