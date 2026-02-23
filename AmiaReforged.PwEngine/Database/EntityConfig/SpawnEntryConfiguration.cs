using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class SpawnEntryConfiguration : IEntityTypeConfiguration<SpawnEntry>
{
    public void Configure(EntityTypeBuilder<SpawnEntry> builder)
    {
        builder.ToTable("SpawnEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SpawnGroupId)
            .HasColumnName("spawn_group_id")
            .IsRequired();

        builder.Property(e => e.CreatureResRef)
            .HasColumnName("creature_resref")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.RelativeWeight)
            .HasColumnName("relative_weight")
            .HasDefaultValue(1);

        builder.Property(e => e.MinCount)
            .HasColumnName("min_count")
            .HasDefaultValue(1);

        builder.Property(e => e.MaxCount)
            .HasColumnName("max_count")
            .HasDefaultValue(4);

        builder.HasIndex(e => e.SpawnGroupId)
            .HasDatabaseName("IX_SpawnEntries_SpawnGroupId");
    }
}
