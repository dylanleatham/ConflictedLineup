using SpotifyAPI.Web;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyTopTracksService
{
    /// <summary>
    /// Get an artist's top tracks, most popular first (up to 10)
    /// </summary>
    Task<List<TrackInfo>> GetTopTracksAsync(ISpotifyClient spotify, string artistId, CancellationToken cancel = default);
}

public class SpotifyTopTracksService : ISpotifyTopTracksService
{
    private const string Market = "US";
    private const int MaxTracks = 10;

    public async Task<List<TrackInfo>> GetTopTracksAsync(ISpotifyClient spotify, string artistId, CancellationToken cancel = default)
    {
        var response = await spotify.Artists.GetTopTracks(artistId, new ArtistsTopTracksRequest(Market), cancel);

        return (response.Tracks ?? [])
            .Take(MaxTracks)
            .Select(track => new TrackInfo(
                SpotifyTrackId: track.Id,
                Name: track.Name,
                ArtistName: track.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
                AlbumName: track.Album?.Name,
                DurationMs: track.DurationMs))
            .ToList();
    }
}
