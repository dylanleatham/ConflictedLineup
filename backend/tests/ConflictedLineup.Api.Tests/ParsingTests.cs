using ConflictedLineup.Api.Controllers;
using ConflictedLineup.Api.Services;

namespace ConflictedLineup.Api.Tests;

public class ArtistNameMatcherTests
{
    [Theory]
    [InlineData("SVDDEN DEATH", "SVDDEN DEATH")]
    [InlineData("svdden death", "SVDDEN DEATH")]      // lineups shout, Spotify doesn't
    [InlineData("The Chemical Brothers", "Chemical Brothers")]
    [InlineData("Hol!", "HOL!")]
    [InlineData("Beyoncé", "Beyoncé")]                // accents are letters, not punctuation
    [InlineData(" Kaytranada ", "KAYTRANADA")]
    public void Matches_the_same_artist_written_differently(string lineupName, string spotifyName)
    {
        Assert.True(ArtistNameMatcher.IsMatch(lineupName, spotifyName));
    }

    [Theory]
    [InlineData("Snake", "DJ Snake")]                  // containment alone isn't enough
    [InlineData("Hol!", "Wooli")]                      // Spotify's actual top result for "Hol!"
    [InlineData("Excision", "Excision Tribute Band")]
    [InlineData("!!!", "!!!")]                         // nothing left to compare after normalizing
    public void Rejects_different_artists(string lineupName, string spotifyName)
    {
        Assert.False(ArtistNameMatcher.IsMatch(lineupName, spotifyName));
    }

    [Fact]
    public void SanitizeQuery_strips_punctuation_that_confuses_spotify_search()
    {
        Assert.Equal("Hol", ArtistNameMatcher.SanitizeQuery("Hol!"));
        Assert.Equal("Marrow Moth", ArtistNameMatcher.SanitizeQuery("Marrow & Moth"));
    }
}

public class LineupResponseParserTests
{
    private const string Json = """{"festival":"Driftwood Valley 2026","source":"web","source_url":"https://example.com/lineup","artists":["Lunar Static","Tiny Comet"]}""";

    [Fact]
    public void Parses_bare_json()
    {
        var lineup = LineupResponseParser.Parse([Json]);

        Assert.NotNull(lineup);
        Assert.Equal("Driftwood Valley 2026", lineup.Festival);
        Assert.Equal("web", lineup.Source);
        Assert.Equal("https://example.com/lineup", lineup.SourceUrl);
        Assert.Equal(["Lunar Static", "Tiny Comet"], lineup.Artists);
    }

    [Fact]
    public void Finds_json_inside_a_markdown_fence_after_prose_split_across_blocks()
    {
        var lineup = LineupResponseParser.Parse(
        [
            "I found the official lineup announcement.",
            "Here it is:\n```json\n" + Json,
            "```",
        ]);

        Assert.Equal(["Lunar Static", "Tiny Comet"], lineup?.Artists);
    }

    [Fact]
    public void Drops_blank_artist_names_and_trims_the_rest()
    {
        var lineup = LineupResponseParser.Parse(["""{"festival":"F","artists":["  Fennel ","", "   "]}"""]);

        Assert.Equal(["Fennel"], lineup?.Artists);
    }

    [Theory]
    [InlineData("I couldn't find a lineup for that festival.")]
    [InlineData("{ this is not json }")]
    [InlineData("""{"festival":"No artists key"}""")]
    [InlineData("")]
    public void Returns_null_when_there_is_no_lineup(string text)
    {
        Assert.Null(LineupResponseParser.Parse([text]));
    }
}

public class PlaylistNameTests
{
    [Theory]
    [InlineData("Driftwood Valley", 2026, "Driftwood Valley 2026")]
    [InlineData("Driftwood Valley 2026", 2026, "Driftwood Valley 2026")] // year not repeated
    [InlineData("  Driftwood Valley ", null, "Driftwood Valley")]
    [InlineData(null, 2026, "My Festival Playlist")]
    [InlineData("   ", null, "My Festival Playlist")]
    public void Builds_name_from_festival_and_year(string? festival, int? year, string expected)
    {
        Assert.Equal(expected, PlaylistController.BuildPlaylistName(festival, year));
    }
}
