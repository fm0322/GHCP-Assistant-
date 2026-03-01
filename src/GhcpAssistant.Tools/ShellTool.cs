using System.Diagnostics;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class ShellTool : IAssistantTool
{
    private readonly HashSet<string> _allowedCommands;
    private readonly TimeSpan _timeout;

    public ShellTool(IEnumerable<string> allowedCommands, TimeSpan? timeout = null)
    {
        _allowedCommands = new HashSet<string>(allowedCommands, StringComparer.OrdinalIgnoreCase);
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public string Name => "shell";
    public string Description => "Execute a shell command from the allowed list.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var command = parameters.GetProperty("command").GetString()!;
        if (!_allowedCommands.Contains(command))
            throw new UnauthorizedAccessException($"Command '{command}' is not in the allow-list.");

        var arguments = parameters.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() ?? "" : "";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        try
        {
            await process.WaitForExitAsync(ct).WaitAsync(_timeout, ct);
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Command '{command}' exceeded the {_timeout.TotalSeconds}s timeout and was terminated.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return $"Exit code: {process.ExitCode}\n--- stdout ---\n{stdout}\n--- stderr ---\n{stderr}";
    }
}
