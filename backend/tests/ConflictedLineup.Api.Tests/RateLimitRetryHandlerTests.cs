using System.Net;
using ConflictedLineup.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SpotifyAPI.Web.Http;

namespace ConflictedLineup.Api.Tests;

public class RateLimitRetryHandlerTests
{
    private readonly List<TimeSpan> _waits = [];
    private readonly RateLimitRetryHandler _handler;

    public RateLimitRetryHandlerTests()
    {
        _handler = new RateLimitRetryHandler(NullLogger<RateLimitRetryHandler>.Instance, (wait, _) =>
        {
            _waits.Add(wait);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Waits_for_retry_after_then_returns_the_successful_retry()
    {
        var retries = 0;
        var response = await _handler.HandleRetry(new FakeRequest(), Response(429, retryAfter: "3"), (_, _) =>
        {
            retries++;
            return Task.FromResult(Response(200));
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, retries);
        Assert.Equal([TimeSpan.FromSeconds(3)], _waits);
    }

    [Fact]
    public async Task Gives_up_after_max_retries_and_returns_the_last_429()
    {
        var retries = 0;
        var response = await _handler.HandleRetry(new FakeRequest(), Response(429, retryAfter: "1"), (_, _) =>
        {
            retries++;
            return Task.FromResult(Response(429, retryAfter: "1"));
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(RateLimitRetryHandler.MaxRetries, retries);
    }

    [Fact]
    public async Task Fails_fast_when_spotify_asks_for_a_long_backoff()
    {
        var response = await _handler.HandleRetry(new FakeRequest(), Response(429, retryAfter: "86400"), (_, _) =>
            throw new InvalidOperationException("should not retry"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Empty(_waits);
    }

    [Fact]
    public async Task Leaves_other_responses_alone()
    {
        var response = await _handler.HandleRetry(new FakeRequest(), Response(404), (_, _) =>
            throw new InvalidOperationException("should not retry"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Defaults_the_wait_when_retry_after_is_missing()
    {
        await _handler.HandleRetry(new FakeRequest(), Response(429), (_, _) => Task.FromResult(Response(200)));

        Assert.Equal([TimeSpan.FromSeconds(5)], _waits);
    }

    private static IResponse Response(int status, string? retryAfter = null) => new FakeResponse
    {
        StatusCode = (HttpStatusCode)status,
        Headers = retryAfter == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["retry-after"] = retryAfter },
    };

    private sealed class FakeResponse : IResponse
    {
        public object? Body => null;
        public required IReadOnlyDictionary<string, string> Headers { get; init; }
        public required HttpStatusCode StatusCode { get; init; }
        public string? ContentType => null;
    }

    private sealed class FakeRequest : IRequest
    {
        public Uri BaseAddress => new("https://api.spotify.com/v1/");
        public Uri Endpoint => new("search", UriKind.Relative);
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
        public HttpMethod Method => HttpMethod.Get;
        public object? Body { get; set; }
    }
}
