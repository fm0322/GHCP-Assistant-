using System.Runtime.CompilerServices;
using System.Text;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// A local stub implementation of <see cref="ICopilotClientFactory"/> that drives the
/// agent loop without requiring the <c>GitHub.Copilot.SDK</c> NuGet package.
/// It echoes user messages and lists available tools so that the full
/// <see cref="SessionManager"/> pipeline is exercised end-to-end.
/// Replace this class with a real SDK-backed factory when the package ships.
/// </summary>
public sealed class StubCopilotClientFactory : ICopilotClientFactory
{
    public Task<ICopilotClientWrapper> CreateAsync(CancellationToken ct = default)
        => Task.FromResult<ICopilotClientWrapper>(new StubCopilotClientWrapper());
}

internal sealed class StubCopilotClientWrapper : ICopilotClientWrapper
{
    public Task<ICopilotSessionWrapper> CreateSessionAsync(
        string model, IReadOnlyList<IAssistantTool> tools, CancellationToken ct = default)
        => Task.FromResult<ICopilotSessionWrapper>(new StubCopilotSessionWrapper(model, tools));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class StubCopilotSessionWrapper : ICopilotSessionWrapper
{
    private readonly string _model;
    private readonly IReadOnlyList<IAssistantTool> _tools;

    public StubCopilotSessionWrapper(string model, IReadOnlyList<IAssistantTool> tools)
    {
        _model = model;
        _tools = tools;
    }

    public async IAsyncEnumerable<SessionEvent> SendMessageAsync(
        string message, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Stub · {_model}] Received: \"{message}\"");

        if (_tools.Count > 0)
        {
            sb.AppendLine("Available tools:");
            foreach (var tool in _tools)
                sb.AppendLine($"  • {tool.Name} — {tool.Description}");
        }

        sb.Append("(Connect the GitHub Copilot SDK to enable real LLM responses.)");

        yield return new TextDeltaEvent(sb.ToString());
        yield return new TurnCompleteEvent();

        await Task.CompletedTask; // keep the method async-compatible
    }

    public Task SendToolResultAsync(string toolCallId, string result, CancellationToken ct = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
