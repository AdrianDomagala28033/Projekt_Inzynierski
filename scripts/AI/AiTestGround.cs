using Godot;

public partial class AiTestGround : Node2D
{
    private const int TileSize = 16;

    public override void _Ready()
    {
        var groundLayer = GetNode<TileMapLayer>("GroundLayer");
        // Przywracamy skanowanie warstwy surowców na wypadek używania pędzla (TileMap)
        var resourceLayer = GetNodeOrNull<TileMapLayer>("ResourceLayer"); 
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

        // 1. Skanowanie ręcznie ustawionych obiektów (Nodes)
        ScanForRocks(this, obstacles, width, height);

        // 2. Skanowanie kafelków (TileMaps)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2I pos = new Vector2I(x, y);

                // A: Zablokuj, jeśli na tej kratce w ResourceLayer jest namalowana skała
                if (resourceLayer != null && resourceLayer.GetCellSourceId(pos) != -1)
                {
                    obstacles[x, y] = true;
                }

                // B: Zablokuj, jeśli podłoże (GroundLayer) ma włączoną fizyczną kolizję (woda, klify)
                TileData tileData = groundLayer.GetCellTileData(pos);
                if (tileData != null && tileData.GetCollisionPolygonsCount(0) > 0)
                {
                    obstacles[x, y] = true;
                }
            }
        }

        aiManager.InitializeTestMode(width, height, obstacles);
        GD.Print($"[Poligon] Gotowe! Wymiary: {width}x{height}. Skały i woda zablokowane.");
    }

    private void ScanForRocks(Node parent, bool[,] obstacles, int width, int height)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child is Node2D node2D)
            {
                string nodeName = node2D.Name.ToString().ToLower();
                // Sprawdzamy szerszą pulę słów kluczowych dla ręcznie przeciąganych obiektów
                if (nodeName.Contains("rock") || nodeName.Contains("stone") || nodeName.Contains("sprite"))
                {
                    int gridX = (int)(node2D.GlobalPosition.X / TileSize);
                    int gridY = (int)(node2D.GlobalPosition.Y / TileSize);

                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        obstacles[gridX, gridY] = true;
                    }
                }
            }

            if (child.GetChildCount() > 0)
            {
                ScanForRocks(child, obstacles, width, height);
            }
        }
    }
}