using System;
using System.Collections.Generic;

public class PlayerData
{
    public long Id {get; set;}
    public string Name {get; set;}
    public bool IsReady {get; set;}
    public bool IsDisconnected = false;
    public string AppearanceJson;

    public PlayerData(long id, string name, bool isReady, string appearance)
    {
        this.Id = id;
        this.Name = name;
        IsReady = isReady;
        AppearanceJson = appearance;
    }
}