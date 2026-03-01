# GHCP Assistant — Development Plan

> **Purpose**: This document breaks the [Architecture](ARCHITECTURE.md) into small, self-contained phases that a Copilot agent (or any AI coding assistant) can execute **one phase at a time** without running out of context. Each phase lists the exact files to create, the code to produce, and the acceptance criteria so the agent never needs to read the entire architecture document in a single session.

---

## How to Use This Plan

1. **One phase per session** — Open a new Copilot agent session for each phase.
2. **Copy the phase prompt** — Each phase contains a self-contained prompt with all necessary context. Paste it into the agent session.
3. **Verify before moving on** — Each phase has acceptance criteria. Confirm they pass before starting the next phase.
4. **Phases are sequential** — Later phases depend on artifacts from earlier ones. Do not skip ahead.

---

## Phase Overview

| Phase | Title | Key Deliverables |
|-------|-------|-----------------|
| 1 | Solution & Project Scaffolding | `.sln`, five `.csproj` files, project references |
| 2 | Core Interfaces & Models | `IInputChannel`, `IAssistantTool`, `SessionOptions` |
| 3 | Tool Registry | `ToolRegistry` class in the Sdk project |
| 4 | Session Manager | `SessionManager` and `CopilotClientFactory` in the Sdk project |
| 5 | FileSystem Tool | `FileSystemTool` implementation |
| 6 | Shell Tool | `ShellTool` implementation |
| 7 | Git Tool | `GitTool` implementation |
| 8 | Web Search Tool | `WebSearchTool` implementation |
| 9 | GitHub API Tool | `GitHubTool` implementation (Octokit) |
| 10 | Console Input Channel | `ConsoleInputChannel` implementation |
| 11 | CLI Entry Point | `Program.cs`, `appsettings.json`, DI wiring |
| 12 | Core Unit Tests | Tests for `SessionOptions`, `ToolRegistry` |
| 13 | Tools Unit Tests | Tests for all five tool classes |
| 14 | Sdk Unit Tests | Tests for `SessionManager` orchestration |
| 15 | Integration & Polish | End-to-end smoke test, README update |

---

## Phase 1 — Solution & Project Scaffolding

### Goal

Create the .NET solution file, all five projects (class libraries + console app), and wire up project references. No application code yet — only build infrastructure.

### Context

- Runtime: **.NET 10** (`net10.0`)
- Solution name: `GhcpAssistant`
- Projects live under `src/`; tests under `tests/`

### Steps

1. From the repository root, run:

```bash
# Create solution
dotnet new sln -n GhcpAssistant

# Create projects
dotnet new classlib -n GhcpAssistant.Core     -o src/GhcpAssistant.Core     -f net10.0
dotnet new classlib -n GhcpAssistant.Sdk      -o src/GhcpAssistant.Sdk      -f net10.0
dotnet new classlib -n GhcpAssistant.Tools    -o src/GhcpAssistant.Tools    -f net10.0
dotnet new classlib -n GhcpAssistant.Channels -o src/GhcpAssistant.Channels -f net10.0
dotnet new console  -n GhcpAssistant.Cli      -o src/GhcpAssistant.Cli      -f net10.0

# Create test projects
dotnet new xunit -n GhcpAssistant.Core.Tests  -o tests/GhcpAssistant.Core.Tests  -f net10.0
dotnet new xunit -n GhcpAssistant.Sdk.Tests   -o tests/GhcpAssistant.Sdk.Tests   -f net10.0
dotnet new xunit -n GhcpAssistant.Tools.Tests -o tests/GhcpAssistant.Tools.Tests -f net10.0

# Add all projects to solution
dotnet sln GhcpAssistant.sln add src/GhcpAssistant.Core/GhcpAssistant.Core.csproj
dotnet sln GhcpAssistant.sln add src/GhcpAssistant.Sdk/GhcpAssistant.Sdk.csproj
dotnet sln GhcpAssistant.sln add src/GhcpAssistant.Tools/GhcpAssistant.Tools.csproj
dotnet sln GhcpAssistant.sln add src/GhcpAssistant.Channels/GhcpAssistant.Channels.csproj
dotnet sln GhcpAssistant.sln add src/GhcpAssistant.Cli/GhcpAssistant.Cli.csproj
dotnet sln GhcpAssistant.sln add tests/GhcpAssistant.Core.Tests/GhcpAssistant.Core.Tests.csproj
dotnet sln GhcpAssistant.sln add tests/GhcpAssistant.Sdk.Tests/GhcpAssistant.Sdk.Tests.csproj
dotnet sln GhcpAssistant.sln add tests/GhcpAssistant.Tools.Tests/GhcpAssistant.Tools.Tests.csproj
```

2. Add **project references** (dependency graph):

```bash
# Sdk depends on Core
dotnet add src/GhcpAssistant.Sdk/GhcpAssistant.Sdk.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj

# Tools depends on Core
dotnet add src/GhcpAssistant.Tools/GhcpAssistant.Tools.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj

# Channels depends on Core
dotnet add src/GhcpAssistant.Channels/GhcpAssistant.Channels.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj

# Cli depends on all
dotnet add src/GhcpAssistant.Cli/GhcpAssistant.Cli.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj
dotnet add src/GhcpAssistant.Cli/GhcpAssistant.Cli.csproj reference src/GhcpAssistant.Sdk/GhcpAssistant.Sdk.csproj
dotnet add src/GhcpAssistant.Cli/GhcpAssistant.Cli.csproj reference src/GhcpAssistant.Tools/GhcpAssistant.Tools.csproj
dotnet add src/GhcpAssistant.Cli/GhcpAssistant.Cli.csproj reference src/GhcpAssistant.Channels/GhcpAssistant.Channels.csproj

# Test projects reference their targets
dotnet add tests/GhcpAssistant.Core.Tests/GhcpAssistant.Core.Tests.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj
dotnet add tests/GhcpAssistant.Sdk.Tests/GhcpAssistant.Sdk.Tests.csproj reference src/GhcpAssistant.Sdk/GhcpAssistant.Sdk.csproj
dotnet add tests/GhcpAssistant.Sdk.Tests/GhcpAssistant.Sdk.Tests.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj
dotnet add tests/GhcpAssistant.Tools.Tests/GhcpAssistant.Tools.Tests.csproj reference src/GhcpAssistant.Tools/GhcpAssistant.Tools.csproj
dotnet add tests/GhcpAssistant.Tools.Tests/GhcpAssistant.Tools.Tests.csproj reference src/GhcpAssistant.Core/GhcpAssistant.Core.csproj
```

3. Delete the auto-generated `Class1.cs` files from each class library.

4. Verify the build:

```bash
dotnet build GhcpAssistant.sln
```

### Acceptance Criteria

- [ ] `dotnet build GhcpAssistant.sln` succeeds with zero errors.
- [ ] Solution contains eight projects (five src + three test).
- [ ] Project reference graph matches the dependency diagram above.

---

## Phase 2 — Core Interfaces & Models

### Goal

Define the foundational interfaces and configuration types in `GhcpAssistant.Core`. These have **zero external NuGet dependencies** — only framework types.

### Files to Create

#### `src/GhcpAssistant.Core/Channels/IInputChannel.cs`

```csharp
namespace GhcpAssistant.Core.Channels;

/// <summary>Abstraction for user input/output channels (CLI, HTTP, etc.).</summary>
public interface IInputChannel
{
    /// <summary>Yields user messages as they arrive.</summary>
    IAsyncEnumerable<string> ReadMessagesAsync(CancellationToken ct = default);

    /// <summary>Writes a streamed response chunk to the user.</summary>
    Task WriteResponseAsync(string chunk, CancellationToken ct = default);
}
```

#### `src/GhcpAssistant.Core/Tools/IAssistantTool.cs`

```csharp
using System.Text.Json;

namespace GhcpAssistant.Core.Tools;

/// <summary>Contract that all agent-callable tools must implement.</summary>
public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct = default);
}
```

#### `src/GhcpAssistant.Core/Sessions/SessionOptions.cs`

```csharp
namespace GhcpAssistant.Core.Sessions;

/// <summary>Configuration for a Copilot session.</summary>
public sealed class SessionOptions
{
    /// <summary>LLM model identifier (e.g., "gpt-4o").</summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Optional system prompt prepended to every conversation.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Maximum number of conversation turns before compaction.</summary>
    public int MaxTurns { get; set; } = 50;
}
```

### Steps

1. Create the three directories (`Channels/`, `Tools/`, `Sessions/`) under `src/GhcpAssistant.Core/`.
2. Create the three files above.
3. Run `dotnet build src/GhcpAssistant.Core/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Core/` succeeds.
- [ ] `IInputChannel`, `IAssistantTool`, and `SessionOptions` are public and in the correct namespaces.

---

## Phase 3 — Tool Registry

### Goal

Implement `ToolRegistry` in `GhcpAssistant.Sdk`. The registry stores `IAssistantTool` instances by name and dispatches calls by name.

### Dependencies

- Project reference: `GhcpAssistant.Core`
- NuGet: none (tool registration is manual; `AIFunctionFactory` integration is deferred to Phase 11).

### Files to Create

#### `src/GhcpAssistant.Sdk/ToolRegistry.cs`

```csharp
using System.Collections.Concurrent;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Maintains a dictionary of registered tools and dispatches invocations by name.
/// </summary>
public sealed class ToolRegistry
{
    private readonly ConcurrentDictionary<string, IAssistantTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a tool instance.</summary>
    public void Register(IAssistantTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (!_tools.TryAdd(tool.Name, tool))
            throw new InvalidOperationException($"A tool named '{tool.Name}' is already registered.");
    }

    /// <summary>Return all registered tool metadata.</summary>
    public IReadOnlyList<IAssistantTool> GetRegisteredTools() => _tools.Values.ToList().AsReadOnly();

    /// <summary>Invoke a tool by name.</summary>
    public async Task<string> InvokeAsync(string toolName, JsonElement args, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            throw new KeyNotFoundException($"No tool registered with the name '{toolName}'.");

        return await tool.ExecuteAsync(args, ct);
    }
}
```

### Steps

1. Create the file above.
2. Delete the placeholder `Class1.cs` in `GhcpAssistant.Sdk/` if it still exists.
3. Run `dotnet build src/GhcpAssistant.Sdk/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Sdk/` succeeds.
- [ ] `ToolRegistry` can register tools, list them, and invoke by name.

---

## Phase 4 — Session Manager & CopilotClientFactory

### Goal

Implement the `SessionManager` orchestrator and `CopilotClientFactory` in `GhcpAssistant.Sdk`.

### NuGet Dependencies to Add

```bash
dotnet add src/GhcpAssistant.Sdk/ package GitHub.Copilot.SDK --prerelease
dotnet add src/GhcpAssistant.Sdk/ package Microsoft.Extensions.AI --prerelease
```

> **Note**: The `GitHub.Copilot.SDK` package (v0.1.29) is now available on NuGet and has been integrated via `CopilotSdkClientFactory`. A `StubCopilotClientFactory` is also provided for testing and offline use.

### Files to Create

#### `src/GhcpAssistant.Sdk/CopilotClientFactory.cs`

```csharp
namespace GhcpAssistant.Sdk;

/// <summary>
/// Factory that creates and configures CopilotClient instances.
/// Abstracted so that tests can substitute a mock client.
/// </summary>
public interface ICopilotClientFactory
{
    /// <summary>Creates and starts a new CopilotClient.</summary>
    Task<ICopilotClientWrapper> CreateAsync(CancellationToken ct = default);
}

/// <summary>Abstraction over the SDK's CopilotClient for testability.</summary>
public interface ICopilotClientWrapper : IAsyncDisposable
{
    Task<ICopilotSessionWrapper> CreateSessionAsync(string model, IReadOnlyList<Core.Tools.IAssistantTool> tools, CancellationToken ct = default);
}

/// <summary>Abstraction over the SDK's CopilotSession for testability.</summary>
public interface ICopilotSessionWrapper : IAsyncDisposable
{
    IAsyncEnumerable<SessionEvent> SendMessageAsync(string message, CancellationToken ct = default);
    Task SendToolResultAsync(string toolCallId, string result, CancellationToken ct = default);
}

/// <summary>Events the session can emit.</summary>
public abstract record SessionEvent;
public sealed record TextDeltaEvent(string Text) : SessionEvent;
public sealed record ToolCallRequestEvent(string ToolCallId, string ToolName, System.Text.Json.JsonElement Arguments) : SessionEvent;
public sealed record TurnCompleteEvent() : SessionEvent;
```

#### `src/GhcpAssistant.Sdk/SessionManager.cs`

```csharp
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;

namespace GhcpAssistant.Sdk;

/// <summary>
/// Central orchestrator: reads from the input channel, sends messages through
/// the Copilot session, dispatches tool calls, and streams responses back.
/// </summary>
public sealed class SessionManager
{
    private readonly ICopilotClientFactory _clientFactory;
    private readonly IInputChannel _inputChannel;
    private readonly ToolRegistry _toolRegistry;
    private readonly SessionOptions _options;

    public SessionManager(
        ICopilotClientFactory clientFactory,
        IInputChannel inputChannel,
        ToolRegistry toolRegistry,
        SessionOptions options)
    {
        _clientFactory = clientFactory;
        _inputChannel = inputChannel;
        _toolRegistry = toolRegistry;
        _options = options;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var client = await _clientFactory.CreateAsync(ct);
        await using var session = await client.CreateSessionAsync(
            _options.Model, _toolRegistry.GetRegisteredTools(), ct);

        await foreach (var userMessage in _inputChannel.ReadMessagesAsync(ct))
        {
            await foreach (var evt in session.SendMessageAsync(userMessage, ct))
            {
                switch (evt)
                {
                    case TextDeltaEvent delta:
                        await _inputChannel.WriteResponseAsync(delta.Text, ct);
                        break;

                    case ToolCallRequestEvent toolCall:
                        var result = await _toolRegistry.InvokeAsync(
                            toolCall.ToolName, toolCall.Arguments, ct);
                        await session.SendToolResultAsync(toolCall.ToolCallId, result, ct);
                        break;

                    case TurnCompleteEvent:
                        await _inputChannel.WriteResponseAsync("\n", ct);
                        break;
                }
            }
        }
    }
}
```

### Steps

1. Attempt to add the NuGet packages. If `GitHub.Copilot.SDK` is unavailable, proceed without it — the interfaces above provide the abstraction.
2. Create both files.
3. Run `dotnet build src/GhcpAssistant.Sdk/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Sdk/` succeeds.
- [ ] `SessionManager` compiles and references `ToolRegistry`, `IInputChannel`, and `SessionOptions`.

---

## Phase 5 — FileSystem Tool

### Goal

Implement the `FileSystemTool` — reads, writes, and lists files, scoped to a configurable root directory.

### Files to Create

#### `src/GhcpAssistant.Tools/FileSystemTool.cs`

The tool must:

- Accept JSON parameters: `{ "action": "read"|"write"|"list", "path": "...", "content": "..." }`
- Validate that all resolved paths stay **within** a configurable `rootDirectory`.
- Return file contents, write confirmation, or directory listing as a string.

### Implementation Notes

```csharp
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class FileSystemTool : IAssistantTool
{
    private readonly string _rootDirectory;

    public FileSystemTool(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string Name => "file_system";
    public string Description => "Read, write, or list files and directories within the workspace.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!;
        var path = parameters.GetProperty("path").GetString()!;
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, path));

        if (!fullPath.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected.");

        return action.ToLowerInvariant() switch
        {
            "read" => await File.ReadAllTextAsync(fullPath, ct),
            "write" => await WriteFileAsync(fullPath, parameters, ct),
            "list" => string.Join('\n', Directory.GetFileSystemEntries(fullPath)),
            _ => throw new ArgumentException($"Unknown action '{action}'.")
        };
    }

    private static async Task<string> WriteFileAsync(string fullPath, JsonElement parameters, CancellationToken ct)
    {
        var content = parameters.GetProperty("content").GetString()!;
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null) Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(fullPath, content, ct);
        return $"Wrote {content.Length} characters to {fullPath}.";
    }
}
```

### Steps

1. Create the file (and delete `Class1.cs` if present).
2. Run `dotnet build src/GhcpAssistant.Tools/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Tools/` succeeds.
- [ ] Path traversal outside root is rejected.

---

## Phase 6 — Shell Tool

### Goal

Implement the `ShellTool` — executes shell commands in a child process with an allow-list.

### Files to Create

#### `src/GhcpAssistant.Tools/ShellTool.cs`

The tool must:

- Accept JSON parameters: `{ "command": "...", "arguments": "..." }`
- Validate the command against a configurable allow-list (e.g., `["dotnet", "git", "ls", "cat", "echo"]`).
- Capture and return stdout + stderr.
- Enforce a timeout (default 30 seconds).

### Implementation Notes

```csharp
using System.Diagnostics;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class ShellTool : IAssistantTool
{
    private readonly HashSet<string> _allowedCommands;
    private readonly TimeSpan _timeout;

    public ShellTool(IEnumerable<string> allowedCommands, TimeSpan? timeout = null)
    {
        _allowedCommands = new HashSet<string>(allowedCommands, StringComparer.OrdinalIgnoreCase);
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public string Name => "shell";
    public string Description => "Execute a shell command from the allowed list.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var command = parameters.GetProperty("command").GetString()!;
        if (!_allowedCommands.Contains(command))
            throw new UnauthorizedAccessException($"Command '{command}' is not in the allow-list.");

        var arguments = parameters.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() ?? "" : "";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct).WaitAsync(_timeout, ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return $"Exit code: {process.ExitCode}\n--- stdout ---\n{stdout}\n--- stderr ---\n{stderr}";
    }
}
```

### Steps

1. Create the file.
2. Run `dotnet build src/GhcpAssistant.Tools/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Tools/` succeeds.
- [ ] Commands not in the allow-list are rejected.

---

## Phase 7 — Git Tool

### Goal

Implement the `GitTool` — runs common `git` operations.

### Files to Create

#### `src/GhcpAssistant.Tools/GitTool.cs`

The tool must:

- Accept JSON parameters: `{ "action": "status"|"diff"|"log"|"commit", "message": "..." }`
- Delegate to `git` via `Process.Start`.
- Operate in a configurable working directory.

### Implementation Notes

```csharp
using System.Diagnostics;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class GitTool : IAssistantTool
{
    private readonly string _workingDirectory;

    public GitTool(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public string Name => "git";
    public string Description => "Run git operations: status, diff, log, commit.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!.ToLowerInvariant();

        var args = action switch
        {
            "status" => "status --short",
            "diff" => "diff",
            "log" => "log --oneline -20",
            "commit" => $"commit -m \"{parameters.GetProperty("message").GetString()}\"",
            _ => throw new ArgumentException($"Unknown git action '{action}'.")
        };

        return await RunGitAsync(args, ct);
    }

    private async Task<string> RunGitAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return $"Exit code: {process.ExitCode}\n{stdout}\n{stderr}".Trim();
    }
}
```

### Steps

1. Create the file.
2. Run `dotnet build src/GhcpAssistant.Tools/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Tools/` succeeds.
- [ ] Supports `status`, `diff`, `log`, and `commit` actions.

---

## Phase 8 — Web Search Tool

### Goal

Implement a `WebSearchTool` that returns search results via the DuckDuckGo Instant Answer API. No API key is required.

### Files to Create

#### `src/GhcpAssistant.Tools/WebSearchTool.cs`

The tool must:

- Accept JSON parameters: `{ "query": "..." }`
- Make an HTTP GET request to the DuckDuckGo Instant Answer API and parse the JSON response.
- Return results as a text summary.

### Implementation Notes

```csharp
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class WebSearchTool : IAssistantTool
{
    private readonly HttpClient _httpClient;

    public WebSearchTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public string Name => "web_search";
    public string Description => "Search the web and return a summary of results.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var query = parameters.GetProperty("query").GetString()!;

        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        // Parse JSON response and extract AbstractText, RelatedTopics, etc.
    }
}
```

### Steps

1. Create the file.
2. Run `dotnet build src/GhcpAssistant.Tools/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Tools/` succeeds.
- [x] Tool compiles and returns real search results from DuckDuckGo.

---

## Phase 9 — GitHub API Tool

### Goal

Implement `GitHubTool` using `Octokit` to query the GitHub REST API.

### NuGet Dependency

```bash
dotnet add src/GhcpAssistant.Tools/ package Octokit
```

### Files to Create

#### `src/GhcpAssistant.Tools/GitHubTool.cs`

The tool must:

- Accept JSON parameters: `{ "action": "get_repo"|"list_issues"|"list_prs", "owner": "...", "repo": "..." }`
- Use `Octokit.GitHubClient` to make API calls.
- Return results as formatted text.

### Implementation Notes

```csharp
using System.Text.Json;
using GhcpAssistant.Core.Tools;
using Octokit;

namespace GhcpAssistant.Tools;

public sealed class GitHubTool : IAssistantTool
{
    private readonly GitHubClient _client;

    public GitHubTool(string? token = null)
    {
        _client = new GitHubClient(new ProductHeaderValue("GhcpAssistant"));
        if (!string.IsNullOrEmpty(token))
            _client.Credentials = new Credentials(token);
    }

    public string Name => "github";
    public string Description => "Query GitHub REST API: get repo info, list issues, list PRs.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!;
        var owner = parameters.GetProperty("owner").GetString()!;
        var repo = parameters.GetProperty("repo").GetString()!;

        return action.ToLowerInvariant() switch
        {
            "get_repo" => await GetRepoAsync(owner, repo),
            "list_issues" => await ListIssuesAsync(owner, repo),
            "list_prs" => await ListPullRequestsAsync(owner, repo),
            _ => throw new ArgumentException($"Unknown GitHub action '{action}'.")
        };
    }

    private async Task<string> GetRepoAsync(string owner, string repo)
    {
        var r = await _client.Repository.Get(owner, repo);
        return $"Name: {r.FullName}\nDescription: {r.Description}\nStars: {r.StargazersCount}\nLanguage: {r.Language}";
    }

    private async Task<string> ListIssuesAsync(string owner, string repo)
    {
        var issues = await _client.Issue.GetAllForRepository(owner, repo,
            new RepositoryIssueRequest { State = ItemStateFilter.Open });
        return string.Join('\n', issues.Take(10).Select(i => $"#{i.Number} {i.Title}"));
    }

    private async Task<string> ListPullRequestsAsync(string owner, string repo)
    {
        var prs = await _client.PullRequest.GetAllForRepository(owner, repo);
        return string.Join('\n', prs.Take(10).Select(p => $"#{p.Number} {p.Title} ({p.State})"));
    }
}
```

### Steps

1. Add the Octokit NuGet package.
2. Create the file.
3. Run `dotnet build src/GhcpAssistant.Tools/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Tools/` succeeds.
- [ ] `GitHubTool` supports `get_repo`, `list_issues`, `list_prs`.

---

## Phase 10 — Console Input Channel

### Goal

Implement `ConsoleInputChannel` for interactive terminal I/O.

### NuGet Dependency (optional)

```bash
dotnet add src/GhcpAssistant.Channels/ package Spectre.Console
```

### Files to Create

#### `src/GhcpAssistant.Channels/ConsoleInputChannel.cs`

```csharp
using GhcpAssistant.Core.Channels;

namespace GhcpAssistant.Channels;

/// <summary>Interactive terminal-based input/output channel.</summary>
public sealed class ConsoleInputChannel : IInputChannel
{
    public async IAsyncEnumerable<string> ReadMessagesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            Console.Write("\n> ");
            var line = await Task.Run(() => Console.ReadLine(), ct);

            if (line is null || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                yield break;

            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }

    public Task WriteResponseAsync(string chunk, CancellationToken ct = default)
    {
        Console.Write(chunk);
        return Task.CompletedTask;
    }
}
```

### Steps

1. Create the file (and delete `Class1.cs` if present).
2. Run `dotnet build src/GhcpAssistant.Channels/`.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Channels/` succeeds.
- [ ] Channel reads from `Console.ReadLine` and writes to `Console.Write`.

---

## Phase 11 — CLI Entry Point

### Goal

Wire everything together in `GhcpAssistant.Cli` with dependency injection and configuration.

### NuGet Dependencies

```bash
dotnet add src/GhcpAssistant.Cli/ package Microsoft.Extensions.Hosting
```

### Files to Create / Modify

#### `src/GhcpAssistant.Cli/appsettings.json`

```json
{
  "Session": {
    "Model": "gpt-4o",
    "SystemPrompt": "You are GHCP Assistant, an autonomous AI agent. Use the available tools to help the user.",
    "MaxTurns": 50
  },
  "Shell": {
    "AllowedCommands": ["dotnet", "git", "ls", "cat", "echo", "pwd", "mkdir"]
  }
}
```

#### `src/GhcpAssistant.Cli/Program.cs`

```csharp
using GhcpAssistant.Channels;
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;
using GhcpAssistant.Sdk;
using GhcpAssistant.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Bind configuration
var sessionOptions = builder.Configuration.GetSection("Session").Get<SessionOptions>() ?? new SessionOptions();
var allowedCommands = builder.Configuration.GetSection("Shell:AllowedCommands").Get<string[]>() ?? ["dotnet", "git", "ls"];

// Register services
builder.Services.AddSingleton(sessionOptions);
builder.Services.AddSingleton<IInputChannel, ConsoleInputChannel>();
builder.Services.AddSingleton<ToolRegistry>(sp =>
{
    var registry = new ToolRegistry();
    registry.Register(new FileSystemTool(Directory.GetCurrentDirectory()));
    registry.Register(new ShellTool(allowedCommands));
    registry.Register(new GitTool(Directory.GetCurrentDirectory()));
    registry.Register(new WebSearchTool());
    registry.Register(new GitHubTool());
    return registry;
});

var host = builder.Build();

// Run the assistant
Console.WriteLine("GHCP Assistant started. Type 'exit' to quit.\n");

var sessionManager = host.Services.GetRequiredService<SessionManager>();
await sessionManager.RunAsync();
```

### Steps

1. Add the NuGet packages.
2. Create/update the files above.
3. Run `dotnet build src/GhcpAssistant.Cli/`.
4. Run `dotnet run --project src/GhcpAssistant.Cli/` to verify startup.

### Acceptance Criteria

- [ ] `dotnet build src/GhcpAssistant.Cli/` succeeds.
- [ ] `dotnet run` prints the registered tools and startup message.

---

## Phase 12 — Core Unit Tests

### Goal

Add unit tests for `SessionOptions` defaults and `ToolRegistry` behavior.

### Files to Create

#### `tests/GhcpAssistant.Core.Tests/SessionOptionsTests.cs`

Test that default values are set correctly (`Model = "gpt-4o"`, `MaxTurns = 50`, `SystemPrompt = null`).

#### `tests/GhcpAssistant.Sdk.Tests/ToolRegistryTests.cs`

Test:
- Registering a tool and retrieving it.
- Invoking a registered tool returns expected results.
- Invoking an unregistered tool throws `KeyNotFoundException`.
- Registering a duplicate name throws `InvalidOperationException`.

### Steps

1. Create mock `IAssistantTool` implementations inside the test files.
2. Write xUnit `[Fact]` tests.
3. Run `dotnet test tests/GhcpAssistant.Core.Tests/` and `dotnet test tests/GhcpAssistant.Sdk.Tests/`.

### Acceptance Criteria

- [ ] All tests pass.

---

## Phase 13 — Tools Unit Tests

### Goal

Add unit tests for each tool class (`FileSystemTool`, `ShellTool`, `GitTool`, `WebSearchTool`, `GitHubTool`).

### Files to Create

#### `tests/GhcpAssistant.Tools.Tests/FileSystemToolTests.cs`

- Test reading a file that exists.
- Test writing a file and reading it back.
- Test listing a directory.
- Test path traversal rejection (`../../../etc/passwd`).

#### `tests/GhcpAssistant.Tools.Tests/ShellToolTests.cs`

- Test executing an allowed command (e.g., `echo`).
- Test rejection of a command not in the allow-list.

#### `tests/GhcpAssistant.Tools.Tests/WebSearchToolTests.cs`

- Test that the tool returns search results containing the query.

#### `tests/GhcpAssistant.Tools.Tests/GitHubToolTests.cs`

- Test that an unknown action throws `ArgumentException`.

### Steps

1. Use temporary directories for `FileSystemTool` tests.
2. Write xUnit `[Fact]` tests.
3. Run `dotnet test tests/GhcpAssistant.Tools.Tests/`.

### Acceptance Criteria

- [ ] All tests pass.

---

## Phase 14 — Sdk Unit Tests

### Goal

Add unit tests for `SessionManager` using mock implementations of `ICopilotClientFactory` and `IInputChannel`.

### Files to Create

#### `tests/GhcpAssistant.Sdk.Tests/SessionManagerTests.cs`

- Test that text delta events are forwarded to the input channel.
- Test that tool call events invoke the tool registry and send results back.
- Test that the session ends when the input channel yields no more messages.

### Steps

1. Create in-memory mock implementations of `ICopilotClientFactory`, `ICopilotClientWrapper`, `ICopilotSessionWrapper`, and `IInputChannel`.
2. Write xUnit `[Fact]` tests.
3. Run `dotnet test tests/GhcpAssistant.Sdk.Tests/`.

### Acceptance Criteria

- [ ] All tests pass.
- [ ] SessionManager orchestration logic is validated without real SDK calls.

---

## Phase 15 — Integration & Polish

### Goal

Final integration pass: ensure the full solution builds, all tests pass, and documentation is updated.

### Steps

1. Run `dotnet build GhcpAssistant.sln` — fix any warnings.
2. Run `dotnet test GhcpAssistant.sln` — ensure all tests pass.
3. Update `README.md` with:
   - Quick-start instructions.
   - Link to this development plan.
   - List of available tools.
4. Review and close out any remaining `// TODO` comments.
5. Verify `dotnet run --project src/GhcpAssistant.Cli/` starts without errors.

### Acceptance Criteria

- [ ] `dotnet build GhcpAssistant.sln` — zero errors, zero warnings.
- [ ] `dotnet test GhcpAssistant.sln` — all tests pass.
- [ ] README.md is up to date.
- [ ] Application starts and lists all five tools.

---

## Appendix: Context-Window Tips for the Agent

When executing each phase, the Copilot agent should:

1. **Read only the relevant phase section** — do not load the entire `ARCHITECTURE.md` into context.
2. **Verify file existence before creating** — run `ls` or `find` to check if files already exist from a previous phase.
3. **Build incrementally** — run `dotnet build <project>` after each file, not the entire solution.
4. **Commit after each phase** — use `git add . && git commit -m "Phase N: <title>"` so progress is saved.
5. **Do not carry forward conversation history** — start a fresh agent session for each phase to maximize available context.
