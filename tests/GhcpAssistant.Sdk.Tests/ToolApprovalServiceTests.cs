using GhcpAssistant.Core.Tools;
using GhcpAssistant.Sdk;

namespace GhcpAssistant.Sdk.Tests;

public class ToolApprovalServiceTests
{
    private static AssistantConfigService CreateConfigService(bool autoApprove = false)
    {
        return new AssistantConfigService(new AssistantConfig { AutoApproveTools = autoApprove });
    }

    [Fact]
    public async Task RequestApprovalAsync_AutoApproveDisabled_ReturnsPending()
    {
        var configService = CreateConfigService(autoApprove: false);
        var service = new ToolApprovalService(configService);

        var result = await service.RequestApprovalAsync("test_tool", "A test tool", "TestToolType");

        Assert.Equal(ToolApprovalStatus.Pending, result.ApprovalStatus);
        Assert.Null(result.ApprovedAt);
        Assert.Equal("test_tool", result.Name);
        Assert.Equal("A test tool", result.Description);
        Assert.Equal("TestToolType", result.ToolTypeName);
    }

    [Fact]
    public async Task RequestApprovalAsync_AutoApproveEnabled_ReturnsApproved()
    {
        var configService = CreateConfigService(autoApprove: true);
        var service = new ToolApprovalService(configService);

        var result = await service.RequestApprovalAsync("test_tool", "A test tool", "TestToolType");

        Assert.Equal(ToolApprovalStatus.Approved, result.ApprovalStatus);
        Assert.NotNull(result.ApprovedAt);
    }

    [Fact]
    public async Task ApproveToolAsync_PendingTool_ReturnsApproved()
    {
        var configService = CreateConfigService(autoApprove: false);
        var service = new ToolApprovalService(configService);
        var pending = await service.RequestApprovalAsync("tool1", "Test", "TestType");

        var approved = await service.ApproveToolAsync(pending.Id);

        Assert.Equal(ToolApprovalStatus.Approved, approved.ApprovalStatus);
        Assert.NotNull(approved.ApprovedAt);
    }

    [Fact]
    public async Task RejectToolAsync_PendingTool_ReturnsRejected()
    {
        var configService = CreateConfigService(autoApprove: false);
        var service = new ToolApprovalService(configService);
        var pending = await service.RequestApprovalAsync("tool1", "Test", "TestType");

        var rejected = await service.RejectToolAsync(pending.Id);

        Assert.Equal(ToolApprovalStatus.Rejected, rejected.ApprovalStatus);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsPendingToolsOnly()
    {
        var configService = CreateConfigService(autoApprove: false);
        var service = new ToolApprovalService(configService);
        var tool1 = await service.RequestApprovalAsync("tool1", "Test1", "TestType1");
        var tool2 = await service.RequestApprovalAsync("tool2", "Test2", "TestType2");
        await service.ApproveToolAsync(tool1.Id);

        var pending = await service.GetPendingApprovalsAsync();

        Assert.Single(pending);
        Assert.Equal("tool2", pending[0].Name);
    }

    [Fact]
    public async Task GetToolStatusAsync_ReturnsCorrectStatus()
    {
        var configService = CreateConfigService(autoApprove: false);
        var service = new ToolApprovalService(configService);
        var tool = await service.RequestApprovalAsync("tool1", "Test", "TestType");

        var status = await service.GetToolStatusAsync(tool.Id);

        Assert.Equal(ToolApprovalStatus.Pending, status);
    }

    [Fact]
    public async Task ApproveToolAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var configService = CreateConfigService();
        var service = new ToolApprovalService(configService);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ApproveToolAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RejectToolAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var configService = CreateConfigService();
        var service = new ToolApprovalService(configService);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RejectToolAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetToolStatusAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var configService = CreateConfigService();
        var service = new ToolApprovalService(configService);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetToolStatusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RequestApprovalAsync_EmptyName_ThrowsArgumentException()
    {
        var configService = CreateConfigService();
        var service = new ToolApprovalService(configService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RequestApprovalAsync("", "desc", "type"));
    }

    [Fact]
    public async Task RequestApprovalAsync_EmptyToolTypeName_ThrowsArgumentException()
    {
        var configService = CreateConfigService();
        var service = new ToolApprovalService(configService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RequestApprovalAsync("name", "desc", ""));
    }
}
