namespace GhcpAssistant.Core.Tools;

/// <summary>Approval status for a dynamically discovered tool.</summary>
public enum ToolApprovalStatus
{
    /// <summary>Tool has been discovered but not yet approved by the user.</summary>
    Pending,

    /// <summary>Tool has been approved for use.</summary>
    Approved,

    /// <summary>Tool has been rejected by the user.</summary>
    Rejected
}
