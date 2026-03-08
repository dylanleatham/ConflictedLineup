using System.Net;
using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyTrackService
{
    /// <summary>
    /// Select tracks for all artists using popularity-based algorithm with cross-artist deduplication.
    /// Processes artists from least to most popular so smaller artists claim collab tracks first.
    /// </summary>
    /// <param name="artistNames">List of artist names to process</param>
    /// <param name="accessToken">User's Spotify access token</param>
    /// <param name="progress">Optional progress reporter for streaming updates</param>
    /// <returns>Track selection results including skipped artists</returns>
    Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null);
}

public class SpotifyTrackService : ISpotifyTrackService
{
    private readonly ISpotifySearchService _searchService;
    private readonly ISpotifyTopTracksService _topTracksService;
    private readonly ISpotifyRecentReleasesService _recentReleasesService;
    private readonly ISpotifyUserLibraryService _userLibraryService;
    private readonly ILogger<SpotifyTrackService> _logger;
    private const int MaxRetries = 3;
    private const int ArtistBatchSize = 50;
    private const int AlbumBatchSize = 20;

    public SpotifyTrackService(
        ISpotifySearchService searchService,
        ISpotifyTopTracksService topTracksService,
        ISpotifyRecentReleasesService recentReleasesService,
        ISpotifyUserLibraryService userLibraryService,
        ILogger<SpotifyTrackService> logger)
    {
        _searchService = searchService;
        _topTracksService = topTracksService;
        _recentReleasesService = recentReleasesService;
        _userLibraryService = userLibraryService;
        _logger = logger;
    }

    /// <summary>
    /// Returns track allocation based on artist popularity tier.
    /// High (70+): 6 tracks, Medium (40-69): 4 tracks, Low (&lt;40): 2 tracks
    /// </summary>
    private static int GetTracksForPopularity(int popularity) => popularity switch
    {
        >= 70 => 6,
        >= 40 => 4,
        _ => 2
    };

    public async Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null)
    {
        var spotify = new SpotifyClient(accessToken);

        // Dedupe input list (case-insensitive)
        var uniqueArtistNames = artistNames
            .GroupBy(n => n.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Starting track selection for {Count} unique artists (from {OriginalCount} input)",
            uniqueArtistNames.Count, artistNames.Count);

        var skipped = new List<SkippedArtist>();

        // Phase 1: Search all artists and collect IDs
        _logger.LogInformation("Phase 1: Searching for artists...");
        var searchResults = new List<(string SearchName, string ArtistId, string SpotifyName)>();
        var seenArtistIds = new HashSet<string>();

        for (int i = 0; i < uniqueArtistNames.Count; i++)
        {
            var artistName = uniqueArtistNames[i];
            var searchResult = await _searchService.SearchArtistAsync(spotify, artistName);

            if (searchResult == null)
            {
                _logger.LogInformation("Artist not found on Spotify: {ArtistName}", artistName);
                skipped.Add(new SkippedArtist(artistName, "No Spotify match found"));
                continue;
            }

            // Dedupe by Spotify artist ID (different search names can resolve to same artist)
            if (!seenArtistIds.Add(searchResult.ArtistId))
            {
                _logger.LogInformation("Artist '{ArtistName}' resolved to already-seen artist ID {ArtistId}, skipping duplicate",
                    artistName, searchResult.ArtistId);
                continue;
            }

            searchResults.Add((artistName, searchResult.ArtistId, searchResult.ArtistName));
        }

        if (searchResults.Count == 0)
        {
            return new TrackSelectionResponse(new List<ArtistTrackResult>(), skipped);
        }

        // Phase 2: Batch fetch artist details with popularity scores
        _logger.LogInformation("Phase 2: Fetching artist popularity scores...");
        var artistsWithPopularity = await BatchFetchArtistPopularityAsync(
            spotify,
            searchResults.Select(r => (r.ArtistId, r.SpotifyName, r.SearchName)).ToList());

        // Phase 3: Familiar tracks disabled to reduce Spotify API calls (rate limiting)
        // TODO: Re-enable when rate limiting is resolved
        // var userProfile = await spotify.UserProfile.Current();
        // familiarTracksByArtist = await _userLibraryService.ScanUserLibraryAsync(spotify, userProfile.Id);

        // Phase 4: Fetch top tracks per artist
        _logger.LogInformation("Phase 3: Fetching top tracks...");
        var topTracks = new Dictionary<string, List<TrackCandidate>>();

        foreach (var artist in artistsWithPopularity)
        {
            var tracks = await _topTracksService.GetTopTracksAsync(spotify, artist.ArtistId);
            topTracks[artist.ArtistId] = tracks
                .Select((t, index) => new TrackCandidate(
                    TrackUri: $"spotify:track:{t.SpotifyTrackId}",
                    TrackId: t.SpotifyTrackId,
                    TrackName: t.Name,
                    ArtistId: artist.ArtistId,
                    ArtistName: t.ArtistName,
                    AlbumName: t.AlbumName,
                    DurationMs: t.DurationMs,
                    PreviewUrl: t.PreviewUrl,
                    Priority: index))
                .ToList();
        }

        // Phase 5: Fetch recent album IDs per artist
        _logger.LogInformation("Phase 5: Fetching recent album IDs...");
        var artistAlbumIds = new Dictionary<string, List<string>>();

        foreach (var artist in artistsWithPopularity)
        {
            var albumIds = await _recentReleasesService.GetRecentAlbumIdsAsync(spotify, artist.ArtistId, 2);
            artistAlbumIds[artist.ArtistId] = albumIds;
        }

        // Phase 6: Batch fetch album tracks
        _logger.LogInformation("Phase 6: Batch fetching album tracks...");
        var recentTracks = await BatchFetchAlbumTracksAsync(spotify, artistsWithPopularity, artistAlbumIds);

        // Phase 7: Build playlist with popularity-weighted allocation
        _logger.LogInformation("Phase 7: Building playlist with popularity-weighted allocation...");
        var claimedUris = new HashSet<string>();
        var results = new List<ArtistTrackResult>();

        // Sort by popularity ascending (smallest artists first)
        var sortedArtists = artistsWithPopularity.OrderBy(a => a.Popularity).ToList();

        foreach (var artist in sortedArtists)
        {
            var maxTracks = GetTracksForPopularity(artist.Popularity);
            var selectedTop = new List<TrackInfo>();
            var selectedRecent = new List<TrackInfo>();

            int TotalSelected() => selectedTop.Count + selectedRecent.Count;

            // Priority 1: Top tracks
            if (topTracks.TryGetValue(artist.ArtistId, out var artistTopTracks))
            {
                foreach (var track in artistTopTracks.OrderBy(t => t.Priority))
                {
                    if (TotalSelected() >= maxTracks) break;
                    if (claimedUris.Add(track.TrackUri))
                    {
                        selectedTop.Add(MapToTrackInfo(track));
                    }
                }
            }

            // Priority 2: Recent tracks
            if (recentTracks.TryGetValue(artist.ArtistId, out var artistRecentTracks))
            {
                foreach (var track in artistRecentTracks.OrderBy(t => t.Priority))
                {
                    if (TotalSelected() >= maxTracks) break;
                    if (claimedUris.Add(track.TrackUri))
                    {
                        selectedRecent.Add(MapToTrackInfo(track));
                    }
                }
            }

            // Only include artists that have at least one track
            if (TotalSelected() > 0)
            {
                var result = new ArtistTrackResult(
                    ArtistName: artist.ArtistName,
                    SpotifyArtistId: artist.ArtistId,
                    Popularity: artist.Popularity,
                    FamiliarTracks: new List<TrackInfo>(),
                    TopTracks: selectedTop,
                    RecentTracks: selectedRecent
                );

                results.Add(result);

                _logger.LogInformation(
                    "Selected {TopCount} top + {RecentCount} recent tracks for {ArtistName} (popularity: {Popularity}, max: {MaxTracks})",
                    selectedTop.Count, selectedRecent.Count, artist.ArtistName, artist.Popularity, maxTracks);
            }
            else
            {
                _logger.LogWarning(
                    "No tracks available for {ArtistName} (popularity: {Popularity}) - all tracks may have been claimed by other artists",
                    artist.ArtistName, artist.Popularity);
            }
        }

        // Reverse so headliners (most popular) come first
        results.Reverse();

        // Report progress for all artists at the end (since we process them all at once now)
        for (int i = 0; i < results.Count; i++)
        {
            progress?.Report(new ArtistProgressUpdate(
                Current: i + 1,
                Total: results.Count + skipped.Count,
                ArtistName: results[i].ArtistName,
                Result: results[i]
            ));
        }

        // Report skipped artists
        foreach (var skippedArtist in skipped)
        {
            progress?.Report(new ArtistProgressUpdate(
                Current: results.Count + skipped.IndexOf(skippedArtist) + 1,
                Total: results.Count + skipped.Count,
                ArtistName: skippedArtist.Name,
                Result: null
            ));
        }

        _logger.LogInformation(
            "Track selection complete: {Processed} artists processed, {Skipped} skipped, {UniqueTrackCount} unique tracks claimed",
            results.Count, skipped.Count, claimedUris.Count);

        return new TrackSelectionResponse(results, skipped);
    }

    private async Task<List<ArtistWithPopularity>> BatchFetchArtistPopularityAsync(
        ISpotifyClient spotify,
        List<(string ArtistId, string SpotifyName, string SearchName)> artists)
    {
        var results = new List<ArtistWithPopularity>();

        // Process in batches of 50
        for (int i = 0; i < artists.Count; i += ArtistBatchSize)
        {
            var batch = artists.Skip(i).Take(ArtistBatchSize).ToList();
            var artistIds = batch.Select(a => a.ArtistId).ToList();

            var response = await ExecuteWithRetryAsync(async () =>
                await spotify.Artists.GetSeveral(new ArtistsRequest(artistIds)));

            if (response?.Artists != null)
            {
                foreach (var fullArtist in response.Artists)
                {
                    if (fullArtist == null) continue;

                    var originalArtist = batch.FirstOrDefault(a => a.ArtistId == fullArtist.Id);
                    results.Add(new ArtistWithPopularity(
                        ArtistId: fullArtist.Id,
                        ArtistName: fullArtist.Name,
                        Popularity: fullArtist.Popularity,
                        OriginalSearchName: originalArtist.SearchName
                    ));
                }
            }
        }

        return results;
    }

    private async Task<Dictionary<string, List<TrackCandidate>>> BatchFetchAlbumTracksAsync(
        ISpotifyClient spotify,
        List<ArtistWithPopularity> artists,
        Dictionary<string, List<string>> artistAlbumIds)
    {
        var results = new Dictionary<string, List<TrackCandidate>>();

        // Initialize empty lists for all artists
        foreach (var artist in artists)
        {
            results[artist.ArtistId] = new List<TrackCandidate>();
        }

        // Build mapping from album ID to artist ID
        var albumToArtist = new Dictionary<string, string>();
        var allAlbumIds = new List<string>();

        foreach (var artist in artists)
        {
            if (artistAlbumIds.TryGetValue(artist.ArtistId, out var albumIds))
            {
                foreach (var albumId in albumIds)
                {
                    albumToArtist[albumId] = artist.ArtistId;
                    allAlbumIds.Add(albumId);
                }
            }
        }

        // Batch fetch albums (20 at a time)
        for (int i = 0; i < allAlbumIds.Count; i += AlbumBatchSize)
        {
            var batch = allAlbumIds.Skip(i).Take(AlbumBatchSize).ToList();

            var response = await ExecuteWithRetryAsync(async () =>
                await spotify.Albums.GetSeveral(new AlbumsRequest(batch)));

            if (response?.Albums != null)
            {
                foreach (var album in response.Albums)
                {
                    if (album?.Tracks?.Items == null) continue;
                    if (!albumToArtist.TryGetValue(album.Id, out var artistId)) continue;

                    var priority = results[artistId].Count;
                    foreach (var track in album.Tracks.Items)
                    {
                        results[artistId].Add(new TrackCandidate(
                            TrackUri: $"spotify:track:{track.Id}",
                            TrackId: track.Id,
                            TrackName: track.Name,
                            ArtistId: artistId,
                            ArtistName: track.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
                            AlbumName: album.Name,
                            DurationMs: track.DurationMs,
                            PreviewUrl: track.PreviewUrl,
                            Priority: priority++
                        ));
                    }
                }
            }
        }

        return results;
    }

    private static TrackInfo MapToTrackInfo(TrackCandidate track)
    {
        return new TrackInfo(
            SpotifyTrackId: track.TrackId,
            Name: track.TrackName,
            ArtistName: track.ArtistName,
            AlbumName: track.AlbumName,
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
            catch (APIException ex) when (ex.Response?.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == MaxRetries - 1)
                {
                    _logger.LogError("Max retries ({MaxRetries}) exceeded for Spotify API call", MaxRetries);
                    throw;
                }

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
