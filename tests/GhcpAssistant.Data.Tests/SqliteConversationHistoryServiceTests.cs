using GhcpAssistant.Core.History;
using GhcpAssistant.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhcpAssistant.Data.Tests;

public class SqliteConversationHistoryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AssistantDbContext _db;
    private readonly SqliteConversationHistoryService _service;

    public SqliteConversationHistoryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AssistantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AssistantDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SqliteConversationHistoryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateSession_ReturnsNewSession()
    {
        var session = await _service.CreateSessionAsync("Test Session");

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal("Test Session", session.Title);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateSession_WithoutTitle_ReturnsSessionWithNullTitle()
    {
        var session = await _service.CreateSessionAsync();

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Null(session.Title);
    }

    [Fact]
    public async Task GetSession_ExistingId_ReturnsSession()
    {
        var created = await _service.CreateSessionAsync("My Session");

        var retrieved = await _service.GetSessionAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("My Session", retrieved.Title);
    }

    [Fact]
    public async Task GetSession_NonExistentId_ReturnsNull()
    {
        var result = await _service.GetSessionAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListSessions_ReturnsSessionsOrderedByUpdatedAtDescending()
    {
        var session1 = await _service.CreateSessionAsync("First");
        await Task.Delay(10); // ensure different timestamps
        var session2 = await _service.CreateSessionAsync("Second");

        var sessions = await _service.ListSessionsAsync();

        Assert.Equal(2, sessions.Count);
        Assert.Equal("Second", sessions[0].Title);
        Assert.Equal("First", sessions[1].Title);
    }

    [Fact]
    public async Task AddMessage_AddsMessageToSession()
    {
        var session = await _service.CreateSessionAsync("Test");

        var message = await _service.AddMessageAsync(session.Id, MessageRole.User, "Hello!");

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(session.Id, message.SessionId);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("Hello!", message.Content);
    }

    [Fact]
    public async Task AddMessage_NonExistentSession_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddMessageAsync(Guid.NewGuid(), MessageRole.User, "Hello"));
    }

    [Fact]
    public async Task GetMessages_ReturnsMessagesInChronologicalOrder()
    {
        var session = await _service.CreateSessionAsync("Test");
        await _service.AddMessageAsync(session.Id, MessageRole.User, "First");
        await _service.AddMessageAsync(session.Id, MessageRole.Assistant, "Second");
        await _service.AddMessageAsync(session.Id, MessageRole.User, "Third");

        var messages = await _service.GetMessagesAsync(session.Id);

        Assert.Equal(3, messages.Count);
        Assert.Equal("First", messages[0].Content);
        Assert.Equal(MessageRole.User, messages[0].Role);
        Assert.Equal("Second", messages[1].Content);
        Assert.Equal(MessageRole.Assistant, messages[1].Role);
        Assert.Equal("Third", messages[2].Content);
    }

    [Fact]
    public async Task GetSession_IncludesMessagesInOrder()
    {
        var session = await _service.CreateSessionAsync("Test");
        await _service.AddMessageAsync(session.Id, MessageRole.User, "Q");
        await _service.AddMessageAsync(session.Id, MessageRole.Assistant, "A");

        var retrieved = await _service.GetSessionAsync(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Messages.Count);
        Assert.Equal("Q", retrieved.Messages[0].Content);
        Assert.Equal("A", retrieved.Messages[1].Content);
    }

    [Fact]
    public async Task DeleteSession_ExistingSession_ReturnsTrueAndRemovesData()
    {
        var session = await _service.CreateSessionAsync("Test");
        await _service.AddMessageAsync(session.Id, MessageRole.User, "msg");

        var result = await _service.DeleteSessionAsync(session.Id);

        Assert.True(result);
        Assert.Null(await _service.GetSessionAsync(session.Id));
        Assert.Empty(await _service.GetMessagesAsync(session.Id));
    }

    [Fact]
    public async Task DeleteSession_NonExistentSession_ReturnsFalse()
    {
        var result = await _service.DeleteSessionAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task AddMessage_UpdatesSessionUpdatedAt()
    {
        var session = await _service.CreateSessionAsync("Test");
        var originalUpdatedAt = session.UpdatedAt;

        await Task.Delay(10);
        await _service.AddMessageAsync(session.Id, MessageRole.User, "Hello");

        var retrieved = await _service.GetSessionAsync(session.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.UpdatedAt >= originalUpdatedAt);
    }
}
