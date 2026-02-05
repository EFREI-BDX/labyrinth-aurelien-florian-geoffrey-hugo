using Labyrinth.Tiles;

namespace Labyrinth.Pathfinding
{
    /// <summary>
    /// Interface for pathfinding algorithms in the labyrinth.
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// Finds the shortest path from start to goal.
        /// </summary>
        /// <param name="start">The starting position.</param>
        /// <param name="goal">The target position.</param>
        /// <param name="map">The tile map of the labyrinth.</param>
        /// <returns>A PathResult containing the path and status.</returns>
        /// <remarks>
        /// - Walls and locked doors are treated as obstacles.
        /// - Unknown tiles are treated as frontiers (search stops at them).
        /// - The returned path excludes the start position but includes the goal.
        /// </remarks>
        PathResult FindPath(Position start, Position goal, Tile[,] map);

        /// <summary>
        /// Finds the nearest unknown tile from the start position.
        /// Useful for exploration strategies.
        /// </summary>
        /// <param name="start">The starting position.</param>
        /// <param name="map">The tile map of the labyrinth.</param>
        /// <returns>A PathResult with the path to the nearest unknown tile.</returns>
        PathResult FindNearestUnknown(Position start, Tile[,] map);
    }
}
