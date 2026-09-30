using Microsoft.AspNetCore.Mvc;
using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;

namespace ConflictedLineup.Api.Controllers;

[ApiController]
[Route("api/playlist")]
public class PlaylistController : ControllerBase
{
    private readonly ISpotifyPlaylistService _playlistService;
    private readonly ILogger<PlaylistController> _logger;

    public PlaylistController(ISpotifyPlaylistService playlistService, ILogger<PlaylistController> logger)
    {
        _playlistService = playlistService;
        _logger = logger;
    }

    /// <summary>
    /// Create a Spotify playlist from track selection results
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<PlaylistCreationResponse>> CreatePlaylist([FromBody] PlaylistCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SpotifyAccessToken))
        {
            return BadRequest(new { error = "Spotify access token is required" });
        }

        if (request.Artists is not { Count: > 0 })
        {
            return BadRequest(new { error = "At least one artist with tracks is required" });
        }

        try
        {
            return Ok(await _playlistService.CreatePlaylistAsync(
                request.SpotifyAccessToken,
                BuildPlaylistName(request.FestivalName, request.Year),
                request.Artists,
                HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create playlist");
            return SpotifyErrors.ToResult(ex, "Failed to create playlist");
        }
    }

    internal static string BuildPlaylistName(string? festivalName, int? year)
    {
        if (string.IsNullOrWhiteSpace(festivalName))
        {
            return "My Festival Playlist";
        }

        var name = festivalName.Trim();
        return year is { } y && !name.Contains(y.ToString()) ? $"{name} {y}" : name;
    }
}
