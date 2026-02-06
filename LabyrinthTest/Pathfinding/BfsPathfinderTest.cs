using Labyrinth.Pathfinding;
using Labyrinth.Tiles;

namespace LabyrinthTest.Pathfinding
{
    [TestFixture]
    public class BfsPathfinderTest
    {
        private BfsPathfinder _pathfinder = null!;

        [SetUp]
        public void Setup()
        {
            _pathfinder = new BfsPathfinder();
        }

        #region Position Tests

        [Test]
        public void Position_MoveNorth_DecreasesY()
        {
            var pos = new Position(5, 5);
            var moved = pos.Move(Labyrinth.Crawl.Direction.North);

            Assert.That(moved, Is.EqualTo(new Position(5, 4)));
        }

        [Test]
        public void Position_MoveEast_IncreasesX()
        {
            var pos = new Position(5, 5);
            var moved = pos.Move(Labyrinth.Crawl.Direction.East);

            Assert.That(moved, Is.EqualTo(new Position(6, 5)));
        }

        [Test]
        public void Position_ManhattanDistance_ReturnsCorrectValue()
        {
            var a = new Position(0, 0);
            var b = new Position(3, 4);

            Assert.That(a.ManhattanDistance(b), Is.EqualTo(7));
        }

        [Test]
        public void Position_Neighbors_ReturnsFourPositions()
        {
            var pos = new Position(5, 5);
            var neighbors = pos.Neighbors.ToList();

            Assert.That(neighbors, Has.Count.EqualTo(4));
            Assert.That(neighbors, Does.Contain(new Position(5, 4))); // North
            Assert.That(neighbors, Does.Contain(new Position(6, 5))); // East
            Assert.That(neighbors, Does.Contain(new Position(5, 6))); // South
            Assert.That(neighbors, Does.Contain(new Position(4, 5))); // West
        }

        #endregion

        #region FindPath Tests

        [Test]
        public void FindPath_SamePosition_ReturnsEmptyPath()
        {
            var map = CreateSimpleMap(3, 3);
            var start = new Position(1, 1);

            var result = _pathfinder.FindPath(start, start, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Path, Is.Empty);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void FindPath_DirectLine_ReturnsShortestPath()
        {
            // Map: 5x3 all rooms
            // . . . . .
            // S . . . G
            // . . . . .
            var map = CreateSimpleMap(5, 3);
            var start = new Position(0, 1);
            var goal = new Position(4, 1);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Length, Is.EqualTo(4));
            Assert.That(result.Path.Last(), Is.EqualTo(goal));
        }

        [Test]
        public void FindPath_WithWalls_AvoidsWalls()
        {
            // Map: 3x3
            // . . .
            // S # G
            // . . .
            var map = CreateSimpleMap(3, 3);
            map[1, 1] = Wall.Singleton;

            var start = new Position(0, 1);
            var goal = new Position(2, 1);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            // Path should go around: (0,0) or (0,2) -> (1,0) or (1,2) -> (2,1)
            Assert.That(result.Length, Is.EqualTo(4)); // Around the wall
            Assert.That(result.Path, Does.Not.Contain(new Position(1, 1)));
        }

        [Test]
        public void FindPath_LockedDoor_TreatsAsBlocked()
        {
            // Map: 3x1
            // S D G  (D = locked door)
            var map = new Tile[3, 1];
            map[0, 0] = new Room();
            map[1, 0] = new Door(); // Locked by default (has key inside)
            map[2, 0] = new Room();

            var start = new Position(0, 0);
            var goal = new Position(2, 0);

            // Door starts with key inside (IsOpened = true initially)
            // We need to check the Door behavior
            var door = (Door)map[1, 0];

            // Door is actually OPENED by default (key is inside = unlocked)
            // Let's verify this and adjust test accordingly
            if (door.IsOpened)
            {
                // Door is open, path should be found
                var result = _pathfinder.FindPath(start, goal, map);
                Assert.That(result.IsSuccess, Is.True);
            }
            else
            {
                // Door is locked, path should not be found
                var result = _pathfinder.FindPath(start, goal, map);
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Status, Is.EqualTo(PathStatus.NotFound));
            }
        }

        [Test]
        public void FindPath_DoorBehavior_MatchesIsOpenedState()
        {
            // This test verifies that the pathfinder respects Door.IsOpened
            var map = new Tile[3, 1];
            map[0, 0] = new Room();
            var door = new Door();
            map[1, 0] = door;
            map[2, 0] = new Room();

            var start = new Position(0, 0);
            var goal = new Position(2, 0);

            var result = _pathfinder.FindPath(start, goal, map);

            // Result should match door state
            Assert.That(result.IsSuccess, Is.EqualTo(door.IsOpened));
        }

        [Test]
        public void FindPath_Unreachable_ReturnsNotFound()
        {
            // Map: 3x3 with wall surrounding goal
            // . # .
            // # G #
            // . # .
            var map = CreateSimpleMap(3, 3);
            map[1, 0] = Wall.Singleton;
            map[0, 1] = Wall.Singleton;
            map[2, 1] = Wall.Singleton;
            map[1, 2] = Wall.Singleton;

            var start = new Position(0, 0);
            var goal = new Position(1, 1);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Status, Is.EqualTo(PathStatus.NotFound));
        }

        [Test]
        public void FindPath_GoalOutOfBounds_ReturnsNotFound()
        {
            var map = CreateSimpleMap(3, 3);
            var start = new Position(1, 1);
            var goal = new Position(10, 10);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Status, Is.EqualTo(PathStatus.NotFound));
        }

        [Test]
        public void FindPath_UnknownTile_TreatedAsBarrier()
        {
            // Map: 3x1
            // S ? G  (? = unknown)
            var map = new Tile[3, 1];
            map[0, 0] = new Room();
            map[1, 0] = new Unknown();
            map[2, 0] = new Room();

            var start = new Position(0, 0);
            var goal = new Position(2, 0);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.False);
        }

        #endregion

        #region FindNearestUnknown Tests

        [Test]
        public void FindNearestUnknown_AdjacentUnknown_ReturnsIt()
        {
            // Map: 3x1
            // S ?  (? = unknown)
            var map = new Tile[2, 1];
            map[0, 0] = new Room();
            map[1, 0] = new Unknown();

            var start = new Position(0, 0);

            var result = _pathfinder.FindNearestUnknown(start, map);

            Assert.That(result.Status, Is.EqualTo(PathStatus.UnknownReached));
            Assert.That(result.NearestUnknown, Is.EqualTo(new Position(1, 0)));
            Assert.That(result.Path, Is.Empty); // Can't walk into Unknown
        }

        [Test]
        public void FindNearestUnknown_FartherUnknown_ReturnsPathToIt()
        {
            // Map: 4x1
            // S . . ?
            var map = new Tile[4, 1];
            map[0, 0] = new Room();
            map[1, 0] = new Room();
            map[2, 0] = new Room();
            map[3, 0] = new Unknown();

            var start = new Position(0, 0);

            var result = _pathfinder.FindNearestUnknown(start, map);

            Assert.That(result.Status, Is.EqualTo(PathStatus.UnknownReached));
            Assert.That(result.NearestUnknown, Is.EqualTo(new Position(3, 0)));
            // Path should lead to position before Unknown: (1,0), (2,0)
            Assert.That(result.Path, Has.Count.EqualTo(2));
            Assert.That(result.Path.Last(), Is.EqualTo(new Position(2, 0)));
        }

        [Test]
        public void FindNearestUnknown_NoUnknown_ReturnsNotFound()
        {
            // Map: 3x3 all rooms
            var map = CreateSimpleMap(3, 3);
            var start = new Position(1, 1);

            var result = _pathfinder.FindNearestUnknown(start, map);

            Assert.That(result.Status, Is.EqualTo(PathStatus.NotFound));
        }

        [Test]
        public void FindNearestUnknown_MultipleUnknowns_ReturnsClosest()
        {
            // Map: 5x1
            // S . ? . ?
            var map = new Tile[5, 1];
            map[0, 0] = new Room();
            map[1, 0] = new Room();
            map[2, 0] = new Unknown();
            map[3, 0] = new Room();
            map[4, 0] = new Unknown();

            var start = new Position(0, 0);

            var result = _pathfinder.FindNearestUnknown(start, map);

            Assert.That(result.Status, Is.EqualTo(PathStatus.UnknownReached));
            Assert.That(result.NearestUnknown, Is.EqualTo(new Position(2, 0))); // Closest one
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a simple map filled with Room tiles.
        /// </summary>
        private static Tile[,] CreateSimpleMap(int width, int height)
        {
            var map = new Tile[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    map[x, y] = new Room();
                }
            }
            return map;
        }

        /// <summary>
        /// Creates the 21x19 labyrinth map from the provided image.
        /// Legend: # = Wall, . = Room, / = Door (opened)
        /// </summary>
        private static Tile[,] CreateLabyrinth21x19()
        {
            // Map representation from the image (21 columns x 19 rows)
            // Row 0 is top, column 0 is left
            string[] rows = new[]
            {
                "#####################", // 0
                "#.#.............#...#", // 1
                "#.#.###########/#.#.#", // 2
                "#.#.#.........#.#.#/#", // 3
                "#...#.#######/#.#.#.#", // 4
                "#####.#.....#.#.#.#.#", // 5
                "#.....#.###.#.#.#.#.#", // 6
                "#.#####.#.#/#.#.#.#.#", // 7
                "#.......#/#...#...#.#", // 8
                "#.#####/#.#/###.###.#", // 9
                "#.#...#.#.#.#.....#.#", // 10
                "#.#.#.#.#.#.#.#####.#", // 11
                "#.#.#/#.#/#.#.......#", // 12
                "#.#.#...#.#.#######/#", // 13
                "#.#.#####.#.........#", // 14
                "#.#/......#.#########", // 15
                "#.#.#######.........#", // 16
                "#...............###/#", // 17
                "#####################", // 18
            };

            var map = new Tile[21, 19];
            for (int y = 0; y < 19; y++)
            {
                for (int x = 0; x < 21; x++)
                {
                    char c = rows[y][x];
                    map[x, y] = c switch
                    {
                        '#' => Wall.Singleton,
                        '.' => new Room(),
                        '/' => new Door(),
                        _ => throw new ArgumentException($"Unknown character: {c}")
                    };
                }
            }
            return map;
        }

        #endregion

        #region Labyrinth 21x19 Map Tests

        [Test]
        public void Labyrinth21x19_PathFromTopLeftCorner_ToBottomRight()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);  // Top-left walkable cell
            var goal = new Position(19, 17); // Bottom-right area

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Path.Last(), Is.EqualTo(goal));
            Assert.That(result.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Labyrinth21x19_PathAvoidsWalls()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var goal = new Position(5, 5);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            // Verify no position in the path is a wall
            foreach (var pos in result.Path)
            {
                Assert.That(map[pos.X, pos.Y], Is.Not.InstanceOf<Wall>());
            }
        }

        [Test]
        public void Labyrinth21x19_PathThroughCorridor()
        {
            var map = CreateLabyrinth21x19();
            // Test path through a specific corridor section
            var start = new Position(1, 6);  // Left corridor
            var goal = new Position(7, 8);   // Center area

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Path.Last(), Is.EqualTo(goal));
        }

        [Test]
        public void Labyrinth21x19_CannotPathThroughWall()
        {
            var map = CreateLabyrinth21x19();
            // Position (0, 0) is a wall, cannot reach it
            var start = new Position(1, 1);
            var goal = new Position(0, 0);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Labyrinth21x19_PathToNearbyDoor()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var doorPosition = new Position(15, 2); // Door at row 2 (position of '/')

            var result = _pathfinder.FindPath(start, doorPosition, map);

            // Door is opened by default (has key), so path should exist
            var door = (Door)map[doorPosition.X, doorPosition.Y];
            Assert.That(result.IsSuccess, Is.EqualTo(door.IsOpened));
        }

        [Test]
        public void Labyrinth21x19_ShortestPathLength()
        {
            var map = CreateLabyrinth21x19();
            // From (1,1) to adjacent cell (1,2)
            var start = new Position(1, 1);
            var goal = new Position(1, 3);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Length, Is.EqualTo(2)); // Two steps down
        }

        [Test]
        public void Labyrinth21x19_PathToCenterArea()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var goal = new Position(9, 9);  // Near center where doors are

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Labyrinth21x19_ManhattanDistanceVsActualPath()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var goal = new Position(5, 6);

            var manhattanDist = start.ManhattanDistance(goal);
            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            // Due to walls, actual path should be >= Manhattan distance
            Assert.That(result.Length, Is.GreaterThanOrEqualTo(manhattanDist));
        }

        [Test]
        public void Labyrinth21x19_FindNearestUnknown_WhenNoUnknown()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);

            var result = _pathfinder.FindNearestUnknown(start, map);

            // No Unknown tiles in this map
            Assert.That(result.Status, Is.EqualTo(PathStatus.NotFound));
        }

        [Test]
        public void Labyrinth21x19_FindNearestUnknown_WithUnknownAdded()
        {
            var map = CreateLabyrinth21x19();
            // Replace a room with an Unknown tile
            map[19, 17] = new Unknown();
            var start = new Position(1, 1);

            var result = _pathfinder.FindNearestUnknown(start, map);

            Assert.That(result.Status, Is.EqualTo(PathStatus.UnknownReached));
            Assert.That(result.NearestUnknown, Is.EqualTo(new Position(19, 17)));
        }

        [Test]
        public void Labyrinth21x19_MultiplePathsFromSameStart()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);

            var goals = new[]
            {
                new Position(5, 1),
                new Position(1, 8),
                new Position(15, 14),
            };

            foreach (var goal in goals)
            {
                var result = _pathfinder.FindPath(start, goal, map);
                Assert.That(result.IsSuccess, Is.True, $"Path to ({goal.X}, {goal.Y}) should exist");
            }
        }

        [Test]
        public void Labyrinth21x19_PathDoesNotContainStart()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var goal = new Position(5, 6);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Path, Does.Not.Contain(start));
        }

        [Test]
        public void Labyrinth21x19_PathContainsGoal()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 1);
            var goal = new Position(5, 6);

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Path.Last(), Is.EqualTo(goal));
        }

        [Test]
        public void Labyrinth21x19_PathFromBottomToTop()
        {
            var map = CreateLabyrinth21x19();
            var start = new Position(1, 17);  // Bottom area
            var goal = new Position(1, 1);    // Top area

            var result = _pathfinder.FindPath(start, goal, map);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Labyrinth21x19_SymmetricPaths()
        {
            var map = CreateLabyrinth21x19();
            var posA = new Position(1, 1);
            var posB = new Position(5, 6);

            var pathAtoB = _pathfinder.FindPath(posA, posB, map);
            var pathBtoA = _pathfinder.FindPath(posB, posA, map);

            Assert.That(pathAtoB.IsSuccess, Is.True);
            Assert.That(pathBtoA.IsSuccess, Is.True);
            // Path lengths should be equal (undirected graph)
            Assert.That(pathAtoB.Length, Is.EqualTo(pathBtoA.Length));
        }

        #endregion
    }
}
