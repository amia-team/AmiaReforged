using AmiaReforged.PwEngine.Database;

namespace WorldSimulator.Infrastructure.Persistence;

/// <summary>
/// DbContext for the WorldSimulator service.
/// Extends PwEngineContext for read access to shared WorldEngine data while managing its own simulation tables.
/// </summary>
public class SimulationDbContext : PwEngineContext
{
    public SimulationDbContext() : base() { }

    public SimulationDbContext(DbContextOptions<SimulationDbContext> options) : base()
    {
        // Inherit connection from base or use provided options
    }

    /// <summary>
    /// Work items owned by the simulation service
    /// </summary>
    public DbSet<SimulationWorkItem> WorkItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure simulation-specific tables in the 'simulation' schema
        modelBuilder.Entity<SimulationWorkItem>(entity =>
        {
            entity.ToTable("work_items", "simulation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Error).HasMaxLength(2000);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasColumnName("xmin")
                .HasColumnType("xid");

            // Index for efficient polling
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });
    }
}

