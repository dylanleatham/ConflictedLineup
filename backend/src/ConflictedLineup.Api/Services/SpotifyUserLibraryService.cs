using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyUserLibraryService
{
    /// <summary>
    /// Scan user's saved tracks and owned playlists once, returning all tracks grouped by artist ID.
    /// Much more efficient than per-artist scanning — O(pages) instead of O(artists * pages).
    /// </summary>
    Task<Dictionary<string, List<TrackInfo>>> ScanUserLibraryAsync(ISpotifyClient spotify, string userId);

    /// <summary>
    /// Get familiar tracks from user's saved tracks and owned playlists for a single artist.
    /// Prefer ScanUserLibraryAsync for bulk operations.
    /// </summary>
    Task<List<TrackInfo>> GetFamiliarTracksAsync(ISpotifyClient spotify, string artistId, string userId);
}

public class SpotifyUserLibraryService : ISpotifyUserLibraryService
{
    private readonly ILogger<SpotifyUserLibraryService> _logger;
    private const int MaxRetries = 3;
    private const string Market = "US";
    private const int MaxFamiliarTracks = 3;
    private const int MaxSavedTracksToScan = 100; // Reduced to prevent rate limits
    private const int MaxPlaylistsToScan = 10;   // Reduced to prevent rate limits
    private const int MaxTracksPerPlaylist = 100;

    public SpotifyUserLibraryService(ILogger<SpotifyUserLibraryService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, List<TrackInfo>>> ScanUserLibraryAsync(ISpotifyClient spotify, string userId)
    {
        var tracksByArtist = new Dictionary<string, List<TrackInfo>>();

        try
        {
            // Step 1: Scan saved tracks (up to 100)
            _logger.LogInformation("Scanning user's saved tracks...");
            await ScanSavedTracksBulkAsync(spotify, tracksByArtist);

            // Step 2: Scan user's owned playlists (up to 10 playlists, 100 tracks each)
            _logger.LogInformation("Scanning user's playlists...");
            await ScanUserPlaylistsBulkAsync(spotify, userId, tracksByArtist);

            var totalTracks = tracksByArtist.Values.Sum(list => list.Count);
            var totalArtists = tracksByArtist.Count;
            _logger.LogInformation("Library scan complete: found {TotalTracks} familiar tracks across {TotalArtists} artists",
                totalTracks, totalArtists);
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited during library scan, returning partial results ({Count} artists found so far)",
                tracksByArtist.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during library scan, returning partial results ({Count} artists found so far)",
                tracksByArtist.Count);
        }

        return tracksByArtist;
    }

    private async Task ScanSavedTracksBulkAsync(ISpotifyClient spotify, Dictionary<string, List<TrackInfo>> tracksByArtist)
    {
        var scannedCount = 0;
        var firstPage = await ExecuteWithRetryAsync(async () =>
            await spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 50 }));

        if (firstPage == null) return;

        await foreach (var savedTrack in spotify.Paginate(firstPage))
        {
            scannedCount++;

            if (savedTrack.Track?.Artists != null)
            {
                foreach (var artist in savedTrack.Track.Artists)
                {
                    if (string.IsNullOrEmpty(artist.Id)) continue;

                    if (!tracksByArtist.TryGetValue(artist.Id, out var list))
                    {
                        list = new List<TrackInfo>();
                        tracksByArtist[artist.Id] = list;
                    }

                    // Cap per-artist familiar tracks
                    if (list.Count < MaxFamiliarTracks)
                    {
                        list.Add(MapSavedTrackToTrackInfo(savedTrack.Track));
                    }
                }
            }

            if (scannedCount >= MaxSavedTracksToScan) break;
        }

        _logger.LogDebug("Scanned {Count} saved tracks", scannedCount);
    }

    private async Task ScanUserPlaylistsBulkAsync(ISpotifyClient spotify, string userId, Dictionary<string, List<TrackInfo>> tracksByArtist)
    {
        var playlistsPage = await ExecuteWithRetryAsync(async () =>
            await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest { Limit = 50 }));

        if (playlistsPage == null) return;

        var playlistsScanned = 0;
        var seenTrackIds = new HashSet<string>(
            tracksByArtist.Values.SelectMany(list => list.Select(t => t.SpotifyTrackId)));

        await foreach (var playlist in spotify.Paginate(playlistsPage))
        {
            if (playlist.Owner?.Id != userId) continue;

            playlistsScanned++;
            if (playlistsScanned > MaxPlaylistsToScan) break;

            var playlistTracksPage = await ExecuteWithRetryAsync(async () =>
                await spotify.Playlists.GetItems(playlist.Id!, new PlaylistGetItemsRequest { Limit = 50 }));

            if (playlistTracksPage == null) continue;

            var tracksInPlaylist = 0;

            await foreach (var playlistTrack in spotify.Paginate(playlistTracksPage))
            {
                tracksInPlaylist++;
                if (tracksInPlaylist > MaxTracksPerPlaylist) break;

                if (playlistTrack.Track is FullTrack track && !seenTrackIds.Contains(track.Id))
                {
                    foreach (var artist in track.Artists ?? Enumerable.Empty<SimpleArtist>())
                    {
                        if (string.IsNullOrEmpty(artist.Id)) continue;

                        if (!tracksByArtist.TryGetValue(artist.Id, out var list))
                        {
                            list = new List<TrackInfo>();
                            tracksByArtist[artist.Id] = list;
                        }

                        if (list.Count < MaxFamiliarTracks)
                        {
                            list.Add(MapFullTrackToTrackInfo(track));
                            seenTrackIds.Add(track.Id);
                        }
                    }
                }
            }
        }

        _logger.LogDebug("Scanned {Count} owned playlists", playlistsScanned);
    }

    public async Task<List<TrackInfo>> GetFamiliarTracksAsync(ISpotifyClient spotify, string artistId, string userId)
    {
        var familiarTracks = new List<TrackInfo>();

        try
        {
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
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Gracefully degrade - if rate limited during library scan, skip familiar tracks
            _logger.LogWarning("Rate limited while scanning library for artist {ArtistId}, skipping familiar tracks", artistId);
            return new List<TrackInfo>();
        }
        catch (Exception ex)
        {
            // Log but don't fail - familiar tracks are a nice-to-have
            _logger.LogWarning(ex, "Error scanning library for artist {ArtistId}, skipping familiar tracks", artistId);
            return new List<TrackInfo>();
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
