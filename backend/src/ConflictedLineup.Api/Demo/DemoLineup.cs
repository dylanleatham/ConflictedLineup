using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;

namespace ConflictedLineup.Api.Demo;

/// <summary>
/// A fictional festival for demo mode. Every artist and track is invented.
/// </summary>
/// <remarks>
/// The data exercises the real allocation rules: headliners, mid-card and small acts get different track
/// budgets, "Afterglow" is a collaboration in both Lunar Static's and Tiny Comet's top tracks (Tiny Comet,
/// the smaller act, claims it), and "DJ Unfound" has no Spotify match, so it lands in the skipped list.
/// </remarks>
public static class DemoLineup
{
    public const string FestivalName = "Driftwood Valley 2026";
    public const string Unmatched = "DJ Unfound";
    private const string CollabTrackId = "demo-afterglow";

    private static readonly DemoArtist[] Artists =
    [
        new("Lunar Static", 82,
            ["Afterglow (with Tiny Comet)", "Satellite Hearts", "Low Orbit", "Signal Fade", "Zero Gravity Love", "Moonwalk Protocol", "Static Bloom", "Blue Hour"],
            ["Night Shift (Single)", "Echo Chamber"]),
        new("The Velvet Hours", 76,
            ["Slow Motion Summer", "Cassette Girl", "Paper Crowns", "Heatwave Radio", "Golden Static", "Last Dance at the Lido", "Honey Tape"],
            ["Glass Coast"]),
        new("Kaleo Drift", 71,
            ["Tidewater", "Sunburnt Frequencies", "Salt & Neon", "Undertow", "Coral Signal", "Offshore"],
            ["High Tide Theory", "Driftline"]),
        new("Glasshouse Radio", 64,
            ["Prism Break", "Greenhouse Effect", "Window Seat", "Fog Machine", "Refraction"],
            ["Conservatory"]),
        new("Marrow & Moth", 58,
            ["Lanternfly", "Bone China", "Wax Wings", "Porchlight Hymn"],
            ["Night Garden"]),
        new("Nova Kestrel", 52,
            ["Hover", "Thermal", "Wingspan", "Updraft"],
            ["Glide Path"]),
        new("Palm Circuit", 47,
            ["Voltage Palms", "Modular Paradise", "Circuit Breaker Beach"],
            ["Solder & Sand"]),
        new("Juniper Vale", 43,
            ["Evergreen Static", "Pine Needle Pop", "Woodsmoke"],
            ["First Frost"]),
        new("Saltwater Arcade", 36,
            ["Pinball Tide", "High Score Heart", "Token Machine"],
            ["Extra Life"]),
        new("Tiny Comet", 29,
            ["Afterglow (with Tiny Comet)", "Small Planet", "Tail Light"],
            ["Stardust Demo"]),
        new("Fennel", 22,
            ["Herb Garden", "Bitter Sweet"],
            ["Aniseed"]),
        new("Orchid Tapes", 18,
            ["Greenhouse Tapes", "Petal Loop"],
            []),
    ];

    /// <summary>
    /// The lineup as Claude would return it: headliners first, the way posters list them.
    /// "Fennel b2b Orchid Tapes" is split into two artists by the extraction prompt's rules.
    /// </summary>
    public static IReadOnlyList<string> ArtistNames { get; } =
        [.. Artists.Select(a => a.Name).Take(9), Unmatched, .. Artists.Select(a => a.Name).Skip(9)];

    public static DemoArtist? Find(string name) =>
        Artists.FirstOrDefault(a => ArtistNameMatcher.IsMatch(name, a.Name));

    public record DemoArtist(string Name, int Popularity, string[] TopTracks, string[] RecentTracks)
    {
        public string Id => $"demo-{Slug(Name)}";

        public ArtistCandidates ToCandidates() => new(
            Id, Name, Popularity,
            [.. TopTracks.Select(ToTrack)],
            [.. RecentTracks.Select(ToTrack)]);

        private TrackInfo ToTrack(string title) => new(
            SpotifyTrackId: title.StartsWith("Afterglow") ? CollabTrackId : $"{Id}-{Slug(title)}",
            Name: title,
            ArtistName: Name,
            AlbumName: null,
            DurationMs: DurationFor(title));
    }

    private static string Slug(string s) =>
        string.Concat(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');

    /// <summary>
    /// A plausible 2:30-4:30 duration that is the same on every run (string.GetHashCode is randomized per process)
    /// </summary>
    private static int DurationFor(string title)
    {
        var hash = title.Aggregate(17u, (h, c) => unchecked(h * 31 + c));
        return 150_000 + (int)(hash % 120) * 1_000;
    }
}
