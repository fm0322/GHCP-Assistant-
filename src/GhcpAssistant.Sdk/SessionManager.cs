using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Orchestrates a Copilot session: reads user input, streams model responses,
/// and dispatches tool calls via the <see cref="ToolRegistry"/>.
/// </summary>
public sealed class SessionManager
{
    private readonly ICopilotClientFactory _clientFactory;
    private readonly IInputChannel _inputChannel;
    private readonly ToolRegistry _toolRegistry;
    private readonly SessionOptions _options;

    public SessionManager(
        ICopilotClientFactory clientFactory,
        IInputChannel inputChannel,
        ToolRegistry toolRegistry,
        SessionOptions options)
    {
        _clientFactory = clientFactory;
        _inputChannel = inputChannel;
        _toolRegistry = toolRegistry;
        _options = options;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var client = await _clientFactory.CreateAsync(ct);
        await using var session = await client.CreateSessionAsync(
            _options.Model, _toolRegistry.GetRegisteredTools(), ct);

        await foreach (var userMessage in _inputChannel.ReadMessagesAsync(ct))
        {
            await foreach (var evt in session.SendMessageAsync(userMessage, ct))
            {
                switch (evt)
                {
                    case TextDeltaEvent delta:
                        await _inputChannel.WriteResponseAsync(delta.Text, ct);
                        break;

                    case ToolCallRequestEvent toolCall:
                        var result = await _toolRegistry.InvokeAsync(
                            toolCall.ToolName, toolCall.Arguments, ct);
                        await session.SendToolResultAsync(toolCall.ToolCallId, result, ct);
                        break;

                    case TurnCompleteEvent:
                        await _inputChannel.WriteResponseAsync("\n", ct);
                        break;
                }
            }
        }
    }
}
