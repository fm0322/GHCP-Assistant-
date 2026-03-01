namespace GhcpAssistant.Core.Channels;

/// <summary>Abstraction for user input/output channels (CLI, HTTP, etc.).</summary>
public interface IInputChannel
{
    /// <summary>Yields user messages as they arrive.</summary>
    IAsyncEnumerable<string> ReadMessagesAsync(CancellationToken ct = default);

    /// <summary>Writes a streamed response chunk to the user.</summary>
    Task WriteResponseAsync(string chunk, CancellationToken ct = default);
}
