using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

/// <summary>
/// Everything fetched from Spotify for one artist, before tracks are allocated
/// </summary>
public record ArtistCandidates(
    string ArtistId,
    string ArtistName,
    int Popularity,
    IReadOnlyList<TrackInfo> TopTracks,
    IReadOnlyList<TrackInfo> RecentTracks
);

/// <summary>
/// Decides which tracks each artist contributes to the playlist. Pure: no I/O, so the rules are unit tested.
/// </summary>
public static class PlaylistAllocator
{
    /// <summary>
    /// How many tracks an artist gets, by Spotify popularity (0-100). Headliners get more so the playlist
    /// feels like the festival; smaller acts still get enough to be discovered.
    /// </summary>
    public static int TrackBudget(int popularity) => popularity switch
    {
        >= 70 => 6,
        >= 40 => 4,
        _ => 2
    };

    /// <summary>
    /// Fill each artist's budget from their top tracks first, then recent singles, with every track used
    /// at most once across the whole playlist.
    /// </summary>
    /// <remarks>
    /// A collaboration appears in both artists' top tracks. Allocating least popular artists first lets the
    /// smaller act claim it: the headliner has plenty of other hits, the opener may not.
    /// Artists left with no tracks are omitted. Results are ordered most popular first.
    /// </remarks>
    public static List<ArtistTrackResult> Allocate(IEnumerable<ArtistCandidates> artists)
    {
        var claimedTrackIds = new HashSet<string>();
        var results = new List<ArtistTrackResult>();

        foreach (var artist in artists.OrderBy(a => a.Popularity))
        {
            var budget = TrackBudget(artist.Popularity);
            var top = Claim(artist.TopTracks, budget);
            var recent = Claim(artist.RecentTracks, budget - top.Count);

            if (top.Count + recent.Count > 0)
            {
                results.Add(new ArtistTrackResult(artist.ArtistName, artist.ArtistId, artist.Popularity, top, recent));
            }
        }

        // Stable sort: equally popular artists keep their lineup order
        return results.OrderByDescending(r => r.Popularity).ToList();

        List<TrackInfo> Claim(IEnumerable<TrackInfo> candidates, int limit)
        {
            var claimed = new List<TrackInfo>();
            foreach (var track in candidates)
            {
                if (claimed.Count >= limit) break;
                if (claimedTrackIds.Add(track.SpotifyTrackId)) claimed.Add(track);
            }
            return claimed;
        }
    }
}
