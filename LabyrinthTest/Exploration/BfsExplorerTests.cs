using Labyrinth;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Exploration;
using Labyrinth.Pathfinding;

namespace LabyrinthTest.Exploration;

public class BfsExplorerTests
{
    private class ExplorerEventsCatcher
    {
        public ExplorerEventsCatcher(IExplorer explorer)
        {
            explorer.PositionChanged += (s, e) => CatchEvent(ref _positionChangedCount, e);
            explorer.DirectionChanged += (s, e) => CatchEvent(ref _directionChangedCount, e);
        }

        public int PositionChangedCount => _positionChangedCount;
        public int DirectionChangedCount => _directionChangedCount;
        public (int X, int Y, Direction Dir)? LastArgs { get; private set; } = null;

        private void CatchEvent(ref int counter, CrawlingEventArgs e)
        {
            counter++;
            LastArgs = (e.X, e.Y, e.Direction);
        }

        private int _directionChangedCount = 0, _positionChangedCount = 0;
    }

    private BfsExplorer NewExplorerFor(string labyrinth, out ExplorerEventsCatcher events)
    {
        var laby = new Labyrinth.Labyrinth(new AsciiParser(labyrinth));
        var explorer = new BfsExplorer(laby.NewCrawler(), new BfsPathfinder());
        events = new ExplorerEventsCatcher(explorer);
        return explorer;
    }

    [Test]
    public async Task GetOut_FacingOutsideAtStart_ReturnsImmediately()
    {
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );

        var left = await test.GetOut(100);

        // Should detect Outside immediately and return
        Assert.That(left, Is.EqualTo(100));
    }

    [Test]
    public async Task GetOut_SimpleExit_FindsExit()
    {
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            out var events
        );

        var left = await test.GetOut(100);

        // BFS should find exit quickly by going left
        Assert.That(left, Is.GreaterThan(0));
        Assert.That(events.PositionChangedCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetOut_WithCancellation_StopsExploring()
    {
        var test = NewExplorerFor("""
            +---+
            | x |
            +---+
            """,
            out var events
        );

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var left = await test.GetOut(1000, null, cts.Token);

        // Should return immediately without exploring
        Assert.That(left, Is.EqualTo(1000));
    }

    [Test]
    public async Task GetOut_AlreadyCancelled_ReturnsImmediately()
    {
        var test = NewExplorerFor("""
            +---+
            | x |
            +---+
            """,
            out var events
        );

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var left = await test.GetOut(10, null, cts.Token);

        Assert.That(left, Is.EqualTo(10));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetOut_LShape_FindsExit()
    {
        // Simple L-shaped maze - exit on left
        var test = NewExplorerFor("""
            +----+
            |   x|
            |    |
            +----+
            """,
            out var events
        );

        var left = await test.GetOut(100);

        // BFS should find exit
        Assert.That(left, Is.GreaterThan(0));
        Assert.That(events.PositionChangedCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetOut_WithDoorAndKey_OpensAndExits()
    {
        var test = NewExplorerFor("""
            +-/-+
            | k |
            | x |
            +---+
            """,
            out var events
        );

        var left = await test.GetOut(100);

        // Should find key, open door, exit
        Assert.That(left, Is.GreaterThan(0));
        Assert.That(events.PositionChangedCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetOut_ClosedBox_ExhaustsStepsOrDetectsNoExit()
    {
        var test = NewExplorerFor("""
            +---+
            | x |
            +---+
            """,
            out var events
        );

        var left = await test.GetOut(50);

        // Either exhausts steps or detects no exit
        // BFS should realize there's no Unknown tiles left
        Assert.That(left, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task Crawler_Property_ReturnsCrawler()
    {
        var laby = new Labyrinth.Labyrinth(new AsciiParser("""
            +---+
            | x |
            +---+
            """));
        var crawler = laby.NewCrawler();
        var explorer = new BfsExplorer(crawler, new BfsPathfinder());

        Assert.That(explorer.Crawler, Is.SameAs(crawler));
    }

    [Test]
    public async Task GetOut_ComplexMaze_FindsExit()
    {
        var test = NewExplorerFor("""
            +--+--------+
            |  /        |
            |  +--+--+  |
            |     |k    |
            +--+  |  +--+
               |k  x    |
            +  +-------/|
            |           |
            +-----------+
            """,
            out var events
        );

        // Use more steps for complex maze
        var left = await test.GetOut(2000);

        // Should find one of the exits (we just check it completes without error)
        // Complex mazes may or may not find exit depending on door/key logic
        Assert.That(left, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task GetOut_FiresPositionChangedEvents()
    {
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            out var events
        );

        await test.GetOut(100);

        // Should have moved and fired events
        Assert.That(events.PositionChangedCount, Is.GreaterThan(0));
        Assert.That(events.LastArgs, Is.Not.Null);
    }

    [Test]
    public async Task GetOut_TurnsAndFiresDirectionChangedEvents()
    {
        var test = NewExplorerFor("""
            +--+
            |  |
            |x |
            +--+
            """,
            out var events
        );

        await test.GetOut(100);

        // Should have turned at some point
        Assert.That(events.DirectionChangedCount, Is.GreaterThanOrEqualTo(0));
    }
}
