using System.Runtime.CompilerServices;
using System.Text.Json;
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk.Tests;

public class StubCopilotClientFactoryTests
{
    private sealed class FakeTool : IAssistantTool
    {
        public string Name { get; }
        public string Description => "A fake tool";
        public FakeTool(string name) => Name = name;
        public Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct = default)
            => Task.FromResult("ok");
    }

    [Fact]
    public async Task CreateAsync_ReturnsNonNullClient()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        Assert.NotNull(client);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsNonNullSession()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", []);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task SendMessageAsync_EmitsTextDeltaAndTurnComplete()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", []);

        var events = new List<SessionEvent>();
        await foreach (var evt in session.SendMessageAsync("hello"))
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.IsType<TextDeltaEvent>(events[0]);
        Assert.IsType<TurnCompleteEvent>(events[1]);
    }

    [Fact]
    public async Task SendMessageAsync_EchoesUserMessage()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", []);

        var events = new List<SessionEvent>();
        await foreach (var evt in session.SendMessageAsync("test input"))
            events.Add(evt);

        var delta = (TextDeltaEvent)events[0];
        Assert.Contains("test input", delta.Text);
    }

    [Fact]
    public async Task SendMessageAsync_IncludesModelName()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", []);

        var events = new List<SessionEvent>();
        await foreach (var evt in session.SendMessageAsync("hi"))
            events.Add(evt);

        var delta = (TextDeltaEvent)events[0];
        Assert.Contains("gpt-4o", delta.Text);
    }

    [Fact]
    public async Task SendMessageAsync_ListsRegisteredTools()
    {
        var tools = new List<IAssistantTool> { new FakeTool("my_tool") };
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", tools);

        var events = new List<SessionEvent>();
        await foreach (var evt in session.SendMessageAsync("hi"))
            events.Add(evt);

        var delta = (TextDeltaEvent)events[0];
        Assert.Contains("my_tool", delta.Text);
    }

    [Fact]
    public async Task SendToolResultAsync_DoesNotThrow()
    {
        var factory = new StubCopilotClientFactory();
        await using var client = await factory.CreateAsync();
        await using var session = await client.CreateSessionAsync("gpt-4o", []);

        await session.SendToolResultAsync("call-1", "some result");
    }

    [Fact]
    public async Task AgentLoop_WorksEndToEnd_WithSessionManager()
    {
        var factory = new StubCopilotClientFactory();
        var inputChannel = new MockInputChannel("hello");
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("test_tool"));
        var options = new SessionOptions();

        var manager = new SessionManager(factory, inputChannel, registry, options);
        await manager.RunAsync();

        Assert.NotEmpty(inputChannel.WrittenChunks);
        Assert.Contains(inputChannel.WrittenChunks, c => c.Contains("hello"));
    }

    private sealed class MockInputChannel : IInputChannel
    {
        private readonly string[] _messages;
        public List<string> WrittenChunks { get; } = [];

        public MockInputChannel(params string[] messages) => _messages = messages;

        public async IAsyncEnumerable<string> ReadMessagesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var msg in _messages)
                yield return msg;
            await Task.Yield();
        }

        public Task WriteResponseAsync(string chunk, CancellationToken ct = default)
        {
            WrittenChunks.Add(chunk);
            return Task.CompletedTask;
        }
    }
}
