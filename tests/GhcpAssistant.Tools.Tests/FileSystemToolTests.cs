using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

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
