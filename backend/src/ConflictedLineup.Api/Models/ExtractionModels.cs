namespace ConflictedLineup.Api.Models;

/// <summary>
/// Raw response from Claude lineup extraction
/// </summary>
public record LineupExtractionResponse(
    string Festival,
    string Source,       // "web" | "image"
    string? SourceUrl,
    List<string> Artists
);

/// <summary>
/// Information about an extracted artist with confidence level
/// </summary>
public record ArtistInfo(
    string Name,
    string Confidence  // "high" | "uncertain"
);

/// <summary>
/// Result of artist extraction from a festival poster image
/// </summary>
public record ArtistExtractionResult(
    List<ArtistInfo> Artists,
    string? FestivalName = null,
    string? Source = null,
    string? SourceUrl = null,
    string? Warning = null
);

/// <summary>
/// Result of festival lineup search via web search
/// </summary>
public record FestivalSearchResult(
    string FestivalName,
    int Year,
    List<ArtistInfo> Artists,
    List<string> Sources
);

/// <summary>
/// Request to extract artists from a poster image
/// </summary>
public record PosterExtractionRequest(
    string ImageBase64,
    string MediaType
);

/// <summary>
/// Request to search for festival lineup
/// </summary>
public record FestivalSearchRequest(
    string FestivalName,
    int? Year = null
);
