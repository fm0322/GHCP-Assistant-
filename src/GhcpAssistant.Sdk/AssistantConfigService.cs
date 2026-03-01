using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Manages the assistant's global configuration.
/// Only users with the <see cref="UserRole.Humaniser"/> role are permitted to modify the config.
/// All other roles will receive an <see cref="UnauthorizedAccessException"/>.
/// </summary>
public sealed class AssistantConfigService : IAssistantConfigService
{
    private readonly object _lock = new();
    private AssistantConfig _config;

    public AssistantConfigService(AssistantConfig? initialConfig = null)
    {
        _config = initialConfig ?? new AssistantConfig();
    }

    /// <inheritdoc />
    public Task<AssistantConfig> GetConfigAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_config);
        }
    }

    /// <inheritdoc />
    public Task<AssistantConfig> UpdateConfigAsync(UserRole callerRole, AssistantConfig updatedConfig, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updatedConfig);

        if (callerRole != UserRole.Humaniser)
            throw new UnauthorizedAccessException(
                "Only users with the Humaniser role are permitted to edit the assistant configuration.");

        lock (_lock)
        {
            _config = updatedConfig;
            _config.UpdatedAt = DateTime.UtcNow;
            _config.UpdatedByRole = callerRole.ToString();
        }

        return Task.FromResult(_config);
    }
}
