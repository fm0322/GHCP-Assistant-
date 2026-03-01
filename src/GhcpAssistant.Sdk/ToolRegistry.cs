using System.Collections.Concurrent;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Maintains a dictionary of registered tools and dispatches invocations by name.
/// Supports both static registration and dynamic registration of discovered tools
/// (subject to approval via <see cref="IToolApprovalService"/>).
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

    /// <summary>
    /// Attempt to register a dynamically discovered tool.
    /// The tool is only registered if its approval status is <see cref="ToolApprovalStatus.Approved"/>.
    /// Returns <c>true</c> if the tool was registered, <c>false</c> if it was not approved or already exists.
    /// </summary>
    public bool TryRegisterDiscovered(IAssistantTool tool, ToolApprovalStatus approvalStatus)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (approvalStatus != ToolApprovalStatus.Approved)
            return false;

        return _tools.TryAdd(tool.Name, tool);
    }

    /// <summary>Check whether a tool with the given name is already registered.</summary>
    public bool IsRegistered(string toolName) => _tools.ContainsKey(toolName);

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
