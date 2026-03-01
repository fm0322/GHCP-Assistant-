using GhcpAssistant.Core.History;
using Microsoft.EntityFrameworkCore;

namespace GhcpAssistant.Data;

/// <summary>
/// SQLite-backed implementation of <see cref="IConversationHistoryService"/>.
/// </summary>
public sealed class SqliteConversationHistoryService : IConversationHistoryService
{
    private readonly AssistantDbContext _db;

    public SqliteConversationHistoryService(AssistantDbContext db)
    {
        _db = db;
    }

    public async Task<ConversationSession> CreateSessionAsync(string? title = null, CancellationToken ct = default)
    {
        var session = new ConversationSession
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public async Task<IReadOnlyList<ConversationSession>> ListSessionsAsync(CancellationToken ct = default)
    {
        return await _db.Sessions
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<ConversationMessage> AddMessageAsync(
        Guid sessionId, MessageRole role, string content, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([sessionId], ct)
            ?? throw new KeyNotFoundException($"Session '{sessionId}' not found.");

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };

        session.UpdatedAt = DateTime.UtcNow;
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([sessionId], ct);
        if (session is null)
            return false;

        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
