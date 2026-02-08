using Labyrinth;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Orchestration;
using Labyrinth.Sys;
using Labyrinth.Tiles;
using Moq;
using static Labyrinth.RandExplorer;

namespace LabyrinthTest.Orchestration;

public class CrawlerOrchestratorTests
{
    private Labyrinth.Labyrinth CreateLabyrinth(string ascii) =>
        new Labyrinth.Labyrinth(new AsciiParser(ascii));

    private RandExplorer CreateExplorer(ICrawler crawler, params Actions[] actions)
    {
        var mockRnd = new Mock<IEnumRandomizer<Actions>>();
        mockRnd.Setup(r => r.Next()).Returns(new Queue<Actions>(actions).Dequeue);
        return new RandExplorer(crawler, mockRnd.Object);
    }

    // Open labyrinth with exit on top (same line lengths)
    private const string OpenLabyrinth = """
        | x |
        |   |
        +---+
        """;

    // Closed labyrinth (no exit)
    private const string ClosedLabyrinth = """
        +---+
        | x |
        +---+
        """;

    [Test]
    public void Constructor_InvalidMaxCrawlers_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlerOrchestrator(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlerOrchestrator(4));
    }

    [Test]
    public void Constructor_ValidMaxCrawlers_Succeeds()
    {
        Assert.DoesNotThrow(() => new CrawlerOrchestrator(1));
        Assert.DoesNotThrow(() => new CrawlerOrchestrator(2));
        Assert.DoesNotThrow(() => new CrawlerOrchestrator(3));
    }

    [Test]
    public async Task RunAsync_InvalidCrawlerCount_ThrowsException()
    {
        var orchestrator = new CrawlerOrchestrator(3);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        await Task.CompletedTask;

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.RunAsync(
                () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
                c => CreateExplorer(c),
                crawlerCount: 0,
                maxSteps: 10
            ));

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.RunAsync(
                () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
                c => CreateExplorer(c),
                crawlerCount: 4,
                maxSteps: 10
            ));
    }

    [Test]
    public async Task RunAsync_InvalidMaxSteps_ThrowsException()
    {
        var orchestrator = new CrawlerOrchestrator(3);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await orchestrator.RunAsync(
                () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
                c => CreateExplorer(c),
                crawlerCount: 1,
                maxSteps: 0
            ));
    }

    [Test]
    public async Task RunAsync_SingleCrawler_FindsExit()
    {
        var orchestrator = new CrawlerOrchestrator(1);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        var result = await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, Actions.Walk),
            crawlerCount: 1,
            maxSteps: 10
        );

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FoundExit, Is.True);
        Assert.That(result.CrawlerId, Is.EqualTo(1));
        Assert.That(result.WasCancelled, Is.False);
    }

    [Test]
    public async Task RunAsync_SingleCrawler_ExhaustsSteps()
    {
        var orchestrator = new CrawlerOrchestrator(1);
        var labyrinth = CreateLabyrinth(ClosedLabyrinth);

        var result = await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, Actions.TurnLeft, Actions.TurnLeft, Actions.TurnLeft),
            crawlerCount: 1,
            maxSteps: 3
        );

        // No exit found, result is null
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RunAsync_MultipleCrawlers_FirstWinnerCancelsOthers()
    {
        var orchestrator = new CrawlerOrchestrator(2);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        var finishedCrawlers = new List<CrawlerOrchestrator.CrawlerResult>();
        orchestrator.CrawlerFinished += (s, result) => finishedCrawlers.Add(result);

        var result = await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, Actions.Walk),
            crawlerCount: 2,
            maxSteps: 10
        );

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FoundExit, Is.True);
        Assert.That(finishedCrawlers.Count, Is.EqualTo(2));
        
        // At least one should be cancelled or both should find exit (race condition)
        var cancelled = finishedCrawlers.Count(r => r.WasCancelled);
        var found = finishedCrawlers.Count(r => r.FoundExit);
        Assert.That(cancelled + found, Is.EqualTo(2));
    }

    [Test]
    public async Task RunAsync_CrawlerStartedEvent_Fires()
    {
        var orchestrator = new CrawlerOrchestrator(2);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        var startedCrawlers = new List<int>();
        orchestrator.CrawlerStarted += (s, e) => startedCrawlers.Add(e.CrawlerId);

        await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, Actions.Walk),
            crawlerCount: 2,
            maxSteps: 10
        );

        Assert.That(startedCrawlers, Has.Count.EqualTo(2));
        Assert.That(startedCrawlers, Does.Contain(1));
        Assert.That(startedCrawlers, Does.Contain(2));
    }

    [Test]
    public async Task RunAsync_ExternalCancellation_StopsAllCrawlers()
    {
        var orchestrator = new CrawlerOrchestrator(2);
        var labyrinth = CreateLabyrinth(ClosedLabyrinth);

        using var cts = new CancellationTokenSource();
        var finishedCrawlers = new List<CrawlerOrchestrator.CrawlerResult>();
        orchestrator.CrawlerFinished += (s, result) => finishedCrawlers.Add(result);

        // Cancel immediately
        cts.Cancel();

        var result = await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, 
                Actions.TurnLeft, Actions.TurnLeft, Actions.TurnLeft, Actions.TurnLeft,
                Actions.TurnLeft, Actions.TurnLeft, Actions.TurnLeft, Actions.TurnLeft
            ),
            crawlerCount: 2,
            maxSteps: 1000,
            externalCt: cts.Token
        );

        // No winner since cancelled
        Assert.That(result, Is.Null);
        // All crawlers should have been cancelled or not found exit
        Assert.That(finishedCrawlers.All(r => r.WasCancelled || !r.FoundExit), Is.True);
    }

    [Test]
    public async Task RunAsync_ThreeCrawlers_AllStart()
    {
        var orchestrator = new CrawlerOrchestrator(3);
        var labyrinth = CreateLabyrinth(OpenLabyrinth);

        var startedCount = 0;
        orchestrator.CrawlerStarted += (s, e) => Interlocked.Increment(ref startedCount);

        await orchestrator.RunAsync(
            () => Task.FromResult<(ICrawler, Inventory)>((labyrinth.NewCrawler(), new MyInventory())),
            c => CreateExplorer(c, Actions.Walk),
            crawlerCount: 3,
            maxSteps: 10
        );

        Assert.That(startedCount, Is.EqualTo(3));
    }
}
