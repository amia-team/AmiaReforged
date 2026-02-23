using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class SpawnGroupConfiguration : IEntityTypeConfiguration<SpawnGroup>
{
    public void Configure(EntityTypeBuilder<SpawnGroup> builder)
    {
        builder.ToTable("SpawnGroups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.SpawnProfileId)
            .HasColumnName("spawn_profile_id")
            .IsRequired();

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(g => g.Weight)
            .HasColumnName("weight")
            .HasDefaultValue(1);

        builder.HasIndex(g => g.SpawnProfileId)
            .HasDatabaseName("IX_SpawnGroups_SpawnProfileId");

        builder.HasMany(g => g.Conditions)
            .WithOne(c => c.SpawnGroup)
            .HasForeignKey(c => c.SpawnGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Entries)
            .WithOne(e => e.SpawnGroup)
            .HasForeignKey(e => e.SpawnGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
