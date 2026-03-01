using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class GitHubToolTests
{
    [Fact]
    public void Name_IsGitHub()
    {
        var tool = new GitHubTool();
        Assert.Equal("github", tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var tool = new GitHubTool();
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }

    [Fact]
    public async Task UnknownAction_ThrowsArgumentException()
    {
        var tool = new GitHubTool();
        var parameters = JsonDocument.Parse("""{"action":"unknown","owner":"test","repo":"test"}""").RootElement;

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.ExecuteAsync(parameters, CancellationToken.None));
    }
}
