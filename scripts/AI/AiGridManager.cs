using Godot;
using System;
using System.Collections.Generic;

public partial class AiGridManager : Node
{
    private WorldMap _worldMap;
    public int[,] Heatmap { get; private set; }
    public bool[,] OccupiedCells { get; private set; } 
    public int[,] ObstaclePenalties { get; private set; }

    private int _width;
    private int _height;

    private bool _isTestMode = false;
    private bool[,] _testObstacles;

    public void Initialize(WorldMap map)
    {
        _worldMap = map;
        _width = map.worldWidth;
        _height = map.worldHeight;

        Heatmap = new int[_width, _height];
        OccupiedCells = new bool[_width, _height];
        ObstaclePenalties = new int[_width, _height];
        GD.Print($"[AI] AIGridManager gotowy! Wymiary siatki: {_width}x{_height}");    
    }

    public void InitializeTestMode(int width, int height, bool[,] obstacles)
    {
        _width = width;
        _height = height;
        _testObstacles = obstacles;
        _isTestMode = true;
        
        Heatmap = new int[_width, _height];
        OccupiedCells = new bool[_width, _height];
        ObstaclePenalties = new int[_width, _height];
    }

    public void SetObstaclePenalty(Vector2I gridPos, int penalty)
    {
        if (gridPos.X >= 0 && gridPos.X < _width && gridPos.Y >= 0 && gridPos.Y < _height)
        {
            ObstaclePenalties[gridPos.X, gridPos.Y] = penalty;
            
            if (penalty > 0)
            {
                OccupiedCells[gridPos.X, gridPos.Y] = false;
                if (_isTestMode && _testObstacles != null)
                {
                    _testObstacles[gridPos.X, gridPos.Y] = false;
                }
            }
        }
    }

    public void BlockCell(Vector2I gridPos)
    {
        if (gridPos.X >= 0 && gridPos.X < _width && gridPos.Y >= 0 && gridPos.Y < _height)
        {
            if (_isTestMode && _testObstacles != null)
            {
                _testObstacles[gridPos.X, gridPos.Y] = true;
            }
            else
            {
                OccupiedCells[gridPos.X, gridPos.Y] = true;
            }
        }
    }

    public bool IsCellWalkable(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        if (_isTestMode)
        {
            if (ObstaclePenalties[x, y] > 0) return true;
            return !_testObstacles[x, y];
        }

        if (_worldMap != null && _worldMap.GetTile(x, y) == Tiles.water)
            return false;

        if (ObstaclePenalties[x, y] > 0)
            return true;

        if (OccupiedCells[x, y])
            return false;

        return true;
    }

    public void AddHeat(Vector2I center, int radius, int heatValue)
    {
        for (int x = center.X - radius; x <= center.X + radius; x++)
        {
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    Heatmap[x, y] += heatValue;
                }
            }
        }
    }

    // ---  ALGORYTM A* ---
    public class PathNode
    {
        public Vector2I Position;
        public PathNode Parent;
        public int GCost; 
        public int HCost;
        public int FCost { get { return GCost + HCost; } }

        public PathNode(Vector2I pos) 
        { 
            Position = pos; 
        }
    }

    // TUTAJ POPRAWKA: Dodano int obstacleMultiplier = 1
    public List<Vector2I> FindPath(Vector2I startPos, Vector2I targetPos, int obstacleMultiplier = 1)
    {
        if (targetPos.X < 0 || targetPos.X >= _width || targetPos.Y < 0 || targetPos.Y >= _height)
        {
            return null;
        }

        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2I> closedList = new HashSet<Vector2I>();

        PathNode startNode = new PathNode(startPos);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost || 
                (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            if (currentNode.Position == targetPos)
            {
                return RetracePath(startNode, currentNode);
            }

            Vector2I[] directions = { 
                new Vector2I(0, 1), new Vector2I(0, -1), new Vector2I(1, 0), new Vector2I(-1, 0),
                new Vector2I(1, 1), new Vector2I(1, -1), new Vector2I(-1, 1), new Vector2I(-1, -1) 
            };
            
            foreach (var dir in directions)
            {
                Vector2I neighborPos = currentNode.Position + dir;
                bool isTarget = (neighborPos == targetPos);

                if ((!IsCellWalkable(neighborPos.X, neighborPos.Y) && !isTarget) || closedList.Contains(neighborPos))
                {
                    continue;
                }

                if (dir.X != 0 && dir.Y != 0)
                {
                    int x1 = currentNode.Position.X + dir.X;
                    int y1 = currentNode.Position.Y;
                    int x2 = currentNode.Position.X;
                    int y2 = currentNode.Position.Y + dir.Y;

                    bool isCorner1Solid = !IsCellWalkable(x1, y1) || ObstaclePenalties[x1, y1] > 0;
                    bool isCorner2Solid = !IsCellWalkable(x2, y2) || ObstaclePenalties[x2, y2] > 0;

                    // ZMIANA NA || (LUB) - Blokuje skos nawet jeśli tylko jedna kratka obok to mur
                    if (isCorner1Solid || isCorner2Solid)
                    {
                        if (new Vector2I(x1, y1) != targetPos && new Vector2I(x2, y2) != targetPos)
                        {
                            continue; 
                        }
                    }
                }

                int heatPenalty = Heatmap[neighborPos.X, neighborPos.Y];
                int obstaclePenalty = ObstaclePenalties[neighborPos.X, neighborPos.Y];
                
                int moveCost = (dir.X != 0 && dir.Y != 0) ? 14 : 10;
                
                // MNOŻENIE KARY
                int newMovementCostToNeighbor = currentNode.GCost + moveCost + heatPenalty + (obstaclePenalty * obstacleMultiplier);
                
                PathNode neighborNode = null;
                foreach (var node in openList)
                {
                    if (node.Position == neighborPos)
                    {
                        neighborNode = node;
                        break;
                    }
                }

                if (neighborNode == null || newMovementCostToNeighbor < neighborNode.GCost)
                {
                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighborPos);
                        openList.Add(neighborNode);
                    }

                    neighborNode.GCost = newMovementCostToNeighbor;
                    neighborNode.HCost = GetOctileDistance(neighborPos, targetPos);
                    neighborNode.Parent = currentNode;
                }
            }
        }

        return null; 
    }

    private int GetOctileDistance(Vector2I a, Vector2I b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        return 10 * (dx + dy) - 6 * Math.Min(dx, dy);
    }

    private List<Vector2I> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2I> path = new List<Vector2I>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Reverse();
        return path;
    }
}