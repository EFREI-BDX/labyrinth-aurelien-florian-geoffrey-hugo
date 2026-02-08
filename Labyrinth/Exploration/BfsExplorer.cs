using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Pathfinding;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration;

/// <summary>
/// BFS Frontier explorer that uses breadth-first search to systematically
/// explore unknown tiles until finding the exit.
/// </summary>
public class BfsExplorer : IExplorer
{
    private readonly IPathfinder _pathfinder;
    private readonly Tile[,] _localMap;
    private readonly int _mapSize;
    private Position _currentPos;
    private Direction _currentDir;

    // Singleton instance for Unknown tiles (memory optimization)
    private static readonly Unknown UnknownTile = new();

    /// <inheritdoc />
    public ICrawler Crawler { get; }

    /// <inheritdoc />
    public event EventHandler<CrawlingEventArgs>? PositionChanged;

    /// <inheritdoc />
    public event EventHandler<CrawlingEventArgs>? DirectionChanged;

    /// <summary>
    /// Creates a new BFS explorer with its own local map.
    /// </summary>
    /// <param name="crawler">The crawler to control.</param>
    /// <param name="pathfinder">The pathfinder to use for BFS.</param>
    /// <param name="mapSize">Size of the local map (default 100x100).</param>
    public BfsExplorer(ICrawler crawler, IPathfinder pathfinder, int mapSize = 100)
    {
        Crawler = crawler;
        _pathfinder = pathfinder;
        _mapSize = mapSize;

        // Initialize local map with Unknown tiles
        _localMap = new Tile[mapSize, mapSize];
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                _localMap[x, y] = UnknownTile;
            }
        }

        // Start position is in the center of the map
        int center = mapSize / 2;
        _currentPos = new Position(center, center);
        _currentDir = (Direction)crawler.Direction.Clone();

        // Mark starting position as traversable (Room)
        _localMap[center, center] = new Room();
    }

    /// <inheritdoc />
    public async Task<int> GetOut(int maxSteps, Inventory? bag = null, CancellationToken ct = default)
    {
        bag ??= new MyInventory();

        while (maxSteps > 0 && !ct.IsCancellationRequested)
        {
            // 1. Observe the facing tile and update local map
            var facingType = await Crawler.FacingTileType;
            var facingPos = _currentPos.Move(Crawler.Direction);
            UpdateMapWithTileType(facingPos, facingType);

            // 2. If Outside is detected, we found the exit!
            if (facingType == typeof(Outside))
            {
                return maxSteps;
            }

            // 3. Find the nearest unknown tile using BFS
            var pathResult = _pathfinder.FindNearestUnknown(_currentPos, _localMap);

            if (pathResult.Status == PathStatus.NotFound)
            {
                // Map fully explored, no exit found - give up
                return maxSteps;
            }

            // 4. We need to move to the frontier position (last tile before Unknown)
            // If path is empty, the Unknown is adjacent - just turn to face it
            if (pathResult.Path.Count == 0 && pathResult.NearestUnknown.HasValue)
            {
                // Turn to face the unknown tile
                var targetDir = GetDirectionTo(_currentPos, pathResult.NearestUnknown.Value);
                maxSteps = await TurnToFace(targetDir, maxSteps, ct);
                if (ct.IsCancellationRequested) break;

                // Try to walk into it
                maxSteps = await TryWalkForward(bag, maxSteps, ct);
            }
            else if (pathResult.Path.Count > 0)
            {
                // Follow the path one step at a time, then re-evaluate
                // This way we re-check for unknown tiles after each step
                var nextPos = pathResult.Path[0];
                var targetDir = GetDirectionTo(_currentPos, nextPos);
                
                maxSteps = await TurnToFace(targetDir, maxSteps, ct);
                if (ct.IsCancellationRequested || maxSteps <= 0) break;
                
                maxSteps = await TryWalkForward(bag, maxSteps, ct);
            }
        }

        return maxSteps;
    }

    /// <summary>
    /// Follows a path by turning and walking step by step.
    /// </summary>
    private async Task<int> FollowPath(IReadOnlyList<Position> path, Inventory bag, int maxSteps, CancellationToken ct)
    {
        foreach (var nextPos in path)
        {
            if (ct.IsCancellationRequested || maxSteps <= 0)
                break;

            // Determine direction to next position
            var targetDir = GetDirectionTo(_currentPos, nextPos);

            // Turn to face the target direction
            maxSteps = await TurnToFace(targetDir, maxSteps, ct);
            if (ct.IsCancellationRequested || maxSteps <= 0)
                break;

            // Walk forward
            maxSteps = await TryWalkForward(bag, maxSteps, ct);
        }

        return maxSteps;
    }

    /// <summary>
    /// Turns the crawler to face the specified direction.
    /// </summary>
    private async Task<int> TurnToFace(Direction targetDir, int maxSteps, CancellationToken ct)
    {
        while (!Crawler.Direction.Equals(targetDir) && maxSteps > 0 && !ct.IsCancellationRequested)
        {
            // Determine shortest turn direction
            var testDir = (Direction)Crawler.Direction.Clone();
            testDir.TurnRight();

            if (testDir.Equals(targetDir))
            {
                // Turn right
                Crawler.Direction.TurnRight();
            }
            else
            {
                // Turn left (covers left, or 180 degrees - we'll just keep turning left)
                Crawler.Direction.TurnLeft();
            }

            // Keep our local direction in sync
            _currentDir = (Direction)Crawler.Direction.Clone();
            
            maxSteps--;
            DirectionChanged?.Invoke(this, new CrawlingEventArgs(Crawler));

            // Small delay to allow cancellation checks
            await Task.Yield();
        }

        return maxSteps;
    }

    /// <summary>
    /// Tries to walk forward into the facing tile.
    /// </summary>
    private async Task<int> TryWalkForward(Inventory bag, int maxSteps, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || maxSteps <= 0)
            return maxSteps;

        // First observe what's ahead
        var facingType = await Crawler.FacingTileType;
        var facingPos = _currentPos.Move(Crawler.Direction);
        UpdateMapWithTileType(facingPos, facingType);

        // Try to walk
        var roomInventory = await Crawler.TryWalk(bag);
        maxSteps--;

        if (roomInventory != null)
        {
            // Successfully walked - update local position
            _currentPos = facingPos;

            // Collect all items from the room (same pattern as RandExplorer)
            await bag.TryMoveItemsFrom(
                roomInventory,
                roomInventory.ItemTypes.Select(_ => true).ToList()
            );

            PositionChanged?.Invoke(this, new CrawlingEventArgs(Crawler));
        }
        else
        {
            // Failed to walk - mark as wall if we haven't already
            if (IsInBounds(facingPos) && _localMap[facingPos.X, facingPos.Y] is Unknown)
            {
                _localMap[facingPos.X, facingPos.Y] = Wall.Singleton;
            }
        }

        return maxSteps;
    }

    /// <summary>
    /// Updates the local map with the observed tile type.
    /// </summary>
    private void UpdateMapWithTileType(Position pos, Type tileType)
    {
        if (!IsInBounds(pos)) return;

        // Only update if currently unknown
        if (_localMap[pos.X, pos.Y] is not Unknown) return;

        if (tileType == typeof(Room))
        {
            _localMap[pos.X, pos.Y] = new Room();
        }
        else if (tileType == typeof(Wall))
        {
            _localMap[pos.X, pos.Y] = Wall.Singleton;
        }
        else if (tileType == typeof(Door))
        {
            _localMap[pos.X, pos.Y] = new Door(); // Will be created locked by default
        }
        else if (tileType == typeof(Outside))
        {
            _localMap[pos.X, pos.Y] = Outside.Singleton;
        }
        // Keep as Unknown for unrecognized types
    }

    /// <summary>
    /// Gets the direction from one position to an adjacent position.
    /// </summary>
    private static Direction GetDirectionTo(Position from, Position to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        return (dx, dy) switch
        {
            (0, -1) => Direction.North,
            (1, 0) => Direction.East,
            (0, 1) => Direction.South,
            (-1, 0) => Direction.West,
            _ => throw new ArgumentException($"Positions are not adjacent: {from} -> {to}")
        };
    }

    /// <summary>
    /// Checks if a position is within the local map bounds.
    /// </summary>
    private bool IsInBounds(Position pos) =>
        pos.X >= 0 && pos.X < _mapSize && pos.Y >= 0 && pos.Y < _mapSize;
}
