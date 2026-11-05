using System;
using System.Formats.Asn1;
using Godot;

public static partial class SpawnLocator
{
    public static Vector2 GetFreeSpot(Vector2 preferredSpawnPoint, PhysicsDirectSpaceState2D spaceState, Vector2 searchSize, TileMapLayer groundLayer, uint collisionMask = 1)
    {
        RectangleShape2D shape2D = new RectangleShape2D { Size = searchSize };
        
        PhysicsShapeQueryParameters2D physicsQuery = new PhysicsShapeQueryParameters2D
        {
            Shape = shape2D,
            CollisionMask = collisionMask,
            CollideWithAreas = true,
            CollideWithBodies = true
        };

        int x = 0;
        int y = 0;
        int dx = 0;
        int dy = -1;
        int tileSize = 16;
        int maxSteps = 10000;

        for (int i = 0; i < maxSteps; i++)
        {
            Vector2 currentTestPos = preferredSpawnPoint + new Vector2(x*tileSize, y*tileSize);
            physicsQuery.Transform = new Transform2D(0, currentTestPos);
            var intersections = spaceState.IntersectShape(physicsQuery);
            if(intersections.Count == 0)
            {
                Vector2I mapCoords = groundLayer.LocalToMap(currentTestPos);
                TileData tileData = groundLayer.GetCellTileData(mapCoords);
                if (tileData != null && tileData.GetCustomData("is_walkable").AsBool())
                {
                    if (i == 0)
                    {
                        GD.Print($"[SpawnLocator] Domyślny punkt {preferredSpawnPoint} był od razu wolny.");
                    }
                    else
                    {
                        GD.Print($"[SpawnLocator] Zajęte! Znaleziono wolne miejsce w {currentTestPos} po {i} przesunięciach na spirali.");
                    }
                    return currentTestPos;   
                }
            }
            if(x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                int temp = dx;
                dx = -dy;
                dy = temp;
            }
            x += dx;
            y += dy;
        }
        GD.PrintErr($"[SpawnLocator] BŁĄD: Nie znaleziono wolnego miejsca (sprawdzono {maxSteps} pól). Zwracam punkt domyślny.");
        return preferredSpawnPoint;
    }
}