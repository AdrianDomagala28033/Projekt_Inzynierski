
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.FastNoiseLite;

public enum Tiles
{
    grass = 0,
    water = 1,
    sand = 2,
    road = 3,
    cliff = 4,
    ground = 5
}

public enum BiomeType
{
    Forest,
    Desert,
    Snow,
    Cave
}

public partial class WorldMap : Node2D
{
    [Export] public BiomeType currentBiome = BiomeType.Forest;
    [Export] public int worldWidth;
    [Export] public int worldHeight;
    [Export] public int brushSize = 5;
    [Export] public TileMapLayer groundLayer;
    [Export] public TileMapLayer resourcesLayer;
    [Export] public Camera2D camera;
    [Export] public PackedScene playerScene;

    [Export] public PackedScene[] trees;
    [Export] public PackedScene[] rocks;

    private Tiles[,] worldMap;
    private FastNoiseLite noise;
    private Random rng;

    public override void _Ready()
    {
        var connectionManager = GetNode<ConnectionManager>("/root/ConnectionManager");
        int globalSeed = connectionManager.GlobalMapSeed;

        noise = new FastNoiseLite();
        rng = new Random(globalSeed);

        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        noise.Seed = globalSeed;
        noise.Frequency = 0.05f;

        worldMap = new Tiles[worldWidth, worldHeight];

        if (currentBiome == BiomeType.Cave)
        {
            GD.Print($"Pierwsze pole mapy: {worldMap[0, 0]}");
            GD.Print($"Środkowe pole mapy: {worldMap[worldWidth / 2, worldHeight / 2]}");
            GenerateCave();
        }
        else
        {
            CleanMap();
            GeneratePath(
                (worldHeight * worldWidth) / 2,
                new Vector2I(worldWidth / 2, worldHeight / 2)
            );
            SmoothMap();
            GenerateBeach();
            EnsureWaterBorder();
        }

        if (Multiplayer.IsServer())
            GenerateResources();

        DrawGround();

        //if (currentBiome == BiomeType.Cave)
        //{
        //    CreateCaveWallCollisions();
        //}
    }

    private void GenerateCave()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                worldMap[x, y] = Tiles.cliff;
            }
        }

        int centerY = 50;
        int direction = 0;

        for (int x = 0; x < worldWidth; x++)
        {
            if (rng.NextDouble() < 0.25)
            {
                direction = rng.Next(-2, 3);
            }

            centerY += direction;
            centerY = Math.Clamp(centerY, 12, worldHeight - 13);

            int radius = rng.Next(4, 7);

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                worldMap[x, y] = Tiles.ground;
            }
        }

        int branchCount = worldWidth / 5;

        for (int i = 0; i < branchCount; i++)
        {
            int startX = rng.Next(5, worldWidth - 5);
            int startY = -1;

            for (int y = 0; y < worldHeight; y++)
            {
                if (worldMap[startX, y] == Tiles.ground)
                {
                    startY = y;
                    break;
                }
            }

            if (startY == -1)
                continue;

            int branchDirection = rng.Next(0, 2) == 0 ? -1 : 1;

            int currentX = startX;
            int currentY = startY;

            int branchLength = rng.Next(25, 55);
            int branchRadius = rng.Next(5, 9);

            for (int step = 0; step < branchLength; step++)
            {
                currentY += branchDirection;

                if (rng.NextDouble() < 0.3)
                {
                    currentX += rng.Next(-2, 3);
                }

                currentX = Math.Clamp(currentX, 2, worldWidth - 3);

                if (currentY < 3 || currentY >= worldHeight - 3)
                    break;

                for (int offset = -branchRadius; offset <= branchRadius; offset++)
                {
                    int y = currentY + offset;

                    if (y >= 0 && y < worldHeight)
                    {
                        worldMap[currentX, y] = Tiles.ground;
                    }
                }
            }

            if (rng.NextDouble() < 0.5)
            {
                int splitX = currentX;
                int splitY = currentY;

                int splitDirection = -branchDirection;
                int splitLength = rng.Next(12, 30);
                int splitRadius = rng.Next(4, 7);

                for (int step = 0; step < splitLength; step++)
                {
                    splitY += splitDirection;

                    if (rng.NextDouble() < 0.25)
                    {
                        splitX += rng.Next(-2, 3);
                    }

                    splitX = Math.Clamp(splitX, 2, worldWidth - 3);

                    if (splitY < 3 || splitY >= worldHeight - 3)
                        break;

                    for (int offset = -splitRadius; offset <= splitRadius; offset++)
                    {
                        int y = splitY + offset;

                        if (y >= 0 && y < worldHeight)
                        {
                            worldMap[splitX, y] = Tiles.ground;
                        }
                    }
                }
            }
        }
    }

    private void DrawGround()
    {
        groundLayer.Clear();

        int groundCount = 0;
        int cliffCount = 0;

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (worldMap[x, y] == Tiles.ground)
                    groundCount++;

                if (worldMap[x, y] == Tiles.cliff)
                    cliffCount++;
            }
        }

        GD.Print($"DRAWGROUND: ground = {groundCount}, cliff = {cliffCount}");

        int waterAtlasId = 5;
        int sandAtlasId = 6;
        int grassAtlasId = 7;

        int caveFloor1AtlasId = 2;
        int caveWallsAtlasId = 4;

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector2I position = new Vector2I(x, y);

                if (currentBiome == BiomeType.Cave)
                {
                    if (worldMap[x, y] == Tiles.ground)
                    {
                        groundLayer.SetCell(
                            position,
                            2,
                            new Vector2I(0, 0)
                        );
                    }

                    continue;
                }

                if (worldMap[x, y] == Tiles.water)
                {
                    groundLayer.SetCell(
                        position, waterAtlasId, new Vector2I(1, 1)
                    );
                }
                else if (currentBiome == BiomeType.Desert)
                {
                    groundLayer.SetCell(
                        position, sandAtlasId,
                        new Vector2I(x % 2, y % 2)
                    );
                }
                else if (worldMap[x, y] == Tiles.sand)
                {
                    groundLayer.SetCell(
                        position, sandAtlasId,
                        new Vector2I(x % 2, y % 2)
                    );
                }
                else if (worldMap[x, y] == Tiles.grass)
                {
                    groundLayer.SetCell(
                        position, grassAtlasId, new Vector2I(0, 0)
                    );
                }
                else if (worldMap[x, y] == Tiles.ground)
                {
                    groundLayer.SetCell(
                        position, caveFloor1AtlasId, new Vector2I(0, 0)
                    );
                }
            }
        }
    }

    private void CreateCaveWallCollisions()
    {
        Vector2I tileSize = groundLayer.TileSet.TileSize;

        StaticBody2D wallsBody = new StaticBody2D();
        wallsBody.Name = "CaveWallCollisions";
        groundLayer.AddChild(wallsBody);

        for (int y = 0; y < worldHeight; y++)
        {
            for (int x = 0; x < worldWidth; x++)
            {
                if (worldMap[x, y] != Tiles.cliff)
                    continue;

                CollisionShape2D collision = new CollisionShape2D();

                RectangleShape2D rectangle = new RectangleShape2D();
                rectangle.Size = new Vector2(tileSize.X, tileSize.Y);

                collision.Shape = rectangle;

                collision.Position = groundLayer.MapToLocal(
                    new Vector2I(x, y)
                );

                wallsBody.AddChild(collision);
            }
        }
    }

    private void GeneratePath(int maxSteps, Vector2I position)
    {
        Vector2I direction = new Vector2I(0, 1);
        Vector2I[] directions =
        {
            new Vector2I(1, 0),
            new Vector2I(-1, 0),
            new Vector2I(0, 1),
            new Vector2I(0, -1)
        };

        for (int i = 0; i < maxSteps; i++)
        {
            for (int x = position.X - brushSize; x <= position.X + brushSize; x++)
            {
                for (int y = position.Y - brushSize; y <= position.Y + brushSize; y++)
                {
                    int dx = x - position.X;
                    int dy = y - position.Y;

                    if (x < worldWidth && x >= 0 && y < worldHeight && y >= 0)
                    {
                        if ((dx * dx) + (dy * dy) <= (brushSize * brushSize))
                            worldMap[x, y] = 0;
                    }
                }
            }

            float decision = rng.Next(0, 101);

            if (decision <= 25)
                direction = directions[rng.Next(0, directions.Length)];

            if ((position + direction).X < worldWidth &&
                (position + direction).Y < worldHeight &&
                (position + direction).X >= 0 &&
                (position + direction).Y >= 0)
            {
                position += direction;
            }
            else
            {
                direction = directions[rng.Next(0, directions.Length)];
            }
        }
    }

    public void CleanMap()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                worldMap[x, y] = Tiles.water;
            }
        }
    }

    public void SmoothMap()
    {
        Tiles[,] tempMap = (Tiles[,])worldMap.Clone();

        for (int x = 1; x < worldWidth - 1; x++)
        {
            for (int y = 1; y < worldHeight - 1; y++)
            {
                int blocksAround = 0;

                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (tempMap[i, j] == Tiles.grass)
                            blocksAround++;
                    }
                }

                if (tempMap[x, y] == Tiles.water && blocksAround >= 3)
                    worldMap[x, y] = Tiles.grass;

                if (tempMap[x, y] == Tiles.grass && blocksAround < 3)
                    worldMap[x, y] = Tiles.water;
            }
        }
    }

    private void GenerateBeach()
    {
        Tiles[,] tempMap = (Tiles[,])worldMap.Clone();
        int beachBrushSize = 3;

        for (int x = 1; x < worldWidth - 1; x++)
        {
            for (int y = 1; y < worldHeight - 1; y++)
            {
                if (tempMap[x, y] == Tiles.grass &&
                    (tempMap[x - 1, y] == Tiles.water ||
                     tempMap[x + 1, y] == Tiles.water ||
                     tempMap[x, y - 1] == Tiles.water ||
                     tempMap[x, y + 1] == Tiles.water))
                {
                    for (int i = -beachBrushSize; i <= beachBrushSize; i++)
                    {
                        for (int j = -beachBrushSize; j <= beachBrushSize; j++)
                        {
                            int dx = x + i;
                            int dy = y + j;

                            if (dx < worldWidth && dx >= 0 && dy < worldHeight && dy >= 0)
                            {
                                if ((i * i) + (j * j) <= (beachBrushSize * beachBrushSize))
                                {
                                    if (tempMap[dx, dy] == Tiles.grass)
                                        worldMap[dx, dy] = Tiles.sand;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void EnsureWaterBorder()
    {
        int borderSize = 10;

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (x < borderSize ||
                    x >= worldWidth - borderSize ||
                    y < borderSize ||
                    y >= worldHeight - borderSize)
                {
                    worldMap[x, y] = Tiles.water;
                }
            }
        }
    }

    private void GenerateResources()
    {
        if (currentBiome == BiomeType.Cave)
        {
            GenerateCaveResources();
            return;
        }

        if (currentBiome == BiomeType.Desert)
        {
            GenerateDesertResources();
            return;
        }

        bool[,] occupied = new bool[worldWidth, worldHeight];

        FastNoiseLite biomeNoise = new FastNoiseLite();
        biomeNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        biomeNoise.Frequency = 0.02f;
        biomeNoise.Seed = rng.Next();

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (worldMap[x, y] == Tiles.water)
                    continue;

                if (worldMap[x, y] == Tiles.grass)
                {
                    float value = biomeNoise.GetNoise2D(x, y);

                    if (value > 0.2f)
                    {
                        int chance = rng.Next(1, 100);

                        if (chance <= 70)
                        {
                            if (!occupied[x, y])
                            {
                                var instance = trees[0].Instantiate<Node2D>();
                                instance.Position = resourcesLayer.MapToLocal(new Vector2I(x, y));

                                GetNode("ResourcesManager/ResourcesContainer")
                                    .AddChild(instance, forceReadableName: true);

                                for (int i = x; i <= x + 1; i++)
                                {
                                    for (int j = y; j <= y + 1; j++)
                                    {
                                        if (i < worldWidth && j < worldHeight)
                                            occupied[i, j] = true;
                                    }
                                }
                            }
                        }
                    }

                    if (value < -0.45f)
                    {
                        int chance = rng.Next(1, 100);

                        if (chance <= 30)
                        {
                            if (!occupied[x, y])
                            {
                                var instance = rocks[rng.Next(0, rocks.Length)].Instantiate<Node2D>();
                                instance.Position = resourcesLayer.MapToLocal(new Vector2I(x, y));

                                GetNode("ResourcesManager/ResourcesContainer")
                                    .AddChild(instance, forceReadableName: true);

                                for (int i = x; i <= x + 1; i++)
                                {
                                    for (int j = y; j <= y + 1; j++)
                                    {
                                        if (i < worldWidth && j < worldHeight)
                                            occupied[i, j] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void GenerateDesertResources()
    {
        bool[,] occupied = new bool[worldWidth, worldHeight];

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (worldMap[x, y] != Tiles.grass)
                    continue;

                int chance = rng.Next(1, 100);

                if (chance <= 1 && !occupied[x, y])
                {
                    var instance = rocks[rng.Next(0, rocks.Length)]
                        .Instantiate<Node2D>();

                    instance.Position = resourcesLayer.MapToLocal(
                        new Vector2I(x, y));

                    GetNode("ResourcesManager/ResourcesContainer")
                        .AddChild(instance, forceReadableName: true);

                    for (int i = x; i <= x + 1; i++)
                    {
                        for (int j = y; j <= y + 1; j++)
                        {
                            if (i < worldWidth && j < worldHeight)
                                occupied[i, j] = true;
                        }
                    }
                }
            }
        }
    }

    private void GenerateCaveResources()
    {
        bool[,] occupied = new bool[worldWidth, worldHeight];

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (worldMap[x, y] != Tiles.ground)
                    continue;

                if (occupied[x, y])
                    continue;

                bool validArea = true;

                for (int i = x; i <= x + 1; i++)
                {
                    for (int j = y; j <= y + 1; j++)
                    {
                        if (i >= worldWidth || j >= worldHeight ||
                            worldMap[i, j] != Tiles.ground)
                        {
                            validArea = false;
                        }
                    }
                }

                if (!validArea)
                    continue;

                int chance = rng.Next(1, 101);

                if (chance <= 5)
                {
                    var instance = rocks[rng.Next(0, rocks.Length)]
                        .Instantiate<Node2D>();

                    instance.Position = resourcesLayer.MapToLocal(
                        new Vector2I(x, y)
                    );

                    GetNode("ResourcesManager/ResourcesContainer")
                        .AddChild(instance, forceReadableName: true);

                    for (int i = x; i <= x + 1; i++)
                    {
                        for (int j = y; j <= y + 1; j++)
                        {
                            occupied[i, j] = true;
                        }
                    }
                }
            }
        }
    }

    public Tiles GetTile(int x, int y)
    {
        if (x >= 0 && x < worldWidth && y >= 0 && y < worldHeight)
            return worldMap[x, y];

        return Tiles.water;
    }
}
