namespace Labyrinth.Pathfinding
{
    /// <summary>
    /// Status of the pathfinding operation.
    /// </summary>
    public enum PathStatus
    {
        /// <summary>
        /// A valid path to the goal was found.
        /// </summary>
        Found,

        /// <summary>
        /// No path exists to the goal (blocked by walls/doors).
        /// </summary>
        NotFound,

        /// <summary>
        /// The search reached an unknown tile (frontier for exploration).
        /// </summary>
        UnknownReached
    }

    /// <summary>
    /// Result of a pathfinding operation.
    /// </summary>
    public class PathResult
    {
        /// <summary>
        /// The status of the pathfinding operation.
        /// </summary>
        public required PathStatus Status { get; init; }

        /// <summary>
        /// The path from start to goal (excluding start, including goal).
        /// Empty if no path was found.
        /// </summary>
        public required IReadOnlyList<Position> Path { get; init; }

        /// <summary>
        /// The position of the nearest unknown tile found during search.
        /// Only set when exploring for frontiers.
        /// </summary>
        public Position? NearestUnknown { get; init; }

        /// <summary>
        /// True if a valid path was found.
        /// </summary>
        public bool IsSuccess => Status == PathStatus.Found;

        /// <summary>
        /// The number of steps in the path.
        /// </summary>
        public int Length => Path.Count;

        /// <summary>
        /// Creates a successful path result.
        /// </summary>
        public static PathResult Success(IReadOnlyList<Position> path) =>
            new() { Status = PathStatus.Found, Path = path };

        /// <summary>
        /// Creates a not found result.
        /// </summary>
        public static PathResult NotFound() =>
            new() { Status = PathStatus.NotFound, Path = Array.Empty<Position>() };

        /// <summary>
        /// Creates a result indicating an unknown tile was reached.
        /// </summary>
        public static PathResult UnknownFound(IReadOnlyList<Position> pathToUnknown, Position unknownPosition) =>
            new()
            {
                Status = PathStatus.UnknownReached,
                Path = pathToUnknown,
                NearestUnknown = unknownPosition
            };
    }
}
