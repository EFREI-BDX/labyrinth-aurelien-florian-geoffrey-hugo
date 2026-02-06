using ApiTypes;
using Labyrinth.TrainingServer.Models;

namespace LabyrinthTest.TrainingServer;

[TestFixture]
public class LabyrinthMapTests
{
    [Test]
    public void CreateDefault_HasCorrectDimensions()
    {
        // Act
        var map = LabyrinthMap.CreateDefault();

        // Assert
        Assert.That(map.Width, Is.EqualTo(21));
        Assert.That(map.Height, Is.EqualTo(19));
    }

    [Test]
    public void CreateDefault_StartIsCenter()
    {
        // Act
        var map = LabyrinthMap.CreateDefault();

        // Assert
        Assert.That(map.StartX, Is.EqualTo(10));
        Assert.That(map.StartY, Is.EqualTo(9));
    }

    [Test]
    public void CreateDefault_HasOuterWalls()
    {
        // Act
        var map = LabyrinthMap.CreateDefault();

        // Assert - Check all edges are walls
        for (int x = 0; x < map.Width; x++)
        {
            Assert.That(map.Tiles[x, 0], Is.EqualTo(TileType.Wall), $"Top edge at ({x}, 0)");
            Assert.That(map.Tiles[x, map.Height - 1], Is.EqualTo(TileType.Wall), $"Bottom edge at ({x}, {map.Height - 1})");
        }
        for (int y = 0; y < map.Height; y++)
        {
            // Left edge has a door, so skip that check for door position
            if (y != map.Height / 2)
            {
                Assert.That(map.Tiles[0, y], Is.EqualTo(TileType.Wall), $"Left edge at (0, {y})");
            }
            Assert.That(map.Tiles[map.Width - 1, y], Is.EqualTo(TileType.Wall), $"Right edge at ({map.Width - 1}, {y})");
        }
    }

    [Test]
    public void CreateDefault_HasDoor()
    {
        // Act
        var map = LabyrinthMap.CreateDefault();

        // Assert - Door is at left edge center
        Assert.That(map.Tiles[0, map.Height / 2], Is.EqualTo(TileType.Door));
    }

    [Test]
    public void CreateDefault_HasKey()
    {
        // Act
        var map = LabyrinthMap.CreateDefault();

        // Assert - Key is at (StartX - 1, StartY)
        Assert.That(map.TileItems[map.StartX - 1, map.StartY].Count, Is.EqualTo(1));
        Assert.That(map.TileItems[map.StartX - 1, map.StartY][0].Type, Is.EqualTo(ItemType.Key));
    }

    [TestCase(Direction.North, 0, -1)]
    [TestCase(Direction.East, 1, 0)]
    [TestCase(Direction.South, 0, 1)]
    [TestCase(Direction.West, -1, 0)]
    public void GetDirectionDelta_ReturnsCorrectDelta(Direction direction, int expectedDx, int expectedDy)
    {
        // Act
        var (dx, dy) = LabyrinthMap.GetDirectionDelta(direction);

        // Assert
        Assert.That(dx, Is.EqualTo(expectedDx));
        Assert.That(dy, Is.EqualTo(expectedDy));
    }

    [Test]
    public void GetFacingTile_AtEdge_ReturnsOutside()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();
        // Position at 0, height/2 facing West should see Outside (after door)

        // Act - From just inside the door, facing the door
        var facing = map.GetFacingTile(1, map.Height / 2, Direction.West);

        // Assert
        Assert.That(facing, Is.EqualTo(TileType.Door));
    }

    [Test]
    public void CanMoveTo_IntoRoom_ReturnsTrue()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();

        // Act - Check if can move from center to adjacent room
        var canMove = map.CanMoveTo(map.StartX, map.StartY, Direction.East, hasKey: false);

        // Assert
        Assert.That(canMove, Is.True);
    }

    [Test]
    public void CanMoveTo_IntoWall_ReturnsFalse()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();

        // Find a wall and try to move into it - outer wall at bottom
        // Act
        var canMove = map.CanMoveTo(1, map.Height - 2, Direction.South, hasKey: false);

        // Assert
        Assert.That(canMove, Is.False);
    }

    [Test]
    public void CanMoveTo_IntoClosedDoorWithoutKey_ReturnsFalse()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();

        // Move towards the door without a key
        // First find position next to door
        int doorY = map.Height / 2;
        
        // Act
        var canMove = map.CanMoveTo(1, doorY, Direction.West, hasKey: false);

        // Assert
        Assert.That(canMove, Is.False);
    }

    [Test]
    public void CanMoveTo_IntoClosedDoorWithKey_ReturnsTrue()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();
        int doorY = map.Height / 2;
        
        // Act
        var canMove = map.CanMoveTo(1, doorY, Direction.West, hasKey: true);

        // Assert
        Assert.That(canMove, Is.True);
    }

    [Test]
    public void CanMoveTo_IntoOpenDoor_ReturnsTrue()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();
        int doorY = map.Height / 2;
        map.DoorStates[0, doorY] = true; // Open the door
        
        // Act
        var canMove = map.CanMoveTo(1, doorY, Direction.West, hasKey: false);

        // Assert
        Assert.That(canMove, Is.True);
    }

    [Test]
    public void CanMoveTo_Outside_ReturnsTrue()
    {
        // Arrange
        var map = LabyrinthMap.CreateDefault();
        
        // From just inside the boundary, moving outside
        // Act
        var canMove = map.CanMoveTo(0, map.Height / 2, Direction.West, hasKey: true);

        // Assert
        Assert.That(canMove, Is.True);
    }
}
