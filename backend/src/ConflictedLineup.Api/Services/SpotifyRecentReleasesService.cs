using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyRecentReleasesService
{
    /// <summary>
    /// Get tracks from an artist's most recent release (up to 3)
    /// </summary>
    /// <param name="spotify">Authenticated Spotify client</param>
    /// <param name="artistId">Spotify artist ID</param>
    /// <returns>List of up to 3 tracks from the most recent album/EP/single</returns>
    Task<List<TrackInfo>> GetRecentTracksAsync(ISpotifyClient spotify, string artistId);

    /// <summary>
    /// Get recent album/single IDs for an artist (for batch fetching)
    /// </summary>
    /// <param name="spotify">Authenticated Spotify client</param>
    /// <param name="artistId">Spotify artist ID</param>
    /// <param name="limit">Maximum number of albums to return</param>
    /// <returns>List of album IDs</returns>
    Task<List<string>> GetRecentAlbumIdsAsync(ISpotifyClient spotify, string artistId, int limit = 2);
}

public class SpotifyRecentReleasesService : ISpotifyRecentReleasesService
{
    private readonly ILogger<SpotifyRecentReleasesService> _logger;
    private const int MaxRetries = 3;
    private const string Market = "US";
    private const int MaxTracks = 3;

    public SpotifyRecentReleasesService(ILogger<SpotifyRecentReleasesService> logger)
    {
        _logger = logger;
    }

    public async Task<List<TrackInfo>> GetRecentTracksAsync(ISpotifyClient spotify, string artistId)
    {
        var recentSingles = await GetRecentSinglesAsync(spotify, artistId);

        if (recentSingles.Count == 0)
        {
            _logger.LogDebug("No recent singles found for artist {ArtistId}", artistId);
            return new List<TrackInfo>();
        }

        var collectedTracks = new List<TrackInfo>();

        foreach (var single in recentSingles)
        {
            if (collectedTracks.Count >= MaxTracks)
                break;

            var tracksFromSingle = await GetAlbumTracksAsync(spotify, single.Id);
            var tracksNeeded = MaxTracks - collectedTracks.Count;
            collectedTracks.AddRange(tracksFromSingle.Take(tracksNeeded));

            _logger.LogDebug("Collected {Count} tracks from single '{Name}', total now {Total}",
                Math.Min(tracksFromSingle.Count, tracksNeeded), single.Name, collectedTracks.Count);
        }

        return collectedTracks;
    }

    public async Task<List<string>> GetRecentAlbumIdsAsync(ISpotifyClient spotify, string artistId, int limit = 2)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var singlesRequest = new ArtistsAlbumsRequest
            {
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Single,
                Market = Market,
                Limit = limit
            };

            var singlesResponse = await spotify.Artists.GetAlbums(artistId, singlesRequest);
            return singlesResponse.Items?.Select(a => a.Id).ToList() ?? new List<string>();
        }) ?? new List<string>();
    }

    private async Task<List<SimpleAlbum>> GetRecentSinglesAsync(ISpotifyClient spotify, string artistId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var singlesRequest = new ArtistsAlbumsRequest
            {
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Single,
                Market = Market,
                Limit = 5 // Get enough singles to likely yield 3 tracks
            };

            var singlesResponse = await spotify.Artists.GetAlbums(artistId, singlesRequest);
            return singlesResponse.Items?.ToList() ?? new List<SimpleAlbum>();
        }) ?? new List<SimpleAlbum>();
    }

    private async Task<List<TrackInfo>> GetAlbumTracksAsync(ISpotifyClient spotify, string albumId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var fullAlbum = await spotify.Albums.Get(albumId, new AlbumRequest { Market = Market });

            return fullAlbum.Tracks.Items?
                .Select(t => MapSimpleTrackToTrackInfo(t, fullAlbum.Name, fullAlbum.Artists.FirstOrDefault()?.Name ?? "Unknown Artist"))
                .ToList() ?? new List<TrackInfo>();
        }) ?? new List<TrackInfo>();
    }

    private static TrackInfo MapSimpleTrackToTrackInfo(SimpleTrack track, string albumName, string artistName)
    {
        return new TrackInfo(
            SpotifyTrackId: track.Id,
            Name: track.Name,
            ArtistName: track.Artists.FirstOrDefault()?.Name ?? artistName,
            AlbumName: albumName,
            DurationMs: track.DurationMs,
            PreviewUrl: track.PreviewUrl
        );
    }

    /// <summary>
    /// Execute API call with retry logic for rate limiting (for non-nullable returns)
    /// </summary>
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

                var retryAfterSeconds = GetRetryAfterSeconds(ex);
                _logger.LogWarning("Rate limited by Spotify API. Waiting {Seconds}s before retry (attempt {Attempt}/{MaxRetries})",
                    retryAfterSeconds, attempt + 1, MaxRetries);

                await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));
            }
        }

        return default;
    }

    private static int GetRetryAfterSeconds(APIException ex)
    {
        // Read Retry-After header (in seconds), default to 5 if not present
        var retryAfterSeconds = 5;
        if (ex.Response?.Headers?.TryGetValue("Retry-After", out var retryAfterHeader) == true)
        {
            if (int.TryParse(retryAfterHeader, out var parsed))
            {
                retryAfterSeconds = parsed;
            }
        }
        return retryAfterSeconds;
    }
}
