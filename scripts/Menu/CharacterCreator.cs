using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterCreator : CanvasLayer
{
    [Export] public LineEdit UserNick;
    public List<string> CatalogNames = ["Chest", "Feet", "Head", "Legs"];
    public Dictionary<string, List<string>> AllTextures = new Dictionary<string, List<string>>();
    public Dictionary<string, int> TextureNumber = new Dictionary<string, int>();
    public event Action OnLeavePressed;

    public override void _Ready()
    {
        foreach (var category in CatalogNames)
        {
            string path = "res://assets/characterAssets/Player/" + category;
            TextureNumber[category] = 0;
            AllTextures[category] = ScanFiles(path);
        }
        if (FileAccess.FileExists("user://profile.json"))
        {
            using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            var parsedData = Json.ParseString(jsonText).AsGodotDictionary();

            if (parsedData.ContainsKey("PlayerName"))
            {
                UserNick.Text = parsedData["PlayerName"].AsString();
            }
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
    public void ChangeItem(string category, int direction)
    {
        int textureNumber = TextureNumber[category];
        textureNumber += direction;
        if(textureNumber < 0)
            textureNumber = AllTextures[category].Count - 1;
        else if(textureNumber >= AllTextures[category].Count)
            textureNumber = 0;
        TextureNumber[category] = textureNumber;
        string path = AllTextures[category][textureNumber];
        var node = GetNode<Sprite2D>("%CharacterPreview/" + category);
        node.Texture = GD.Load<Texture2D>(path);
    }
    public void SaveProfile()
    {
        if(string.IsNullOrEmpty(UserNick.Text)) UserNick.Text = "Player";
        var PlayerDetails = new Godot.Collections.Dictionary();
        PlayerDetails.Add("PlayerName", UserNick.Text);

        var PlayerSuit = new Godot.Collections.Dictionary();
        foreach (var item in TextureNumber)
            PlayerSuit.Add(item.Key, item.Value);
        PlayerDetails.Add("Appearance", PlayerSuit);
        string jsonText = Json.Stringify(PlayerDetails);
        using var file = FileAccess.Open("user://profile.json", FileAccess.ModeFlags.Write);
        file.StoreString(jsonText);
    }
    public void OnLeaveButtonPressed()
    {
        SaveProfile();
        OnLeavePressed?.Invoke();
    }
}
