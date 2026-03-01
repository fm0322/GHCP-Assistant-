using System.Text.Json;
using GhcpAssistant.Core.Tools;
using GhcpAssistant.Sdk;

namespace GhcpAssistant.Sdk.Tests;

public class ToolRegistryTests
{
    private sealed class FakeTool : IAssistantTool
    {
        public string Name { get; }
        public string Description { get; }
        private readonly string _result;

        public FakeTool(string name, string description = "A fake tool", string result = "ok")
        {
            Name = name;
            Description = description;
            _result = result;
        }

        public Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    [Fact]
    public void Register_AndGetRegisteredTools_ReturnsTool()
    {
        var registry = new ToolRegistry();
        var tool = new FakeTool("test_tool");

        registry.Register(tool);

        var tools = registry.GetRegisteredTools();
        Assert.Single(tools);
        Assert.Equal("test_tool", tools[0].Name);
    }

    [Fact]
    public async Task InvokeAsync_RegisteredTool_ReturnsExpectedResult()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("my_tool", result: "hello world"));

        using var doc = JsonDocument.Parse("{}");
        var result = await registry.InvokeAsync("my_tool", doc.RootElement);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task InvokeAsync_UnregisteredTool_ThrowsKeyNotFoundException()
    {
        var registry = new ToolRegistry();

        using var doc = JsonDocument.Parse("{}");
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => registry.InvokeAsync("nonexistent", doc.RootElement));
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("dup_tool"));

        Assert.Throws<InvalidOperationException>(
            () => registry.Register(new FakeTool("dup_tool")));
    }

    [Fact]
    public void TryRegisterDiscovered_ApprovedTool_RegistersSuccessfully()
    {
        var registry = new ToolRegistry();
        var tool = new FakeTool("discovered_tool");

        var result = registry.TryRegisterDiscovered(tool, ToolApprovalStatus.Approved);

        Assert.True(result);
        Assert.True(registry.IsRegistered("discovered_tool"));
    }

    [Fact]
    public void TryRegisterDiscovered_PendingTool_DoesNotRegister()
    {
        var registry = new ToolRegistry();
        var tool = new FakeTool("pending_tool");

        var result = registry.TryRegisterDiscovered(tool, ToolApprovalStatus.Pending);

        Assert.False(result);
        Assert.False(registry.IsRegistered("pending_tool"));
    }

    [Fact]
    public void TryRegisterDiscovered_RejectedTool_DoesNotRegister()
    {
        var registry = new ToolRegistry();
        var tool = new FakeTool("rejected_tool");

        var result = registry.TryRegisterDiscovered(tool, ToolApprovalStatus.Rejected);

        Assert.False(result);
        Assert.False(registry.IsRegistered("rejected_tool"));
    }

    [Fact]
    public void TryRegisterDiscovered_DuplicateApproved_ReturnsFalse()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("existing_tool"));

        var result = registry.TryRegisterDiscovered(new FakeTool("existing_tool"), ToolApprovalStatus.Approved);

        Assert.False(result);
    }

    [Fact]
    public void IsRegistered_ExistingTool_ReturnsTrue()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("my_tool"));

        Assert.True(registry.IsRegistered("my_tool"));
    }

    [Fact]
    public void IsRegistered_NonExistingTool_ReturnsFalse()
    {
        var registry = new ToolRegistry();

        Assert.False(registry.IsRegistered("nonexistent"));
    }

    [Fact]
    public void IsRegistered_CaseInsensitive()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("My_Tool"));

        Assert.True(registry.IsRegistered("MY_TOOL"));
        Assert.True(registry.IsRegistered("my_tool"));
    }
}
