using ConflictedLineup.Api.Demo;
using ConflictedLineup.Api.Services;
using DotNetEnv;

// Load .env file (searches current directory and parents)
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Demo mode serves a fictional festival from fixtures, so the whole flow runs without
// an Anthropic key or a Spotify account
var demoMode = builder.Configuration.GetValue<bool>("DEMO_MODE");

if (demoMode)
{
    builder.Services.AddSingleton<IClaudeService, DemoClaudeService>();
    builder.Services.AddSingleton<ISpotifyTrackService, DemoSpotifyTrackService>();
    builder.Services.AddSingleton<ISpotifyPlaylistService, DemoSpotifyPlaylistService>();
}
else
{
    builder.Services.AddSingleton<IClaudeService, ClaudeService>();
    builder.Services.AddSingleton<ISpotifyClientFactory, SpotifyClientFactory>();
    builder.Services.AddSingleton<ISpotifySearchService, SpotifySearchService>();
    builder.Services.AddSingleton<ISpotifyTopTracksService, SpotifyTopTracksService>();
    builder.Services.AddSingleton<ISpotifyRecentReleasesService, SpotifyRecentReleasesService>();
    builder.Services.AddSingleton<ISpotifyTrackService, SpotifyTrackService>();
    builder.Services.AddSingleton<ISpotifyPlaylistService, SpotifyPlaylistService>();
}

var app = builder.Build();

if (demoMode)
{
    app.Logger.LogWarning("DEMO_MODE is on: serving fixture data, not calling Claude or Spotify");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapMethods("/healthz", ["GET", "HEAD"], () => Results.Ok(new { status = "Healthy" }));

// Read by the SPA at startup, before it decides whether to show the Spotify login
app.MapGet("/api/config", () => new { demoMode });

app.MapControllers();

// SPA fallback for client-side routes
app.MapFallbackToFile("index.html");

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests
public partial class Program;
