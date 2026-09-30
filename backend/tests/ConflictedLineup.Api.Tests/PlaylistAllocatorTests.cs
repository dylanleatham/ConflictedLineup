using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;

namespace ConflictedLineup.Api.Tests;

public class PlaylistAllocatorTests
{
    [Theory]
    [InlineData(100, 6)]
    [InlineData(70, 6)]
    [InlineData(69, 4)]
    [InlineData(40, 4)]
    [InlineData(39, 2)]
    [InlineData(0, 2)]
    public void TrackBudget_scales_with_popularity_tier(int popularity, int expected)
    {
        Assert.Equal(expected, PlaylistAllocator.TrackBudget(popularity));
    }

    [Fact]
    public void Fills_budget_from_top_tracks_before_recent_releases()
    {
        var artist = Artist("a", popularity: 50, top: Tracks("a-top", 3), recent: Tracks("a-new", 3));

        var result = Assert.Single(PlaylistAllocator.Allocate([artist]));

        Assert.Equal(["a-top-1", "a-top-2", "a-top-3"], Ids(result.TopTracks));
        Assert.Equal(["a-new-1"], Ids(result.RecentTracks)); // budget 4 = 3 top + 1 recent
    }

    [Fact]
    public void Shared_collaboration_goes_to_the_less_popular_artist()
    {
        var collab = Track("collab");
        var headliner = Artist("headliner", popularity: 90, top: [collab, .. Tracks("h", 10)]);
        var opener = Artist("opener", popularity: 20, top: [collab, Track("o-1")]);

        var results = PlaylistAllocator.Allocate([headliner, opener]);

        var openerResult = results.Single(r => r.SpotifyArtistId == "opener");
        var headlinerResult = results.Single(r => r.SpotifyArtistId == "headliner");
        Assert.Contains("collab", Ids(openerResult.TopTracks));
        Assert.DoesNotContain("collab", Ids(headlinerResult.TopTracks));
        Assert.Equal(6, headlinerResult.TopTracks.Count); // still gets a full budget from its other hits
    }

    [Fact]
    public void No_track_appears_twice_in_the_playlist()
    {
        var shared = Tracks("shared", 4);
        var artists = new[]
        {
            Artist("a", 80, top: shared, recent: shared),
            Artist("b", 50, top: shared, recent: Tracks("b", 2)),
            Artist("c", 10, top: shared),
        };

        var allIds = PlaylistAllocator.Allocate(artists)
            .SelectMany(r => r.TopTracks.Concat(r.RecentTracks))
            .Select(t => t.SpotifyTrackId)
            .ToList();

        Assert.Equal(allIds.Distinct().Count(), allIds.Count);
    }

    [Fact]
    public void Artist_left_with_no_tracks_is_omitted()
    {
        var only = Track("only");
        var small = Artist("small", 10, top: [only]);
        var big = Artist("big", 90, top: [only]);

        var result = Assert.Single(PlaylistAllocator.Allocate([big, small]));

        Assert.Equal("small", result.SpotifyArtistId);
    }

    [Fact]
    public void Results_are_ordered_most_popular_first_keeping_lineup_order_for_ties()
    {
        var artists = new[]
        {
            Artist("mid-1", 50, top: Tracks("m1", 1)),
            Artist("top", 90, top: Tracks("t", 1)),
            Artist("mid-2", 50, top: Tracks("m2", 1)),
            Artist("low", 5, top: Tracks("l", 1)),
        };

        var order = PlaylistAllocator.Allocate(artists).Select(r => r.SpotifyArtistId);

        Assert.Equal(["top", "mid-1", "mid-2", "low"], order);
    }

    private static ArtistCandidates Artist(string id, int popularity, List<TrackInfo> top, List<TrackInfo>? recent = null) =>
        new(id, id.ToUpperInvariant(), popularity, top, recent ?? []);

    private static TrackInfo Track(string id) => new(id, id, "artist", null, 200_000);

    private static List<TrackInfo> Tracks(string prefix, int count) =>
        Enumerable.Range(1, count).Select(i => Track($"{prefix}-{i}")).ToList();

    private static List<string> Ids(IEnumerable<TrackInfo> tracks) => tracks.Select(t => t.SpotifyTrackId).ToList();
}
