using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedRegionDefinitionConfiguration : IEntityTypeConfiguration<PersistedRegionDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedRegionDefinition> builder)
    {
        builder.ToTable("RegionDefinitions");

        builder.HasKey(e => e.Tag);

        builder.Property(e => e.Tag)
            .HasColumnName("tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256);

        builder.Property(e => e.DefaultChaosJson)
            .HasColumnName("default_chaos")
            .HasColumnType("jsonb");

        builder.Property(e => e.AreasJson)
            .HasColumnName("areas")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_RegionDefinitions_Name");
    }
}
