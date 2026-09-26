using Godot;
using System;

public partial class CameraController : Camera2D
{
	public override void _Ready()
	{
		LimitLeft = 0;
		LimitTop = 0;
		

	}
	public override void _Process(double delta)
	{
	}
    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
    }

}
