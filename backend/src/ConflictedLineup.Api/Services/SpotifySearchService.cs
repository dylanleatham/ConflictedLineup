using System.Text.RegularExpressions;
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

public partial class SpotifySearchService : ISpotifySearchService
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
                Limit = 5 // Get multiple results to find best match
            };

            var searchResponse = await spotify.Search.Item(searchRequest);

            if (searchResponse.Artists.Items?.Count > 0)
            {
                // Try to find a matching artist from the results
                foreach (var artist in searchResponse.Artists.Items)
                {
                    if (IsArtistNameMatch(artistName, artist.Name))
                    {
                        _logger.LogDebug("Found matching artist '{SpotifyName}' (ID: {Id}) for search '{SearchName}'",
                            artist.Name, artist.Id, artistName);

                        return new ArtistSearchResult(
                            ArtistId: artist.Id,
                            ArtistName: artist.Name,
                            Followers: artist.Followers?.Total ?? 0
                        );
                    }
                }

                // No match found - log the mismatch
                var firstResult = searchResponse.Artists.Items[0];
                _logger.LogWarning(
                    "Artist name mismatch: searched for '{SearchName}' but Spotify returned '{SpotifyName}' - skipping",
                    artistName, firstResult.Name);
                return null;
            }

            _logger.LogDebug("No artist found for search '{ArtistName}'", artistName);
            return null;
        });
    }

    /// <summary>
    /// Check if the Spotify artist name matches the search term.
    /// Normalizes both names for comparison (lowercase, remove punctuation, handle "The" prefix).
    /// </summary>
    private bool IsArtistNameMatch(string searchName, string spotifyName)
    {
        var normalizedSearch = NormalizeArtistName(searchName);
        var normalizedSpotify = NormalizeArtistName(spotifyName);

        // Exact match after normalization
        if (normalizedSearch == normalizedSpotify)
            return true;

        // Check if one contains the other (handles "DJ Snake" matching "Snake" etc.)
        // But require at least 80% overlap to avoid false positives
        if (normalizedSearch.Length > 0 && normalizedSpotify.Length > 0)
        {
            var shorter = normalizedSearch.Length <= normalizedSpotify.Length ? normalizedSearch : normalizedSpotify;
            var longer = normalizedSearch.Length > normalizedSpotify.Length ? normalizedSearch : normalizedSpotify;

            // If the shorter name is contained in the longer one and is at least 80% of its length
            if (longer.Contains(shorter) && (double)shorter.Length / longer.Length >= 0.8)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalize artist name for comparison:
    /// - Lowercase
    /// - Remove "the " prefix
    /// - Remove punctuation and extra whitespace
    /// </summary>
    private string NormalizeArtistName(string name)
    {
        // Lowercase
        var normalized = name.ToLowerInvariant();

        // Remove "the " prefix
        if (normalized.StartsWith("the "))
            normalized = normalized[4..];

        // Remove punctuation and normalize whitespace
        normalized = NonAlphanumericRegex().Replace(normalized, " ");
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

        return normalized;
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

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
