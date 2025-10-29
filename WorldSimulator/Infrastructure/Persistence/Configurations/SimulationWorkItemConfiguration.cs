using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using WorldSimulator.Domain.Aggregates;

namespace WorldSimulator.Infrastructure.Persistence.Configurations;

public class SimulationWorkItemConfiguration : IEntityTypeConfiguration<SimulationWorkItem>
{
    public void Configure(EntityTypeBuilder<SimulationWorkItem> builder)
    {
        builder.ToTable("SimulationWorkItems", "simulation");

        builder.HasKey(x => x.Id);

        // Configure WorkType with JSON value converter for the discriminated union
        builder.Property(x => x.WorkType)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<SimulationWorkType>(v, (JsonSerializerOptions?)null)!)
            .HasColumnName("WorkTypeJson")
            .HasColumnType("jsonb")
            .IsRequired();

        // Backing field for serialization (stored separately for queries)
        builder.Property("_serializedWorkType")
            .HasColumnName("WorkTypeString")
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.StartedAt);
        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        // Optimistic concurrency
        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate();

        // Indexes
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}

