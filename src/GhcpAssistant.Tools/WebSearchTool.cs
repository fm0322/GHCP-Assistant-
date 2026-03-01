using System.Text;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class WebSearchTool : IAssistantTool
{
    private readonly HttpClient _httpClient;

    public WebSearchTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public string Name => "web_search";
    public string Description => "Search the web and return a summary of results.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var query = parameters.GetProperty("query").GetString()!;

        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var sb = new StringBuilder();
        sb.AppendLine($"Search results for: \"{query}\"");
        sb.AppendLine();

        var hasContent = false;

        var abstractText = root.TryGetProperty("AbstractText", out var at) ? at.GetString() : null;
        var abstractUrl = root.TryGetProperty("AbstractURL", out var au) ? au.GetString() : null;
        var abstractSource = root.TryGetProperty("AbstractSource", out var asrc) ? asrc.GetString() : null;

        if (!string.IsNullOrWhiteSpace(abstractText))
        {
            sb.AppendLine($"**{abstractSource}**: {abstractText}");
            if (!string.IsNullOrWhiteSpace(abstractUrl))
                sb.AppendLine($"  URL: {abstractUrl}");
            sb.AppendLine();
            hasContent = true;
        }

        if (root.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
        {
            var count = 0;
            foreach (var topic in topics.EnumerateArray())
            {
                if (count >= 5) break;

                var text = topic.TryGetProperty("Text", out var t) ? t.GetString() : null;
                var firstUrl = topic.TryGetProperty("FirstURL", out var fu) ? fu.GetString() : null;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine($"• {text}");
                    if (!string.IsNullOrWhiteSpace(firstUrl))
                        sb.AppendLine($"  {firstUrl}");
                    count++;
                    hasContent = true;
                }
            }
        }

        if (!hasContent)
            sb.AppendLine("No instant results found. Try refining your query.");

        return sb.ToString().TrimEnd();
    }
}
