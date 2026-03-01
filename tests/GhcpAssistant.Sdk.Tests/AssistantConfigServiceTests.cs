using GhcpAssistant.Core.Tools;
using GhcpAssistant.Sdk;

namespace GhcpAssistant.Sdk.Tests;

public class AssistantConfigServiceTests
{
    [Fact]
    public async Task GetConfigAsync_ReturnsDefaultConfig()
    {
        var service = new AssistantConfigService();

        var config = await service.GetConfigAsync();

        Assert.NotNull(config);
        Assert.False(config.AutoApproveTools);
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsInitialConfig()
    {
        var initial = new AssistantConfig { AutoApproveTools = true };
        var service = new AssistantConfigService(initial);

        var config = await service.GetConfigAsync();

        Assert.True(config.AutoApproveTools);
    }

    [Fact]
    public async Task UpdateConfigAsync_HumaniserRole_Succeeds()
    {
        var service = new AssistantConfigService();
        var updated = new AssistantConfig { AutoApproveTools = true };

        var result = await service.UpdateConfigAsync(UserRole.Humaniser, updated);

        Assert.True(result.AutoApproveTools);
        Assert.Equal(nameof(UserRole.Humaniser), result.UpdatedByRole);
    }

    [Fact]
    public async Task UpdateConfigAsync_UserRole_ThrowsUnauthorizedAccessException()
    {
        var service = new AssistantConfigService();
        var updated = new AssistantConfig { AutoApproveTools = true };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateConfigAsync(UserRole.User, updated));
    }

    [Fact]
    public async Task UpdateConfigAsync_PersistsChange()
    {
        var service = new AssistantConfigService();
        var updated = new AssistantConfig { AutoApproveTools = true };

        await service.UpdateConfigAsync(UserRole.Humaniser, updated);

        var config = await service.GetConfigAsync();
        Assert.True(config.AutoApproveTools);
    }

    [Fact]
    public async Task UpdateConfigAsync_NullConfig_ThrowsArgumentNullException()
    {
        var service = new AssistantConfigService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateConfigAsync(UserRole.Humaniser, null!));
    }

    [Fact]
    public async Task UpdateConfigAsync_SetsUpdatedAtTimestamp()
    {
        var service = new AssistantConfigService();
        var before = DateTime.UtcNow;
        var updated = new AssistantConfig { AutoApproveTools = true };

        var result = await service.UpdateConfigAsync(UserRole.Humaniser, updated);

        Assert.True(result.UpdatedAt >= before);
    }
}
