namespace ConflictedLineup.Api.Models;

/// <summary>
/// Request to create a Spotify playlist from track selection results
/// </summary>
public record PlaylistCreationRequest(
    string FestivalName,
    int? Year,
    List<ArtistTrackResult> Artists,
    string SpotifyAccessToken
);

/// <summary>
/// Response with created playlist details
/// </summary>
public record PlaylistCreationResponse(
    string PlaylistId,
    string PlaylistUrl,
    string PlaylistName,
    int TrackCount,
    int ArtistCount
);
