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

        // TODO: Integrate a real search API (Bing, Google, Tavily, etc.)
        // For now, return a placeholder indicating the feature is stubbed.
        await Task.CompletedTask;
        return $"[WebSearch stub] No search API configured. Query was: \"{query}\". " +
               "Configure a search provider in appsettings.json to enable live results.";
    }
}
