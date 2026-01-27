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
        // Step 1: Get artist's most recent release (prioritize singles per CONTEXT.md)
        var mostRecentRelease = await GetMostRecentReleaseAsync(spotify, artistId);

        if (mostRecentRelease == null)
        {
            _logger.LogDebug("No recent releases found for artist {ArtistId}", artistId);
            return new List<TrackInfo>();
        }

        _logger.LogDebug("Found most recent release '{Album}' ({Type}) for artist {ArtistId}",
            mostRecentRelease.Name, mostRecentRelease.AlbumType, artistId);

        // Step 2: Get tracks from that release
        var tracks = await GetAlbumTracksAsync(spotify, mostRecentRelease.Id, mostRecentRelease.AlbumType);

        return tracks;
    }

    private async Task<SimpleAlbum?> GetMostRecentReleaseAsync(ISpotifyClient spotify, string artistId)
    {
        return await ExecuteWithRetryAsyncNullable(async () =>
        {
            // First check for recent singles (more likely to be played live per CONTEXT.md)
            var singlesRequest = new ArtistsAlbumsRequest
            {
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Single,
                Market = Market,
                Limit = 1 // Just need the most recent
            };

            var singlesResponse = await spotify.Artists.GetAlbums(artistId, singlesRequest);

            // Also get albums/EPs to compare release dates
            var albumsRequest = new ArtistsAlbumsRequest
            {
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album,
                Market = Market,
                Limit = 1
            };

            var albumsResponse = await spotify.Artists.GetAlbums(artistId, albumsRequest);

            var mostRecentSingle = singlesResponse.Items?.FirstOrDefault();
            var mostRecentAlbum = albumsResponse.Items?.FirstOrDefault();

            // If single is more recent (or no album), prioritize single
            if (mostRecentSingle != null && mostRecentAlbum != null)
            {
                // Compare release dates - singles from recent album cycle preferred
                var singleDate = ParseReleaseDate(mostRecentSingle.ReleaseDate);
                var albumDate = ParseReleaseDate(mostRecentAlbum.ReleaseDate);

                // If single is within 90 days of album (same release cycle), prefer single
                // If single is more recent than album, prefer single
                // If album is more recent by > 90 days, use album
                if (singleDate >= albumDate || (albumDate - singleDate).Days <= 90)
                {
                    return mostRecentSingle;
                }
                return mostRecentAlbum;
            }

            // Return whichever exists
            return mostRecentSingle ?? mostRecentAlbum;
        });
    }

    private async Task<List<TrackInfo>> GetAlbumTracksAsync(ISpotifyClient spotify, string albumId, string albumType)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var tracksRequest = new AlbumTracksRequest
            {
                Market = Market,
                Limit = 50 // Get all tracks from album
            };

            var tracksResponse = await spotify.Albums.GetTracks(albumId, tracksRequest);

            if (tracksResponse.Items == null || tracksResponse.Items.Count == 0)
            {
                return new List<TrackInfo>();
            }

            // For singles, return all tracks (usually 1-3)
            // For albums, we need to get full track info to sort by popularity
            if (albumType == "single")
            {
                // Simple tracks don't have all fields, need to get full album
                var fullAlbum = await spotify.Albums.Get(albumId, new AlbumRequest { Market = Market });
                return fullAlbum.Tracks.Items?
                    .Take(MaxTracks)
                    .Select(t => MapSimpleTrackToTrackInfo(t, fullAlbum.Name, fullAlbum.Artists.FirstOrDefault()?.Name ?? "Unknown Artist"))
                    .ToList() ?? new List<TrackInfo>();
            }
            else
            {
                // For albums, get full track details with popularity to sort
                var fullAlbum = await spotify.Albums.Get(albumId, new AlbumRequest { Market = Market });

                // Get track IDs to fetch full tracks with popularity
                var trackIds = fullAlbum.Tracks.Items?.Take(20).Select(t => t.Id).ToList();

                if (trackIds == null || trackIds.Count == 0)
                {
                    return new List<TrackInfo>();
                }

                var fullTracksResponse = await spotify.Tracks.GetSeveral(new TracksRequest(trackIds) { Market = Market });

                // Sort by popularity (higher is more likely a single), take top 3
                var sortedTracks = fullTracksResponse.Tracks
                    .Where(t => t != null)
                    .OrderByDescending(t => t.Popularity)
                    .Take(MaxTracks)
                    .Select(MapFullTrackToTrackInfo)
                    .ToList();

                _logger.LogDebug("Selected {Count} tracks from album by popularity", sortedTracks.Count);
                return sortedTracks;
            }
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

    private static TrackInfo MapFullTrackToTrackInfo(FullTrack track)
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

    private static DateTime ParseReleaseDate(string releaseDate)
    {
        // Spotify returns dates in formats: YYYY, YYYY-MM, or YYYY-MM-DD
        if (DateTime.TryParse(releaseDate, out var date))
        {
            return date;
        }

        // If only year, use Jan 1 of that year
        if (int.TryParse(releaseDate, out var year))
        {
            return new DateTime(year, 1, 1);
        }

        // If year-month format
        if (releaseDate.Length == 7 && releaseDate[4] == '-')
        {
            if (int.TryParse(releaseDate[..4], out var y) && int.TryParse(releaseDate[5..], out var m))
            {
                return new DateTime(y, m, 1);
            }
        }

        return DateTime.MinValue;
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

    /// <summary>
    /// Execute API call with retry logic for rate limiting (for nullable returns like SimpleAlbum?)
    /// </summary>
    private async Task<T?> ExecuteWithRetryAsyncNullable<T>(Func<Task<T?>> apiCall) where T : class
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
