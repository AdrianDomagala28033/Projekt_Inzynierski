using Godot;

public partial class AiTestGround : Node2D
{
    private const int TileSize = 16;

    public override void _Ready()
    {
        var groundLayer = GetNode<TileMapLayer>("GroundLayer");
        var aiManager = GetNodeOrNull<AiGridManager>("AiSystem/AiGridManager");

        if (aiManager == null)
        {
            GD.PrintErr("[Poligon] BŁĄD: Brakuje AiSystem na mapie!");
            return;
        }

        Rect2I mapBounds = groundLayer.GetUsedRect();
        int width = mapBounds.Position.X + mapBounds.Size.X;
        int height = mapBounds.Position.Y + mapBounds.Size.Y;

        bool[,] obstacles = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2I pos = new Vector2I(x, y);

                TileData tileData = groundLayer.GetCellTileData(pos);
                if (tileData != null && tileData.GetCollisionPolygonsCount(0) > 0)
                {
                    obstacles[x, y] = true;
                }
            }
        }

        aiManager.InitializeTestMode(width, height, obstacles);
        GD.Print($"[Poligon] Gotowe! Wymiary: {width}x{height}. AI swobodnie przechodzi przez las i skały, omija wodę.");
    }
}