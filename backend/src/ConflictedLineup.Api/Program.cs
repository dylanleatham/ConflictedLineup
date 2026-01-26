using ConflictedLineup.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for frontend development and production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://conflictedlineup.com",
                  "https://www.conflictedlineup.com"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add controllers
builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<IClaudeService, ClaudeService>();

var app = builder.Build();

app.UseCors();

// Health check endpoint (supports GET and HEAD for Front Door probes)
app.MapMethods("/api/health", new[] { "GET", "HEAD" }, () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow.ToString("o")
    });
});

// Map controllers
app.MapControllers();

app.Run();
