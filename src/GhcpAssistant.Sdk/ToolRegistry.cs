using System.Collections.Concurrent;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Maintains a dictionary of registered tools and dispatches invocations by name.
/// </summary>
public sealed class ToolRegistry
{
    private readonly ConcurrentDictionary<string, IAssistantTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a tool instance.</summary>
    public void Register(IAssistantTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (!_tools.TryAdd(tool.Name, tool))
            throw new InvalidOperationException($"A tool named '{tool.Name}' is already registered.");
    }

    /// <summary>Return all registered tool metadata.</summary>
    public IReadOnlyList<IAssistantTool> GetRegisteredTools() => _tools.Values.ToList().AsReadOnly();

    /// <summary>Invoke a tool by name.</summary>
    public async Task<string> InvokeAsync(string toolName, JsonElement args, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            throw new KeyNotFoundException($"No tool registered with the name '{toolName}'.");

        return await tool.ExecuteAsync(args, ct);
    }
}
