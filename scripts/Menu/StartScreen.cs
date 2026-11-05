using Godot;
using System;

public partial class StartScreen : CanvasLayer
{
    public event Action OnPlayPressed;
    public event Action OnChangeAvatarPressed;

    public void OnPlayButtonPressed()
    {
        OnPlayPressed?.Invoke();
    }
    public void OnChangeAvatarButtonPressed()
    {
        OnChangeAvatarPressed?.Invoke();
    }
}
