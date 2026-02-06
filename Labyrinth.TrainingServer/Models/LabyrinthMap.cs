using ApiTypes;

namespace Labyrinth.TrainingServer.Models;

/// <summary>
/// Represents a labyrinth map with tiles and items.
/// </summary>
public class LabyrinthMap
{
    public int Width { get; }
    public int Height { get; }
    public TileType[,] Tiles { get; }
    public List<InventoryItem>[,] TileItems { get; }
    public bool[,] DoorStates { get; } // true = open, false = closed
    public int StartX { get; }
    public int StartY { get; }

    public LabyrinthMap(int width, int height, int startX, int startY)
    {
        Width = width;
        Height = height;
        StartX = startX;
        StartY = startY;
        Tiles = new TileType[width, height];
        TileItems = new List<InventoryItem>[width, height];
        DoorStates = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileItems[x, y] = new List<InventoryItem>();
            }
        }
    }

    /// <summary>
    /// Creates a default 21x19 labyrinth.
    /// </summary>
    public static LabyrinthMap CreateDefault()
    {
        // Create a simpler labyrinth for testing
        int width = 21;
        int height = 19;
        int startX = width / 2;
        int startY = height / 2;
        
        var map = new LabyrinthMap(width, height, startX, startY);
        
        // Fill with rooms initially
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map.Tiles[x, y] = TileType.Room;
            }
        }
        
        // Create outer walls
        for (int x = 0; x < width; x++)
        {
            map.Tiles[x, 0] = TileType.Wall;
            map.Tiles[x, height - 1] = TileType.Wall;
        }
        for (int y = 0; y < height; y++)
        {
            map.Tiles[0, y] = TileType.Wall;
            map.Tiles[width - 1, y] = TileType.Wall;
        }
        
        // Add some inner structure - checkerboard walls at even positions
        for (int x = 2; x < width - 2; x += 2)
        {
            for (int y = 2; y < height - 2; y += 2)
            {
                map.Tiles[x, y] = TileType.Wall;
            }
        }
        
        // Add a door
        map.Tiles[0, height / 2] = TileType.Door;
        
        // Add a key
        map.TileItems[startX - 1, startY].Add(new InventoryItem { Type = ItemType.Key });
        
        return map;
    }

    public TileType GetFacingTile(int x, int y, Direction direction)
    {
        var (dx, dy) = GetDirectionDelta(direction);
        int newX = x + dx;
        int newY = y + dy;
        
        if (newX < 0 || newX >= Width || newY < 0 || newY >= Height)
        {
            return TileType.Outside;
        }
        
        return Tiles[newX, newY];
    }

    public bool CanMoveTo(int x, int y, Direction direction, bool hasKey)
    {
        var (dx, dy) = GetDirectionDelta(direction);
        int newX = x + dx;
        int newY = y + dy;
        
        if (newX < 0 || newX >= Width || newY < 0 || newY >= Height)
        {
            return true; // Can move outside (win condition)
        }
        
        var tile = Tiles[newX, newY];
        return tile switch
        {
            TileType.Room => true,
            TileType.Wall => false,
            TileType.Door => hasKey || DoorStates[newX, newY],
            TileType.Outside => true,
            _ => false
        };
    }

    public static (int dx, int dy) GetDirectionDelta(Direction direction)
    {
        return direction switch
        {
            Direction.North => (0, -1),
            Direction.East => (1, 0),
            Direction.South => (0, 1),
            Direction.West => (-1, 0),
            _ => (0, 0)
        };
    }
}
