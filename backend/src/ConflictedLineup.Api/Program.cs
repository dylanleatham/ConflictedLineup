var builder = WebApplication.CreateBuilder(args);

// Add CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.Run();
