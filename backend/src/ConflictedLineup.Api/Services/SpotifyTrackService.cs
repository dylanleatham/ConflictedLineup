using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

/// <summary>
/// Phase names streamed to the client in <see cref="ArtistProgressUpdate.Phase"/>
/// </summary>
public static class TrackSelectionPhase
{
    public const string Searching = "Searching";
    public const string FetchingTracks = "Fetching tracks";
    public const string BuildingPlaylist = "Building playlist";
}

public interface ISpotifyTrackService
{
    /// <summary>
    /// Resolve lineup names to Spotify artists and choose tracks for each (see <see cref="PlaylistAllocator"/>).
    /// </summary>
    /// <param name="artistNames">Artist names as they appear on the lineup</param>
    /// <param name="accessToken">User's Spotify access token</param>
    /// <param name="progress">Optional per-artist progress, for streaming to the client</param>
    /// <param name="cancel">Cancelled when the client disconnects, to stop spending Spotify quota</param>
    Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null,
        CancellationToken cancel = default);
}

public class SpotifyTrackService : ISpotifyTrackService
{
    private readonly ISpotifyClientFactory _clientFactory;
    private readonly ISpotifySearchService _searchService;
    private readonly ISpotifyTopTracksService _topTracksService;
    private readonly ISpotifyRecentReleasesService _recentReleasesService;
    private readonly ILogger<SpotifyTrackService> _logger;

    private const int ArtistBatchSize = 50;   // Spotify's limit for GET /artists
    private const int AlbumBatchSize = 20;    // Spotify's limit for GET /albums
    private const int RecentSinglesPerArtist = 2;
    private static readonly TimeSpan PacingDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Above this many artists, skip recent releases: it costs one extra call per artist and big lineups
    /// already hit Spotify's rate limit on top tracks alone.
    /// </summary>
    private const int RecentReleasesArtistThreshold = 50;

    public SpotifyTrackService(
        ISpotifyClientFactory clientFactory,
        ISpotifySearchService searchService,
        ISpotifyTopTracksService topTracksService,
        ISpotifyRecentReleasesService recentReleasesService,
        ILogger<SpotifyTrackService> logger)
    {
        _clientFactory = clientFactory;
        _searchService = searchService;
        _topTracksService = topTracksService;
        _recentReleasesService = recentReleasesService;
        _logger = logger;
    }

    public async Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null,
        CancellationToken cancel = default)
    {
        var spotify = _clientFactory.Create(accessToken);

        var uniqueNames = artistNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .DistinctBy(n => n.ToLowerInvariant())
            .ToList();

        // 1. Resolve each lineup name to a Spotify artist
        var skipped = new List<SkippedArtist>();
        var artistIds = new List<string>();

        for (var i = 0; i < uniqueNames.Count; i++)
        {
            var name = uniqueNames[i];
            progress?.Report(new ArtistProgressUpdate(i + 1, uniqueNames.Count, name, TrackSelectionPhase.Searching));

            var match = await _searchService.SearchArtistAsync(spotify, name, cancel);
            if (match == null)
            {
                skipped.Add(new SkippedArtist(name, "No Spotify match found"));
            }
            else if (!artistIds.Contains(match.ArtistId))
            {
                // Two spellings on a lineup can resolve to the same artist; keep one
                artistIds.Add(match.ArtistId);
            }

            await Task.Delay(PacingDelay, cancel);
        }

        if (artistIds.Count == 0)
        {
            return new TrackSelectionResponse([], skipped);
        }

        // 2. Popularity for every artist, 50 per request
        var artists = await FetchArtistsAsync(spotify, artistIds, cancel);

        // 3. Top tracks, one request per artist
        var topTracks = new Dictionary<string, List<TrackInfo>>();
        for (var i = 0; i < artists.Count; i++)
        {
            progress?.Report(new ArtistProgressUpdate(i + 1, artists.Count, artists[i].Name, TrackSelectionPhase.FetchingTracks));
            topTracks[artists[i].Id] = await _topTracksService.GetTopTracksAsync(spotify, artists[i].Id, cancel);
            await Task.Delay(PacingDelay, cancel);
        }

        // 4. Recent singles, fetched in album batches
        var recentTracks = artists.Count > RecentReleasesArtistThreshold
            ? new Dictionary<string, List<TrackInfo>>()
            : await FetchRecentTracksAsync(spotify, artists, cancel);

        // 5. Allocate
        progress?.Report(new ArtistProgressUpdate(artists.Count, artists.Count, "", TrackSelectionPhase.BuildingPlaylist));

        var candidates = artists.Select(a => new ArtistCandidates(
            a.Id,
            a.Name,
            a.Popularity,
            topTracks.GetValueOrDefault(a.Id) ?? [],
            recentTracks.GetValueOrDefault(a.Id) ?? []));

        var results = PlaylistAllocator.Allocate(candidates);

        _logger.LogInformation(
            "Track selection complete: {Included} artists included, {Skipped} skipped, {Tracks} tracks",
            results.Count, skipped.Count, results.Sum(r => r.TopTracks.Count + r.RecentTracks.Count));

        return new TrackSelectionResponse(results, skipped);
    }

    private static async Task<List<FullArtist>> FetchArtistsAsync(
        ISpotifyClient spotify, List<string> artistIds, CancellationToken cancel)
    {
        var artists = new List<FullArtist>();

        foreach (var batch in artistIds.Chunk(ArtistBatchSize))
        {
            var response = await spotify.Artists.GetSeveral(new ArtistsRequest(batch), cancel);
            artists.AddRange(response.Artists.Where(a => a != null));
        }

        return artists;
    }

    private async Task<Dictionary<string, List<TrackInfo>>> FetchRecentTracksAsync(
        ISpotifyClient spotify, List<FullArtist> artists, CancellationToken cancel)
    {
        var albumOwner = new Dictionary<string, string>(); // album ID -> artist ID

        foreach (var artist in artists)
        {
            var albumIds = await _recentReleasesService.GetRecentAlbumIdsAsync(spotify, artist.Id, RecentSinglesPerArtist, cancel);
            foreach (var albumId in albumIds)
            {
                albumOwner.TryAdd(albumId, artist.Id);
            }

            await Task.Delay(PacingDelay, cancel);
        }

        var tracksByArtist = new Dictionary<string, List<TrackInfo>>();

        foreach (var batch in albumOwner.Keys.Chunk(AlbumBatchSize))
        {
            var response = await spotify.Albums.GetSeveral(new AlbumsRequest(batch), cancel);

            foreach (var album in response.Albums.Where(a => a?.Tracks?.Items != null))
            {
                // Spotify can relink an album to a different ID than the one requested; skip what we can't place
                if (!albumOwner.TryGetValue(album.Id, out var artistId)) continue;

                var tracks = tracksByArtist.TryGetValue(artistId, out var existing)
                    ? existing
                    : tracksByArtist[artistId] = [];

                tracks.AddRange(album.Tracks.Items!.Select(t => new TrackInfo(
                    SpotifyTrackId: t.Id,
                    Name: t.Name,
                    ArtistName: t.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
                    AlbumName: album.Name,
                    DurationMs: t.DurationMs)));
            }
        }

        return tracksByArtist;
    }
}
