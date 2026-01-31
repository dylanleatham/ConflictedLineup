namespace ConflictedLineup.Api.Models;

/// <summary>
/// Request from frontend to select tracks for artists
/// </summary>
public record TrackSelectionRequest(
    List<string> ArtistNames,
    string SpotifyAccessToken
);

/// <summary>
/// Response with tracks per artist and skipped artists
/// </summary>
public record TrackSelectionResponse(
    List<ArtistTrackResult> Artists,
    List<SkippedArtist> Skipped
);

/// <summary>
/// Tracks selected for one artist
/// </summary>
public record ArtistTrackResult(
    string ArtistName,
    string SpotifyArtistId,
    int Popularity,
    List<TrackInfo> FamiliarTracks,
    List<TrackInfo> TopTracks,
    List<TrackInfo> RecentTracks
);

/// <summary>
/// Artist with popularity score for sorting
/// </summary>
public record ArtistWithPopularity(
    string ArtistId,
    string ArtistName,
    int Popularity,
    string OriginalSearchName
);

/// <summary>
/// Track candidate for playlist building
/// </summary>
public record TrackCandidate(
    string TrackUri,
    string TrackId,
    string TrackName,
    string ArtistId,
    string ArtistName,
    string? AlbumName,
    int DurationMs,
    string? PreviewUrl,
    int Priority
);

/// <summary>
/// Information about a single track
/// </summary>
public record TrackInfo(
    string SpotifyTrackId,
    string Name,
    string ArtistName,
    string? AlbumName,
    int DurationMs,
    string? PreviewUrl
);

/// <summary>
/// Artist that was skipped (no Spotify match)
/// </summary>
public record SkippedArtist(
    string Name,
    string Reason
);

/// <summary>
/// Progress update for streaming progress
/// </summary>
public record ArtistProgressUpdate(
    int Current,
    int Total,
    string ArtistName,
    ArtistTrackResult? Result
);
