using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.AdminPanel.Data;

/// <summary>
/// Database context for the Admin Panel, handling Identity and monitoring configuration.
/// </summary>
public class AdminPanelDbContext : IdentityDbContext<AdminUser>
{
    public AdminPanelDbContext(DbContextOptions<AdminPanelDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Containers selected for monitoring by the admin.
    /// </summary>
    public DbSet<MonitoredContainer> MonitoredContainers => Set<MonitoredContainer>();

    /// <summary>
    /// Log entries for crash/restart events.
    /// </summary>
    public DbSet<ContainerEvent> ContainerEvents => Set<ContainerEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MonitoredContainer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContainerId).IsUnique();
            entity.Property(e => e.ContainerId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.WatchPatterns).HasMaxLength(2048);
        });

        builder.Entity<ContainerEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContainerId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.ContainerId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(4096);
        });
    }
}

/// <summary>
/// Custom Identity user for the Admin Panel.
/// </summary>
public class AdminUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Represents a Docker container that has been selected for monitoring.
/// </summary>
public class MonitoredContainer
{
    public int Id { get; set; }

    /// <summary>
    /// Docker container ID (short or full form).
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Friendly display name for the container.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this container is actively being monitored.
    /// </summary>
    public bool IsMonitoringEnabled { get; set; } = true;

    /// <summary>
    /// Whether to automatically restart on crash detection.
    /// </summary>
    public bool AutoRestartEnabled { get; set; } = false;

    /// <summary>
    /// Pipe-delimited list of regex patterns to watch for (e.g., "segfault|SIGSEGV|core dumped").
    /// </summary>
    public string WatchPatterns { get; set; } = "segfault|SIGSEGV|core dumped";

    /// <summary>
    /// When monitoring was enabled for this container.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time settings were modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Records events like crashes, restarts, pattern matches for auditing.
/// </summary>
public class ContainerEvent
{
    public int Id { get; set; }
    public string ContainerId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "PatternMatch", "AutoRestart", "ManualRestart", etc.
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
