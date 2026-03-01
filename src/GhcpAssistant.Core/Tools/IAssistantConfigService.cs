namespace GhcpAssistant.Core.Tools;

/// <summary>
/// Manages the assistant's global configuration.
/// Only users with the <see cref="UserRole.Humaniser"/> role are permitted to modify the config.
/// </summary>
public interface IAssistantConfigService
{
    /// <summary>Retrieve the current configuration.</summary>
    Task<AssistantConfig> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Update the configuration. Only callers with <see cref="UserRole.Humaniser"/> role are allowed.
    /// Throws <see cref="UnauthorizedAccessException"/> if the caller is not a Humaniser.
    /// </summary>
    Task<AssistantConfig> UpdateConfigAsync(UserRole callerRole, AssistantConfig updatedConfig, CancellationToken ct = default);
}
