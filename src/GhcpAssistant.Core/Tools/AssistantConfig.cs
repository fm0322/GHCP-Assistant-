namespace GhcpAssistant.Core.Tools;

/// <summary>
/// System-wide configuration for the assistant.
/// Only users with the <see cref="UserRole.Humaniser"/> role are permitted to edit this.
/// </summary>
public sealed class AssistantConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When <c>true</c>, newly discovered tools are automatically approved without user authorization.
    /// When <c>false</c> (default), users must explicitly approve each new tool.
    /// </summary>
    public bool AutoApproveTools { get; set; }

    /// <summary>When the configuration was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Role of the user who last updated the configuration.</summary>
    public string UpdatedByRole { get; set; } = nameof(UserRole.Humaniser);
}
