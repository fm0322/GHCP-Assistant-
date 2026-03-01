namespace GhcpAssistant.Core.Tasks;

/// <summary>Abstraction for persisting and retrieving tasks.</summary>
public interface ITaskService
{
    /// <summary>Create a new task and return it.</summary>
    Task<TaskItem> CreateTaskAsync(string title, string? description = null, TaskPriority priority = TaskPriority.Medium, DateTime? dueDate = null, CancellationToken ct = default);

    /// <summary>Retrieve a task by id.</summary>
    Task<TaskItem?> GetTaskAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>List all tasks, optionally filtering by completion status.</summary>
    Task<IReadOnlyList<TaskItem>> ListTasksAsync(bool? isCompleted = null, CancellationToken ct = default);

    /// <summary>Update an existing task's properties.</summary>
    Task<TaskItem> UpdateTaskAsync(Guid taskId, string? title = null, string? description = null, TaskPriority? priority = null, DateTime? dueDate = null, CancellationToken ct = default);

    /// <summary>Mark a task as completed.</summary>
    Task<TaskItem> CompleteTaskAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Delete a task by id.</summary>
    Task<bool> DeleteTaskAsync(Guid taskId, CancellationToken ct = default);
}
