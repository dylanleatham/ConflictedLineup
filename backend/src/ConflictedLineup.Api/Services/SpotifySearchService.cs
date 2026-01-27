using SpotifyAPI.Web;

namespace ConflictedLineup.Api.Services;

/// <summary>
/// Result of searching for an artist on Spotify
/// </summary>
public record ArtistSearchResult(string ArtistId, string ArtistName, int Followers);

public interface ISpotifySearchService
{
    /// <summary>
    /// Search for an artist by name and return the first result
    /// </summary>
    /// <param name="spotify">Authenticated Spotify client</param>
    /// <param name="artistName">Name of artist to search for</param>
    /// <returns>Artist info if found, null otherwise</returns>
    Task<ArtistSearchResult?> SearchArtistAsync(ISpotifyClient spotify, string artistName);
}

public class SpotifySearchService : ISpotifySearchService
{
    private readonly ILogger<SpotifySearchService> _logger;
    private const int MaxRetries = 3;
    private const string Market = "US";

    public SpotifySearchService(ILogger<SpotifySearchService> logger)
    {
        _logger = logger;
    }

    public async Task<ArtistSearchResult?> SearchArtistAsync(ISpotifyClient spotify, string artistName)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Artist, artistName)
            {
                Market = Market,
                Limit = 5 // Get multiple results - Limit=1 caused issues with rate limiting
            };

            var searchResponse = await spotify.Search.Item(searchRequest);

            if (searchResponse.Artists.Items?.Count > 0)
            {
                var artist = searchResponse.Artists.Items[0];
                _logger.LogDebug("Found artist '{SpotifyName}' (ID: {Id}) for search '{SearchName}'",
                    artist.Name, artist.Id, artistName);

                return new ArtistSearchResult(
                    ArtistId: artist.Id,
                    ArtistName: artist.Name,
                    Followers: artist.Followers?.Total ?? 0
                );
            }

            _logger.LogDebug("No artist found for search '{ArtistName}'", artistName);
            return null;
        });
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> apiCall)
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
