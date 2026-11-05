using Godot;
using System;
using System.Collections.Generic;
public static class AppearanceLoader
{
    public static List<string> ScanFiles(string path)
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
    public static void Apply(Node targetNode, string appearanceJson, string nodePrefix = "")
    {
        List<string> catalogNames = new List<string> { "Chest", "Feet", "Head", "Legs" };
        var allTextures = new Dictionary<string, List<string>>();

        foreach (var category in catalogNames)
        {
            string path = "res://assets/characterAssets/Player/" + category;
            allTextures[category] = ScanFiles(path);
        }
        var appearance = Json.ParseString(appearanceJson).AsGodotDictionary();
        foreach (var item in appearance)
        {
            var category = item.Key.AsString();
            var value = item.Value.AsInt32();

            string imgPath = allTextures[category][value];
            var sprite = targetNode.GetNode<Sprite2D>($"{nodePrefix}{category}");
            sprite.Texture = GD.Load<Texture2D>(imgPath);
        }
    }
}