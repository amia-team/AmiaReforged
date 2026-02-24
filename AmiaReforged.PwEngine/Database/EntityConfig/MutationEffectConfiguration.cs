using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class MutationEffectConfiguration : IEntityTypeConfiguration<MutationEffect>
{
    public void Configure(EntityTypeBuilder<MutationEffect> builder)
    {
        builder.ToTable("MutationEffects");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MutationTemplateId)
            .HasColumnName("mutation_template_id")
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(e => e.Value)
            .HasColumnName("value");

        builder.Property(e => e.AbilityType)
            .HasColumnName("ability_type")
            .HasConversion<int?>();

        builder.Property(e => e.DamageType)
            .HasColumnName("damage_type")
            .HasConversion<int?>();

        builder.Property(e => e.DurationSeconds)
            .HasColumnName("duration_seconds")
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(e => e.MutationTemplateId)
            .HasDatabaseName("IX_MutationEffects_MutationTemplateId");
    }
}
