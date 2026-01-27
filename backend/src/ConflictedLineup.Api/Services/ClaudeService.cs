using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using ConflictedLineup.Api.Models;
using System.Text.Json;

namespace ConflictedLineup.Api.Services;

public interface IClaudeService
{
    Task<ArtistExtractionResult> ExtractArtistsFromPosterAsync(string imageBase64, string mediaType);
    Task<FestivalSearchResult> SearchFestivalLineupAsync(string festivalName, int? year);
}

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;
    private const string Model = "claude-sonnet-4-20250514";
    private const int MaxTokens = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ClaudeService(IConfiguration configuration, ILogger<ClaudeService> logger)
    {
        var apiKey = configuration["ANTHROPIC_API_KEY"]
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not configured");

        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<ArtistExtractionResult> ExtractArtistsFromPosterAsync(string imageBase64, string mediaType)
    {
        try
        {
            // Read unified prompt - handles both image analysis and web search with fallback
            var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "extract-lineup.txt");
            var promptText = await File.ReadAllTextAsync(promptPath);

            // Create vision message with image
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new ImageContent
                        {
                            Source = new ImageSource
                            {
                                MediaType = mediaType,
                                Data = imageBase64
                            }
                        },
                        new TextContent { Text = promptText }
                    }
                }
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = Model,
                MaxTokens = MaxTokens,
                Stream = false,
                Tools = new List<Anthropic.SDK.Common.Tool>
                {
                    ServerTools.GetWebSearchTool(maxUses: 5)
                },
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto }
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);

            // With web search, response may have multiple text blocks - combine them all
            var allTextContent = response.Content
                .OfType<TextContent>()
                .Select(tc => tc.Text)
                .ToList();

            _logger.LogInformation("Claude response has {Count} text blocks", allTextContent.Count);

            // Join all text and find JSON
            var combinedText = string.Join("\n", allTextContent);
            _logger.LogDebug("Combined response text: {Text}", combinedText);

            var responseText = ExtractJson(combinedText);

            if (string.IsNullOrWhiteSpace(responseText) || !responseText.StartsWith("{"))
            {
                _logger.LogError("Could not extract JSON from response. Raw text: {Text}", combinedText);
                return new ArtistExtractionResult(
                    Artists: new List<ArtistInfo>(),
                    Warning: "Failed to parse Claude response - no JSON found"
                );
            }

            // Parse Claude's response format
            var rawResult = JsonSerializer.Deserialize<LineupExtractionResponse>(responseText, JsonOptions);

            if (rawResult == null)
            {
                return new ArtistExtractionResult(
                    Artists: new List<ArtistInfo>(),
                    Warning: "Failed to parse Claude response"
                );
            }

            // Map to ArtistExtractionResult
            var artists = rawResult.Artists
                .Select(name => new ArtistInfo(name, "high"))
                .ToList();

            return new ArtistExtractionResult(
                Artists: artists,
                FestivalName: rawResult.Festival,
                Source: rawResult.Source,
                SourceUrl: rawResult.SourceUrl
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response as JSON");
            return new ArtistExtractionResult(
                Artists: new List<ArtistInfo>(),
                Warning: "Failed to parse artist list from response"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting artists from poster");
            throw;
        }
    }

    public async Task<FestivalSearchResult> SearchFestivalLineupAsync(string festivalName, int? year)
    {
        try
        {
            // Read the same prompt used for poster extraction
            var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "extract-lineup.txt");
            var promptText = await File.ReadAllTextAsync(promptPath);

            // Prepend context so Claude skips Step 1 (no image provided)
            var yearText = year?.ToString() ?? DateTime.Now.Year.ToString();
            var prompt = $"The user is looking for the lineup for {festivalName} {yearText}. No image provided - skip Step 1 and proceed directly to Step 2 (web search).\n\n{promptText}";

            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = prompt }
                    }
                }
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = Model,
                MaxTokens = MaxTokens,
                Stream = false,
                Tools = new List<Anthropic.SDK.Common.Tool>
                {
                    ServerTools.GetWebSearchTool(maxUses: 5)
                },
                ToolChoice = new ToolChoice { Type = ToolChoiceType.Auto }
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);

            // With web search, response may have multiple text blocks - combine them all
            var allTextContent = response.Content
                .OfType<TextContent>()
                .Select(tc => tc.Text)
                .ToList();

            _logger.LogInformation("Claude response has {Count} text blocks", allTextContent.Count);

            // Join all text and find JSON
            var combinedText = string.Join("\n", allTextContent);
            _logger.LogDebug("Combined response text: {Text}", combinedText);

            var responseText = ExtractJson(combinedText);

            if (string.IsNullOrWhiteSpace(responseText) || !responseText.StartsWith("{"))
            {
                _logger.LogError("Could not extract JSON from response. Raw text: {Text}", combinedText);
                throw new InvalidOperationException("No valid JSON found in Claude response");
            }

            // Parse Claude's response format
            var rawResult = JsonSerializer.Deserialize<LineupExtractionResponse>(responseText, JsonOptions);

            if (rawResult == null)
            {
                throw new InvalidOperationException("Failed to parse festival search response");
            }

            // Map to FestivalSearchResult
            var artists = rawResult.Artists
                .Select(name => new ArtistInfo(name, "high"))
                .ToList();

            var sources = rawResult.SourceUrl != null
                ? new List<string> { rawResult.SourceUrl }
                : new List<string>();

            // Parse year from festival name or use provided year
            var resultYear = year ?? DateTime.Now.Year;

            return new FestivalSearchResult(
                FestivalName: rawResult.Festival,
                Year: resultYear,
                Artists: artists,
                Sources: sources
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching festival lineup for {FestivalName}", festivalName);
            throw;
        }
    }

    /// <summary>
    /// Extract JSON object from response text, handling potential markdown code blocks
    /// </summary>
    private static string ExtractJson(string text)
    {
        text = text.Trim();

        // Handle markdown code blocks
        if (text.StartsWith("```json"))
        {
            text = text[7..];
        }
        else if (text.StartsWith("```"))
        {
            text = text[3..];
        }

        if (text.EndsWith("```"))
        {
            text = text[..^3];
        }

        // Find the JSON object
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            text = text[startIndex..(endIndex + 1)];
        }

        return text.Trim();
    }
}
