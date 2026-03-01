using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.History;
using GhcpAssistant.Core.Sessions;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Orchestrates a Copilot session: reads user input, streams model responses,
/// and dispatches tool calls via the <see cref="ToolRegistry"/>.
/// Optionally persists conversation history when an <see cref="IConversationHistoryService"/> is provided.
/// </summary>
public sealed class SessionManager
{
    private readonly ICopilotClientFactory _clientFactory;
    private readonly IInputChannel _inputChannel;
    private readonly ToolRegistry _toolRegistry;
    private readonly SessionOptions _options;
    private readonly IConversationHistoryService? _historyService;

    public SessionManager(
        ICopilotClientFactory clientFactory,
        IInputChannel inputChannel,
        ToolRegistry toolRegistry,
        SessionOptions options,
        IConversationHistoryService? historyService = null)
    {
        _clientFactory = clientFactory;
        _inputChannel = inputChannel;
        _toolRegistry = toolRegistry;
        _options = options;
        _historyService = historyService;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var client = await _clientFactory.CreateAsync(ct);
        await using var session = await client.CreateSessionAsync(
            _options.Model, _toolRegistry.GetRegisteredTools(), ct);

        ConversationSession? historySession = null;
        if (_historyService is not null)
        {
            historySession = await _historyService.CreateSessionAsync(ct: ct);
        }

        await foreach (var userMessage in _inputChannel.ReadMessagesAsync(ct))
        {
            if (_historyService is not null && historySession is not null)
            {
                await _historyService.AddMessageAsync(
                    historySession.Id, MessageRole.User, userMessage, ct);
            }

            var responseBuilder = new System.Text.StringBuilder();

            await foreach (var evt in session.SendMessageAsync(userMessage, ct))
            {
                switch (evt)
                {
                    case TextDeltaEvent delta:
                        await _inputChannel.WriteResponseAsync(delta.Text, ct);
                        responseBuilder.Append(delta.Text);
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

            if (_historyService is not null && historySession is not null && responseBuilder.Length > 0)
            {
                await _historyService.AddMessageAsync(
                    historySession.Id, MessageRole.Assistant, responseBuilder.ToString(), ct);
            }
        }
    }
}
