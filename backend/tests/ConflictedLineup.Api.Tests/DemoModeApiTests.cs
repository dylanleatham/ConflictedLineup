using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConflictedLineup.Api.Demo;
using ConflictedLineup.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ConflictedLineup.Api.Tests;

/// <summary>
/// Drives the real HTTP pipeline (routing, validation, JSON, server-sent events) in demo mode
/// </summary>
public class DemoModeApiTests : IClassFixture<DemoModeApiTests.DemoApp>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public DemoModeApiTests(DemoApp app)
    {
        _client = app.CreateClient();
    }

    [Fact]
    public async Task Config_reports_demo_mode()
    {
        var config = await _client.GetFromJsonAsync<JsonElement>("/api/config");

        Assert.True(config.GetProperty("demoMode").GetBoolean());
    }

    [Fact]
    public async Task Festival_search_returns_the_demo_lineup()
    {
        var response = await _client.PostAsJsonAsync("/api/extraction/festival", new { festivalName = "Anything", year = 2026 });
        var result = await response.Content.ReadFromJsonAsync<FestivalSearchResult>(Json);

        Assert.Equal(DemoLineup.FestivalName, result!.FestivalName);
        Assert.Equal(DemoLineup.ArtistNames, result.Artists.Select(a => a.Name));
    }

    [Fact]
    public async Task Streamed_selection_reports_progress_in_order_then_completes()
    {
        var names = DemoLineup.ArtistNames.ToList();
        var response = await _client.PostAsJsonAsync("/api/tracks/select/stream", new { artistNames = names, spotifyAccessToken = "demo" });

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var events = (await response.Content.ReadAsStringAsync())
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(e => JsonDocument.Parse(e["data: ".Length..]).RootElement)
            .ToList();

        var progress = events.SkipLast(1).ToList();
        Assert.All(progress, e => Assert.Equal("progress", e.GetProperty("type").GetString()));

        // Searching counts 1..N over every lineup name, in order
        var searching = progress.Where(e => e.GetProperty("phase").GetString() == "Searching").ToList();
        Assert.Equal(Enumerable.Range(1, names.Count), searching.Select(e => e.GetProperty("current").GetInt32()));

        var complete = events[^1];
        Assert.Equal("complete", complete.GetProperty("type").GetString());

        var result = complete.GetProperty("result").Deserialize<TrackSelectionResponse>(Json)!;
        Assert.Equal([DemoLineup.Unmatched], result.Skipped.Select(s => s.Name));
        Assert.Equal("Lunar Static", result.Artists[0].ArtistName); // headliner first

        // The shared collab goes to the smaller act
        var tinyComet = result.Artists.Single(a => a.ArtistName == "Tiny Comet");
        var lunarStatic = result.Artists.Single(a => a.ArtistName == "Lunar Static");
        Assert.Contains(tinyComet.TopTracks, t => t.Name.StartsWith("Afterglow"));
        Assert.DoesNotContain(lunarStatic.TopTracks, t => t.Name.StartsWith("Afterglow"));
    }

    [Theory]
    [InlineData("/api/tracks/select", """{"artistNames":[],"spotifyAccessToken":"t"}""")]
    [InlineData("/api/tracks/select", """{"artistNames":["A"],"spotifyAccessToken":""}""")]
    [InlineData("/api/tracks/select/stream", """{"artistNames":[],"spotifyAccessToken":"t"}""")]
    [InlineData("/api/extraction/festival", """{"festivalName":" "}""")]
    [InlineData("/api/extraction/poster", """{"imageBase64":"aGk=","mediaType":"image/gif"}""")]
    [InlineData("/api/playlist/create", """{"festivalName":"F","artists":[],"spotifyAccessToken":"t"}""")]
    public async Task Invalid_requests_are_rejected(string url, string body)
    {
        var response = await _client.PostAsync(url, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_check_responds()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public sealed class DemoApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("DEMO_MODE", "true");
        }
    }
}
