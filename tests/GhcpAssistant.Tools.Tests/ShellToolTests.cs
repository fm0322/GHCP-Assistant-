using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class ShellToolTests
{
    [Fact]
    public async Task AllowedCommand_Succeeds()
    {
        var tool = new ShellTool(["echo"]);
        var parameters = JsonDocument.Parse("""{"command":"echo","arguments":"hello"}""").RootElement;

        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("Exit code: 0", result);
        Assert.Contains("hello", result);
    }

    [Fact]
    public async Task DisallowedCommand_IsRejected()
    {
        var tool = new ShellTool(["echo"]);
        var parameters = JsonDocument.Parse("""{"command":"rm","arguments":"-rf /"}""").RootElement;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => tool.ExecuteAsync(parameters, CancellationToken.None));
    }

    [Fact]
    public void Name_IsShell()
    {
        var tool = new ShellTool(["echo"]);
        Assert.Equal("shell", tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var tool = new ShellTool(["echo"]);
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }
}
