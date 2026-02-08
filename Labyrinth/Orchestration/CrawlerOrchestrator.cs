using Labyrinth.Crawl;
using Labyrinth.Exploration;
using Labyrinth.Items;
using Labyrinth.Sys;
using Labyrinth.Tiles;

namespace Labyrinth.Orchestration;

/// <summary>
/// Orchestrates multiple crawlers exploring a labyrinth concurrently.
/// Implements "first to exit wins" strategy - when one crawler finds the exit, all others stop.
/// </summary>
public class CrawlerOrchestrator
{
    /// <summary>
    /// Result of a crawler's exploration attempt.
    /// </summary>
    /// <param name="CrawlerId">Unique identifier for the crawler</param>
    /// <param name="Crawler">The crawler instance</param>
    /// <param name="FoundExit">Whether the crawler found the exit</param>
    /// <param name="StepsRemaining">Number of steps remaining when exploration ended</param>
    /// <param name="WasCancelled">Whether the crawler was cancelled by another crawler winning</param>
    public record CrawlerResult(
        int CrawlerId,
        ICrawler Crawler,
        bool FoundExit,
        int StepsRemaining,
        bool WasCancelled
    );

    /// <summary>
    /// Event arguments for crawler lifecycle events.
    /// </summary>
    public class CrawlerEventArgs : EventArgs
    {
        public int CrawlerId { get; }
        public ICrawler Crawler { get; }

        public CrawlerEventArgs(int crawlerId, ICrawler crawler)
        {
            CrawlerId = crawlerId;
            Crawler = crawler;
        }
    }

    /// <summary>
    /// Fired when a crawler starts exploring.
    /// </summary>
    public event EventHandler<CrawlerEventArgs>? CrawlerStarted;

    /// <summary>
    /// Fired when a crawler finishes exploring (exit found, cancelled, or max steps reached).
    /// </summary>
    public event EventHandler<CrawlerResult>? CrawlerFinished;

    private readonly int _maxCrawlers;

    /// <summary>
    /// Creates a new orchestrator with a maximum number of crawlers.
    /// </summary>
    /// <param name="maxCrawlers">Maximum number of crawlers allowed (1-3)</param>
    public CrawlerOrchestrator(int maxCrawlers = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCrawlers, 1, nameof(maxCrawlers));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxCrawlers, 3, nameof(maxCrawlers));
        _maxCrawlers = maxCrawlers;
    }

    /// <summary>
    /// Runs multiple crawlers concurrently until one finds the exit or all exhaust their steps.
    /// </summary>
    /// <param name="crawlerFactory">Factory function to create crawler and its inventory</param>
    /// <param name="explorerFactory">Factory function to create an explorer for a crawler</param>
    /// <param name="crawlerCount">Number of crawlers to run (1 to maxCrawlers)</param>
    /// <param name="maxSteps">Maximum steps each crawler can take</param>
    /// <param name="externalCt">External cancellation token for stopping all crawlers</param>
    /// <returns>The result of the winning crawler, or null if none found the exit</returns>
    public async Task<CrawlerResult?> RunAsync(
        Func<Task<(ICrawler Crawler, Inventory Bag)>> crawlerFactory,
        Func<ICrawler, IExplorer> explorerFactory,
        int crawlerCount,
        int maxSteps,
        CancellationToken externalCt = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(crawlerCount, 1, nameof(crawlerCount));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(crawlerCount, _maxCrawlers, nameof(crawlerCount));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxSteps, 0, nameof(maxSteps));

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var tasks = new List<Task<CrawlerResult>>();

        // Create and start all crawlers
        for (int i = 0; i < crawlerCount; i++)
        {
            var crawlerId = i + 1;
            var (crawler, bag) = await crawlerFactory();
            var explorer = explorerFactory(crawler);

            CrawlerStarted?.Invoke(this, new CrawlerEventArgs(crawlerId, crawler));

            tasks.Add(RunSingleCrawlerAsync(
                crawlerId,
                crawler,
                explorer,
                bag,
                maxSteps,
                linkedCts.Token
            ));
        }

        CrawlerResult? winner = null;

        // Wait for first crawler to find exit (or all to complete)
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            try
            {
                var result = await completedTask;
                CrawlerFinished?.Invoke(this, result);

                if (result.FoundExit && winner is null)
                {
                    winner = result;
                    // Cancel all other crawlers
                    await linkedCts.CancelAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
        }

        return winner;
    }

    private async Task<CrawlerResult> RunSingleCrawlerAsync(
        int crawlerId,
        ICrawler crawler,
        IExplorer explorer,
        Inventory bag,
        int maxSteps,
        CancellationToken ct)
    {
        bool foundExit = false;
        int stepsRemaining = maxSteps;
        bool wasCancelled = false;

        try
        {
            stepsRemaining = await explorer.GetOut(maxSteps, bag, ct);
            foundExit = await crawler.FacingTileType == typeof(Outside);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }

        return new CrawlerResult(crawlerId, crawler, foundExit, stepsRemaining, wasCancelled);
    }
}
