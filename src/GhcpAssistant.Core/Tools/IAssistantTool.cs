using System.Text.Json;

namespace GhcpAssistant.Core.Tools;

/// <summary>Contract that all agent-callable tools must implement.</summary>
public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct = default);
}
