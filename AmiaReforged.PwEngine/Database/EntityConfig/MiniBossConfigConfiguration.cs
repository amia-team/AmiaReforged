using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class MiniBossConfigConfiguration : IEntityTypeConfiguration<MiniBossConfig>
{
    public void Configure(EntityTypeBuilder<MiniBossConfig> builder)
    {
        builder.ToTable("MiniBossConfigs");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SpawnProfileId)
            .HasColumnName("spawn_profile_id")
            .IsRequired();

        builder.Property(m => m.CreatureResRef)
            .HasColumnName("creature_resref")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.SpawnChancePercent)
            .HasColumnName("spawn_chance_percent")
            .HasDefaultValue(5);

        builder.HasIndex(m => m.SpawnProfileId)
            .IsUnique()
            .HasDatabaseName("IX_MiniBossConfigs_SpawnProfileId");

        builder.HasMany(m => m.Bonuses)
            .WithOne(b => b.MiniBossConfig)
            .HasForeignKey(b => b.MiniBossConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
