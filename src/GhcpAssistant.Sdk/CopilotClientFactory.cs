using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

// Replace StubCopilotClientFactory with a real implementation when GitHub.Copilot.SDK becomes available on NuGet.

/// <summary>Factory that creates a Copilot client wrapper.</summary>
public interface ICopilotClientFactory
{
    /// <summary>Create a new Copilot client.</summary>
    Task<ICopilotClientWrapper> CreateAsync(CancellationToken ct = default);
}

/// <summary>Wrapper around a Copilot client that can create sessions.</summary>
public interface ICopilotClientWrapper : IAsyncDisposable
{
    /// <summary>Create a new session with the specified model and tools.</summary>
    Task<ICopilotSessionWrapper> CreateSessionAsync(string model, IReadOnlyList<IAssistantTool> tools, CancellationToken ct = default);
}

/// <summary>Wrapper around a Copilot session that can send and receive messages.</summary>
public interface ICopilotSessionWrapper : IAsyncDisposable
{
    /// <summary>Send a user message and stream back session events.</summary>
    IAsyncEnumerable<SessionEvent> SendMessageAsync(string message, CancellationToken ct = default);

    /// <summary>Send the result of a tool invocation back to the session.</summary>
    Task SendToolResultAsync(string toolCallId, string result, CancellationToken ct = default);
}

/// <summary>Base record for events emitted during a session turn.</summary>
public abstract record SessionEvent;

/// <summary>A streamed text delta from the model.</summary>
public sealed record TextDeltaEvent(string Text) : SessionEvent;

/// <summary>A request from the model to invoke a tool.</summary>
public sealed record ToolCallRequestEvent(string ToolCallId, string ToolName, JsonElement Arguments) : SessionEvent;

/// <summary>Signals that the current turn is complete.</summary>
public sealed record TurnCompleteEvent() : SessionEvent;
