# GHCP Assistant — Architecture Document

## Overview

GHCP Assistant is a clawbot-inspired, autonomous AI assistant built in **C# / .NET 10** using the **GitHub Copilot CLI SDK** (`GitHub.Copilot.SDK`). Like clawbot, it is designed as a modular, layered agent platform that separates communication, session orchestration, reasoning, and tool execution into distinct, independently testable components. Unlike a simple chatbot, GHCP Assistant is an *active agent* that can invoke registered C# tools, stream responses in real time, and maintain stateful, multi-turn conversations.

---

## Design Goals

| Goal | Description |
|---|---|
| **Modularity** | Each layer (input, orchestration, agent, tools) is decoupled and replaceable. |
| **Extensibility** | New tools/skills are registered via a standard interface; no core changes required. |
| **Security** | Credentials are managed exclusively by the authenticated GitHub CLI; no secrets in application code. |
| **Responsiveness** | Responses are streamed token-by-token, giving immediate feedback to the user. |
| **Cross-platform** | Targets .NET 10 and runs on Windows, Linux, and macOS. |

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          GHCP Assistant                             │
│                                                                     │
│  ┌──────────────┐    ┌──────────────────┐    ┌───────────────────┐  │
│  │  Input Layer │───▶│ Session Manager  │───▶│  Tool Registry &  │  │
│  │  (CLI / TUI) │    │ (Orchestrator)   │    │  Execution Layer  │  │
│  └──────────────┘    └────────┬─────────┘    └───────────────────┘  │
│                               │                        ▲            │
│                               ▼                        │            │
│                    ┌──────────────────┐                │            │
│                    │  CopilotClient   │────────────────┘            │
│                    │  (SDK / JSON-RPC)│                             │
│                    └────────┬─────────┘                             │
│                             │                                       │
└─────────────────────────────┼───────────────────────────────────────┘
                              │ JSON-RPC (local socket)
                    ┌─────────▼─────────┐
                    │  GitHub Copilot   │
                    │  CLI (local agent │
                    │  runtime / auth)  │
                    └───────────────────┘
```

---

## Layers

### 1. Input Layer

Responsible for accepting user input and rendering responses. The initial implementation is a **CLI/TUI** (terminal-based). The layer is abstracted behind an `IInputChannel` interface so that future channels (e.g., HTTP API, Slack bot, Discord bot) can be added without touching the orchestration layer.

```csharp
public interface IInputChannel
{
    IAsyncEnumerable<string> ReadMessagesAsync(CancellationToken ct = default);
    Task WriteResponseAsync(string chunk, CancellationToken ct = default);
}
```

**Built-in implementations**

| Class | Transport |
|---|---|
| `ConsoleInputChannel` | Standard input / output (interactive terminal) |
| `HttpInputChannel` *(optional)* | ASP.NET Core minimal API endpoint |

---

### 2. Session Manager (Orchestrator)

The central coordinator — equivalent to clawbot's *Gateway Server*. It:

- Receives normalised messages from the active `IInputChannel`.
- Creates and owns a `CopilotSession` for each conversation context.
- Routes tool-call requests from the agent back to the **Tool Registry**.
- Forwards streamed response chunks back to the `IInputChannel`.
- Handles session lifecycle (creation, compaction, teardown).

```csharp
public sealed class SessionManager(
    CopilotClient copilotClient,
    IInputChannel inputChannel,
    ToolRegistry toolRegistry,
    SessionOptions options)
{
    public Task RunAsync(CancellationToken ct = default);
}
```

**Responsibilities**

| Concern | Mechanism |
|---|---|
| Session state | One `CopilotSession` per logical conversation |
| Streaming | `AssistantMessageDeltaEvent` forwarded token-by-token |
| Tool dispatch | `ToolCallEvent` → `ToolRegistry.InvokeAsync` |
| Error handling | Retry with back-off on transient failures; surface fatal errors to the user |

---

### 3. CopilotClient (SDK Bridge)

This is the thin wrapper around `GitHub.Copilot.SDK`. Its sole job is to manage the connection lifecycle with the GitHub Copilot CLI runtime over a local JSON-RPC socket.

```csharp
// Startup
await using CopilotClient client = new CopilotClient();
await client.StartAsync();

// Per-conversation session
await using CopilotSession session = await client.CreateSessionAsync(
    new SessionConfig { Model = options.Model });
```

Key SDK types used:

| Type | Role |
|---|---|
| `CopilotClient` | Connection + session factory |
| `CopilotSession` | Multi-turn conversation; streams events |
| `SessionConfig` | Model selection, system prompt, tool list |
| `AssistantMessageDeltaEvent` | Streamed response token |
| `ToolCallEvent` | AI-initiated tool invocation request |
| `ToolResultEvent` | Application's response to a tool call |

The GitHub Copilot CLI owns all authentication (OAuth tokens, GitHub PAT, etc.). The .NET application never handles credentials directly.

---

### 4. Tool Registry & Execution Layer

Equivalent to clawbot's *Tooling / Lambda Execution Layer*. Tools are regular C# methods decorated with `[AIFunction]` (from `Microsoft.Extensions.AI`) and registered at startup via `AIFunctionFactory`.

```csharp
public sealed class ToolRegistry
{
    public void Register<T>() where T : IAssistantTool;
    public IReadOnlyList<AIFunction> GetRegisteredFunctions();
    public Task<string> InvokeAsync(string toolName, JsonElement args, CancellationToken ct);
}
```

**Built-in tools (initial set)**

| Tool class | Capability |
|---|---|
| `FileSystemTool` | Read / write / list files and directories |
| `ShellTool` | Execute shell commands in a sandboxed child process |
| `GitTool` | Run common `git` operations (status, diff, log, commit) |
| `WebSearchTool` | Perform a web search and return a summary |
| `GitHubTool` | Query GitHub REST API (issues, PRs, repos) via `Octokit.net` |
| `HomeAssistantTool` | Interact with Home Assistant (entity states, service calls) via REST API |

All tools implement `IAssistantTool`:

```csharp
public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct);
}
```

---

## Project Structure

```
GHCP-Assistant/
├── src/
│   ├── GhcpAssistant.Core/                 # Domain models, interfaces
│   │   ├── Channels/
│   │   │   └── IInputChannel.cs
│   │   ├── Tools/
│   │   │   └── IAssistantTool.cs
│   │   └── Sessions/
│   │       └── SessionOptions.cs
│   │
│   ├── GhcpAssistant.Sdk/                  # CopilotClient wrapper + SessionManager
│   │   ├── SessionManager.cs
│   │   ├── ToolRegistry.cs
│   │   └── CopilotClientFactory.cs
│   │
│   ├── GhcpAssistant.Tools/                # Built-in tool implementations
│   │   ├── FileSystemTool.cs
│   │   ├── ShellTool.cs
│   │   ├── GitTool.cs
│   │   ├── WebSearchTool.cs
│   │   ├── GitHubTool.cs
│   │   └── HomeAssistantTool.cs
│   │
│   ├── GhcpAssistant.Channels/             # Input channel implementations
│   │   ├── ConsoleInputChannel.cs
│   │   └── HttpInputChannel.cs             # optional REST endpoint
│   │
│   └── GhcpAssistant.Cli/                  # Entry point (console app)
│       ├── Program.cs
│       ├── appsettings.json
│       └── GhcpAssistant.Cli.csproj
│
└── tests/
    ├── GhcpAssistant.Core.Tests/
    ├── GhcpAssistant.Sdk.Tests/
    └── GhcpAssistant.Tools.Tests/
```

---

## Key Technologies

| Technology | Version | Purpose |
|---|---|---|
| .NET | 10 | Runtime and base class libraries |
| `GitHub.Copilot.SDK` | 0.1.x (preview) | Copilot agent sessions and streaming |
| `Microsoft.Extensions.AI` | Latest | `AIFunction`, `AIFunctionFactory`, tool calling |
| `Microsoft.Agents.AI.GitHub.Copilot` | Preview | Optional Microsoft Agent Framework bridge |
| `Microsoft.Extensions.Hosting` | .NET 10 | Dependency injection, configuration, lifetime |
| `Spectre.Console` | Latest | Rich terminal UI (markdown rendering, spinners) |
| `Octokit` | Latest | GitHub REST API client (GitHubTool) |
| `System.Text.Json` | .NET 10 | JSON serialization of tool parameters |

---

## Data Flow

```
User types a message (ConsoleInputChannel)
        │
        ▼
SessionManager.RunAsync()
        │
        ├── session.SendMessageAsync(userMessage)
        │           │
        │           ▼
        │    GitHub Copilot CLI (JSON-RPC)
        │           │
        │    ┌──────┴────────────────────────────────────┐
        │    │  Agent reasons using LLM                  │
        │    │  → may emit AssistantMessageDeltaEvents   │
        │    │  → may emit ToolCallEvents                │
        │    └──────┬────────────────────────────────────┘
        │           │
        │    For each AssistantMessageDeltaEvent:
        │        inputChannel.WriteResponseAsync(chunk)  ──▶  Terminal
        │
        │    For each ToolCallEvent:
        │        toolRegistry.InvokeAsync(name, args)
        │            │
        │            ▼
        │        Tool.ExecuteAsync() (FileSystem / Shell / Git / …)
        │            │
        │            ▼
        │        session.SendToolResultAsync(result)
        │
        ▼
Next user message (loop)
```

---

## Extension Points

### Adding a New Tool

1. Create a class in `GhcpAssistant.Tools` that implements `IAssistantTool`.
2. Decorate its `ExecuteAsync` override with `[AIFunction]` and a description attribute.
3. Register it at startup:

```csharp
toolRegistry.Register<MyCustomTool>();
```

No other changes are needed — the SDK advertises the function to the model automatically via the `SessionConfig.Functions` list.

### Adding a New Input Channel

1. Implement `IInputChannel` in `GhcpAssistant.Channels`.
2. Register it in DI and configure it in `appsettings.json`:

```json
{
  "Channel": "Http",
  "Http": { "Port": 5050 }
}
```

---

## Security Considerations

| Concern | Mitigation |
|---|---|
| Credential management | All OAuth tokens handled by the GitHub CLI; never stored in the .NET process |
| Shell command execution | `ShellTool` runs in a sandboxed child process with a configurable allow-list of commands |
| File system access | `FileSystemTool` is scoped to a configurable root directory; path traversal is validated |
| Tool invocation | Only tools explicitly registered in `ToolRegistry` can be called by the agent |
| Home Assistant access | `HomeAssistantTool` is only registered when a base URL is configured; long-lived access token should be stored in user secrets or environment variables |
| Secrets in prompts | System prompt and user messages are never logged at `Information` level or above |

---

## Prerequisites & Getting Started

### Prerequisites

| Requirement | Details |
|---|---|
| .NET 10 SDK | [https://dot.net](https://dot.net) |
| GitHub CLI (`gh`) | [https://cli.github.com](https://cli.github.com) — must be authenticated (`gh auth login`) |
| GitHub Copilot subscription | Individual, Business, or Enterprise |

### Build

```bash
dotnet restore
dotnet build
```

### Run

```bash
cd src/GhcpAssistant.Cli
dotnet run
```

### Test

```bash
dotnet test
```

---

## Future Roadmap

- [ ] Persistent conversation history (SQLite via EF Core)
- [ ] Plugin discovery from NuGet packages at runtime
- [ ] Multi-model routing (select model per tool category)
- [ ] Web UI front-end (Blazor) backed by `HttpInputChannel`
- [ ] Observability: OpenTelemetry traces for every tool call and session event
- [ ] GitHub Actions integration — run the assistant headless in CI pipelines
