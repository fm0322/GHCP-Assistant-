namespace GhcpAssistant.Core.History;

/// <summary>Abstraction for persisting and retrieving conversation history.</summary>
public interface IConversationHistoryService
{
    /// <summary>Create a new conversation session and return its id.</summary>
    Task<ConversationSession> CreateSessionAsync(string? title = null, CancellationToken ct = default);

    /// <summary>Retrieve a session by id, including its messages.</summary>
    Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>List all sessions ordered by most recently updated first.</summary>
    Task<IReadOnlyList<ConversationSession>> ListSessionsAsync(CancellationToken ct = default);

    /// <summary>Add a message to an existing session.</summary>
    Task<ConversationMessage> AddMessageAsync(Guid sessionId, MessageRole role, string content, CancellationToken ct = default);

    /// <summary>Get all messages for a session in chronological order.</summary>
    Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Delete a session and all its messages.</summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
}
