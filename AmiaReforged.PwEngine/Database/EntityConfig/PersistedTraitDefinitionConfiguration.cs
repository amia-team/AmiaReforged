using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for global trait definitions.
/// </summary>
public sealed class PersistedTraitDefinitionConfiguration : IEntityTypeConfiguration<PersistedTraitDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedTraitDefinition> builder)
    {
        builder.ToTable("trait_definitions");

        builder.HasKey(d => d.Tag)
            .HasName("trait_definitions_pkey");

        builder.Property(d => d.Tag)
            .HasColumnName("tag")
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(d => d.PointCost)
            .HasColumnName("point_cost")
            .IsRequired();

        builder.Property(d => d.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DeathBehavior)
            .HasColumnName("death_behavior")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.RequiresUnlock)
            .HasColumnName("requires_unlock")
            .HasDefaultValue(false);

        builder.Property(d => d.DmOnly)
            .HasColumnName("dm_only")
            .HasDefaultValue(false);

        // === JSON collection columns ===

        builder.Property(d => d.EffectsJson)
            .HasColumnName("effects")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.AllowedRacesJson)
            .HasColumnName("allowed_races")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.AllowedClassesJson)
            .HasColumnName("allowed_classes")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.ForbiddenRacesJson)
            .HasColumnName("forbidden_races")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.ForbiddenClassesJson)
            .HasColumnName("forbidden_classes")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.ConflictingTraitsJson)
            .HasColumnName("conflicting_traits")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.PrerequisiteTraitsJson)
            .HasColumnName("prerequisite_traits")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // === Indexes ===

        builder.HasIndex(d => d.Category)
            .HasDatabaseName("trait_definitions_category_idx");

        builder.HasIndex(d => d.DeathBehavior)
            .HasDatabaseName("trait_definitions_death_behavior_idx");

        builder.HasIndex(d => d.DmOnly)
            .HasDatabaseName("trait_definitions_dm_only_idx");

        builder.HasIndex(d => d.RequiresUnlock)
            .HasDatabaseName("trait_definitions_requires_unlock_idx");
    }
}
