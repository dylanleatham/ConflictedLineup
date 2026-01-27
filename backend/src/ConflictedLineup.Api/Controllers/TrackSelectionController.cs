using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConflictedLineup.Api.Controllers;

[ApiController]
[Route("api/tracks")]
public class TrackSelectionController : ControllerBase
{
    private readonly ISpotifyTrackService _trackService;
    private readonly ILogger<TrackSelectionController> _logger;

    public TrackSelectionController(ISpotifyTrackService trackService, ILogger<TrackSelectionController> logger)
    {
        _trackService = trackService;
        _logger = logger;
    }

    [HttpPost("select")]
    public async Task<ActionResult<TrackSelectionResponse>> SelectTracks(
        [FromBody] TrackSelectionRequest request)
    {
        // Validate artist names
        if (request.ArtistNames == null || request.ArtistNames.Count == 0)
        {
            return BadRequest(new { error = "Artist names are required" });
        }

        // Validate access token
        if (string.IsNullOrWhiteSpace(request.SpotifyAccessToken))
        {
            return BadRequest(new { error = "Spotify access token is required" });
        }

        try
        {
            _logger.LogInformation("Starting track selection for {Count} artists", request.ArtistNames.Count);

            var result = await _trackService.SelectTracksAsync(
                request.ArtistNames,
                request.SpotifyAccessToken
            );

            _logger.LogInformation(
                "Track selection complete: {Processed} artists, {Skipped} skipped",
                result.Artists.Count, result.Skipped.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during track selection");
            return StatusCode(500, new { error = "Failed to select tracks for artists" });
        }
    }
}
