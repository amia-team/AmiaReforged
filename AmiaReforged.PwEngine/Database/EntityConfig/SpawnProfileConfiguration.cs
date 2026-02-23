using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class SpawnProfileConfiguration : IEntityTypeConfiguration<SpawnProfile>
{
    public void Configure(EntityTypeBuilder<SpawnProfile> builder)
    {
        builder.ToTable("SpawnProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.AreaResRef)
            .HasColumnName("area_resref")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(false);

        builder.Property(p => p.CooldownSeconds)
            .HasColumnName("cooldown_seconds")
            .HasDefaultValue(900);

        builder.Property(p => p.DespawnSeconds)
            .HasColumnName("despawn_seconds")
            .HasDefaultValue(600);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(p => p.AreaResRef)
            .IsUnique()
            .HasDatabaseName("IX_SpawnProfiles_AreaResRef");

        builder.HasMany(p => p.SpawnGroups)
            .WithOne(g => g.SpawnProfile)
            .HasForeignKey(g => g.SpawnProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Bonuses)
            .WithOne(b => b.SpawnProfile)
            .HasForeignKey(b => b.SpawnProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.MiniBoss)
            .WithOne(m => m.SpawnProfile)
            .HasForeignKey<MiniBossConfig>(m => m.SpawnProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
