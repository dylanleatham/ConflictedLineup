using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyUserLibraryService
{
    /// <summary>
    /// Get familiar tracks from user's saved tracks and owned playlists
    /// </summary>
    /// <param name="spotify">Authenticated Spotify client</param>
    /// <param name="artistId">Spotify artist ID to find tracks for</param>
    /// <param name="userId">Current user's Spotify user ID (for filtering owned playlists)</param>
    /// <returns>List of up to 3 familiar tracks</returns>
    Task<List<TrackInfo>> GetFamiliarTracksAsync(ISpotifyClient spotify, string artistId, string userId);
}

public class SpotifyUserLibraryService : ISpotifyUserLibraryService
{
    private readonly ILogger<SpotifyUserLibraryService> _logger;
    private const int MaxRetries = 3;
    private const string Market = "US";
    private const int MaxFamiliarTracks = 3;
    private const int MaxSavedTracksToScan = 500;
    private const int MaxPlaylistsToScan = 50;
    private const int MaxTracksPerPlaylist = 200;

    public SpotifyUserLibraryService(ILogger<SpotifyUserLibraryService> logger)
    {
        _logger = logger;
    }

    public async Task<List<TrackInfo>> GetFamiliarTracksAsync(ISpotifyClient spotify, string artistId, string userId)
    {
        var familiarTracks = new List<TrackInfo>();

        // Step 1: Scan saved tracks first
        var savedTracks = await ScanSavedTracksAsync(spotify, artistId);
        familiarTracks.AddRange(savedTracks);

        _logger.LogDebug("Found {Count} familiar tracks from saved library for artist {ArtistId}",
            familiarTracks.Count, artistId);

        // Step 2: If fewer than 3 found, scan user's owned playlists
        if (familiarTracks.Count < MaxFamiliarTracks)
        {
            var playlistTracks = await ScanUserPlaylistsAsync(
                spotify,
                artistId,
                userId,
                MaxFamiliarTracks - familiarTracks.Count,
                familiarTracks.Select(t => t.SpotifyTrackId).ToHashSet());

            familiarTracks.AddRange(playlistTracks);

            _logger.LogDebug("Total familiar tracks after playlist scan: {Count} for artist {ArtistId}",
                familiarTracks.Count, artistId);
        }

        return familiarTracks;
    }

    private async Task<List<TrackInfo>> ScanSavedTracksAsync(ISpotifyClient spotify, string artistId)
    {
        var foundTracks = new List<TrackInfo>();

        return await ExecuteWithRetryAsync(async () =>
        {
            var scannedCount = 0;
            var firstPage = await spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 50 });

            // Use Paginate() for memory-efficient streaming (not PaginateAll)
            await foreach (var savedTrack in spotify.Paginate(firstPage))
            {
                scannedCount++;

                // Check if any artist on the track matches target artist
                if (savedTrack.Track?.Artists?.Any(a => a.Id == artistId) == true)
                {
                    foundTracks.Add(MapSavedTrackToTrackInfo(savedTrack.Track));

                    if (foundTracks.Count >= MaxFamiliarTracks)
                    {
                        _logger.LogDebug("Found {Count} familiar tracks after scanning {Scanned} saved tracks",
                            foundTracks.Count, scannedCount);
                        break;
                    }
                }

                // Stop after scanning limit to prevent excessive API calls
                if (scannedCount >= MaxSavedTracksToScan)
                {
                    _logger.LogDebug("Reached saved tracks scan limit ({Limit}), found {Count} familiar tracks",
                        MaxSavedTracksToScan, foundTracks.Count);
                    break;
                }
            }

            return foundTracks;
        }) ?? new List<TrackInfo>();
    }

    private async Task<List<TrackInfo>> ScanUserPlaylistsAsync(
        ISpotifyClient spotify,
        string artistId,
        string userId,
        int maxNeeded,
        HashSet<string> alreadyFoundTrackIds)
    {
        var foundTracks = new List<TrackInfo>();

        return await ExecuteWithRetryAsync(async () =>
        {
            // Get user's playlists
            var playlistsPage = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 });

            var playlistsScanned = 0;

            await foreach (var playlist in spotify.Paginate(playlistsPage))
            {
                // Only scan playlists owned by the user (not followed playlists per CONTEXT.md)
                if (playlist.Owner?.Id != userId)
                {
                    continue;
                }

                playlistsScanned++;

                if (playlistsScanned > MaxPlaylistsToScan)
                {
                    _logger.LogDebug("Reached playlist scan limit ({Limit})", MaxPlaylistsToScan);
                    break;
                }

                // Scan tracks in this playlist
                var playlistTracksPage = await spotify.Playlists.GetItems(playlist.Id!,
                    new PlaylistGetItemsRequest { Limit = 50 });

                var tracksScannedInPlaylist = 0;

                await foreach (var playlistTrack in spotify.Paginate(playlistTracksPage))
                {
                    tracksScannedInPlaylist++;

                    if (tracksScannedInPlaylist > MaxTracksPerPlaylist)
                    {
                        break;
                    }

                    // PlaylistTrack.Track can be FullTrack or FullEpisode
                    if (playlistTrack.Track is FullTrack track)
                    {
                        // Check if track matches artist and isn't already found
                        if (track.Artists?.Any(a => a.Id == artistId) == true &&
                            !alreadyFoundTrackIds.Contains(track.Id))
                        {
                            foundTracks.Add(MapFullTrackToTrackInfo(track));
                            alreadyFoundTrackIds.Add(track.Id);

                            if (foundTracks.Count >= maxNeeded)
                            {
                                _logger.LogDebug("Found {Count} familiar tracks from playlists for artist",
                                    foundTracks.Count);
                                return foundTracks;
                            }
                        }
                    }
                }
            }

            _logger.LogDebug("Scanned {Playlists} owned playlists, found {Count} familiar tracks",
                playlistsScanned, foundTracks.Count);

            return foundTracks;
        }) ?? new List<TrackInfo>();
    }

    private static TrackInfo MapSavedTrackToTrackInfo(FullTrack track)
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
