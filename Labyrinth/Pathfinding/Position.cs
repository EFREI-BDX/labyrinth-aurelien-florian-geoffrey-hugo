using Labyrinth.Crawl;

namespace Labyrinth.Pathfinding
{
    /// <summary>
    /// Represents a 2D position in the labyrinth grid.
    /// </summary>
    public readonly record struct Position(int X, int Y)
    {
        /// <summary>
        /// Creates a new position by moving in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        /// <returns>A new position offset by the direction's delta.</returns>
        public Position Move(Direction direction) => 
            new(X + direction.DeltaX, Y + direction.DeltaY);

        /// <summary>
        /// Calculates the Manhattan distance to another position.
        /// </summary>
        /// <param name="other">The target position.</param>
        /// <returns>The sum of absolute differences in X and Y coordinates.</returns>
        public int ManhattanDistance(Position other) => 
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>
        /// Gets all four cardinal directions.
        /// </summary>
        public static IEnumerable<Direction> AllDirections =>
            [Direction.North, Direction.East, Direction.South, Direction.West];

        /// <summary>
        /// Gets all neighboring positions (in all four cardinal directions).
        /// </summary>
        public IEnumerable<Position> Neighbors =>
            AllDirections.Select(Move);

        public override string ToString() => $"({X}, {Y})";
    }
}
