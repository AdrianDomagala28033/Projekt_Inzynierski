using System;
using System.Collections.Generic;

public class PlayerData
{
    public long Id {get; set;}
    public string Name {get; set;}
    public bool IsReady {get; set;}
    public bool IsDisconnected = false;

    public PlayerData(long id, string name, bool isReady)
    {
        this.Id = id;
        this.Name = name;
        IsReady = isReady;

    }
}