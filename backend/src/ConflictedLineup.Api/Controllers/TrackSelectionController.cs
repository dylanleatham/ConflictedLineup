using System.Text.Json;
using System.Threading.Channels;
using ConflictedLineup.Api.Models;
using ConflictedLineup.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConflictedLineup.Api.Controllers;

[ApiController]
[Route("api/tracks")]
public class TrackSelectionController : ControllerBase
{
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISpotifyTrackService _trackService;
    private readonly ILogger<TrackSelectionController> _logger;

    public TrackSelectionController(ISpotifyTrackService trackService, ILogger<TrackSelectionController> logger)
    {
        _trackService = trackService;
        _logger = logger;
    }

    [HttpPost("select")]
    public async Task<ActionResult<TrackSelectionResponse>> SelectTracks([FromBody] TrackSelectionRequest request)
    {
        if (Validate(request) is { } error)
        {
            return BadRequest(new { error });
        }

        try
        {
            return Ok(await _trackService.SelectTracksAsync(request.ArtistNames, request.SpotifyAccessToken, cancel: HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during track selection");
            return SpotifyErrors.ToResult(ex, "Failed to select tracks for artists");
        }
    }

    /// <summary>
    /// Same as <see cref="SelectTracks"/>, streamed as server-sent events: a "progress" event per artist per
    /// phase, then one "complete" event with the result, or an "error" event.
    /// </summary>
    /// <remarks>
    /// Selection reports progress from whatever thread its awaits resume on, and a response body can only be
    /// written by one writer at a time. Progress therefore goes into a channel that this request drains in
    /// order; nothing writes to the response concurrently, and "complete" is always the last event.
    /// </remarks>
    [HttpPost("select/stream")]
    public async Task SelectTracksStream([FromBody] TrackSelectionRequest request)
    {
        if (Validate(request) is { } error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error });
            return;
        }

        var cancel = HttpContext.RequestAborted;
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var updates = Channel.CreateUnbounded<ArtistProgressUpdate>(new UnboundedChannelOptions { SingleReader = true });
        var progress = new SynchronousProgress<ArtistProgressUpdate>(update => updates.Writer.TryWrite(update));

        var selection = _trackService.SelectTracksAsync(request.ArtistNames, request.SpotifyAccessToken, progress, cancel);
        _ = selection.ContinueWith(_ => updates.Writer.Complete(), TaskScheduler.Default);

        await foreach (var update in updates.Reader.ReadAllAsync(cancel))
        {
            await WriteEventAsync(new { type = "progress", update.Current, update.Total, update.ArtistName, update.Phase }, cancel);
        }

        try
        {
            await WriteEventAsync(new { type = "complete", result = await selection }, cancel);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during streaming track selection");
            await WriteEventAsync(new { type = "error", message = SpotifyErrors.Describe(ex, "Failed to select tracks for artists") }, cancel);
        }
    }

    private async Task WriteEventAsync(object payload, CancellationToken cancel)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload, SseJsonOptions)}\n\n", cancel);
        await Response.Body.FlushAsync(cancel);
    }

    private static string? Validate(TrackSelectionRequest request)
    {
        if (request.ArtistNames is not { Count: > 0 })
            return "Artist names are required";
        if (string.IsNullOrWhiteSpace(request.SpotifyAccessToken))
            return "Spotify access token is required";
        return null;
    }

    /// <summary>
    /// Invokes the handler on the reporting thread. The framework's Progress&lt;T&gt; posts to the thread
    /// pool instead, which in ASP.NET Core means reports can run out of order or after the work has finished.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
