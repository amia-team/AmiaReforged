using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class SpawnBonusConfiguration : IEntityTypeConfiguration<SpawnBonus>
{
    public void Configure(EntityTypeBuilder<SpawnBonus> builder)
    {
        builder.ToTable("SpawnBonuses");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.SpawnProfileId)
            .HasColumnName("spawn_profile_id");

        builder.Property(b => b.MiniBossConfigId)
            .HasColumnName("mini_boss_config_id");

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(b => b.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(b => b.Value)
            .HasColumnName("value");

        builder.Property(b => b.DurationSeconds)
            .HasColumnName("duration_seconds")
            .HasDefaultValue(0);

        builder.Property(b => b.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(b => b.SpawnProfileId)
            .HasDatabaseName("IX_SpawnBonuses_SpawnProfileId");

        builder.HasIndex(b => b.MiniBossConfigId)
            .HasDatabaseName("IX_SpawnBonuses_MiniBossConfigId");
    }
}
