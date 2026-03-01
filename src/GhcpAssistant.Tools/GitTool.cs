using System.Diagnostics;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class GitTool : IAssistantTool
{
    private readonly string _workingDirectory;

    public GitTool(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public string Name => "git";
    public string Description => "Run git operations: status, diff, log, commit.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!.ToLowerInvariant();

        var args = action switch
        {
            "status" => "status --short",
            "diff" => "diff",
            "log" => "log --oneline -20",
            "commit" => $"commit -m \"{parameters.GetProperty("message").GetString()}\"",
            _ => throw new ArgumentException($"Unknown git action '{action}'.")
        };

        return await RunGitAsync(args, ct);
    }

    private async Task<string> RunGitAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return $"Exit code: {process.ExitCode}\n{stdout}\n{stderr}".Trim();
    }
}
