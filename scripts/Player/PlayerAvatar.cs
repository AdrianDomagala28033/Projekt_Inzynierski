using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerAvatar : Control
{
    [Export] public Label UserNick;
    [Export] public Sprite2D ReadyIcon;
    [Export] public Sprite2D NotReadyIcon;
    public List<string> CatalogNames = ["Chest", "Feet", "Head", "Legs"];
    public Dictionary<string, List<string>> AllTextures = new Dictionary<string, List<string>>();
    private bool isNetworkPlayer = false;

    public override void _Ready()
    {
        
        if(isNetworkPlayer) return;
        if(!FileAccess.FileExists("user://profile.json")) return;
            
        using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        var parsedData = Json.ParseString(jsonText).AsGodotDictionary();

        string playerName = parsedData["PlayerName"].AsString();
        UserNick.Text = playerName;

        AppearanceLoader.Apply(this, Json.Stringify(parsedData["Appearance"]));
    }

    
    public void SetNetworkData(string name, bool isReady, string appearanceJson)
    {
        isNetworkPlayer = true;
        UserNick.Text = name;
        ReadyIcon.Visible = isReady;
        NotReadyIcon.Visible = !isReady;
        AppearanceLoader.Apply(this, appearanceJson);
    }
}
