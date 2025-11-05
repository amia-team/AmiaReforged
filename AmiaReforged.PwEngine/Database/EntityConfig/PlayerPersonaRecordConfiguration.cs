using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for player personas.
/// </summary>
public sealed class PlayerPersonaRecordConfiguration : IEntityTypeConfiguration<PlayerPersonaRecord>
{
    public void Configure(EntityTypeBuilder<PlayerPersonaRecord> builder)
    {
        builder.ToTable("player_personas");

        builder.HasKey(p => p.CdKey)
            .HasName("player_personas_pkey");

        builder.Property(p => p.CdKey)
            .HasColumnName("cd_key")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.PersonaIdString)
            .HasColumnName("persona_id")
            .HasMaxLength(256);

        builder.Property(p => p.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.LastSeenUtc)
            .HasColumnName("last_seen_utc");

        builder.HasIndex(p => p.PersonaIdString)
            .HasDatabaseName("player_personas_persona_id_idx")
            .HasFilter("persona_id IS NOT NULL")
            .IsUnique();
    }
}
