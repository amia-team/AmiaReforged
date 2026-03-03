using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class BossConfigConfiguration : IEntityTypeConfiguration<BossConfig>
{
    public void Configure(EntityTypeBuilder<BossConfig> builder)
    {
        builder.ToTable("BossConfigs");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.SpawnProfileId)
            .HasColumnName("spawn_profile_id")
            .IsRequired();

        builder.Property(b => b.CreatureResRef)
            .HasColumnName("creature_resref")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(b => b.Weight)
            .HasColumnName("weight")
            .HasDefaultValue(1);

        builder.Property(b => b.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(b => b.SpawnProfileId)
            .HasDatabaseName("IX_BossConfigs_SpawnProfileId");

        builder.HasMany(b => b.Conditions)
            .WithOne(c => c.BossConfig)
            .HasForeignKey(c => c.BossConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Bonuses)
            .WithOne(bonus => bonus.BossConfig)
            .HasForeignKey(bonus => bonus.BossConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
