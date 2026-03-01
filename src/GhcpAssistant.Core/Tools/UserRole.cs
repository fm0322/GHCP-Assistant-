namespace GhcpAssistant.Core.Tools;

/// <summary>Roles that control access to assistant configuration.</summary>
public enum UserRole
{
    /// <summary>Standard user — can authorize individual tools but cannot edit global config.</summary>
    User,

    /// <summary>Humaniser (admin) — the only role permitted to edit the assistant configuration.</summary>
    Humaniser
}
