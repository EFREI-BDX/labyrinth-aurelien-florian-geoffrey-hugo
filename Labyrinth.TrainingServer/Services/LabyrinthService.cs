using ApiTypes;
using Labyrinth.TrainingServer.Models;

namespace Labyrinth.TrainingServer.Services;

/// <summary>
/// Result of creating a crawler.
/// </summary>
public enum CreateCrawlerResult
{
    Success,
    TooManyCrawlers
}

/// <summary>
/// Result of updating a crawler.
/// </summary>
public enum UpdateCrawlerResult
{
    Success,
    NotFound,
    Forbidden,
    Conflict // Tile not traversable
}

/// <summary>
/// Result of a transfer operation.
/// </summary>
public enum TransferResult
{
    Success,
    NotFound,
    Forbidden
}

/// <summary>
/// Result of deleting a crawler.
/// </summary>
public enum DeleteCrawlerResult
{
    Success,
    NotFound,
    Forbidden
}

/// <summary>
/// Service that manages the labyrinth state and crawler operations.
/// </summary>
public class LabyrinthService
{
    private const int MaxCrawlersPerAppKey = 3;

    private LabyrinthMap? _map;
    private readonly Dictionary<Guid, CrawlerState> _crawlers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all crawlers for a specific appKey.
    /// </summary>
    public Crawler[] GetAllCrawlers(Guid appKey)
    {
        lock (_lock)
        {
            if (_map == null)
            {
                return Array.Empty<Crawler>();
            }

            return _crawlers.Values
                .Where(c => c.AppKey == appKey)
                .Select(c => c.ToDto(
                    _map.GetFacingTile(c.X, c.Y, c.Direction),
                    GetTileItemsSafe(c.X, c.Y)))
                .ToArray();
        }
    }

    /// <summary>
    /// Gets a specific crawler.
    /// </summary>
    public (Crawler? crawler, bool forbidden) GetCrawler(Guid crawlerId, Guid appKey)
    {
        lock (_lock)
        {
            if (_map == null || !_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, false);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, true); // Forbidden
            }

            var facingTile = _map.GetFacingTile(crawlerState.X, crawlerState.Y, crawlerState.Direction);
            var tileItems = GetTileItemsSafe(crawlerState.X, crawlerState.Y);
            return (crawlerState.ToDto(facingTile, tileItems), false);
        }
    }

    /// <summary>
    /// Gets the bag contents of a crawler.
    /// </summary>
    public (InventoryItem[]? items, bool forbidden) GetCrawlerBag(Guid crawlerId, Guid appKey)
    {
        lock (_lock)
        {
            if (!_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, false);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, true);
            }

            return (crawlerState.Bag.ToArray(), false);
        }
    }

    /// <summary>
    /// Gets the items on the crawler's current tile.
    /// </summary>
    public (InventoryItem[]? items, bool forbidden) GetCrawlerItems(Guid crawlerId, Guid appKey)
    {
        lock (_lock)
        {
            if (_map == null || !_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, false);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, true);
            }

            var tileItems = GetTileItemsSafe(crawlerState.X, crawlerState.Y);
            return (tileItems.ToArray(), false);
        }
    }

    private List<InventoryItem> GetTileItemsSafe(int x, int y)
    {
        if (_map == null || x < 0 || x >= _map.Width || y < 0 || y >= _map.Height)
        {
            return new List<InventoryItem>();
        }
        return _map.TileItems[x, y];
    }

    /// <summary>
    /// Creates a new crawler in the labyrinth.
    /// </summary>
    public (Crawler? crawler, CreateCrawlerResult result) CreateCrawler(Guid appKey, Settings? settings)
    {
        lock (_lock)
        {
            // Initialize the labyrinth on first crawler creation
            if (_map == null)
            {
                _map = LabyrinthMap.CreateDefault();
            }

            // Check max crawlers per appKey
            var existingCount = _crawlers.Values.Count(c => c.AppKey == appKey);
            if (existingCount >= MaxCrawlersPerAppKey)
            {
                return (null, CreateCrawlerResult.TooManyCrawlers);
            }

            var crawlerState = new CrawlerState
            {
                Id = Guid.NewGuid(),
                AppKey = appKey,
                X = _map.StartX,
                Y = _map.StartY,
                Direction = Direction.North,
                Walking = false,
                Bag = new List<InventoryItem>()
            };

            _crawlers[crawlerState.Id] = crawlerState;

            var facingTile = _map.GetFacingTile(crawlerState.X, crawlerState.Y, crawlerState.Direction);
            var tileItems = _map.TileItems[crawlerState.X, crawlerState.Y];

            return (crawlerState.ToDto(facingTile, tileItems), CreateCrawlerResult.Success);
        }
    }

    /// <summary>
    /// Updates a crawler (move/turn).
    /// </summary>
    public (Crawler? crawler, UpdateCrawlerResult result) UpdateCrawler(Guid crawlerId, Guid appKey, Crawler update)
    {
        lock (_lock)
        {
            if (_map == null || !_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, UpdateCrawlerResult.NotFound);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, UpdateCrawlerResult.Forbidden);
            }

            bool movementBlocked = false;

            // Handle direction change first (before walking)
            crawlerState.Direction = update.Dir;

            // Handle walking
            if (update.Walking)
            {
                bool hasKey = crawlerState.Bag.Any(i => i.Type == ItemType.Key);

                if (_map.CanMoveTo(crawlerState.X, crawlerState.Y, crawlerState.Direction, hasKey))
                {
                    var (dx, dy) = LabyrinthMap.GetDirectionDelta(crawlerState.Direction);
                    int newX = crawlerState.X + dx;
                    int newY = crawlerState.Y + dy;

                    // Check if moving to a door - use key
                    if (newX >= 0 && newX < _map.Width && newY >= 0 && newY < _map.Height)
                    {
                        if (_map.Tiles[newX, newY] == TileType.Door && !_map.DoorStates[newX, newY])
                        {
                            // Use a key to open the door
                            var keyIndex = crawlerState.Bag.FindIndex(i => i.Type == ItemType.Key);
                            if (keyIndex >= 0)
                            {
                                crawlerState.Bag.RemoveAt(keyIndex);
                                _map.DoorStates[newX, newY] = true;
                            }
                        }

                        crawlerState.X = newX;
                        crawlerState.Y = newY;
                    }
                    else
                    {
                        // Moving outside - crawler escapes
                        crawlerState.X = newX;
                        crawlerState.Y = newY;
                    }
                }
                else
                {
                    // Tile not traversable
                    movementBlocked = true;
                }
            }

            crawlerState.Walking = false;

            // Determine facing tile
            TileType facingTile;
            if (crawlerState.X < 0 || crawlerState.X >= _map.Width ||
                crawlerState.Y < 0 || crawlerState.Y >= _map.Height)
            {
                facingTile = TileType.Outside;
            }
            else
            {
                facingTile = _map.GetFacingTile(crawlerState.X, crawlerState.Y, crawlerState.Direction);
            }

            // Get tile items (empty if outside)
            var tileItems = (crawlerState.X >= 0 && crawlerState.X < _map.Width &&
                            crawlerState.Y >= 0 && crawlerState.Y < _map.Height)
                ? _map.TileItems[crawlerState.X, crawlerState.Y]
                : new List<InventoryItem>();

            var result = movementBlocked ? UpdateCrawlerResult.Conflict : UpdateCrawlerResult.Success;
            return (crawlerState.ToDto(facingTile, tileItems), result);
        }
    }

    /// <summary>
    /// Transfers items from the crawler's bag to the ground.
    /// </summary>
    public (InventoryItem[]? items, TransferResult result) TransferFromBag(Guid crawlerId, Guid appKey, InventoryItem[] itemsToTransfer)
    {
        lock (_lock)
        {
            if (_map == null || !_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, TransferResult.NotFound);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, TransferResult.Forbidden);
            }

            if (crawlerState.X < 0 || crawlerState.X >= _map.Width ||
                crawlerState.Y < 0 || crawlerState.Y >= _map.Height)
            {
                return (null, TransferResult.NotFound); // Can't transfer if outside
            }

            var tileItems = _map.TileItems[crawlerState.X, crawlerState.Y];

            foreach (var item in itemsToTransfer)
            {
                if (item.MoveRequired == true)
                {
                    var bagItem = crawlerState.Bag.FirstOrDefault(i => i.Type == item.Type);
                    if (bagItem != null)
                    {
                        crawlerState.Bag.Remove(bagItem);
                        tileItems.Add(new InventoryItem { Type = item.Type });
                    }
                }
            }

            return (crawlerState.Bag.ToArray(), TransferResult.Success);
        }
    }

    /// <summary>
    /// Transfers items from the ground to the crawler's bag.
    /// </summary>
    public (InventoryItem[]? items, TransferResult result) TransferFromGround(Guid crawlerId, Guid appKey, InventoryItem[] itemsToTransfer)
    {
        lock (_lock)
        {
            if (_map == null || !_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return (null, TransferResult.NotFound);
            }

            if (crawlerState.AppKey != appKey)
            {
                return (null, TransferResult.Forbidden);
            }

            if (crawlerState.X < 0 || crawlerState.X >= _map.Width ||
                crawlerState.Y < 0 || crawlerState.Y >= _map.Height)
            {
                return (null, TransferResult.NotFound); // Can't transfer if outside
            }

            var tileItems = _map.TileItems[crawlerState.X, crawlerState.Y];

            foreach (var item in itemsToTransfer)
            {
                if (item.MoveRequired == true)
                {
                    var groundItem = tileItems.FirstOrDefault(i => i.Type == item.Type);
                    if (groundItem != null)
                    {
                        tileItems.Remove(groundItem);
                        crawlerState.Bag.Add(new InventoryItem { Type = item.Type });
                    }
                }
            }

            return (tileItems.ToArray(), TransferResult.Success);
        }
    }

    /// <summary>
    /// Deletes a crawler.
    /// </summary>
    public DeleteCrawlerResult DeleteCrawler(Guid crawlerId, Guid appKey)
    {
        lock (_lock)
        {
            if (!_crawlers.TryGetValue(crawlerId, out var crawlerState))
            {
                return DeleteCrawlerResult.NotFound;
            }

            if (crawlerState.AppKey != appKey)
            {
                return DeleteCrawlerResult.Forbidden;
            }

            _crawlers.Remove(crawlerId);
            return DeleteCrawlerResult.Success;
        }
    }

    /// <summary>
    /// Resets the labyrinth state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _map = null;
            _crawlers.Clear();
        }
    }
}
