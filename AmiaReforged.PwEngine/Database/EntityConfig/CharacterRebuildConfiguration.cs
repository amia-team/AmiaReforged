using AmiaReforged.PwEngine.Database.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for character rebuild records.
/// </summary>
public sealed class CharacterRebuildConfiguration : IEntityTypeConfiguration<CharacterRebuild>
{
    public void Configure(EntityTypeBuilder<CharacterRebuild> builder)
    {
        builder.ToTable("character_rebuilds");

        builder.HasKey(r => r.Id)
            .HasName("character_rebuilds_pkey");

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.PlayerCdKey)
            .HasColumnName("player_cd_key")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.Property(r => r.RequestedUtc)
            .HasColumnName("requested_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.CompletedUtc)
            .HasColumnName("completed_utc");

        // Navigation properties are ignored - no foreign key constraints needed
        builder.Ignore(r => r.Player);
        builder.Ignore(r => r.Character);

        builder.HasIndex(r => r.PlayerCdKey)
            .HasDatabaseName("character_rebuilds_player_cd_key_idx");

        builder.HasIndex(r => r.CharacterId)
            .HasDatabaseName("character_rebuilds_character_id_idx");
    }
}

