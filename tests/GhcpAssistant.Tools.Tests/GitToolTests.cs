using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class GitToolTests
{
    [Fact]
    public void Name_IsGit()
    {
        var tool = new GitTool("/tmp");
        Assert.Equal("git", tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var tool = new GitTool("/tmp");
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }

    [Fact]
    public async Task StatusAction_RunsSuccessfully()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"git_tool_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var initProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = testDir,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        await initProcess.WaitForExitAsync();

        var tool = new GitTool(testDir);
        var parameters = JsonDocument.Parse("""{"action":"status"}""").RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("Exit code: 0", result);
    }

    [Fact]
    public async Task UnknownAction_ThrowsArgumentException()
    {
        var tool = new GitTool("/tmp");
        var parameters = JsonDocument.Parse("""{"action":"unknown"}""").RootElement;

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.ExecuteAsync(parameters, CancellationToken.None));
    }
}
