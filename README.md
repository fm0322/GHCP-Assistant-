# GHCP Assistant

A clawbot-inspired, autonomous AI assistant built in **C# / .NET 10** using the [GitHub Copilot CLI SDK](https://github.com/github/copilot-sdk).

## Quick Start

```bash
# Build the solution
dotnet build GhcpAssistant.sln

# Run the CLI application
dotnet run --project src/GhcpAssistant.Cli/

# Run all tests
dotnet test GhcpAssistant.sln
```

## Available Tools

| Tool | Description |
|------|-------------|
| **file_system** | Read, write, or list files and directories within the workspace. Includes path-traversal protection. |
| **shell** | Execute shell commands from a configurable allow-list with timeout support. |
| **git** | Run git operations: status, diff, log, commit. |
| **web_search** | Search the web and return a summary of results via the DuckDuckGo Instant Answer API. |
| **github** | Query the GitHub REST API: get repo info, list issues, list PRs. |
| **home_assistant** | Interact with Home Assistant: get entity states, call services (e.g., turn on/off lights, locks, switches). Configure `HomeAssistant:BaseUrl` and `HomeAssistant:AccessToken` in `appsettings.json` to enable. |

## Persistent Conversation History

Conversation history is automatically persisted to a local **SQLite** database via **EF Core**. This enables the assistant to retain multi-turn context across restarts and allows users to review past sessions.

The database is created automatically on first run. Configure the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AssistantDb": "Data Source=ghcpassistant.db"
  }
}
```

## Documentation

- [Architecture Document](ARCHITECTURE.md) — system design, component breakdown, data flow, and extension guide.
- [Development Plan](DEVELOPMENT_PLAN.md) — phased implementation plan designed for a Copilot agent to execute one phase per session without running out of context.