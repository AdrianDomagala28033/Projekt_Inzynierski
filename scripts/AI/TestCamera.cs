using Godot;

public partial class TestCamera : Camera2D
{
    [Export] public float ZoomSpeed = 0.1f;
    [Export] public float MinZoom = 0.5f;
    [Export] public float MaxZoom = 4.0f;

    private bool _isPanning = false;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Middle)
            {
                _isPanning = mouseBtn.Pressed;
            }

            if (mouseBtn.Pressed)
            {
                if (mouseBtn.ButtonIndex == MouseButton.WheelUp)
                {
                    Zoom += new Vector2(ZoomSpeed, ZoomSpeed);
                }
                else if (mouseBtn.ButtonIndex == MouseButton.WheelDown)
                {
                    Zoom -= new Vector2(ZoomSpeed, ZoomSpeed);
                }

                Zoom = new Vector2(
                    Mathf.Clamp(Zoom.X, MinZoom, MaxZoom),
                    Mathf.Clamp(Zoom.Y, MinZoom, MaxZoom)
                );
            }
        }
        
        else if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            Position -= mouseMotion.Relative / Zoom;
        }
    }
}