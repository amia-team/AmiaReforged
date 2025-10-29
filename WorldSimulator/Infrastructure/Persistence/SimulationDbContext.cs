using Microsoft.EntityFrameworkCore;
using WorldSimulator.Domain.Aggregates;
using WorldSimulator.Infrastructure.Persistence.Configurations;

namespace WorldSimulator.Infrastructure.Persistence;

/// <summary>
/// DbContext for the WorldSimulator service.
/// Completely independent from game server - manages only simulation-specific tables.
/// </summary>
public class SimulationDbContext : DbContext
{
    public SimulationDbContext(DbContextOptions<SimulationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Work items owned by the simulation service
    /// </summary>
    public DbSet<SimulationWorkItem> WorkItems { get; set; } = null!;

    /// <summary>
    /// Dominion turn jobs for government processing
    /// </summary>
    public DbSet<DominionTurnJob> DominionTurnJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from separate classes
        modelBuilder.ApplyConfiguration(new SimulationWorkItemConfiguration());
        modelBuilder.ApplyConfiguration(new DominionTurnJobConfiguration());

        // Set default schema for all simulation tables
        modelBuilder.HasDefaultSchema("simulation");
    }
}

