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
