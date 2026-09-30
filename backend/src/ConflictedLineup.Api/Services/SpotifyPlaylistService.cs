using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyPlaylistService
{
    /// <summary>
    /// Create a private playlist in the user's account containing every selected track, headliners first
    /// </summary>
    Task<PlaylistCreationResponse> CreatePlaylistAsync(
        string accessToken,
        string playlistName,
        List<ArtistTrackResult> artistResults,
        CancellationToken cancel = default);
}

public class SpotifyPlaylistService : ISpotifyPlaylistService
{
    private readonly ISpotifyClientFactory _clientFactory;
    private readonly ILogger<SpotifyPlaylistService> _logger;
    private const int BatchSize = 100; // Spotify's limit for adding items to a playlist
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromMilliseconds(500);

    public SpotifyPlaylistService(ISpotifyClientFactory clientFactory, ILogger<SpotifyPlaylistService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<PlaylistCreationResponse> CreatePlaylistAsync(
        string accessToken,
        string playlistName,
        List<ArtistTrackResult> artistResults,
        CancellationToken cancel = default)
    {
        var spotify = _clientFactory.Create(accessToken);
        var user = await spotify.UserProfile.Current(cancel);

        var trackUris = artistResults
            .SelectMany(a => a.TopTracks.Concat(a.RecentTracks))
            .Select(t => $"spotify:track:{t.SpotifyTrackId}")
            .Distinct()
            .ToList();

        var playlist = await spotify.Playlists.Create(user.Id, new PlaylistCreateRequest(playlistName)
        {
            Public = false,
            Description = "Created by Conflicted Lineup"
        }, cancel);

        if (playlist.Id == null)
        {
            throw new InvalidOperationException("Spotify did not return an ID for the new playlist");
        }

        var batches = trackUris.Chunk(BatchSize).ToList();
        for (var i = 0; i < batches.Count; i++)
        {
            if (i > 0) await Task.Delay(DelayBetweenBatches, cancel);
            await spotify.Playlists.AddItems(playlist.Id, new PlaylistAddItemsRequest(batches[i]), cancel);
        }

        _logger.LogInformation("Created playlist {PlaylistId} with {Count} tracks", playlist.Id, trackUris.Count);

        return new PlaylistCreationResponse(
            PlaylistId: playlist.Id,
            PlaylistUrl: playlist.ExternalUrls?.GetValueOrDefault("spotify") ?? $"https://open.spotify.com/playlist/{playlist.Id}",
            PlaylistName: playlist.Name ?? playlistName,
            TrackCount: trackUris.Count,
            ArtistCount: artistResults.Count
        );
    }
}
