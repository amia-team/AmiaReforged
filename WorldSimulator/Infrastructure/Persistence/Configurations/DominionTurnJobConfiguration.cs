using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorldSimulator.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity configuration for DominionTurnJob aggregate
    /// </summary>
    public class DominionTurnJobConfiguration : IEntityTypeConfiguration<DominionTurnJob>
    {
        public void Configure(EntityTypeBuilder<DominionTurnJob> builder)
        {
            builder.ToTable("dominion_turn_jobs", "simulation");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.GovernmentId)
                .IsRequired();

            builder.Property(e => e.GovernmentName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.TurnDate)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            builder.Property(e => e.Version)
                .IsRowVersion()
                .HasColumnName("xmin")
                .HasColumnType("xid");

            // Indices for querying
            builder.HasIndex(e => e.GovernmentId)
                .HasDatabaseName("idx_dominion_jobs_government");

            builder.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("idx_dominion_jobs_status_created");

            builder.HasIndex(e => e.TurnDate)
                .HasDatabaseName("idx_dominion_jobs_turn_date");
        }
    }
}

namespace WorldSimulator.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity configuration for SimulationWorkItem aggregate
    /// </summary>
    public class SimulationWorkItemConfiguration : IEntityTypeConfiguration<SimulationWorkItem>
    {
        public void Configure(EntityTypeBuilder<SimulationWorkItem> builder)
        {
            builder.ToTable("work_items", "simulation");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.WorkType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Payload)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.Error)
                .HasMaxLength(2000);

            builder.Property(e => e.Version)
                .IsRowVersion()
                .HasColumnName("xmin")
                .HasColumnType("xid");

            // Index for efficient polling
            builder.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("idx_work_items_status_created");
        }
    }
}
