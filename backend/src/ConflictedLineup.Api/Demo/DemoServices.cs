using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;

namespace ConflictedLineup.Api.Demo;

/// <summary>
/// Stands in for Claude: any festival search or poster returns <see cref="DemoLineup"/>
/// </summary>
public class DemoClaudeService : IClaudeService
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(1200);

    public async Task<ArtistExtractionResult> ExtractArtistsFromPosterAsync(string imageBase64, string mediaType, CancellationToken cancel = default)
    {
        await Task.Delay(SimulatedLatency, cancel);
        return new ArtistExtractionResult(Lineup(), DemoLineup.FestivalName, Source: "image");
    }

    public async Task<FestivalSearchResult> SearchFestivalLineupAsync(string festivalName, int? year, CancellationToken cancel = default)
    {
        await Task.Delay(SimulatedLatency, cancel);
        return new FestivalSearchResult(DemoLineup.FestivalName, 2026, Lineup(), []);
    }

    private static List<ArtistInfo> Lineup() => DemoLineup.ArtistNames.Select(n => new ArtistInfo(n)).ToList();
}

/// <summary>
/// Stands in for Spotify during track selection. Reports the same progress phases as the real service, at
/// a pace you can watch, then runs the real <see cref="PlaylistAllocator"/> over the fixture tracks.
/// </summary>
public class DemoSpotifyTrackService : ISpotifyTrackService
{
    private static readonly TimeSpan PerArtistDelay = TimeSpan.FromMilliseconds(120);

    public async Task<TrackSelectionResponse> SelectTracksAsync(
        List<string> artistNames,
        string accessToken,
        IProgress<ArtistProgressUpdate>? progress = null,
        CancellationToken cancel = default)
    {
        var names = artistNames.DistinctBy(n => n.Trim().ToLowerInvariant()).ToList();
        var found = new List<DemoLineup.DemoArtist>();
        var skipped = new List<SkippedArtist>();

        for (var i = 0; i < names.Count; i++)
        {
            progress?.Report(new ArtistProgressUpdate(i + 1, names.Count, names[i], TrackSelectionPhase.Searching));
            await Task.Delay(PerArtistDelay, cancel);

            if (DemoLineup.Find(names[i]) is { } artist)
            {
                if (!found.Contains(artist)) found.Add(artist);
            }
            else
            {
                skipped.Add(new SkippedArtist(names[i], "No Spotify match found"));
            }
        }

        for (var i = 0; i < found.Count; i++)
        {
            progress?.Report(new ArtistProgressUpdate(i + 1, found.Count, found[i].Name, TrackSelectionPhase.FetchingTracks));
            await Task.Delay(PerArtistDelay, cancel);
        }

        progress?.Report(new ArtistProgressUpdate(found.Count, found.Count, "", TrackSelectionPhase.BuildingPlaylist));

        return new TrackSelectionResponse(PlaylistAllocator.Allocate(found.Select(a => a.ToCandidates())), skipped);
    }
}

/// <summary>
/// Stands in for Spotify's playlist API. Nothing is created; the response has no URL to open.
/// </summary>
public class DemoSpotifyPlaylistService : ISpotifyPlaylistService
{
    public async Task<PlaylistCreationResponse> CreatePlaylistAsync(
        string accessToken,
        string playlistName,
        List<ArtistTrackResult> artistResults,
        CancellationToken cancel = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(800), cancel);

        return new PlaylistCreationResponse(
            PlaylistId: "demo-playlist",
            PlaylistUrl: null,
            PlaylistName: playlistName,
            TrackCount: artistResults.Sum(a => a.TopTracks.Count + a.RecentTracks.Count),
            ArtistCount: artistResults.Count);
    }
}
