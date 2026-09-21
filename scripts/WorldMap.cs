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
public partial class WorldMap : Node2D
{
	[Export] public int worldWidth;
	[Export] public int worldHeight;
	[Export] public int brushSize = 5;
	[Export] public TileMapLayer groundLayer;
	[Export] public TileMapLayer resourcesLayer;
	[Export] public Camera2D camera;

	[Export] public PackedScene[] trees;
	[Export] public PackedScene[] rocks;
	[Export] public PackedScene[] shrubs;

	private Tiles[,] worldMap;
	private bool isDragging;
	private float zoomSpeed = 0.1f;
	private float minZoom = 0.5f;
	private float maxZoom = 3.0f;
	private FastNoiseLite noise;
	public override void _Ready()
	{
		noise = new FastNoiseLite();
		Random rng = new Random();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
		noise.Seed = rng.Next();
		noise.Frequency = 0.05f;

		worldMap = new Tiles[worldWidth, worldHeight];
		CleanMap();
		GeneratePath((worldHeight*worldWidth)/2, new Vector2I((int)worldWidth/2, (int)worldHeight/2));
		SmoothMap();
		GenerateBeach();
		GenerateResources();
		DrawGround();
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEvent)
		{
			if(inputEvent.ButtonIndex == MouseButton.WheelUp)
				{
					camera.Zoom += new Vector2(zoomSpeed, zoomSpeed);
					camera.Zoom = new Vector2(Mathf.Clamp(camera.Zoom.X + zoomSpeed, minZoom, maxZoom), Mathf.Clamp(camera.Zoom.Y + zoomSpeed, minZoom, maxZoom));
				}
				if(inputEvent.ButtonIndex == MouseButton.WheelDown)
				{
					camera.Zoom -= new Vector2(zoomSpeed, zoomSpeed);
					Mathf.Clamp(camera.Zoom.X, minZoom, maxZoom);
					Mathf.Clamp(camera.Zoom.Y, minZoom, maxZoom);
					//camera.Zoom = new Vector2(Mathf.Clamp(camera.Zoom.X - zoomSpeed, minZoom, maxZoom), Mathf.Clamp(camera.Zoom.Y - zoomSpeed, minZoom, maxZoom));
				}
			if(inputEvent.Pressed && inputEvent.ButtonIndex == MouseButton.Middle)
				isDragging = true;
				
			else
				isDragging = false;
			
		}
		
		if(@event is InputEventMouseMotion input && isDragging)
		{
			camera.Position -= input.Relative/camera.Zoom;
		}
	}

	private void DrawGround()
	{
		Random rng = new Random();
		for (int x = 0; x < worldWidth; x++)
		{
			for (int y = 0; y < worldHeight; y++)
			{
				if(worldMap[x, y] == Tiles.grass)
				{
					float value = noise.GetNoise2D(x, y);
					if(value >= -0.4 && value <= 0.4) //grass
						groundLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(0,3));
					else if(value < -0.4) //darkGrass
					{
						List<Vector2I> darkGrass = new List<Vector2I>();
						darkGrass.Add(new Vector2I(4,2));
						darkGrass.Add(new Vector2I(5,2));
						darkGrass.Add(new Vector2I(6,2));
						groundLayer.SetCell(new Vector2I(x, y), 0, darkGrass[rng.Next(0, 3)]);
					}
					else if(value > 0.4) //lightGrass
					{
						List<Vector2I> darkGrass = new List<Vector2I>();
						darkGrass.Add(new Vector2I(1,2));
						darkGrass.Add(new Vector2I(2,2));
						darkGrass.Add(new Vector2I(3,2));
						darkGrass.Add(new Vector2I(2,3));
						darkGrass.Add(new Vector2I(3,3));
						darkGrass.Add(new Vector2I(1,4));
						darkGrass.Add(new Vector2I(2,4));
						groundLayer.SetCell(new Vector2I(x, y), 0, darkGrass[rng.Next(0, 7)]);
					}
						
				}
				else if(worldMap[x, y] == Tiles.water)
				{
					int offsetX = x % 4;
					int offsetY = y % 4;
					groundLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(11 + offsetX, 18 + offsetY));
				}
				else if(worldMap[x, y] == Tiles.sand)
				{
					float value = noise.GetNoise2D(x, y);
					if(value >= -0.4 && value <= 0.4)
						groundLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(5, 1));
					else if(value < -0.4)
					{
						List<Vector2I> darkGrass = new List<Vector2I>();
						darkGrass.Add(new Vector2I(7,1));
						darkGrass.Add(new Vector2I(8,1));
						groundLayer.SetCell(new Vector2I(x, y), 0, darkGrass[rng.Next(0, 2)]);
					}
					else if(value > 0.4)
					{
						List<Vector2I> darkGrass = new List<Vector2I>();
						darkGrass.Add(new Vector2I(8,1));
						darkGrass.Add(new Vector2I(9,1));
						groundLayer.SetCell(new Vector2I(x, y), 0, darkGrass[rng.Next(0, 2)]);
					}
				}
				
			}
		}
	}
	private void DrawResources()
	{
		
	}

	private void GeneratePath(int maxSteps, Vector2I position)
	{
		Vector2I direction = new Vector2I(0,1);
		Random random = new Random();
		Vector2I[] directions = {new Vector2I(1,0), new Vector2I(-1,0), new Vector2I(0,1), new Vector2I(0, -1)};
		for (int i = 0; i < maxSteps; i++)
		{
			for (int x = position.X-brushSize; x <= position.X+brushSize; x++)
			{
				for (int y = position.Y-brushSize; y <= position.Y+brushSize; y++)
				{
					int dx = x - position.X;
					int dy = y - position.Y;
					if(x < worldWidth && x >= 0 && y < worldHeight && y >= 0)
						if((dx*dx) + (dy*dy) <= (brushSize*brushSize))
							worldMap[x, y] = 0; //0 oznacza trawe
				}
			}
			float decision = random.Next(0, 101);
			if(decision <= 25)
				direction = directions[random.Next(0, directions.Length)];
			if ((position + direction).X < worldWidth && (position + direction).Y < worldHeight && (position + direction).X >= 0 && (position + direction).Y >= 0)
				position += direction;
			else
				direction = directions[random.Next(0, directions.Length)];
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
			for(int y = 1; y < worldHeight - 1; y++)
			{
				int blocksAround = 0;
				for (int i = x - 1; i <= x+1; i++)
				{
					for (int j = y-1; j <= y+1; j++)
					{
						if(tempMap[i,j] == Tiles.grass)
							blocksAround++;
					}
				}
				if(tempMap[x, y] == Tiles.water && blocksAround >= 3)
					worldMap[x,y] = Tiles.grass;
				if(tempMap[x, y] == Tiles.grass && blocksAround <3)
					worldMap[x,y] = Tiles.water;
			}
		}
	}
	private void GenerateBeach()
	{
		Tiles[,] tempMap = (Tiles[,])worldMap.Clone();
		int beachBrushSize = 3;
		for (int x = 1; x < worldWidth - 1; x++)
		{
			for(int y = 1; y < worldHeight - 1; y++)
			{
				if (tempMap[x,y] == Tiles.grass && (tempMap[x-1,y] == Tiles.water || tempMap[x+1,y] == Tiles.water || tempMap[x,y-1] == Tiles.water || tempMap[x,y+1] == Tiles.water))
				{
					for (int i = -beachBrushSize; i <= beachBrushSize; i++)
					{
						for (int j = -beachBrushSize; j <= beachBrushSize; j++)
						{
							int dx = x + i;
							int dy = y + j;
							if(dx < worldWidth && dx >= 0 && dy < worldHeight && dy >= 0)
								if((i*i) + (j*j) <= (beachBrushSize*beachBrushSize))
									if(tempMap[dx,dy] == Tiles.grass)
										worldMap[dx, dy] = Tiles.sand;
						}
						
					}
				}
			}
		}
	}
	private void GenerateResources()
	{
		bool[,] occupied = new bool[worldWidth, worldHeight];
		Random rng = new Random();
		FastNoiseLite biomeNoise = new FastNoiseLite();
		biomeNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
		biomeNoise.Frequency = 0.02f;
		biomeNoise.Seed = rng.Next();
		for (int x = 0; x < worldWidth; x++)
		{
			for (int y = 0; y < worldHeight; y++)
			{
				if(worldMap[x,y] == Tiles.water)
					continue;
				if(worldMap[x,y] == Tiles.grass)
				{
					float value = biomeNoise.GetNoise2D(x, y);
					if(value > 0.2f)
					{
						int chance = rng.Next(1, 100);
						if (chance <= 70)
						{
							if (!occupied[x, y])
							{
								var instance = trees[1].Instantiate<Node2D>();
								instance.Position = resourcesLayer.MapToLocal(new Vector2I(x, y));
								AddChild(instance);
								for (int i = x; i <= x+1; i++)
									for (int j = y; j <= y+1; j++)
										if(i < worldWidth && j < worldHeight)
											occupied[i,j] = true;
							}
							
						}
					}
					if(value < -0.45f)
					{
						int chance = rng.Next(1, 100);
						if (chance <= 30)
						{
							if (!occupied[x, y])
							{
								var instance = rocks[rng.Next(0, rocks.Length)].Instantiate<Node2D>();
								instance.Position = resourcesLayer.MapToLocal(new Vector2I(x, y));
								AddChild(instance);
								for (int i = x; i <= x+1; i++)
									for (int j = y; j <= y+1; j++)
										if(i < worldWidth && j < worldHeight)
											occupied[i,j] = true;
								
							}
							
						}
						
					}
				}
			}
		}
	}
	
	
}
