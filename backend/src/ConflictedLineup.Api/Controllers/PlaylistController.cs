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

    public PlaylistController(
        ISpotifyPlaylistService playlistService,
        ILogger<PlaylistController> logger)
    {
        _playlistService = playlistService;
        _logger = logger;
    }

    /// <summary>
    /// Create a Spotify playlist from track selection results
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<PlaylistCreationResponse>> CreatePlaylist(
        [FromBody] PlaylistCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SpotifyAccessToken))
        {
            return BadRequest(new { error = "Spotify access token is required" });
        }

        if (request.Artists == null || request.Artists.Count == 0)
        {
            return BadRequest(new { error = "At least one artist with tracks is required" });
        }

        try
        {
            // Get current user ID from Spotify
            var spotify = new SpotifyAPI.Web.SpotifyClient(request.SpotifyAccessToken);
            var currentUser = await spotify.UserProfile.Current();

            // Build playlist name from festival name and year
            var playlistName = BuildPlaylistName(request.FestivalName, request.Year);

            _logger.LogInformation(
                "Creating playlist '{Name}' for user {UserId} with {ArtistCount} artists",
                playlistName, currentUser.Id, request.Artists.Count);

            var result = await _playlistService.CreatePlaylistAsync(
                userId: currentUser.Id,
                playlistName: playlistName,
                artistResults: request.Artists,
                accessToken: request.SpotifyAccessToken);

            return Ok(result);
        }
        catch (SpotifyAPI.Web.APIException ex)
        {
            _logger.LogError(ex, "Spotify API error during playlist creation");
            return StatusCode(502, new { error = "Spotify API error: " + ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create playlist");
            return StatusCode(500, new { error = "Failed to create playlist" });
        }
    }

    private static string BuildPlaylistName(string? festivalName, int? year)
    {
        if (string.IsNullOrWhiteSpace(festivalName))
        {
            return "My Festival Playlist";
        }

        return year.HasValue
            ? $"{festivalName.Trim()} {year}"
            : festivalName.Trim();
    }
}
