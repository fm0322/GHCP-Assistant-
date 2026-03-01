using System.Runtime.CompilerServices;
using System.Text.Json;
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk.Tests;

public class SessionManagerTests
{
    #region Mock implementations

    private sealed class MockCopilotClientFactory : ICopilotClientFactory
    {
        private readonly ICopilotClientWrapper _client;

        public MockCopilotClientFactory(ICopilotClientWrapper client) => _client = client;

        public Task<ICopilotClientWrapper> CreateAsync(CancellationToken ct = default)
            => Task.FromResult(_client);
    }

    private sealed class MockCopilotClientWrapper : ICopilotClientWrapper
    {
        private readonly ICopilotSessionWrapper _session;

        public MockCopilotClientWrapper(ICopilotSessionWrapper session) => _session = session;

        public Task<ICopilotSessionWrapper> CreateSessionAsync(
            string model, IReadOnlyList<IAssistantTool> tools, CancellationToken ct = default)
            => Task.FromResult(_session);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class MockCopilotSessionWrapper : ICopilotSessionWrapper
    {
        private readonly Func<string, IAsyncEnumerable<SessionEvent>> _onMessage;
        public List<(string ToolCallId, string Result)> ToolResults { get; } = [];

        public MockCopilotSessionWrapper(Func<string, IAsyncEnumerable<SessionEvent>> onMessage)
            => _onMessage = onMessage;

        public IAsyncEnumerable<SessionEvent> SendMessageAsync(string message, CancellationToken ct = default)
            => _onMessage(message);

        public Task SendToolResultAsync(string toolCallId, string result, CancellationToken ct = default)
        {
            ToolResults.Add((toolCallId, result));
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
            {
                yield return msg;
            }
            await Task.CompletedTask;
        }

        public Task WriteResponseAsync(string chunk, CancellationToken ct = default)
        {
            WrittenChunks.Add(chunk);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTool : IAssistantTool
    {
        public string Name { get; }
        public string Description => "A fake tool for testing";
        private readonly string _result;

        public FakeTool(string name, string result = "tool_result")
        {
            Name = name;
            _result = result;
        }

        public Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private static async IAsyncEnumerable<SessionEvent> YieldEvents(
        params SessionEvent[] events)
    {
        foreach (var evt in events)
        {
            yield return evt;
        }
        await Task.CompletedTask;
    }

    #endregion

    [Fact]
    public async Task TextDeltaEvents_AreForwardedToInputChannel()
    {
        var inputChannel = new MockInputChannel("hello");
        var session = new MockCopilotSessionWrapper(_ =>
            YieldEvents(
                new TextDeltaEvent("Hi "),
                new TextDeltaEvent("there!"),
                new TurnCompleteEvent()));
        var client = new MockCopilotClientWrapper(session);
        var factory = new MockCopilotClientFactory(client);
        var registry = new ToolRegistry();
        var options = new SessionOptions();

        var manager = new SessionManager(factory, inputChannel, registry, options);
        await manager.RunAsync();

        Assert.Contains("Hi ", inputChannel.WrittenChunks);
        Assert.Contains("there!", inputChannel.WrittenChunks);
        Assert.Contains("\n", inputChannel.WrittenChunks);
    }

    [Fact]
    public async Task ToolCallEvents_InvokeRegistryAndSendResultBack()
    {
        using var argsDoc = JsonDocument.Parse("{}");
        var argsElement = argsDoc.RootElement.Clone();

        var inputChannel = new MockInputChannel("run tool");
        var session = new MockCopilotSessionWrapper(_ =>
            YieldEvents(
                new ToolCallRequestEvent("call-1", "my_tool", argsElement),
                new TurnCompleteEvent()));
        var client = new MockCopilotClientWrapper(session);
        var factory = new MockCopilotClientFactory(client);
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("my_tool", result: "tool output"));
        var options = new SessionOptions();

        var manager = new SessionManager(factory, inputChannel, registry, options);
        await manager.RunAsync();

        Assert.Single(session.ToolResults);
        Assert.Equal("call-1", session.ToolResults[0].ToolCallId);
        Assert.Equal("tool output", session.ToolResults[0].Result);
    }

    [Fact]
    public async Task SessionEnds_WhenInputChannelYieldsNoMessages()
    {
        var inputChannel = new MockInputChannel(); // no messages
        var session = new MockCopilotSessionWrapper(_ =>
            YieldEvents(new TurnCompleteEvent()));
        var client = new MockCopilotClientWrapper(session);
        var factory = new MockCopilotClientFactory(client);
        var registry = new ToolRegistry();
        var options = new SessionOptions();

        var manager = new SessionManager(factory, inputChannel, registry, options);
        await manager.RunAsync(); // should complete without error

        Assert.Empty(inputChannel.WrittenChunks);
    }
}
