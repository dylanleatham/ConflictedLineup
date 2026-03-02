using ConflictedLineup.Api.Services;
using DotNetEnv;

// Load .env file (searches current directory and parents)
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add CORS for frontend development and production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "http://localhost:5175",
                  "http://127.0.0.1:5173",
                  "http://127.0.0.1:5175",
                  "https://conflictedlineup.com",
                  "https://www.conflictedlineup.com",
                  "https://knucklehead.dev"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add controllers
builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<IClaudeService, ClaudeService>();

// Register Spotify services
builder.Services.AddScoped<ISpotifySearchService, SpotifySearchService>();
builder.Services.AddScoped<ISpotifyTopTracksService, SpotifyTopTracksService>();
builder.Services.AddScoped<ISpotifyRecentReleasesService, SpotifyRecentReleasesService>();
builder.Services.AddScoped<ISpotifyUserLibraryService, SpotifyUserLibraryService>();
builder.Services.AddScoped<ISpotifyTrackService, SpotifyTrackService>();
builder.Services.AddScoped<ISpotifyPlaylistService, SpotifyPlaylistService>();

var app = builder.Build();

app.UsePathBase("/conflicted");
app.UseRouting();
app.UseCors();
app.UseStaticFiles();

// Health check at root for platform probes (probes hit /healthz directly;
// UsePathBase is a no-op when the path doesn't start with /conflicted)
app.MapMethods("/healthz", new[] { "GET", "HEAD" }, () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow.ToString("o")
    });
});

// Map controllers
app.MapControllers();

// SPA fallback for client-side routes
app.MapFallbackToFile("index.html");

app.Run();
