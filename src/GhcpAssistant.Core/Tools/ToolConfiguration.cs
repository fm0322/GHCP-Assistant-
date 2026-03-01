namespace GhcpAssistant.Core.Tools;

/// <summary>
/// Represents a tool that the assistant has discovered or configured.
/// Tracks its approval status so that users can authorize new tools before they are used.
/// </summary>
public sealed class ToolConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique tool name (matches <see cref="IAssistantTool.Name"/>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description of the tool.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Fully-qualified type name of the tool class (used for discovery/instantiation).</summary>
    public string ToolTypeName { get; set; } = string.Empty;

    /// <summary>Current approval status.</summary>
    public ToolApprovalStatus ApprovalStatus { get; set; } = ToolApprovalStatus.Pending;

    /// <summary>When the tool configuration was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the tool was approved (null if not yet approved).</summary>
    public DateTime? ApprovedAt { get; set; }
}
