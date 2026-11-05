using Godot;
using System;

public partial class LoadingScreen : CanvasLayer
{
    public override void _Ready()
    {
        this.Visible = false;
    }
    public void ShowLoading()
    {
        this.Visible = true;
    }
    public void HideLoading()
    {
        this.Visible = false;
    }
}
