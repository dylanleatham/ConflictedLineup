using System.Text.Json;
using ConflictedLineup.Api.Models;

namespace ConflictedLineup.Api.Services;

/// <summary>
/// Pulls the lineup JSON out of Claude's reply.
/// </summary>
/// <remarks>
/// Structured outputs would guarantee the shape, but they are rejected alongside citations, and web search
/// results always carry citations. So the prompt asks for JSON and this finds it: around web search the reply
/// can span several text blocks, with prose or a markdown fence around the object.
/// </remarks>
public static class LineupResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <returns>The parsed lineup, or null when the text holds no lineup object</returns>
    public static LineupExtractionResponse? Parse(IEnumerable<string> textBlocks)
    {
        var text = string.Join("\n", textBlocks);

        // The object runs from the first '{' to the last '}'; anything outside is prose or fence markers
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            var lineup = JsonSerializer.Deserialize<LineupExtractionResponse>(text[start..(end + 1)], JsonOptions);
            if (lineup?.Artists == null)
            {
                return null;
            }

            return lineup with
            {
                Artists = lineup.Artists
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .ToList()
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
