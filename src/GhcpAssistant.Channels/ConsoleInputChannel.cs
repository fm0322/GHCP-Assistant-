using GhcpAssistant.Core.Channels;

namespace GhcpAssistant.Channels;

/// <summary>
/// An input channel that reads from the console and writes to the console.
/// </summary>
public sealed class ConsoleInputChannel : IInputChannel
{
    public async IAsyncEnumerable<string> ReadMessagesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            Console.Write("\n> ");
            var line = await Task.Run(() => Console.ReadLine(), ct);

            if (line is null || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                yield break;

            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }

    public Task WriteResponseAsync(string chunk, CancellationToken ct = default)
    {
        Console.Write(chunk);
        return Task.CompletedTask;
    }
}
