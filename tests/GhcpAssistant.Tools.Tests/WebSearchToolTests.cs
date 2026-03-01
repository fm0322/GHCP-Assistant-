using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class WebSearchToolTests
{
    [Fact]
    public void Name_IsWebSearch()
    {
        var tool = new WebSearchTool();
        Assert.Equal("web_search", tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var tool = new WebSearchTool();
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }

    [Fact]
    public async Task Execute_ReturnsPlaceholderWithQuery()
    {
        var tool = new WebSearchTool();
        var parameters = JsonDocument.Parse("""{"query":"test search"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("test search", result);
        Assert.Contains("WebSearch stub", result);
    }
}
