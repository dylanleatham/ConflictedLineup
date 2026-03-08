using System.Text.Json;
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

    [HttpPost("select/stream")]
    public async Task SelectTracksStream([FromBody] TrackSelectionRequest request)
    {
        if (request.ArtistNames == null || request.ArtistNames.Count == 0)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { error = "Artist names are required" });
            return;
        }

        if (string.IsNullOrWhiteSpace(request.SpotifyAccessToken))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { error = "Spotify access token is required" });
            return;
        }

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var progress = new Progress<ArtistProgressUpdate>(async update =>
        {
            var evt = new { type = "progress", update.Current, update.Total, update.ArtistName, update.Phase };
            var line = $"data: {JsonSerializer.Serialize(evt, jsonOptions)}\n\n";
            await Response.WriteAsync(line);
            await Response.Body.FlushAsync();
        });

        try
        {
            _logger.LogInformation("Starting streaming track selection for {Count} artists", request.ArtistNames.Count);

            var result = await _trackService.SelectTracksAsync(
                request.ArtistNames,
                request.SpotifyAccessToken,
                progress);

            var completeEvt = new { type = "complete", result };
            var completeLine = $"data: {JsonSerializer.Serialize(completeEvt, jsonOptions)}\n\n";
            await Response.WriteAsync(completeLine);
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming track selection");
            var errorEvt = new { type = "error", message = "Failed to select tracks for artists" };
            var errorLine = $"data: {JsonSerializer.Serialize(errorEvt, jsonOptions)}\n\n";
            await Response.WriteAsync(errorLine);
            await Response.Body.FlushAsync();
        }
    }
}
