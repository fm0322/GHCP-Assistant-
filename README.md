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

## Persistent Task List

GHCP Assistant includes a **persistent task list** backed by the same SQLite database. Tasks survive application restarts and provide a simple way to track work items, to-dos, and reminders.

Each task supports the following properties:

| Property | Type | Description |
|----------|------|-------------|
| **Title** | `string` | Short name for the task (required) |
| **Description** | `string?` | Optional longer description |
| **Priority** | `TaskPriority` | `Low`, `Medium` (default), or `High` |
| **DueDate** | `DateTime?` | Optional due date (UTC) |
| **IsCompleted** | `bool` | Whether the task has been finished |

The task list is managed through the `ITaskService` interface, which provides full CRUD operations:

- **Create** — add a new task with a title, optional description, priority, and due date
- **Read** — retrieve a single task by ID, or list all tasks with optional filtering by completion status
- **Update** — modify a task's title, description, priority, or due date
- **Complete** — mark a task as done
- **Delete** — remove a task permanently

Tasks are listed with incomplete items first, ordered by priority (high → low), then by due date and creation time. The service is registered via dependency injection and is available to any component in the application.

## Tool Discovery & Approval

The assistant can autonomously **discover tools** it needs to perform actions and configure them for itself. Discovered tools require **user authorisation** before they can be used, unless auto-approve is enabled.

### How it works

1. **Discovery** — The `IToolDiscoveryService` searches a catalog of available tool types by keyword, matching against tool names and descriptions.
2. **Approval** — When a tool is discovered, `IToolApprovalService` creates a `ToolConfiguration` entry in `Pending` status. The user must explicitly approve it before the tool can be registered and invoked.
3. **Registration** — Once approved, `ToolRegistry.TryRegisterDiscovered()` registers the tool. Unapproved tools are blocked.

### Auto-approve override

Set `ToolDiscovery:AutoApproveTools` to `true` in `appsettings.json` to skip the user-authorisation step and automatically approve all discovered tools:

```json
{
  "ToolDiscovery": {
    "AutoApproveTools": true
  }
}
```

By default this is `false`, requiring explicit approval for each new tool.

## Configuration & Role-Based Access

System-wide configuration is managed through the `IAssistantConfigService` interface. **Only users with the `Humaniser` role can edit the configuration.** Standard `User` role callers receive an `UnauthorizedAccessException`.

| Role | Permissions |
|------|-------------|
| **Humaniser** | Read and write configuration (e.g., toggle `AutoApproveTools`) |
| **User** | Read configuration only; can approve/reject individual tools |

Example — updating config (Humaniser only):

```csharp
await configService.UpdateConfigAsync(UserRole.Humaniser, new AssistantConfig { AutoApproveTools = true });
```

## Documentation

- [**Setup & Run Guide**](docs/setup-and-run.md) — step-by-step instructions for prerequisites, configuration, installation, and running the assistant locally.
- [Architecture Document](ARCHITECTURE.md) — system design, component breakdown, data flow, and extension guide.
- [Development Plan](DEVELOPMENT_PLAN.md) — phased implementation plan designed for a Copilot agent to execute one phase per session without running out of context.