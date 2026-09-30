using SpotifyAPI.Web;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyRecentReleasesService
{
    /// <summary>
    /// Get the IDs of an artist's most recent singles, newest first, for batch fetching their tracks
    /// </summary>
    Task<List<string>> GetRecentAlbumIdsAsync(ISpotifyClient spotify, string artistId, int limit = 2, CancellationToken cancel = default);
}

public class SpotifyRecentReleasesService : ISpotifyRecentReleasesService
{
    private const string Market = "US";

    public async Task<List<string>> GetRecentAlbumIdsAsync(ISpotifyClient spotify, string artistId, int limit = 2, CancellationToken cancel = default)
    {
        var request = new ArtistsAlbumsRequest
        {
            IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Single,
            Market = Market,
            Limit = limit
        };

        var response = await spotify.Artists.GetAlbums(artistId, request, cancel);
        return response.Items?.Select(a => a.Id).ToList() ?? [];
    }
}
