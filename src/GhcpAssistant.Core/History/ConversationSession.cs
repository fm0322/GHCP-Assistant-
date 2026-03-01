namespace GhcpAssistant.Core.History;

/// <summary>Represents a persisted conversation session.</summary>
public sealed class ConversationSession
{
    /// <summary>Unique identifier for the session.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable title for the session.</summary>
    public string? Title { get; set; }

    /// <summary>When the session was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the session was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Messages belonging to this session.</summary>
    public List<ConversationMessage> Messages { get; set; } = [];
}
