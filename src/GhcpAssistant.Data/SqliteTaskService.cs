using GhcpAssistant.Core.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GhcpAssistant.Data;

/// <summary>
/// SQLite-backed implementation of <see cref="ITaskService"/>.
/// </summary>
public sealed class SqliteTaskService : ITaskService
{
    private readonly AssistantDbContext _db;

    public SqliteTaskService(AssistantDbContext db)
    {
        _db = db;
    }

    public async Task<TaskItem> CreateTaskAsync(
        string title, string? description = null, TaskPriority priority = TaskPriority.Medium,
        DateTime? dueDate = null, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            IsCompleted = false,
            Priority = priority,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem?> GetTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
    }

    public async Task<IReadOnlyList<TaskItem>> ListTasksAsync(bool? isCompleted = null, CancellationToken ct = default)
    {
        var query = _db.Tasks.AsQueryable();

        if (isCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == isCompleted.Value);

        return await query.OrderBy(t => t.IsCompleted)
                          .ThenByDescending(t => t.Priority)
                          .ThenBy(t => t.DueDate)
                          .ThenBy(t => t.CreatedAt)
                          .ToListAsync(ct);
    }

    public async Task<TaskItem> UpdateTaskAsync(
        Guid taskId, string? title = null, string? description = null,
        TaskPriority? priority = null, DateTime? dueDate = null, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FindAsync([taskId], ct)
            ?? throw new KeyNotFoundException($"Task '{taskId}' not found.");

        if (title is not null)
            task.Title = title;
        if (description is not null)
            task.Description = description;
        if (priority.HasValue)
            task.Priority = priority.Value;
        if (dueDate.HasValue)
            task.DueDate = dueDate.Value;

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem> CompleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FindAsync([taskId], ct)
            ?? throw new KeyNotFoundException($"Task '{taskId}' not found.");

        task.IsCompleted = true;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FindAsync([taskId], ct);
        if (task is null)
            return false;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
