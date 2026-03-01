namespace GhcpAssistant.Core.Tools;

/// <summary>
/// Discovers tools that the assistant can use to perform requested actions.
/// The assistant uses this service to find tools it needs when it doesn't already have them.
/// </summary>
public interface IToolDiscoveryService
{
    /// <summary>
    /// Search for tools matching a natural-language query describing the desired capability.
    /// </summary>
    Task<IReadOnlyList<ToolConfiguration>> DiscoverToolsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Return all tools that have been discovered and are available for registration.
    /// </summary>
    Task<IReadOnlyList<ToolConfiguration>> GetAvailableToolsAsync(CancellationToken ct = default);

    /// <summary>
    /// Register a tool type so it can be discovered by the assistant.
    /// If a tool with the same name is already registered, the call is silently ignored
    /// and the original registration is retained.
    /// </summary>
    void RegisterDiscoverableTool(string name, string description, string toolTypeName);
}
