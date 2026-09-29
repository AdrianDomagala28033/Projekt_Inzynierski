using Godot;
using System;

public partial class CameraController : Camera2D
{
	[Export] public int worldHeight;
	[Export] public int worldWidth;
	private float zoomSpeed = 0.1f;
	private float minZoom = 0.7f;
	private float maxZoom = 1.5f;
	private bool isDragging = false;
	public override void _Ready()
	{
		LimitLeft = 0;
		LimitTop = 0;
		LimitRight = worldWidth*16;
		LimitBottom = worldHeight*16;

	}
	public override void _Process(double delta)
	{
	}
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton inputEvent)
		{
			if(inputEvent.ButtonIndex == MouseButton.WheelUp)
			{
				float newZoomX = Mathf.Clamp(this.Zoom.X + zoomSpeed, minZoom, maxZoom);
				float newZoomY = Mathf.Clamp(this.Zoom.Y + zoomSpeed, minZoom, maxZoom);
				this.Zoom = new Vector2(newZoomX, newZoomY);
			}
			if(inputEvent.ButtonIndex == MouseButton.WheelDown)
			{
				float newZoomX = Mathf.Clamp(this.Zoom.X - zoomSpeed, minZoom, maxZoom);
				float newZoomY = Mathf.Clamp(this.Zoom.Y - zoomSpeed, minZoom, maxZoom);
				this.Zoom = new Vector2(newZoomX, newZoomY);
			}
			if(inputEvent.Pressed && inputEvent.ButtonIndex == MouseButton.Middle)
				isDragging = true;
				
			else
				isDragging = false;
		}
		if(@event is InputEventMouseMotion input && isDragging)
		{
			this.Position -= input.Relative/this.Zoom;

			Vector2 viewportHalfSize = GetViewportRect().Size / (this.Zoom * 2);

			float clampedX = Mathf.Clamp(this.Position.X, LimitLeft + viewportHalfSize.X, LimitRight - viewportHalfSize.X);
			float clampedY = Mathf.Clamp(this.Position.Y, LimitTop + viewportHalfSize.X, LimitBottom - viewportHalfSize.X);
			this.Position = new Vector2(clampedX, clampedY);
		}
    }

}
