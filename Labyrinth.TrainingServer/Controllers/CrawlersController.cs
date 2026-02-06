using ApiTypes;
using Labyrinth.TrainingServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Labyrinth.TrainingServer.Controllers;

/// <summary>
/// Controller for managing crawlers in the labyrinth.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CrawlersController : ControllerBase
{
    private readonly LabyrinthService _labyrinthService;
    private readonly ILogger<CrawlersController> _logger;

    public CrawlersController(LabyrinthService labyrinthService, ILogger<CrawlersController> logger)
    {
        _labyrinthService = labyrinthService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all crawlers associated with the specified application key.
    /// GET /crawlers?appKey={guid}
    /// </summary>
    [HttpGet]
    public ActionResult<Crawler[]> GetAllCrawlers([FromQuery] Guid appKey)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        var crawlers = _labyrinthService.GetAllCrawlers(appKey);
        return Ok(crawlers);
    }

    /// <summary>
    /// Retrieves information about a specific crawler.
    /// GET /crawlers/{id}?appKey={guid}
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Crawler> GetCrawler(Guid id, [FromQuery] Guid appKey)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        var (crawler, forbidden) = _labyrinthService.GetCrawler(id, appKey);

        if (forbidden)
        {
            return StatusCode(403, "This app key cannot access this crawler");
        }

        if (crawler == null)
        {
            return NotFound("Unknown crawler");
        }

        return Ok(crawler);
    }

    /// <summary>
    /// Gets the list of items currently held in the specified crawler's inventory.
    /// GET /crawlers/{id}/bag?appKey={guid}
    /// </summary>
    [HttpGet("{id}/bag")]
    public ActionResult<InventoryItem[]> GetCrawlerBag(Guid id, [FromQuery] Guid appKey)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        var (items, forbidden) = _labyrinthService.GetCrawlerBag(id, appKey);

        if (forbidden)
        {
            return StatusCode(403, "This app key cannot access this crawler");
        }

        if (items == null)
        {
            return NotFound("Unknown crawler");
        }

        return Ok(items);
    }

    /// <summary>
    /// Retrieves the list of inventory items at the crawler's current location.
    /// GET /crawlers/{id}/items?appKey={guid}
    /// </summary>
    [HttpGet("{id}/items")]
    public ActionResult<InventoryItem[]> GetCrawlerItems(Guid id, [FromQuery] Guid appKey)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        var (items, forbidden) = _labyrinthService.GetCrawlerItems(id, appKey);

        if (forbidden)
        {
            return StatusCode(403, "This app key cannot access this crawler");
        }

        if (items == null)
        {
            return NotFound("Unknown crawler");
        }

        return Ok(items);
    }

    /// <summary>
    /// Creates a new crawler in the labyrinth.
    /// POST /crawlers?appKey={guid}
    /// </summary>
    [HttpPost]
    public ActionResult<Crawler> CreateCrawler([FromQuery] Guid appKey, [FromBody] Settings? settings)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        _logger.LogInformation("Creating crawler for appKey: {AppKey}", appKey);

        var (crawler, result) = _labyrinthService.CreateCrawler(appKey, settings);

        return result switch
        {
            CreateCrawlerResult.TooManyCrawlers => StatusCode(403, "This app key reached its 3 instances of simultaneous crawlers"),
            CreateCrawlerResult.Success => Created($"/crawlers/{crawler!.Id}", crawler),
            _ => StatusCode(500, "Unexpected error")
        };
    }

    /// <summary>
    /// Updates a crawler (move/turn).
    /// PATCH /crawlers/{id}?appKey={guid}
    /// </summary>
    [HttpPatch("{id}")]
    public ActionResult<Crawler> UpdateCrawler(Guid id, [FromQuery] Guid appKey, [FromBody] Crawler update)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        _logger.LogInformation("Updating crawler {CrawlerId} for appKey: {AppKey}, Walking: {Walking}, Direction: {Direction}",
            id, appKey, update.Walking, update.Dir);

        var (crawler, result) = _labyrinthService.UpdateCrawler(id, appKey, update);

        return result switch
        {
            UpdateCrawlerResult.NotFound => NotFound("Unknown crawler"),
            UpdateCrawlerResult.Forbidden => StatusCode(403, "This app key cannot access this crawler"),
            UpdateCrawlerResult.Conflict => Conflict("Tile not traversable or crawler does not have the right door key"),
            UpdateCrawlerResult.Success => Ok(crawler),
            _ => StatusCode(500, "Unexpected error")
        };
    }

    /// <summary>
    /// Updates the inventory bag for the specified crawler.
    /// PUT /crawlers/{id}/bag?appKey={guid}
    /// </summary>
    [HttpPut("{id}/bag")]
    public ActionResult<InventoryItem[]> TransferFromBag(Guid id, [FromQuery] Guid appKey, [FromBody] InventoryItem[] items)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        _logger.LogInformation("Transferring items from bag for crawler {CrawlerId}", id);

        var (result, status) = _labyrinthService.TransferFromBag(id, appKey, items);

        return status switch
        {
            TransferResult.NotFound => NotFound("Unknown crawler"),
            TransferResult.Forbidden => StatusCode(403, "This app key cannot access this crawler"),
            TransferResult.Success => Ok(result),
            _ => StatusCode(500, "Unexpected error")
        };
    }

    /// <summary>
    /// Updates the items placed in the tile of the specified crawler.
    /// PUT /crawlers/{id}/items?appKey={guid}
    /// </summary>
    [HttpPut("{id}/items")]
    public ActionResult<InventoryItem[]> TransferFromGround(Guid id, [FromQuery] Guid appKey, [FromBody] InventoryItem[] items)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        _logger.LogInformation("Transferring items from ground for crawler {CrawlerId}", id);

        var (result, status) = _labyrinthService.TransferFromGround(id, appKey, items);

        return status switch
        {
            TransferResult.NotFound => NotFound("Unknown crawler"),
            TransferResult.Forbidden => StatusCode(403, "This app key cannot access this crawler"),
            TransferResult.Success => Ok(result),
            _ => StatusCode(500, "Unexpected error")
        };
    }

    /// <summary>
    /// Deletes a crawler.
    /// DELETE /crawlers/{id}?appKey={guid}
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteCrawler(Guid id, [FromQuery] Guid appKey)
    {
        if (appKey == Guid.Empty)
        {
            return Unauthorized("A valid app key is required");
        }

        _logger.LogInformation("Deleting crawler {CrawlerId} for appKey: {AppKey}", id, appKey);

        var result = _labyrinthService.DeleteCrawler(id, appKey);

        return result switch
        {
            DeleteCrawlerResult.NotFound => NotFound("Unknown crawler"),
            DeleteCrawlerResult.Forbidden => StatusCode(403, "This app key cannot access this crawler"),
            DeleteCrawlerResult.Success => NoContent(),
            _ => StatusCode(500, "Unexpected error")
        };
    }
}
