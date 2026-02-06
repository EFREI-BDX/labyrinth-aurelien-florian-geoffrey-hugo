using ApiTypes;
using Labyrinth.TrainingServer.Models;
using Labyrinth.TrainingServer.Services;

namespace LabyrinthTest.TrainingServer;

[TestFixture]
public class LabyrinthServiceTests
{
    private LabyrinthService _service = null!;
    private Guid _appKey;

    [SetUp]
    public void SetUp()
    {
        _service = new LabyrinthService();
        _appKey = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        _service.Reset();
    }

    [Test]
    public void CreateCrawler_ReturnsValidCrawler()
    {
        // Act
        var (crawler, result) = _service.CreateCrawler(_appKey, null);

        // Assert
        Assert.That(result, Is.EqualTo(CreateCrawlerResult.Success));
        Assert.That(crawler, Is.Not.Null);
        Assert.That(crawler!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(crawler.Bag, Is.Not.Null);
        Assert.That(crawler.Items, Is.Not.Null);
    }

    [Test]
    public void CreateCrawler_StartsAtCenter()
    {
        // Act
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Assert - Default labyrinth is 21x19, center is (10, 9)
        Assert.That(crawler!.X, Is.EqualTo(10));
        Assert.That(crawler.Y, Is.EqualTo(9));
    }

    [Test]
    public void CreateCrawler_FacesNorth()
    {
        // Act
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Assert
        Assert.That(crawler!.Dir, Is.EqualTo(Direction.North));
    }

    [Test]
    public void CreateCrawler_MultipleCrawlers_HaveDifferentIds()
    {
        // Act
        var (crawler1, _) = _service.CreateCrawler(_appKey, null);
        var (crawler2, _) = _service.CreateCrawler(_appKey, null);

        // Assert
        Assert.That(crawler1!.Id, Is.Not.EqualTo(crawler2!.Id));
    }

    [Test]
    public void CreateCrawler_MaxThreeCrawlers()
    {
        // Act
        var (crawler1, result1) = _service.CreateCrawler(_appKey, null);
        var (crawler2, result2) = _service.CreateCrawler(_appKey, null);
        var (crawler3, result3) = _service.CreateCrawler(_appKey, null);
        var (crawler4, result4) = _service.CreateCrawler(_appKey, null);

        // Assert
        Assert.That(result1, Is.EqualTo(CreateCrawlerResult.Success));
        Assert.That(result2, Is.EqualTo(CreateCrawlerResult.Success));
        Assert.That(result3, Is.EqualTo(CreateCrawlerResult.Success));
        Assert.That(result4, Is.EqualTo(CreateCrawlerResult.TooManyCrawlers));
        Assert.That(crawler4, Is.Null);
    }

    [Test]
    public void UpdateCrawler_ChangeDirection_Works()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);
        var update = new Crawler { Dir = Direction.East, Walking = false };

        // Act
        var (result, status) = _service.UpdateCrawler(crawler!.Id, _appKey, update);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.Success));
        Assert.That(result!.Dir, Is.EqualTo(Direction.East));
    }

    [Test]
    public void UpdateCrawler_WrongAppKey_ReturnsForbidden()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);
        var wrongAppKey = Guid.NewGuid();
        var update = new Crawler { Dir = Direction.East, Walking = false };

        // Act
        var (result, status) = _service.UpdateCrawler(crawler!.Id, wrongAppKey, update);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.Forbidden));
        Assert.That(result, Is.Null);
    }

    [Test]
    public void UpdateCrawler_WrongCrawlerId_ReturnsNotFound()
    {
        // Arrange
        _service.CreateCrawler(_appKey, null);
        var update = new Crawler { Dir = Direction.East, Walking = false };

        // Act
        var (result, status) = _service.UpdateCrawler(Guid.NewGuid(), _appKey, update);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.NotFound));
        Assert.That(result, Is.Null);
    }

    [Test]
    public void UpdateCrawler_WalkIntoRoom_Moves()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Walk East (should be Room) - direction change happens first
        var walk = new Crawler { Dir = Direction.East, Walking = true };

        // Act
        var (result, status) = _service.UpdateCrawler(crawler!.Id, _appKey, walk);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.Success));
        Assert.That(result!.X, Is.EqualTo(11)); // Moved East
    }

    [Test]
    public void UpdateCrawler_WalkIntoWall_ReturnsConflict()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);
        // Facing North should be a Wall at center (10, 8) is a wall due to checkerboard pattern
        var walk = new Crawler { Dir = Direction.North, Walking = true };
        int originalX = crawler!.X;
        int originalY = crawler.Y;

        // Act
        var (result, status) = _service.UpdateCrawler(crawler.Id, _appKey, walk);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.Conflict));
        Assert.That(result!.X, Is.EqualTo(originalX));
        Assert.That(result.Y, Is.EqualTo(originalY));
    }

    [Test]
    public void DeleteCrawler_ValidCrawler_ReturnsSuccess()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Act
        var result = _service.DeleteCrawler(crawler!.Id, _appKey);

        // Assert
        Assert.That(result, Is.EqualTo(DeleteCrawlerResult.Success));
    }

    [Test]
    public void DeleteCrawler_WrongAppKey_ReturnsForbidden()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Act
        var result = _service.DeleteCrawler(crawler!.Id, Guid.NewGuid());

        // Assert
        Assert.That(result, Is.EqualTo(DeleteCrawlerResult.Forbidden));
    }

    [Test]
    public void DeleteCrawler_WrongCrawlerId_ReturnsNotFound()
    {
        // Arrange
        _service.CreateCrawler(_appKey, null);

        // Act
        var result = _service.DeleteCrawler(Guid.NewGuid(), _appKey);

        // Assert
        Assert.That(result, Is.EqualTo(DeleteCrawlerResult.NotFound));
    }

    [Test]
    public void DeleteCrawler_ThenUpdate_ReturnsNotFound()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);
        _service.DeleteCrawler(crawler!.Id, _appKey);
        var update = new Crawler { Dir = Direction.East, Walking = false };

        // Act
        var (result, status) = _service.UpdateCrawler(crawler.Id, _appKey, update);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.NotFound));
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Reset_ClearsAllCrawlers()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Act
        _service.Reset();
        var update = new Crawler { Dir = Direction.East, Walking = false };
        var (result, status) = _service.UpdateCrawler(crawler!.Id, _appKey, update);

        // Assert
        Assert.That(status, Is.EqualTo(UpdateCrawlerResult.NotFound));
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAllCrawlers_ReturnsOnlyForAppKey()
    {
        // Arrange
        var otherAppKey = Guid.NewGuid();
        _service.CreateCrawler(_appKey, null);
        _service.CreateCrawler(_appKey, null);
        _service.CreateCrawler(otherAppKey, null);

        // Act
        var crawlers = _service.GetAllCrawlers(_appKey);

        // Assert
        Assert.That(crawlers.Length, Is.EqualTo(2));
    }

    [Test]
    public void GetCrawler_ValidCrawler_ReturnsCrawler()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Act
        var (result, forbidden) = _service.GetCrawler(crawler!.Id, _appKey);

        // Assert
        Assert.That(forbidden, Is.False);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(crawler.Id));
    }

    [Test]
    public void GetCrawler_WrongAppKey_ReturnsForbidden()
    {
        // Arrange
        var (crawler, _) = _service.CreateCrawler(_appKey, null);

        // Act
        var (result, forbidden) = _service.GetCrawler(crawler!.Id, Guid.NewGuid());

        // Assert
        Assert.That(forbidden, Is.True);
        Assert.That(result, Is.Null);
    }
}
