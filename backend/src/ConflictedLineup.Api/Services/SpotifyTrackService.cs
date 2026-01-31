using System.Net;
using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyTrackService
{
    /// <summary>
    /// Select tracks for all artists using the 3+3+3 formula with deduplication
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
    private const int DelayBetweenArtistsMs = 1000; // 1 second between artists to prevent rate limiting

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

    public async Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null)
    {
        var spotify = new SpotifyClient(accessToken);

        // Get current user ID for playlist filtering (with retry for rate limiting)
        var currentUser = await ExecuteWithRetryAsync(async () => await spotify.UserProfile.Current());
        if (currentUser == null)
        {
            throw new InvalidOperationException("Failed to get current user profile from Spotify");
        }
        var userId = currentUser.Id;

        _logger.LogInformation("Starting track selection for {Count} artists", artistNames.Count);

        var artists = new List<ArtistTrackResult>();
        var skipped = new List<SkippedArtist>();

        // Process artists sequentially to avoid rate limits
        for (int i = 0; i < artistNames.Count; i++)
        {
            var artistName = artistNames[i];

            _logger.LogDebug("Processing artist {Index}/{Total}: '{ArtistName}'",
                i + 1, artistNames.Count, artistName);

            // Step 1: Search for artist
            var searchResult = await _searchService.SearchArtistAsync(spotify, artistName);

            if (searchResult == null)
            {
                _logger.LogInformation("Artist not found on Spotify: {ArtistName}", artistName);
                skipped.Add(new SkippedArtist(artistName, "No Spotify match found"));

                // Report progress with null result for skipped artist
                progress?.Report(new ArtistProgressUpdate(
                    Current: i + 1,
                    Total: artistNames.Count,
                    ArtistName: artistName,
                    Result: null
                ));

                continue;
            }

            var artistId = searchResult.ArtistId;

            var topTracks = await _topTracksService.GetTopTracksAsync(spotify, artistId);
            var recentTracks = await _recentReleasesService.GetRecentTracksAsync(spotify, artistId);

            var (_, dedupedTop, dedupedRecent) = DeduplicateTracks(
                new List<TrackInfo>(), topTracks, recentTracks);

            var result = new ArtistTrackResult(
                ArtistName: searchResult.ArtistName,
                SpotifyArtistId: artistId,
                FamiliarTracks: new List<TrackInfo>(),
                TopTracks: dedupedTop,
                RecentTracks: dedupedRecent
            );

            artists.Add(result);

            _logger.LogInformation(
                "Selected {TopCount} top tracks and {RecentCount} recent tracks for {ArtistName}",
                dedupedTop.Count, dedupedRecent.Count, searchResult.ArtistName);

            // Report progress
            progress?.Report(new ArtistProgressUpdate(
                Current: i + 1,
                Total: artistNames.Count,
                ArtistName: searchResult.ArtistName,
                Result: result
            ));

            // Small delay between artists to prevent rate limiting
            if (i < artistNames.Count - 1)
            {
                await Task.Delay(DelayBetweenArtistsMs);
            }
        }

        _logger.LogInformation(
            "Track selection complete: {Processed} artists processed, {Skipped} skipped",
            artists.Count, skipped.Count);

        return new TrackSelectionResponse(artists, skipped);
    }

    /// <summary>
    /// Deduplicate tracks across categories.
    /// Priority: familiar > top > recent (familiar tracks preserved first)
    /// </summary>
    private (List<TrackInfo> Familiar, List<TrackInfo> Top, List<TrackInfo> Recent) DeduplicateTracks(
        List<TrackInfo> familiar,
        List<TrackInfo> top,
        List<TrackInfo> recent)
    {
        var seen = new HashSet<string>();

        // Process in priority order - familiar first
        var dedupedFamiliar = familiar
            .Where(t => seen.Add(t.SpotifyTrackId))
            .ToList();

        // Top tracks second
        var dedupedTop = top
            .Where(t => seen.Add(t.SpotifyTrackId))
            .ToList();

        // Recent tracks last
        var dedupedRecent = recent
            .Where(t => seen.Add(t.SpotifyTrackId))
            .ToList();

        return (dedupedFamiliar, dedupedTop, dedupedRecent);
    }

    /// <summary>
    /// Get additional top tracks beyond the initial 3 for backfill purposes
    /// </summary>
    private async Task<List<TrackInfo>> GetAdditionalTopTracksForBackfill(
        ISpotifyClient spotify,
        string artistId,
        int neededCount,
        List<TrackInfo> alreadyHave)
    {
        if (neededCount <= 0)
        {
            return new List<TrackInfo>();
        }

        // Re-fetch top tracks with more items (up to 10 from Spotify's API)
        try
        {
            var request = new ArtistsTopTracksRequest("US");
            var response = await ExecuteWithRetryAsync(async () =>
                await spotify.Artists.GetTopTracks(artistId, request));

            if (response == null || response.Tracks == null || response.Tracks.Count == 0)
            {
                return new List<TrackInfo>();
            }

            var alreadyHaveIds = alreadyHave.Select(t => t.SpotifyTrackId).ToHashSet();

            // Get tracks we don't already have, skip first 3 (already fetched), take what we need
            var additional = response.Tracks
                .Where(t => !alreadyHaveIds.Contains(t.Id))
                .Skip(alreadyHave.Count) // Skip tracks we already have
                .Take(neededCount)
                .Select(t => new TrackInfo(
                    SpotifyTrackId: t.Id,
                    Name: t.Name,
                    ArtistName: t.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
                    AlbumName: t.Album?.Name,
                    DurationMs: t.DurationMs,
                    PreviewUrl: t.PreviewUrl
                ))
                .ToList();

            return additional;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch additional top tracks for backfill");
            return new List<TrackInfo>();
        }
    }

    /// <summary>
    /// Execute a Spotify API call with retry logic for rate limiting (429 responses)
    /// </summary>
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
