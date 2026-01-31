using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyTopTracksService
{
    /// <summary>
    /// Get top tracks for an artist (up to 10)
    /// </summary>
    /// <param name="spotify">Authenticated Spotify client</param>
    /// <param name="artistId">Spotify artist ID</param>
    /// <returns>List of up to 10 most popular tracks</returns>
    Task<List<TrackInfo>> GetTopTracksAsync(ISpotifyClient spotify, string artistId);
}

public class SpotifyTopTracksService : ISpotifyTopTracksService
{
    private readonly ILogger<SpotifyTopTracksService> _logger;
    private const int MaxRetries = 3;
    private const string Market = "US";
    private const int MaxTracks = 10;

    public SpotifyTopTracksService(ILogger<SpotifyTopTracksService> logger)
    {
        _logger = logger;
    }

    public async Task<List<TrackInfo>> GetTopTracksAsync(ISpotifyClient spotify, string artistId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var request = new ArtistsTopTracksRequest(Market);
            var response = await spotify.Artists.GetTopTracks(artistId, request);

            if (response.Tracks == null || response.Tracks.Count == 0)
            {
                _logger.LogDebug("No top tracks found for artist {ArtistId}", artistId);
                return new List<TrackInfo>();
            }

            // Take first 10 tracks (already sorted by popularity)
            var topTracks = response.Tracks
                .Take(MaxTracks)
                .Select(MapToTrackInfo)
                .ToList();

            _logger.LogDebug("Found {Count} top tracks for artist {ArtistId}", topTracks.Count, artistId);
            return topTracks;
        }) ?? new List<TrackInfo>();
    }

    private static TrackInfo MapToTrackInfo(FullTrack track)
    {
        return new TrackInfo(
            SpotifyTrackId: track.Id,
            Name: track.Name,
            ArtistName: track.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
            AlbumName: track.Album?.Name,
            DurationMs: track.DurationMs,
            PreviewUrl: track.PreviewUrl
        );
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T>> apiCall) where T : class
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await apiCall();
            }
            catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == MaxRetries - 1)
                {
                    _logger.LogError("Max retries ({MaxRetries}) exceeded for Spotify API call", MaxRetries);
                    throw;
                }

                // Read Retry-After header (in seconds), default to 5 if not present
                var retryAfterSeconds = 5;
                if (ex.Response?.Headers?.TryGetValue("Retry-After", out var retryAfterHeader) == true)
                {
                    if (int.TryParse(retryAfterHeader, out var parsed))
                    {
                        retryAfterSeconds = parsed;
                    }
                }

                _logger.LogWarning("Rate limited by Spotify API. Waiting {Seconds}s before retry (attempt {Attempt}/{MaxRetries})",
                    retryAfterSeconds, attempt + 1, MaxRetries);

                await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));
            }
        }

        return default;
    }
}
