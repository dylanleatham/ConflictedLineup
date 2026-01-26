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
    private const string Model = "claude-sonnet-4-5-20250514";
    private const int MaxTokens = 2048;

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
            // Read prompt from file
            var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "poster-extraction.txt");
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
                Stream = false
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var responseText = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("No text response from Claude");

            // Parse JSON response
            var result = JsonSerializer.Deserialize<ArtistExtractionResult>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ArtistExtractionResult(
                Artists: new List<ArtistInfo>(),
                Warning: "Failed to parse Claude response"
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
            // Read prompt from file
            var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "festival-search.txt");
            var promptTemplate = await File.ReadAllTextAsync(promptPath);

            // Replace placeholders
            var yearText = year?.ToString() ?? "latest";
            var prompt = promptTemplate
                .Replace("{FESTIVAL_NAME}", festivalName)
                .Replace("{YEAR}", yearText);

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
                Stream = false
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var responseText = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("No text response from Claude");

            // Parse JSON response
            var result = JsonSerializer.Deserialize<FestivalSearchResult>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new InvalidOperationException("Failed to parse festival search response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching festival lineup for {FestivalName}", festivalName);
            throw;
        }
    }
}
