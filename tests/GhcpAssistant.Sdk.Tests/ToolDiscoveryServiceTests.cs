using GhcpAssistant.Core.Tools;
using GhcpAssistant.Sdk;

namespace GhcpAssistant.Sdk.Tests;

public class ToolDiscoveryServiceTests
{
    [Fact]
    public async Task RegisterDiscoverableTool_AddsToolToAvailableList()
    {
        var service = new ToolDiscoveryService();

        service.RegisterDiscoverableTool("test_tool", "A test tool", "TestToolType");

        var available = await service.GetAvailableToolsAsync();
        Assert.Single(available);
        Assert.Equal("test_tool", available[0].Name);
        Assert.Equal("A test tool", available[0].Description);
        Assert.Equal("TestToolType", available[0].ToolTypeName);
    }

    [Fact]
    public async Task DiscoverToolsAsync_MatchesByName()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("file_system", "Read and write files", "FileSystemTool");
        service.RegisterDiscoverableTool("shell", "Execute shell commands", "ShellTool");

        var results = await service.DiscoverToolsAsync("file");

        Assert.Single(results);
        Assert.Equal("file_system", results[0].Name);
    }

    [Fact]
    public async Task DiscoverToolsAsync_MatchesByDescription()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("file_system", "Read and write files", "FileSystemTool");
        service.RegisterDiscoverableTool("shell", "Execute shell commands", "ShellTool");

        var results = await service.DiscoverToolsAsync("commands");

        Assert.Single(results);
        Assert.Equal("shell", results[0].Name);
    }

    [Fact]
    public async Task DiscoverToolsAsync_CaseInsensitiveMatching()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("git", "Run git operations", "GitTool");

        var results = await service.DiscoverToolsAsync("GIT");

        Assert.Single(results);
        Assert.Equal("git", results[0].Name);
    }

    [Fact]
    public async Task DiscoverToolsAsync_NoMatch_ReturnsEmpty()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("file_system", "Read and write files", "FileSystemTool");

        var results = await service.DiscoverToolsAsync("database");

        Assert.Empty(results);
    }

    [Fact]
    public async Task DiscoverToolsAsync_MultipleMatches_ReturnsAll()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("git", "Run git operations", "GitTool");
        service.RegisterDiscoverableTool("github", "Query GitHub API", "GitHubTool");

        var results = await service.DiscoverToolsAsync("git");

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_EmptyByDefault()
    {
        var service = new ToolDiscoveryService();

        var available = await service.GetAvailableToolsAsync();

        Assert.Empty(available);
    }

    [Fact]
    public void RegisterDiscoverableTool_EmptyName_ThrowsArgumentException()
    {
        var service = new ToolDiscoveryService();

        Assert.Throws<ArgumentException>(
            () => service.RegisterDiscoverableTool("", "desc", "type"));
    }

    [Fact]
    public void RegisterDiscoverableTool_EmptyToolTypeName_ThrowsArgumentException()
    {
        var service = new ToolDiscoveryService();

        Assert.Throws<ArgumentException>(
            () => service.RegisterDiscoverableTool("name", "desc", ""));
    }

    [Fact]
    public async Task DiscoverToolsAsync_EmptyQuery_ThrowsArgumentException()
    {
        var service = new ToolDiscoveryService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DiscoverToolsAsync(""));
    }

    [Fact]
    public async Task DiscoverToolsAsync_AllToolsHavePendingStatus()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("test_tool", "A test tool", "TestToolType");

        var results = await service.DiscoverToolsAsync("test");

        Assert.All(results, r => Assert.Equal(ToolApprovalStatus.Pending, r.ApprovalStatus));
    }

    [Fact]
    public async Task RegisterDiscoverableTool_DuplicateName_DoesNotThrow()
    {
        var service = new ToolDiscoveryService();
        service.RegisterDiscoverableTool("tool1", "desc1", "type1");

        // Duplicate registration should silently skip (TryAdd)
        service.RegisterDiscoverableTool("tool1", "desc2", "type2");

        var available = await service.GetAvailableToolsAsync();
        Assert.Single(available);
        Assert.Equal("desc1", available[0].Description); // Original retained
    }
}
