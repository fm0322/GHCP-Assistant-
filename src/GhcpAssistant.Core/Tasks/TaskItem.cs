namespace GhcpAssistant.Core.Tasks;

/// <summary>Priority level for a task.</summary>
public enum TaskPriority
{
    /// <summary>Low priority.</summary>
    Low,

    /// <summary>Medium priority.</summary>
    Medium,

    /// <summary>High priority.</summary>
    High
}

/// <summary>Represents a persistent task in the task list.</summary>
public sealed class TaskItem
{
    /// <summary>Unique identifier for the task.</summary>
    public Guid Id { get; set; }

    /// <summary>Short title of the task.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the task has been completed.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Priority level of the task.</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Optional due date (UTC).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>When the task was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the task was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}
