using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class FileSystemTool : IAssistantTool
{
    private readonly string _rootDirectory;

    public FileSystemTool(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string Name => "file_system";
    public string Description => "Read, write, or list files and directories within the workspace.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!;
        var path = parameters.GetProperty("path").GetString()!;
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, path));

        if (!fullPath.StartsWith(_rootDirectory, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Path traversal detected.");

        return action.ToLowerInvariant() switch
        {
            "read" => await File.ReadAllTextAsync(fullPath, ct),
            "write" => await WriteFileAsync(fullPath, parameters, ct),
            "list" => string.Join('\n', Directory.GetFileSystemEntries(fullPath)),
            _ => throw new ArgumentException($"Unknown action '{action}'.")
        };
    }

    private static async Task<string> WriteFileAsync(string fullPath, JsonElement parameters, CancellationToken ct)
    {
        var content = parameters.GetProperty("content").GetString()!;
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null) Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(fullPath, content, ct);
        return $"Wrote {content.Length} characters to {Path.GetFileName(fullPath)}.";
    }
}
