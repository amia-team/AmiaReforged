using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class SpawnConditionConfiguration : IEntityTypeConfiguration<SpawnCondition>
{
    public void Configure(EntityTypeBuilder<SpawnCondition> builder)
    {
        builder.ToTable("SpawnConditions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SpawnGroupId)
            .HasColumnName("spawn_group_id")
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(c => c.Operator)
            .HasColumnName("operator")
            .HasMaxLength(16);

        builder.Property(c => c.Value)
            .HasColumnName("value")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(c => c.SpawnGroupId)
            .HasDatabaseName("IX_SpawnConditions_SpawnGroupId");
    }
}
