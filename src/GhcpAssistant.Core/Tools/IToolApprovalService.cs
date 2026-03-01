namespace GhcpAssistant.Core.Tools;

/// <summary>
/// Manages the approval workflow for dynamically discovered tools.
/// Users must authorize new tools before they can be used, unless the
/// <see cref="AssistantConfig.AutoApproveTools"/> override is enabled.
/// </summary>
public interface IToolApprovalService
{
    /// <summary>
    /// Request approval for a discovered tool.
    /// If <see cref="AssistantConfig.AutoApproveTools"/> is <c>true</c>, the tool is approved immediately.
    /// Otherwise, it remains in <see cref="ToolApprovalStatus.Pending"/> until the user authorizes it.
    /// </summary>
    Task<ToolConfiguration> RequestApprovalAsync(string name, string description, string toolTypeName, CancellationToken ct = default);

    /// <summary>Approve a pending tool so it can be registered and used.</summary>
    Task<ToolConfiguration> ApproveToolAsync(Guid toolId, CancellationToken ct = default);

    /// <summary>Reject a pending tool.</summary>
    Task<ToolConfiguration> RejectToolAsync(Guid toolId, CancellationToken ct = default);

    /// <summary>Return all tools awaiting user approval.</summary>
    Task<IReadOnlyList<ToolConfiguration>> GetPendingApprovalsAsync(CancellationToken ct = default);

    /// <summary>Get the current approval status of a tool.</summary>
    Task<ToolApprovalStatus> GetToolStatusAsync(Guid toolId, CancellationToken ct = default);
}
