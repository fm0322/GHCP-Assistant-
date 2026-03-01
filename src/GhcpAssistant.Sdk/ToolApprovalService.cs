using System.Collections.Concurrent;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Manages the approval workflow for dynamically discovered tools.
/// When <see cref="AssistantConfig.AutoApproveTools"/> is enabled, tools are approved immediately.
/// Otherwise, users must explicitly authorize each new tool before it can be used.
/// </summary>
public sealed class ToolApprovalService : IToolApprovalService
{
    private readonly ConcurrentDictionary<Guid, ToolConfiguration> _configurations = new();
    private readonly IAssistantConfigService _configService;

    public ToolApprovalService(IAssistantConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc />
    public async Task<ToolConfiguration> RequestApprovalAsync(
        string name, string description, string toolTypeName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolTypeName);

        var config = await _configService.GetConfigAsync(ct);

        var toolConfig = new ToolConfiguration
        {
            Name = name,
            Description = description ?? string.Empty,
            ToolTypeName = toolTypeName,
            CreatedAt = DateTime.UtcNow
        };

        if (config.AutoApproveTools)
        {
            toolConfig.ApprovalStatus = ToolApprovalStatus.Approved;
            toolConfig.ApprovedAt = DateTime.UtcNow;
        }
        else
        {
            toolConfig.ApprovalStatus = ToolApprovalStatus.Pending;
        }

        _configurations.TryAdd(toolConfig.Id, toolConfig);
        return toolConfig;
    }

    /// <inheritdoc />
    public Task<ToolConfiguration> ApproveToolAsync(Guid toolId, CancellationToken ct = default)
    {
        if (!_configurations.TryGetValue(toolId, out var toolConfig))
            throw new KeyNotFoundException($"No tool configuration found with ID '{toolId}'.");

        toolConfig.ApprovalStatus = ToolApprovalStatus.Approved;
        toolConfig.ApprovedAt = DateTime.UtcNow;
        return Task.FromResult(toolConfig);
    }

    /// <inheritdoc />
    public Task<ToolConfiguration> RejectToolAsync(Guid toolId, CancellationToken ct = default)
    {
        if (!_configurations.TryGetValue(toolId, out var toolConfig))
            throw new KeyNotFoundException($"No tool configuration found with ID '{toolId}'.");

        toolConfig.ApprovalStatus = ToolApprovalStatus.Rejected;
        return Task.FromResult(toolConfig);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ToolConfiguration>> GetPendingApprovalsAsync(CancellationToken ct = default)
    {
        var pending = _configurations.Values
            .Where(t => t.ApprovalStatus == ToolApprovalStatus.Pending)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ToolConfiguration>>(pending);
    }

    /// <inheritdoc />
    public Task<ToolApprovalStatus> GetToolStatusAsync(Guid toolId, CancellationToken ct = default)
    {
        if (!_configurations.TryGetValue(toolId, out var toolConfig))
            throw new KeyNotFoundException($"No tool configuration found with ID '{toolId}'.");

        return Task.FromResult(toolConfig.ApprovalStatus);
    }
}
