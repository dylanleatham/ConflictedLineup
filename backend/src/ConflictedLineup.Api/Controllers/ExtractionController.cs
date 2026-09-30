using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConflictedLineup.Api.Controllers;

[ApiController]
[Route("api/extraction")]
public class ExtractionController : ControllerBase
{
    private const int MaxBase64Length = 7_000_000; // ~5MB raw image
    private static readonly string[] ValidMediaTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly IClaudeService _claudeService;
    private readonly ILogger<ExtractionController> _logger;

    public ExtractionController(IClaudeService claudeService, ILogger<ExtractionController> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    [HttpPost("poster")]
    public async Task<ActionResult<ArtistExtractionResult>> ExtractFromPoster([FromBody] PosterExtractionRequest request)
    {
        if (string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new { error = "Image data is required" });
        }

        if (request.ImageBase64.Length > MaxBase64Length)
        {
            return BadRequest(new { error = "Image size exceeds 5MB limit" });
        }

        if (!ValidMediaTypes.Contains(request.MediaType))
        {
            return BadRequest(new { error = "Media type must be image/jpeg, image/png, or image/webp" });
        }

        try
        {
            return Ok(await _claudeService.ExtractArtistsFromPosterAsync(request.ImageBase64, request.MediaType, HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error extracting artists from poster");
            return StatusCode(500, new { error = "Failed to extract artists from poster" });
        }
    }

    [HttpPost("festival")]
    public async Task<ActionResult<FestivalSearchResult>> SearchFestival([FromBody] FestivalSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FestivalName))
        {
            return BadRequest(new { error = "Festival name is required" });
        }

        try
        {
            return Ok(await _claudeService.SearchFestivalLineupAsync(request.FestivalName.Trim(), request.Year, HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error searching festival lineup for {FestivalName}", request.FestivalName);
            return StatusCode(500, new { error = "Couldn't find that lineup. Try a poster instead." });
        }
    }
}
