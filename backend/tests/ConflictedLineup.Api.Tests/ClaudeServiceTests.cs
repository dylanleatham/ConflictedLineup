using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using ConflictedLineup.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConflictedLineup.Api.Tests;

/// <summary>
/// Runs ClaudeService against a fake HTTP handler: checks the request it sends and how it reads the reply,
/// without an API key or network access.
/// </summary>
public class ClaudeServiceTests
{
    [Fact]
    public async Task Festival_search_sends_web_search_request_and_parses_the_reply()
    {
        var http = new FakeAnthropicHandler(Reply(
            "Found the official announcement.",
            "```json\n{\"festival\":\"Driftwood Valley 2026\",\"source\":\"web\",\"source_url\":\"https://example.com\",\"artists\":[\"Lunar Static\",\"Tiny Comet\"]}\n```"));

        var result = await Service(http).SearchFestivalLineupAsync("Driftwood Valley", 2026);

        Assert.Equal("Driftwood Valley 2026", result.FestivalName);
        Assert.Equal(["Lunar Static", "Tiny Comet"], result.Artists.Select(a => a.Name));
        Assert.Equal(["https://example.com"], result.Sources);

        var body = http.RequestBody!.RootElement;
        Assert.Equal("claude-opus-5-5", body.GetProperty("model").GetString());
        Assert.Equal("default", body.GetProperty("fallbacks").GetString());
        Assert.Equal("web_search_20260209", body.GetProperty("tools")[0].GetProperty("type").GetString());
        Assert.Contains("Driftwood Valley 2026", body.GetProperty("messages")[0].GetRawText());
        Assert.Contains("server-side-fallback-2026-07-01", http.BetaHeader);
    }

    [Fact]
    public async Task Poster_extraction_sends_the_image_before_the_prompt()
    {
        var http = new FakeAnthropicHandler(Reply("""{"festival":"Driftwood Valley 2026","source":"image","artists":["Fennel"]}"""));

        var result = await Service(http).ExtractArtistsFromPosterAsync("aGVsbG8=", "image/png");

        Assert.Equal("image", result.Source);
        Assert.Equal(["Fennel"], result.Artists.Select(a => a.Name));

        var content = http.RequestBody!.RootElement.GetProperty("messages")[0].GetProperty("content");
        Assert.Equal("image", content[0].GetProperty("type").GetString());
        Assert.Equal("image/png", content[0].GetProperty("source").GetProperty("media_type").GetString());
        Assert.Equal("text", content[1].GetProperty("type").GetString());
    }

    [Fact]
    public async Task Poster_extraction_warns_instead_of_throwing_when_no_lineup_comes_back()
    {
        var http = new FakeAnthropicHandler(Reply("That image doesn't look like a festival poster."));

        var result = await Service(http).ExtractArtistsFromPosterAsync("aGVsbG8=", "image/png");

        Assert.Empty(result.Artists);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task Festival_search_throws_when_no_lineup_comes_back()
    {
        var http = new FakeAnthropicHandler(Reply("I couldn't find a lineup for that festival."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(http).SearchFestivalLineupAsync("Nowhere Fest", 2026));
    }

    private static ClaudeService Service(FakeAnthropicHandler http) =>
        new(new AnthropicClient { ApiKey = "test-key", HttpClient = new HttpClient(http), MaxRetries = 0 },
            NullLogger<ClaudeService>.Instance);

    private static string Reply(params string[] textBlocks) => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "claude-opus-5-5",
        content = textBlocks.Select(text => new { type = "text", text, citations = (object?)null }),
        stop_reason = "end_turn",
        stop_sequence = (string?)null,
        usage = new { input_tokens = 10, output_tokens = 10 },
    });

    private sealed class FakeAnthropicHandler(string responseJson) : HttpMessageHandler
    {
        public JsonDocument? RequestBody { get; private set; }
        public string BetaHeader { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            BetaHeader = request.Headers.TryGetValues("anthropic-beta", out var values) ? string.Join(",", values) : "";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
