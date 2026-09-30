using System.Text.RegularExpressions;
using SpotifyAPI.Web;

namespace ConflictedLineup.Api.Services;

/// <summary>
/// Result of searching for an artist on Spotify
/// </summary>
public record ArtistSearchResult(string ArtistId, string ArtistName);

public interface ISpotifySearchService
{
    /// <summary>
    /// Search for an artist by name and return the first result whose name actually matches
    /// </summary>
    /// <returns>Artist info if found, null otherwise</returns>
    Task<ArtistSearchResult?> SearchArtistAsync(ISpotifyClient spotify, string artistName, CancellationToken cancel = default);
}

public class SpotifySearchService : ISpotifySearchService
{
    private readonly ILogger<SpotifySearchService> _logger;
    private const string Market = "US";

    public SpotifySearchService(ILogger<SpotifySearchService> logger)
    {
        _logger = logger;
    }

    public async Task<ArtistSearchResult?> SearchArtistAsync(ISpotifyClient spotify, string artistName, CancellationToken cancel = default)
    {
        // Punctuation confuses Spotify's search (e.g. "Hol!" returns "Wooli" as the top result),
        // so search with a sanitized query but compare results against the original name
        var searchRequest = new SearchRequest(SearchRequest.Types.Artist, ArtistNameMatcher.SanitizeQuery(artistName))
        {
            Market = Market,
            Limit = 5 // Several results, so a near-miss top result doesn't hide the real artist
        };

        var searchResponse = await spotify.Search.Item(searchRequest, cancel);
        var candidates = searchResponse.Artists.Items ?? [];

        var match = candidates.FirstOrDefault(a => ArtistNameMatcher.IsMatch(artistName, a.Name));
        if (match != null)
        {
            return new ArtistSearchResult(match.Id, match.Name);
        }

        if (candidates.Count > 0)
        {
            _logger.LogWarning("Artist name mismatch: searched for '{SearchName}' but Spotify returned '{SpotifyName}' - skipping",
                artistName, candidates[0].Name);
        }

        return null;
    }
}

/// <summary>
/// Decides whether a Spotify search result is the artist a lineup named. Lineups and Spotify disagree on
/// casing, punctuation and "The" prefixes; a plain string compare skips real artists, and trusting the top
/// result puts the wrong artist in the playlist.
/// </summary>
public static partial class ArtistNameMatcher
{
    /// <summary>
    /// Minimum share of the longer name the shorter one must cover for a containment match,
    /// so "Snake" can't match "DJ Snake" but a missing suffix still can.
    /// </summary>
    private const double MinContainmentRatio = 0.8;

    public static bool IsMatch(string searchName, string spotifyName)
    {
        var a = Normalize(searchName);
        var b = Normalize(spotifyName);

        if (a.Length == 0 || b.Length == 0)
            return false;

        if (a == b)
            return true;

        var (shorter, longer) = a.Length <= b.Length ? (a, b) : (b, a);
        return longer.Contains(shorter) && (double)shorter.Length / longer.Length >= MinContainmentRatio;
    }

    /// <summary>
    /// Replace punctuation with spaces and collapse whitespace, keeping letters and digits in any script.
    /// </summary>
    public static string SanitizeQuery(string query) =>
        WhitespaceRegex().Replace(NonAlphanumericRegex().Replace(query, " "), " ").Trim();

    /// <summary>
    /// Lowercase, drop a leading "the ", then sanitize.
    /// </summary>
    public static string Normalize(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();

        if (normalized.StartsWith("the "))
            normalized = normalized[4..];

        return SanitizeQuery(normalized);
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
