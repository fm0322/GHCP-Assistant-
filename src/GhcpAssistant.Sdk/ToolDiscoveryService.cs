using System.Collections.Concurrent;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Discovers tools that the assistant can use based on a natural-language query.
/// Tools are registered as discoverable at startup and matched by name or description keywords.
/// </summary>
public sealed class ToolDiscoveryService : IToolDiscoveryService
{
    private readonly ConcurrentDictionary<string, ToolConfiguration> _discoverableTools = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void RegisterDiscoverableTool(string name, string description, string toolTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolTypeName);

        var config = new ToolConfiguration
        {
            Name = name,
            Description = description ?? string.Empty,
            ToolTypeName = toolTypeName,
            ApprovalStatus = ToolApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _discoverableTools.TryAdd(name, config);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ToolConfiguration>> DiscoverToolsAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matches = _discoverableTools.Values
            .Where(tool => queryTerms.Any(term =>
                tool.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                tool.Description.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<ToolConfiguration>>(matches.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ToolConfiguration>> GetAvailableToolsAsync(CancellationToken ct = default)
    {
        var tools = _discoverableTools.Values.ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<ToolConfiguration>>(tools);
    }
}
