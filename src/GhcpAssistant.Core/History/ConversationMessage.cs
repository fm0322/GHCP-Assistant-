namespace GhcpAssistant.Core.History;

/// <summary>The role of a conversation participant.</summary>
public enum MessageRole
{
    /// <summary>A message from the user.</summary>
    User,

    /// <summary>A response from the assistant.</summary>
    Assistant,

    /// <summary>A system prompt.</summary>
    System
}

/// <summary>A single message within a conversation session.</summary>
public sealed class ConversationMessage
{
    /// <summary>Unique identifier for the message.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the owning session.</summary>
    public Guid SessionId { get; set; }

    /// <summary>Who sent this message.</summary>
    public MessageRole Role { get; set; }

    /// <summary>The text content of the message.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>When the message was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation property to the owning session.</summary>
    public ConversationSession? Session { get; set; }
}
