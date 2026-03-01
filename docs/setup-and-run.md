# GHCP Assistant — Setup & Run Guide

This guide walks you through every step needed to get GHCP Assistant running on your machine.

---

## Prerequisites

| Requirement | Version / Details | Install |
|---|---|---|
| **.NET SDK** | 10 (required) | [https://dot.net/download](https://dot.net/download) |
| **GitHub CLI** (`gh`) | Latest stable | [https://cli.github.com](https://cli.github.com) |
| **GitHub Copilot subscription** | Individual, Business, or Enterprise | [https://github.com/features/copilot](https://github.com/features/copilot) |
| **Git** | Any recent version | [https://git-scm.com](https://git-scm.com) |

> **Note:** GHCP Assistant delegates all authentication to the GitHub CLI. Your GitHub token is **never** stored inside the application.

---

## 1. Authenticate with GitHub CLI

The GitHub Copilot SDK communicates with the Copilot runtime through a local JSON-RPC socket managed by `gh`. You must be signed in before running the assistant.

```bash
gh auth login
```

Follow the interactive prompts. When asked which scopes to request, the defaults are sufficient. Verify your session afterwards:

```bash
gh auth status
```

You should see your GitHub username and that Copilot is enabled for your account.

---

## 2. Clone the Repository

```bash
git clone https://github.com/fm0322/GHCP-Assistant.git
cd GHCP-Assistant
```

---

## 3. Configure the Application

All configuration lives in `src/GhcpAssistant.Cli/appsettings.json`. Open it and adjust the values to match your environment:

```json
{
  "Session": {
    "Model": "gpt-4o",
    "SystemPrompt": "You are GHCP Assistant, an autonomous AI agent. Use the available tools to help the user.",
    "MaxTurns": 50
  },
  "Shell": {
    "AllowedCommands": ["dotnet", "git", "ls", "cat", "echo", "pwd", "mkdir"]
  },
  "ConnectionStrings": {
    "AssistantDb": "Data Source=ghcpassistant.db"
  },
  "HomeAssistant": {
    "BaseUrl": "",
    "AccessToken": ""
  },
  "ToolDiscovery": {
    "AutoApproveTools": false
  }
}
```

### Configuration reference

| Key | Default | Description |
|---|---|---|
| `Session:Model` | `gpt-4o` | Copilot model to use for the session. |
| `Session:SystemPrompt` | *(see above)* | System prompt sent to the model at session start. |
| `Session:MaxTurns` | `50` | Maximum conversation turns before the session is reset. |
| `Shell:AllowedCommands` | `["dotnet","git",…]` | Explicit allow-list of shell commands the `ShellTool` may execute. |
| `ConnectionStrings:AssistantDb` | `Data Source=ghcpassistant.db` | SQLite connection string. Change the path to store the database elsewhere. |
| `HomeAssistant:BaseUrl` | *(empty)* | Base URL of your Home Assistant instance (e.g. `http://homeassistant.local:8123`). Leave empty to disable the Home Assistant tool. |
| `HomeAssistant:AccessToken` | *(empty)* | Long-lived Home Assistant access token. **Prefer user secrets** (see below) over committing a token. |
| `ToolDiscovery:AutoApproveTools` | `false` | When `true`, newly discovered tools are approved automatically without user confirmation. |

### Keeping secrets out of source control

For values such as `HomeAssistant:AccessToken`, use the [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) mechanism instead of editing `appsettings.json`:

```bash
cd src/GhcpAssistant.Cli
dotnet user-secrets init
dotnet user-secrets set "HomeAssistant:BaseUrl" "http://homeassistant.local:8123"
dotnet user-secrets set "HomeAssistant:AccessToken" "<your-long-lived-token>"
```

User secrets are stored outside the repository and are merged with `appsettings.json` at runtime.

---

## 4. Restore Dependencies & Build

```bash
# From the repository root
dotnet restore GhcpAssistant.sln
dotnet build GhcpAssistant.sln
```

Both commands should complete without errors.

---

## 5. Run the Assistant

```bash
dotnet run --project src/GhcpAssistant.Cli/
```

On first launch, the SQLite database (`ghcpassistant.db`) is created automatically in the current working directory. You will then see:

```
GHCP Assistant started. Type 'exit' to quit.
```

Type a message and press **Enter** to start a conversation. Type `exit` to quit.

---

## 6. Run the Tests

```bash
dotnet test GhcpAssistant.sln
```

The test suite covers the Core, SDK, Data, and Tools projects. All tests run without a live Copilot connection (the SDK layer is stubbed).

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `gh auth status` shows an error | Not signed in to GitHub CLI | Run `gh auth login` and complete the flow. |
| `dotnet: command not found` | .NET 10 SDK not on `PATH` | Install from [https://dot.net/download](https://dot.net/download) and open a new terminal. |
| Build fails with `NETSDK1045` | Wrong SDK version | Confirm `dotnet --version` prints `10.*`. Install the .NET 10 SDK. |
| `HomeAssistantTool` not listed | `HomeAssistant:BaseUrl` is empty | Set a non-empty `BaseUrl` in `appsettings.json` or via user secrets. |
| Database locked / EF Core error | Multiple instances running | Stop any other running instances of the assistant, then retry. |
| Tool requires approval | `AutoApproveTools` is `false` | Approve the tool when prompted, or set `ToolDiscovery:AutoApproveTools` to `true` (requires `Humaniser` role). |
