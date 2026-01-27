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

        // Get current user ID for playlist filtering
        var currentUser = await spotify.UserProfile.Current();
        var userId = currentUser.Id;

        _logger.LogInformation("Starting track selection for {Count} artists", artistNames.Count);

        var artists = new List<ArtistTrackResult>();
        var skipped = new List<SkippedArtist>();

        // Process artists sequentially to avoid rate limits
        for (int i = 0; i < artistNames.Count; i++)
        {
            var artistName = artistNames[i];

            _logger.LogDebug("Processing artist {Index}/{Total}: {ArtistName}",
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

            // Step 2: Fetch tracks from all sources
            var familiarTracks = await _userLibraryService.GetFamiliarTracksAsync(spotify, artistId, userId);
            var topTracks = await _topTracksService.GetTopTracksAsync(spotify, artistId);
            var recentTracks = await _recentReleasesService.GetRecentTracksAsync(spotify, artistId);

            // Step 3: Handle backfill if no recent releases
            // If recent tracks < 3, fetch additional top tracks to backfill (up to 6 total from top)
            if (recentTracks.Count < 3 && topTracks.Count >= 3)
            {
                // Get more top tracks for backfill - we need to re-fetch to get more than 3
                var additionalTopTracks = await GetAdditionalTopTracksForBackfill(
                    spotify, artistId, 6 - topTracks.Count, topTracks);
                topTracks.AddRange(additionalTopTracks);
                _logger.LogDebug("Backfilled top tracks to {Count} for artist {ArtistId} (no recent releases)",
                    topTracks.Count, artistId);
            }

            // Step 4: Deduplicate across categories
            // Priority: familiar > top > recent (per RESEARCH.md)
            var (dedupedFamiliar, dedupedTop, dedupedRecent) =
                DeduplicateTracks(familiarTracks, topTracks, recentTracks);

            var result = new ArtistTrackResult(
                ArtistName: searchResult.ArtistName,
                SpotifyArtistId: artistId,
                FamiliarTracks: dedupedFamiliar,
                TopTracks: dedupedTop,
                RecentTracks: dedupedRecent
            );

            artists.Add(result);

            _logger.LogInformation(
                "Selected tracks for {ArtistName}: {Familiar} familiar, {Top} top, {Recent} recent",
                searchResult.ArtistName, dedupedFamiliar.Count, dedupedTop.Count, dedupedRecent.Count);

            // Report progress
            progress?.Report(new ArtistProgressUpdate(
                Current: i + 1,
                Total: artistNames.Count,
                ArtistName: searchResult.ArtistName,
                Result: result
            ));
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
            var response = await spotify.Artists.GetTopTracks(artistId, request);

            if (response.Tracks == null || response.Tracks.Count == 0)
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
}
