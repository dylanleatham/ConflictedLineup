using System.Net;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace ConflictedLineup.Api.Services;

public interface ISpotifyClientFactory
{
    /// <summary>
    /// Create a Spotify client that acts as the user who owns <paramref name="accessToken"/>.
    /// </summary>
    ISpotifyClient Create(string accessToken);
}

public class SpotifyClientFactory : ISpotifyClientFactory
{
    private readonly RateLimitRetryHandler _retryHandler;

    public SpotifyClientFactory(ILogger<RateLimitRetryHandler> logger)
    {
        _retryHandler = new RateLimitRetryHandler(logger);
    }

    public ISpotifyClient Create(string accessToken) =>
        new SpotifyClient(SpotifyClientConfig.CreateDefault(accessToken).WithRetryHandler(_retryHandler));
}

/// <summary>
/// Retries requests Spotify rejects with 429, waiting as long as its Retry-After header asks.
/// Every Spotify call goes through this one handler, so services don't wrap calls in their own retry loops.
/// </summary>
/// <remarks>
/// Apps in Spotify's development mode can be told to back off for hours. Waiting that out would hold the
/// user's request open indefinitely, so a Retry-After beyond <see cref="MaxWait"/> fails fast instead.
/// </remarks>
public class RateLimitRetryHandler : IRetryHandler
{
    public const int MaxRetries = 3;
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(5);

    private readonly ILogger<RateLimitRetryHandler> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public RateLimitRetryHandler(ILogger<RateLimitRetryHandler> logger, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _logger = logger;
        _delay = delay ?? Task.Delay;
    }

    public async Task<IResponse> HandleRetry(
        IRequest request,
        IResponse response,
        IRetryHandler.RetryFunc retry,
        CancellationToken cancel = default)
    {
        for (var attempt = 1; attempt <= MaxRetries && response.StatusCode == HttpStatusCode.TooManyRequests; attempt++)
        {
            var wait = ParseRetryAfter(response) ?? DefaultWait;
            if (wait > MaxWait)
            {
                _logger.LogWarning("Spotify asked us to back off for {Seconds}s; giving up instead of waiting", wait.TotalSeconds);
                break;
            }

            _logger.LogWarning("Rate limited by Spotify. Waiting {Seconds}s before retry {Attempt}/{MaxRetries}",
                wait.TotalSeconds, attempt, MaxRetries);

            await _delay(wait, cancel);
            response = await retry(request, cancel);
        }

        return response;
    }

    internal static TimeSpan? ParseRetryAfter(IResponse response)
    {
        var header = response.Headers
            .FirstOrDefault(h => string.Equals(h.Key, "Retry-After", StringComparison.OrdinalIgnoreCase))
            .Value;

        return int.TryParse(header, out var seconds) && seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }
}
