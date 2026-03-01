using GhcpAssistant.Core.Tasks;
using GhcpAssistant.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhcpAssistant.Data.Tests;

public class SqliteTaskServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AssistantDbContext _db;
    private readonly SqliteTaskService _service;

    public SqliteTaskServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AssistantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AssistantDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SqliteTaskService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateTask_ReturnsNewTask()
    {
        var task = await _service.CreateTaskAsync("Buy groceries");

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Buy groceries", task.Title);
        Assert.False(task.IsCompleted);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.Null(task.Description);
        Assert.Null(task.DueDate);
        Assert.True(task.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateTask_WithAllFields_ReturnsPopulatedTask()
    {
        var dueDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var task = await _service.CreateTaskAsync(
            "Write report", "Quarterly financials", TaskPriority.High, dueDate);

        Assert.Equal("Write report", task.Title);
        Assert.Equal("Quarterly financials", task.Description);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(dueDate, task.DueDate);
    }

    [Fact]
    public async Task GetTask_ExistingId_ReturnsTask()
    {
        var created = await _service.CreateTaskAsync("Test task");

        var retrieved = await _service.GetTaskAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Test task", retrieved.Title);
    }

    [Fact]
    public async Task GetTask_NonExistentId_ReturnsNull()
    {
        var result = await _service.GetTaskAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListTasks_ReturnsAllTasks()
    {
        await _service.CreateTaskAsync("Task 1");
        await _service.CreateTaskAsync("Task 2");
        await _service.CreateTaskAsync("Task 3");

        var tasks = await _service.ListTasksAsync();

        Assert.Equal(3, tasks.Count);
    }

    [Fact]
    public async Task ListTasks_FilterByCompleted_ReturnsOnlyCompleted()
    {
        var task1 = await _service.CreateTaskAsync("Task 1");
        await _service.CreateTaskAsync("Task 2");
        await _service.CompleteTaskAsync(task1.Id);

        var completed = await _service.ListTasksAsync(isCompleted: true);
        var pending = await _service.ListTasksAsync(isCompleted: false);

        Assert.Single(completed);
        Assert.Equal("Task 1", completed[0].Title);
        Assert.Single(pending);
        Assert.Equal("Task 2", pending[0].Title);
    }

    [Fact]
    public async Task UpdateTask_UpdatesTitle()
    {
        var task = await _service.CreateTaskAsync("Old title");

        var updated = await _service.UpdateTaskAsync(task.Id, title: "New title");

        Assert.Equal("New title", updated.Title);
        Assert.True(updated.UpdatedAt >= task.UpdatedAt);
    }

    [Fact]
    public async Task UpdateTask_UpdatesDescription()
    {
        var task = await _service.CreateTaskAsync("Task", "Old desc");

        var updated = await _service.UpdateTaskAsync(task.Id, description: "New desc");

        Assert.Equal("New desc", updated.Description);
    }

    [Fact]
    public async Task UpdateTask_UpdatesPriority()
    {
        var task = await _service.CreateTaskAsync("Task");

        var updated = await _service.UpdateTaskAsync(task.Id, priority: TaskPriority.High);

        Assert.Equal(TaskPriority.High, updated.Priority);
    }

    [Fact]
    public async Task UpdateTask_NonExistentId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateTaskAsync(Guid.NewGuid(), title: "Test"));
    }

    [Fact]
    public async Task CompleteTask_MarksTaskAsCompleted()
    {
        var task = await _service.CreateTaskAsync("Task to complete");

        var completed = await _service.CompleteTaskAsync(task.Id);

        Assert.True(completed.IsCompleted);
        Assert.True(completed.UpdatedAt >= task.UpdatedAt);
    }

    [Fact]
    public async Task CompleteTask_NonExistentId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CompleteTaskAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteTask_ExistingTask_ReturnsTrueAndRemovesData()
    {
        var task = await _service.CreateTaskAsync("Task to delete");

        var result = await _service.DeleteTaskAsync(task.Id);

        Assert.True(result);
        Assert.Null(await _service.GetTaskAsync(task.Id));
    }

    [Fact]
    public async Task DeleteTask_NonExistentTask_ReturnsFalse()
    {
        var result = await _service.DeleteTaskAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ListTasks_NoFilter_ReturnsIncompleteBeforeCompleted()
    {
        var task1 = await _service.CreateTaskAsync("Completed task");
        await _service.CreateTaskAsync("Pending task");
        await _service.CompleteTaskAsync(task1.Id);

        var tasks = await _service.ListTasksAsync();

        Assert.Equal(2, tasks.Count);
        Assert.False(tasks[0].IsCompleted);
        Assert.True(tasks[1].IsCompleted);
    }
}
