using Anthropic;
using Anthropic.Models.Beta.Messages;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

public interface IClaudeService
{
    Task<ArtistExtractionResult> ExtractArtistsFromPosterAsync(string imageBase64, string mediaType, CancellationToken cancel = default);
    Task<FestivalSearchResult> SearchFestivalLineupAsync(string festivalName, int? year, CancellationToken cancel = default);
}

/// <summary>
/// Finds a festival's lineup with Claude: from a poster image (vision), from a festival name (web search),
/// or both, where the poster identifies the festival and the web supplies the full, correctly spelled lineup.
/// </summary>
public class ClaudeService : IClaudeService
{
    private const string Model = "claude-opus-5-5";
    private const int MaxTokens = 16000;
    private const int MaxWebSearches = 5;

    private static readonly Lazy<string> ExtractionPrompt = new(() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Prompts", "extract-lineup.txt")));

    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(IConfiguration configuration, ILogger<ClaudeService> logger)
        : this(new AnthropicClient
        {
            ApiKey = configuration["ANTHROPIC_API_KEY"]
                ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not configured")
        }, logger)
    {
    }

    internal ClaudeService(AnthropicClient client, ILogger<ClaudeService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ArtistExtractionResult> ExtractArtistsFromPosterAsync(string imageBase64, string mediaType, CancellationToken cancel = default)
    {
        var lineup = await ExtractLineupAsync(
        [
            new BetaImageBlockParam { Source = new BetaBase64ImageSource { MediaType = mediaType, Data = imageBase64 } },
            new BetaTextBlockParam { Text = ExtractionPrompt.Value },
        ], cancel);

        if (lineup == null)
        {
            return new ArtistExtractionResult([], Warning: "Couldn't read a lineup from that poster");
        }

        return new ArtistExtractionResult(
            Artists: lineup.Artists.Select(name => new ArtistInfo(name)).ToList(),
            FestivalName: lineup.Festival,
            Source: lineup.Source,
            SourceUrl: lineup.SourceUrl);
    }

    public async Task<FestivalSearchResult> SearchFestivalLineupAsync(string festivalName, int? year, CancellationToken cancel = default)
    {
        var searchYear = year ?? DateTime.UtcNow.Year;

        // Same prompt as the poster path, told there is no image so it goes straight to searching
        var lineup = await ExtractLineupAsync(
        [
            new BetaTextBlockParam
            {
                Text = $"The user is looking for the lineup for {festivalName} {searchYear}. " +
                       $"No image provided - skip Step 1 and proceed directly to Step 2 (web search).\n\n{ExtractionPrompt.Value}"
            },
        ], cancel) ?? throw new InvalidOperationException("Claude's reply contained no lineup");

        return new FestivalSearchResult(
            FestivalName: lineup.Festival ?? $"{festivalName} {searchYear}",
            Year: searchYear,
            Artists: lineup.Artists.Select(name => new ArtistInfo(name)).ToList(),
            Sources: lineup.SourceUrl is { } url ? [url] : []);
    }

    private async Task<LineupExtractionResponse?> ExtractLineupAsync(List<BetaContentBlockParam> content, CancellationToken cancel)
    {
        var response = await _client.Beta.Messages.Create(new MessageCreateParams
        {
            Model = Model,
            MaxTokens = MaxTokens,
            OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            // If a safety classifier declines, the API re-runs the request on its default fallback model
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
            Tools = [new BetaToolUnion(new BetaWebSearchTool20260209 { MaxUses = MaxWebSearches })],
            Messages = [new BetaMessageParam { Role = Role.User, Content = content }],
        }, cancel);

        if (response.StopReason == "refusal")
        {
            _logger.LogWarning("Claude declined the lineup request: {Category}", response.StopDetails?.Category);
            return null;
        }

        var textBlocks = response.Content
            .Select(b => b.Value)
            .OfType<BetaTextBlock>()
            .Select(t => t.Text)
            .ToList();

        var lineup = LineupResponseParser.Parse(textBlocks);
        if (lineup == null)
        {
            _logger.LogError("No lineup JSON in Claude's reply (stop reason {StopReason}): {Text}",
                response.StopReason, string.Join("\n", textBlocks));
        }

        return lineup;
    }
}
