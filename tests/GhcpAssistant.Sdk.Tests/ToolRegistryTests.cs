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

        var result = await registry.InvokeAsync("my_tool", JsonDocument.Parse("{}").RootElement);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task InvokeAsync_UnregisteredTool_ThrowsKeyNotFoundException()
    {
        var registry = new ToolRegistry();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => registry.InvokeAsync("nonexistent", JsonDocument.Parse("{}").RootElement));
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("dup_tool"));

        Assert.Throws<InvalidOperationException>(
            () => registry.Register(new FakeTool("dup_tool")));
    }
}
