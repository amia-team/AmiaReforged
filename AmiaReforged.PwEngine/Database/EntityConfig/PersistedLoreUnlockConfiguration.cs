using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for per-player lore unlock records.
/// </summary>
public sealed class PersistedLoreUnlockConfiguration : IEntityTypeConfiguration<PersistedLoreUnlock>
{
    public void Configure(EntityTypeBuilder<PersistedLoreUnlock> builder)
    {
        builder.ToTable("codex_lore_unlocks");

        // Composite primary key: one unlock per character per lore entry
        builder.HasKey(u => new { u.CharacterId, u.LoreId })
            .HasName("codex_lore_unlocks_pkey");

        builder.Property(u => u.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.Property(u => u.LoreId)
            .HasColumnName("lore_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.DateDiscovered)
            .HasColumnName("date_discovered")
            .IsRequired();

        builder.Property(u => u.DiscoveryLocation)
            .HasColumnName("discovery_location")
            .HasMaxLength(200);

        builder.Property(u => u.DiscoverySource)
            .HasColumnName("discovery_source")
            .HasMaxLength(200);

        // FK to persisted_characters — cascade delete when character is removed
        builder.HasOne<PersistedCharacter>()
            .WithMany()
            .HasForeignKey(u => u.CharacterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("codex_lore_unlocks_character_id_fkey");

        // FK to codex_lore_definitions — cascade delete when definition is removed
        builder.HasOne(u => u.LoreDefinition)
            .WithMany()
            .HasForeignKey(u => u.LoreId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("codex_lore_unlocks_lore_id_fkey");

        // Index for fast lookup by character
        builder.HasIndex(u => u.CharacterId)
            .HasDatabaseName("codex_lore_unlocks_character_id_idx");
    }
}
