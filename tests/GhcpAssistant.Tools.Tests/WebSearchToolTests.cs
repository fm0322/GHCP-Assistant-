using System.Net;
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
    public async Task Execute_WithAbstract_ReturnsAbstractText()
    {
        var json = """
        {
            "AbstractText": "C# is a general-purpose programming language.",
            "AbstractSource": "Wikipedia",
            "AbstractURL": "https://en.wikipedia.org/wiki/C_Sharp_(programming_language)",
            "RelatedTopics": []
        }
        """;
        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var tool = new WebSearchTool(new HttpClient(handler));
        var parameters = JsonDocument.Parse("""{"query":"C# programming"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("C# programming", result);
        Assert.Contains("Wikipedia", result);
        Assert.Contains("C# is a general-purpose programming language.", result);
        Assert.Contains("https://en.wikipedia.org", result);
    }

    [Fact]
    public async Task Execute_WithRelatedTopics_ReturnsThem()
    {
        var json = """
        {
            "AbstractText": "",
            "AbstractSource": "",
            "AbstractURL": "",
            "RelatedTopics": [
                { "Text": "Result one about dotnet", "FirstURL": "https://example.com/1" },
                { "Text": "Result two about csharp", "FirstURL": "https://example.com/2" }
            ]
        }
        """;
        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var tool = new WebSearchTool(new HttpClient(handler));
        var parameters = JsonDocument.Parse("""{"query":"test search"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("Result one about dotnet", result);
        Assert.Contains("Result two about csharp", result);
        Assert.Contains("https://example.com/1", result);
    }

    [Fact]
    public async Task Execute_LimitsRelatedTopicsToFive()
    {
        var topics = string.Join(",", Enumerable.Range(1, 8)
            .Select(i => $"{{ \"Text\": \"Topic {i}\", \"FirstURL\": \"https://example.com/{i}\" }}"));
        var json = $@"{{
            ""AbstractText"": """",
            ""AbstractSource"": """",
            ""AbstractURL"": """",
            ""RelatedTopics"": [{topics}]
        }}";
        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var tool = new WebSearchTool(new HttpClient(handler));
        var parameters = JsonDocument.Parse("""{"query":"many results"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("Topic 5", result);
        Assert.DoesNotContain("Topic 6", result);
    }

    [Fact]
    public async Task Execute_NoResults_ReturnsNoResultsMessage()
    {
        var json = """
        {
            "AbstractText": "",
            "AbstractSource": "",
            "AbstractURL": "",
            "RelatedTopics": []
        }
        """;
        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var tool = new WebSearchTool(new HttpClient(handler));
        var parameters = JsonDocument.Parse("""{"query":"obscure query"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("No instant results found", result);
    }

    [Fact]
    public async Task Execute_IncludesQueryInOutput()
    {
        var json = """{ "AbstractText": "", "RelatedTopics": [] }""";
        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var tool = new WebSearchTool(new HttpClient(handler));
        var parameters = JsonDocument.Parse("""{"query":"my specific query"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("my specific query", result);
    }

    private sealed class FakeHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
