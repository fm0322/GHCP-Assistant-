using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {

    }
}

public class FileSystemToolTests
{
    private readonly string _testRoot;
    private readonly FileSystemTool _tool;

    public FileSystemToolTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"fs_tool_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
        _tool = new FileSystemTool(_testRoot);
    }

    [Fact]
    public async Task ReadFile_ReturnsContents()
    {
        var filePath = Path.Combine(_testRoot, "hello.txt");
        await File.WriteAllTextAsync(filePath, "Hello, world!");

        var parameters = JsonDocument.Parse("""{"action":"read","path":"hello.txt"}""").RootElement;
        var result = await _tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public async Task WriteFile_CreatesFileAndReturnsConfirmation()
    {
        var parameters = JsonDocument.Parse("""{"action":"write","path":"output.txt","content":"test content"}""").RootElement;
        var result = await _tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("12 characters", result);
        Assert.DoesNotContain(_testRoot, result);
        Assert.Equal("test content", await File.ReadAllTextAsync(Path.Combine(_testRoot, "output.txt")));
    }

    [Fact]
    public async Task ListDirectory_ReturnsEntries()
    {
        await File.WriteAllTextAsync(Path.Combine(_testRoot, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(_testRoot, "b.txt"), "b");

        var parameters = JsonDocument.Parse("""{"action":"list","path":"."}""").RootElement;
        var result = await _tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("a.txt", result);
        Assert.Contains("b.txt", result);
    }

    [Fact]
    public async Task PathTraversal_IsRejected()
    {
        var parameters = JsonDocument.Parse("""{"action":"read","path":"../../etc/passwd"}""").RootElement;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _tool.ExecuteAsync(parameters, CancellationToken.None));
    }

    [Fact]
    public async Task UnknownAction_ThrowsArgumentException()
    {
        var parameters = JsonDocument.Parse("""{"action":"delete","path":"file.txt"}""").RootElement;

        await Assert.ThrowsAsync<ArgumentException>(
            () => _tool.ExecuteAsync(parameters, CancellationToken.None));
    }
}

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
