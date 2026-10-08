using Godot;

public partial class SlimeEnemy : MeleeEnemy
{
    [ExportGroup("Slime Settings")]
    [Export] public PackedScene NextSlimeScene; 
    [Export] public float SpawnOffset = 16f; 

    protected override void OnDeath()
    {
        if (NextSlimeScene != null)
        {
            Vector2[] offsets = new Vector2[] {
                new Vector2(-SpawnOffset, 0),
                new Vector2(SpawnOffset, 0),
                new Vector2(0, -SpawnOffset),
                new Vector2(0, SpawnOffset)
            };

            int spawnedCount = 0;
            int offsetIndex = 0;

            while (spawnedCount < 2 && offsetIndex < offsets.Length)
            {
                Vector2 targetPos = this.GlobalPosition + offsets[offsetIndex];

                if (IsPositionSafe(targetPos))
                {
                    SpawnSlimeAt(targetPos);
                    spawnedCount++;
                }
                offsetIndex++;
            }

            while (spawnedCount < 2)
            {
                SpawnSlimeAt(this.GlobalPosition);
                spawnedCount++;
            }
        }
        
        base.OnDeath();
    }

    private void SpawnSlimeAt(Vector2 pos)
    {
        EnemyBase nextSlime = NextSlimeScene.Instantiate<EnemyBase>();
        nextSlime.GlobalPosition = pos;
        nextSlime.TargetPlayer = this.TargetPlayer;
        nextSlime.GridManager = this.GridManager;
        
        GetParent().CallDeferred(Node.MethodName.AddChild, nextSlime);
    }

    private bool IsPositionSafe(Vector2 pos)
    {
        if (GridManager == null || GridManager.ObstaclePenalties == null) return true;

        int x = Mathf.FloorToInt(pos.X / TileSize);
        int y = Mathf.FloorToInt(pos.Y / TileSize);

        if (x < 0 || x >= GridManager.ObstaclePenalties.GetLength(0) || 
            y < 0 || y >= GridManager.ObstaclePenalties.GetLength(1))
        {
            return false;
        }

        if (GridManager.ObstaclePenalties[x, y] > 0 || GridManager.OccupiedCells[x, y])
        {
            return false;
        }

        return true;
    }
}