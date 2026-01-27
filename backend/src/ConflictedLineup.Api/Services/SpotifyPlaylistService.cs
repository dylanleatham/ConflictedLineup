using System.Net;
using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyPlaylistService
{
    Task<PlaylistCreationResponse> CreatePlaylistAsync(
        string userId,
        string playlistName,
        List<ArtistTrackResult> artistResults,
        string accessToken);
}

public class SpotifyPlaylistService : ISpotifyPlaylistService
{
    private readonly ILogger<SpotifyPlaylistService> _logger;
    private const int MaxRetries = 3;
    private const int BatchSize = 100;
    private const int DelayBetweenBatchesMs = 500;

    public SpotifyPlaylistService(ILogger<SpotifyPlaylistService> logger)
    {
        _logger = logger;
    }

    public async Task<PlaylistCreationResponse> CreatePlaylistAsync(
        string userId,
        string playlistName,
        List<ArtistTrackResult> artistResults,
        string accessToken)
    {
        var spotify = new SpotifyClient(accessToken);

        // Extract all track URIs from artist results
        var trackUris = ExtractTrackUris(artistResults);

        _logger.LogInformation(
            "Creating playlist '{Name}' with {Count} tracks for user {UserId}",
            playlistName, trackUris.Count, userId);

        // Step 1: Create empty playlist (private by default)
        var createRequest = new PlaylistCreateRequest(playlistName)
        {
            Public = false,
            Description = "Created by Conflicted Lineup"
        };

        var playlist = await ExecuteWithRetryAsync(async () =>
            await spotify.Playlists.Create(userId, createRequest));

        if (playlist == null || playlist.Id == null)
        {
            throw new InvalidOperationException("Failed to create Spotify playlist");
        }

        _logger.LogInformation("Created playlist {PlaylistId}", playlist.Id);

        // Step 2: Add tracks in batches of 100
        await AddTracksInBatchesAsync(spotify, playlist.Id, trackUris);

        // Get playlist URL from ExternalUrls
        var playlistUrl = playlist.ExternalUrls?.TryGetValue("spotify", out var url) == true
            ? url
            : $"https://open.spotify.com/playlist/{playlist.Id}";

        return new PlaylistCreationResponse(
            PlaylistId: playlist.Id,
            PlaylistUrl: playlistUrl,
            PlaylistName: playlist.Name ?? playlistName,
            TrackCount: trackUris.Count,
            ArtistCount: artistResults.Count
        );
    }

    private List<string> ExtractTrackUris(List<ArtistTrackResult> artistResults)
    {
        var uris = new List<string>();

        foreach (var artist in artistResults)
        {
            // Add all tracks from each category
            uris.AddRange(artist.FamiliarTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
            uris.AddRange(artist.TopTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
            uris.AddRange(artist.RecentTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
        }

        return uris;
    }

    private async Task AddTracksInBatchesAsync(ISpotifyClient spotify, string playlistId, List<string> trackUris)
    {
        for (int i = 0; i < trackUris.Count; i += BatchSize)
        {
            var batch = trackUris.Skip(i).Take(BatchSize).ToList();
            var addRequest = new PlaylistAddItemsRequest(batch);

            _logger.LogDebug(
                "Adding batch {BatchNum}: {Count} tracks to playlist {PlaylistId}",
                (i / BatchSize) + 1, batch.Count, playlistId);

            await ExecuteWithRetryAsync(async () =>
                await spotify.Playlists.AddItems(playlistId, addRequest));

            // Small delay between batches to prevent rate limiting
            if (i + BatchSize < trackUris.Count)
            {
                await Task.Delay(DelayBetweenBatchesMs);
            }
        }

        _logger.LogInformation("Added {Total} tracks to playlist {PlaylistId}", trackUris.Count, playlistId);
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

                _logger.LogWarning(
                    "Rate limited by Spotify API. Waiting {Seconds}s before retry (attempt {Attempt}/{MaxRetries})",
                    retryAfterSeconds, attempt + 1, MaxRetries);

                await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));
            }
        }

        return default;
    }
}
