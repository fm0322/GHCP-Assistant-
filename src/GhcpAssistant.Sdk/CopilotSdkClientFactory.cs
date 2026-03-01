using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using GhcpAssistant.Core.Tools;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Production implementation of <see cref="ICopilotClientFactory"/> backed by the
/// <c>GitHub.Copilot.SDK</c> NuGet package. It manages the Copilot CLI process
/// lifecycle and translates between the SDK's event-based API and the project's
/// <see cref="IAsyncEnumerable{SessionEvent}"/> abstraction.
/// </summary>
public sealed class CopilotSdkClientFactory : ICopilotClientFactory
{
    private readonly CopilotClientOptions? _options;

    public CopilotSdkClientFactory(CopilotClientOptions? options = null)
        => _options = options;

    public async Task<ICopilotClientWrapper> CreateAsync(CancellationToken ct = default)
    {
        var client = new CopilotClient(_options);
        await client.StartAsync();
        return new SdkClientWrapper(client);
    }
}

internal sealed class SdkClientWrapper : ICopilotClientWrapper
{
    private readonly CopilotClient _client;

    public SdkClientWrapper(CopilotClient client) => _client = client;

    public async Task<ICopilotSessionWrapper> CreateSessionAsync(
        string model, IReadOnlyList<IAssistantTool> tools, CancellationToken ct = default)
    {
        var aiFunctions = tools.Select(tool =>
            AIFunctionFactory.Create(
                async ([Description("Tool parameters")] JsonElement parameters) =>
                    await tool.ExecuteAsync(parameters, ct),
                tool.Name,
                tool.Description)).ToList();

        var session = await _client.CreateSessionAsync(new SessionConfig
        {
            Model = model,
            Streaming = true,
            Tools = aiFunctions,
            OnPermissionRequest = PermissionHandler.ApproveAll
        });

        return new SdkSessionWrapper(session);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.StopAsync();
        await _client.DisposeAsync();
    }
}

internal sealed class SdkSessionWrapper : ICopilotSessionWrapper
{
    private readonly CopilotSession _session;

    public SdkSessionWrapper(CopilotSession session) => _session = session;

    public async IAsyncEnumerable<SessionEvent> SendMessageAsync(
        string message, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<SessionEvent>();

        using var subscription = _session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    channel.Writer.TryWrite(new TextDeltaEvent(delta.Data.DeltaContent));
                    break;
                case SessionIdleEvent:
                    channel.Writer.TryWrite(new TurnCompleteEvent());
                    channel.Writer.TryComplete();
                    break;
                case SessionErrorEvent error:
                    channel.Writer.TryComplete(
                        new InvalidOperationException(error.Data.Message));
                    break;
            }
        });

        // Fire-and-forget: SendAndWaitAsync blocks until idle, while events
        // stream into the channel for the caller to consume.
        var sendTask = _session.SendAndWaitAsync(new MessageOptions { Prompt = message });

        await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            yield return evt;

        await sendTask;
    }

    /// <summary>
    /// No-op — the SDK handles tool results internally via <see cref="AIFunction"/> callbacks.
    /// </summary>
    public Task SendToolResultAsync(string toolCallId, string result, CancellationToken ct = default)
        => Task.CompletedTask;

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();
}
