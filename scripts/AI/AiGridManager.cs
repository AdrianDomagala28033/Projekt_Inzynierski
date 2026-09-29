using Godot;
using System;
using System.Collections.Generic;

public partial class AiGridManager : Node
{
    private WorldMap _worldMap;
    public int[,] Heatmap { get; private set; }

    private int _width;
    private int _height;

    public void Initialize(WorldMap map)
    {
        _worldMap = map;
        _width = map.worldWidth;
        _height = map.worldHeight;

        Heatmap = new int[_width, _height];
        GD.Print($"[AI] AIGridManager gotowy! Wymiary siatki: {_width}x{_height}");    }

    public bool IsCellWalkable(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return false;

        if (_worldMap.GetTile(x, y) == Tiles.water)
            return false;

        if (_worldMap.OccupiedCells[x, y])
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

    public List<Vector2I> FindPath(Vector2I startPos, Vector2I targetPos)
    {
        if (!IsCellWalkable(targetPos.X, targetPos.Y))
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

            Vector2I[] directions = { new Vector2I(0, 1), new Vector2I(0, -1), new Vector2I(1, 0), new Vector2I(-1, 0) };
            
            foreach (var dir in directions)
            {
                Vector2I neighborPos = currentNode.Position + dir;

                if (!IsCellWalkable(neighborPos.X, neighborPos.Y) || closedList.Contains(neighborPos))
                {
                    continue;
                }

                int heatPenalty = Heatmap[neighborPos.X, neighborPos.Y];
                int newMovementCostToNeighbor = currentNode.GCost + 10 + heatPenalty;

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
                    neighborNode.HCost = GetManhattanDistance(neighborPos, targetPos);
                    neighborNode.Parent = currentNode;
                }
            }
        }

        return null; 
    }

    private int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)) * 10;
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