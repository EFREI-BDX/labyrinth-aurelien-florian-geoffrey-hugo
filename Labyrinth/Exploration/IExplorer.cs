using Labyrinth.Crawl;
using Labyrinth.Items;

namespace Labyrinth.Exploration;

/// <summary>
/// Interface for labyrinth exploration strategies.
/// </summary>
public interface IExplorer
{
    /// <summary>
    /// Gets the crawler being controlled by this explorer.
    /// </summary>
    ICrawler Crawler { get; }

    /// <summary>
    /// Explores the labyrinth trying to find the exit.
    /// </summary>
    /// <param name="maxSteps">Maximum number of steps (turns + moves) allowed.</param>
    /// <param name="bag">The inventory to use for collecting items and opening doors.</param>
    /// <param name="ct">Cancellation token for stopping exploration.</param>
    /// <returns>The number of steps remaining when exploration ended.</returns>
    Task<int> GetOut(int maxSteps, Inventory? bag = null, CancellationToken ct = default);

    /// <summary>
    /// Fired when the crawler moves to a new position.
    /// </summary>
    event EventHandler<CrawlingEventArgs>? PositionChanged;

    /// <summary>
    /// Fired when the crawler changes direction.
    /// </summary>
    event EventHandler<CrawlingEventArgs>? DirectionChanged;
}
