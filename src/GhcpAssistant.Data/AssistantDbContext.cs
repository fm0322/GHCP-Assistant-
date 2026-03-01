using GhcpAssistant.Core.History;
using GhcpAssistant.Core.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GhcpAssistant.Data;

/// <summary>EF Core database context for conversation history and task persistence.</summary>
public sealed class AssistantDbContext : DbContext
{
    public DbSet<ConversationSession> Sessions => Set<ConversationSession>();
    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public AssistantDbContext(DbContextOptions<AssistantDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.HasMany(e => e.Messages)
                  .WithOne(m => m.Session)
                  .HasForeignKey(m => m.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(16);
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(4096);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(16);
        });
    }
}
