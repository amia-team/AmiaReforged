using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for global lore definitions.
/// </summary>
public sealed class PersistedLoreDefinitionConfiguration : IEntityTypeConfiguration<PersistedLoreDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedLoreDefinition> builder)
    {
        builder.ToTable("codex_lore_definitions");

        builder.HasKey(d => d.LoreId)
            .HasName("codex_lore_definitions_pkey");

        builder.Property(d => d.LoreId)
            .HasColumnName("lore_id")
            .HasMaxLength(100)
            .ValueGeneratedNever();

        builder.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(d => d.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Tier)
            .HasColumnName("tier")
            .IsRequired();

        builder.Property(d => d.Keywords)
            .HasColumnName("keywords")
            .HasMaxLength(1000);

        builder.Property(d => d.IsAlwaysAvailable)
            .HasColumnName("is_always_available")
            .HasDefaultValue(false);

        builder.Property(d => d.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Index for filtering by category
        builder.HasIndex(d => d.Category)
            .HasDatabaseName("codex_lore_definitions_category_idx");

        // Index for filtering by tier
        builder.HasIndex(d => d.Tier)
            .HasDatabaseName("codex_lore_definitions_tier_idx");

        // Index for loading always-available entries efficiently
        builder.HasIndex(d => d.IsAlwaysAvailable)
            .HasDatabaseName("codex_lore_definitions_always_available_idx");
    }
}
