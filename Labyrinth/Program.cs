using Labyrinth;
using Labyrinth.ApiClient;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Orchestration;
using Labyrinth.Tiles;
using Labyrinth.Sys;
using Dto = ApiTypes;
using System.Text.Json;

// Parse command line arguments
var config = ParseArguments(args);

// Crawler colors for multi-crawler display
var crawlerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Magenta };
var consoleLock = new object();

const int OffsetY = 2;
const int MaxSteps = 3000;

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

var TileToChar = new Dictionary<Type, char>
{
    [typeof(Room)] = ' ',
    [typeof(Wall)] = '#',
    [typeof(Door)] = '/'
};

// Track each crawler's previous position for clearing
var crawlerPrevPositions = new Dictionary<int, (int X, int Y)>();

void DrawExplorer(int crawlerId, object? sender, CrawlingEventArgs e)
{
    var crawler = ((RandExplorer)sender!).Crawler;
    var facingTileType = crawler.FacingTileType.Result;
    var color = crawlerColors[(crawlerId - 1) % crawlerColors.Length];

    lock (consoleLock)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;

        if (facingTileType != typeof(Outside) && TileToChar.ContainsKey(facingTileType))
        {
            Console.SetCursorPosition(
                e.X + e.Direction.DeltaX,
                e.Y + e.Direction.DeltaY + OffsetY
            );
            Console.Write(TileToChar[facingTileType]);
        }
        Console.SetCursorPosition(e.X, e.Y + OffsetY);
        Console.Write(DirToChar(e.Direction));
        Console.SetCursorPosition(0, crawlerId - 1);

        if (crawler is ClientCrawler cc)
        {
            Console.Write($"Crawler {crawlerId}: Bag = {cc.Bag.ItemTypes.Count()} item(s)    ");
        }
        else
        {
            Console.Write($"Crawler {crawlerId}: exploring...    ");
        }

        Console.ForegroundColor = originalColor;
        Thread.Sleep(50);
    }
}

void OnPositionChanged(int crawlerId, object? sender, CrawlingEventArgs e)
{
    lock (consoleLock)
    {
        if (crawlerPrevPositions.TryGetValue(crawlerId, out var prev))
        {
            Console.SetCursorPosition(prev.X, prev.Y);
            Console.Write(' ');
        }
        DrawExplorer(crawlerId, sender, e);
        crawlerPrevPositions[crawlerId] = (e.X, e.Y + OffsetY);
    }
}

Labyrinth.Labyrinth labyrinth;
ContestSession? contest = null;
int crawlerIndex = 0;

if (config.ServerUrl is null)
{
    // Local mode
    Console.WriteLine($"Local mode - {config.CrawlerCount} crawler(s), {MaxSteps} max steps");
    Console.WriteLine("Usage: <serverUrl> <appKey> [settings.json] [--crawlers <1-3>]");

    labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
        +--+--------+
        |  /        |
        |  +--+--+  |
        |     |k    |
        +--+  |  +--+
           |k  x    |
        +  +-------/|
        |           |
        +-----------+
        """));

    var orchestrator = new CrawlerOrchestrator(config.CrawlerCount);

    orchestrator.CrawlerStarted += (s, e) =>
    {
        lock (consoleLock)
        {
            var color = crawlerColors[(e.CrawlerId - 1) % crawlerColors.Length];
            Console.ForegroundColor = color;
            Console.SetCursorPosition(0, e.CrawlerId - 1);
            Console.Write($"Crawler {e.CrawlerId}: started at ({e.Crawler.X}, {e.Crawler.Y})    ");
            Console.ResetColor();
        }
    };

    orchestrator.CrawlerFinished += (s, result) =>
    {
        lock (consoleLock)
        {
            var color = crawlerColors[(result.CrawlerId - 1) % crawlerColors.Length];
            Console.ForegroundColor = color;
            Console.SetCursorPosition(0, result.CrawlerId - 1);
            var status = result.FoundExit ? "FOUND EXIT!" :
                         result.WasCancelled ? "cancelled" : "exhausted";
            Console.Write($"Crawler {result.CrawlerId}: {status} (steps left: {result.StepsRemaining})    ");
            Console.ResetColor();
        }
    };

    Console.Clear();
    Console.SetCursorPosition(0, OffsetY);
    Console.WriteLine(labyrinth);

    var winner = await orchestrator.RunAsync(
        crawlerFactory: () =>
        {
            var crawler = labyrinth.NewCrawler();
            return Task.FromResult<(ICrawler, Inventory)>((crawler, new MyInventory()));
        },
        explorerFactory: crawler =>
        {
            var id = Interlocked.Increment(ref crawlerIndex);
            var explorer = new RandExplorer(crawler, new BasicEnumRandomizer<RandExplorer.Actions>());
            explorer.DirectionChanged += (s, e) => DrawExplorer(id, s, e);
            explorer.PositionChanged += (s, e) => OnPositionChanged(id, s, e);
            crawlerPrevPositions[id] = (crawler.X, crawler.Y + OffsetY);
            return explorer;
        },
        crawlerCount: config.CrawlerCount,
        maxSteps: MaxSteps
    );

    Console.SetCursorPosition(0, OffsetY + labyrinth.Height + 2);
    if (winner is not null)
    {
        Console.ForegroundColor = crawlerColors[(winner.CrawlerId - 1) % crawlerColors.Length];
        Console.WriteLine($"Crawler {winner.CrawlerId} won! Found exit with {winner.StepsRemaining} steps remaining.");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("No crawler found the exit.");
    }
}
else
{
    // Contest mode
    Dto.Settings? settings = null;

    if (config.SettingsPath is not null)
    {
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(config.SettingsPath));
    }

    contest = await ContestSession.Open(config.ServerUrl, config.AppKey!.Value, settings);
    labyrinth = new(contest.Builder);

    var orchestrator = new CrawlerOrchestrator(config.CrawlerCount);

    orchestrator.CrawlerStarted += (s, e) =>
    {
        lock (consoleLock)
        {
            var color = crawlerColors[(e.CrawlerId - 1) % crawlerColors.Length];
            Console.ForegroundColor = color;
            Console.SetCursorPosition(0, e.CrawlerId - 1);
            Console.Write($"Crawler {e.CrawlerId}: started at ({e.Crawler.X}, {e.Crawler.Y})    ");
            Console.ResetColor();
        }
    };

    orchestrator.CrawlerFinished += (s, result) =>
    {
        lock (consoleLock)
        {
            var color = crawlerColors[(result.CrawlerId - 1) % crawlerColors.Length];
            Console.ForegroundColor = color;
            Console.SetCursorPosition(0, result.CrawlerId - 1);
            var status = result.FoundExit ? "FOUND EXIT!" :
                         result.WasCancelled ? "cancelled" : "exhausted";
            Console.Write($"Crawler {result.CrawlerId}: {status} (steps left: {result.StepsRemaining})    ");
            Console.ResetColor();
        }
    };

    Console.Clear();
    Console.SetCursorPosition(0, OffsetY);
    Console.WriteLine(labyrinth);

    var bagIndex = 0;
    var winner = await orchestrator.RunAsync(
        crawlerFactory: async () =>
        {
            var crawler = await contest.NewCrawler();
            var bag = contest.Bags.ElementAt(bagIndex++);
            return (crawler, bag);
        },
        explorerFactory: crawler =>
        {
            var id = Interlocked.Increment(ref crawlerIndex);
            var explorer = new RandExplorer(crawler, new BasicEnumRandomizer<RandExplorer.Actions>());
            explorer.DirectionChanged += (s, e) => DrawExplorer(id, s, e);
            explorer.PositionChanged += (s, e) => OnPositionChanged(id, s, e);
            crawlerPrevPositions[id] = (crawler.X, crawler.Y + OffsetY);
            return explorer;
        },
        crawlerCount: config.CrawlerCount,
        maxSteps: MaxSteps
    );

    Console.SetCursorPosition(0, OffsetY + 20);
    if (winner is not null)
    {
        Console.ForegroundColor = crawlerColors[(winner.CrawlerId - 1) % crawlerColors.Length];
        Console.WriteLine($"Crawler {winner.CrawlerId} won! Found exit with {winner.StepsRemaining} steps remaining.");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("No crawler found the exit.");
    }

    await contest.Close();
}

// Argument parsing helper
static AppConfig ParseArguments(string[] args)
{
    var config = new AppConfig();

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--crawlers" && i + 1 < args.Length)
        {
            if (int.TryParse(args[i + 1], out var count) && count >= 1 && count <= 3)
            {
                config.CrawlerCount = count;
            }
            else
            {
                Console.WriteLine("Warning: --crawlers must be 1-3. Using default (1).");
            }
            i++; // Skip the value
        }
        else if (config.ServerUrl is null && Uri.TryCreate(args[i], UriKind.Absolute, out var uri))
        {
            config.ServerUrl = uri;
        }
        else if (config.ServerUrl is not null && config.AppKey is null && Guid.TryParse(args[i], out var guid))
        {
            config.AppKey = guid;
        }
        else if (config.ServerUrl is not null && config.AppKey is not null && config.SettingsPath is null && !args[i].StartsWith("--"))
        {
            config.SettingsPath = args[i];
        }
    }

    return config;
}

record AppConfig
{
    public Uri? ServerUrl { get; set; }
    public Guid? AppKey { get; set; }
    public string? SettingsPath { get; set; }
    public int CrawlerCount { get; set; } = 1;
}
