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
        foreach (var category in CatalogNames)
        {
            string path = "res://assets/characterAssets/Player/" + category;
            AllTextures[category] = ScanFiles(path);
        }
        if(isNetworkPlayer) return;
        if(!FileAccess.FileExists("user://profile.json")) return;
            
        using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        var parsedData = Json.ParseString(jsonText).AsGodotDictionary();

        string playerName = parsedData["PlayerName"].AsString();
        UserNick.Text = playerName;

        var appearance = parsedData["Appearance"].AsGodotDictionary();
        foreach (var item in appearance)
        {
            var category = item.Key.AsString();
            var value = item.Value.AsInt32();
            string path = AllTextures[category][value];
            var node = GetNode<Sprite2D>(category);
            node.Texture = GD.Load<Texture2D>(path);
        }
    }

    public List<string> ScanFiles(string path)
    {
        List<string> results = new List<string>();
        using var dir = DirAccess.Open(path);
        if(dir != null)
        {
            foreach (var fileName in dir.GetFiles())
                if(fileName.EndsWith(".png") && !fileName.EndsWith(".import"))
                    results.Add(path + "/" + fileName);
            foreach (var fileDirectory in dir.GetDirectories())
            {
                if(fileDirectory != "." && fileDirectory != "..")
                {
                    string newPath = path + "/" + fileDirectory;
                    results.AddRange(ScanFiles(newPath));
                }
            }
        }
        return results;
    }
    public void SetNetworkData(string name, bool isReady, string appearanceJson)
    {
        isNetworkPlayer = true;
        UserNick.Text = name;
        ReadyIcon.Visible = isReady;
        NotReadyIcon.Visible = !isReady;
        var appearance = Json.ParseString(appearanceJson).AsGodotDictionary();
        foreach (var category in CatalogNames)
        {
            string path = "res://assets/characterAssets/Player/" + category;
            AllTextures[category] = ScanFiles(path);
        }
        foreach (var item in appearance)
        {
            var category = item.Key.AsString();
            var value = item.Value.AsInt32();
            string path = AllTextures[category][value];
            var node = GetNode<Sprite2D>(category);
            node.Texture = GD.Load<Texture2D>(path);
        }
        
    }
}
