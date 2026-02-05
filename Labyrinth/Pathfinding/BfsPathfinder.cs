using Labyrinth.Tiles;

namespace Labyrinth.Pathfinding
{
    /// <summary>
    /// Breadth-First Search pathfinder implementation.
    /// Guarantees the shortest path in an unweighted graph.
    /// </summary>
    public class BfsPathfinder : IPathfinder
    {
        /// <inheritdoc />
        public PathResult FindPath(Position start, Position goal, Tile[,] map)
        {
            // Same position - no movement needed
            if (start == goal)
            {
                return PathResult.Success(Array.Empty<Position>());
            }

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Validate bounds
            if (!IsInBounds(goal, width, height))
            {
                return PathResult.NotFound();
            }

            var visited = new HashSet<Position> { start };
            var queue = new Queue<Position>();
            var cameFrom = new Dictionary<Position, Position>();

            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.Neighbors)
                {
                    if (visited.Contains(neighbor))
                        continue;

                    if (!IsInBounds(neighbor, width, height))
                        continue;

                    var tile = map[neighbor.X, neighbor.Y];

                    if (tile is Unknown)
                    {
                        // For FindPath, we treat Unknown as a barrier
                        continue;
                    }

                    // Check if the tile is traversable
                    if (!IsTraversable(tile))
                        continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;

                    // Check if we reached the goal
                    if (neighbor == goal)
                    {
                        return PathResult.Success(ReconstructPath(cameFrom, start, goal));
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return PathResult.NotFound();
        }

        /// <inheritdoc />
        public PathResult FindNearestUnknown(Position start, Tile[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            var visited = new HashSet<Position> { start };
            var queue = new Queue<Position>();
            var cameFrom = new Dictionary<Position, Position>();

            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.Neighbors)
                {
                    if (visited.Contains(neighbor))
                        continue;

                    if (!IsInBounds(neighbor, width, height))
                        continue;

                    var tile = map[neighbor.X, neighbor.Y];

                    if (tile is Unknown)
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        var path = ReconstructPath(cameFrom, start, neighbor);

                        // Remove the Unknown tile from the path
                        var pathToFrontier = path.Count > 0
                            ? path.Take(path.Count - 1).ToList()
                            : new List<Position>();

                        return PathResult.UnknownFound(pathToFrontier, neighbor);
                    }

                    // Check if the tile is traversable
                    if (!IsTraversable(tile))
                        continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            // Map is fully explored
            return PathResult.NotFound();
        }

        /// <summary>
        /// Checks if a position is within the map bounds.
        /// </summary>
        private static bool IsInBounds(Position pos, int width, int height) =>
            pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height;

        /// <summary>
        /// Checks if a tile can be traversed.
        /// </summary>
        private static bool IsTraversable(Tile tile) => tile switch
        {
            Wall => false,
            Outside => false,
            Door door => door.IsOpened,
            Room => true,
            _ => false
        };

        /// <summary>
        /// Reconstructs the path from start to goal using the cameFrom dictionary.
        /// </summary>
        private static List<Position> ReconstructPath(
            Dictionary<Position, Position> cameFrom,
            Position start,
            Position goal)
        {
            var path = new List<Position>();
            var current = goal;

            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path;
        }
    }
}
