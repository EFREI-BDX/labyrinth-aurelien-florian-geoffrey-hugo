using ApiTypes;

namespace Labyrinth.TrainingServer.Models;

/// <summary>
/// Represents the server-side state of a crawler.
/// </summary>
public class CrawlerState
{
    public Guid Id { get; set; }
    public Guid AppKey { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Direction { get; set; }
    public bool Walking { get; set; }
    public List<InventoryItem> Bag { get; set; } = new();
    
    /// <summary>
    /// Converts to the API DTO.
    /// </summary>
    public Crawler ToDto(TileType facingTile, List<InventoryItem> tileItems)
    {
        return new Crawler
        {
            Id = Id,
            X = X,
            Y = Y,
            Dir = Direction,
            Walking = false,
            FacingTile = facingTile,
            Bag = Bag.ToArray(),
            Items = tileItems.ToArray()
        };
    }
}
