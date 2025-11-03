using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class PlayerStallMemberConfiguration : IEntityTypeConfiguration<PlayerStallMember>
{
    public void Configure(EntityTypeBuilder<PlayerStallMember> builder)
    {
        builder.ToTable("player_stall_members");

        builder.Property(m => m.PersonaId)
            .HasColumnName("persona_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.CanManageInventory)
            .HasColumnName("can_manage_inventory")
            .HasDefaultValue(false);

        builder.Property(m => m.CanConfigureSettings)
            .HasColumnName("can_configure_settings")
            .HasDefaultValue(false);

        builder.Property(m => m.CanCollectEarnings)
            .HasColumnName("can_collect_earnings")
            .HasDefaultValue(false);

        builder.Property(m => m.AddedByPersonaId)
            .HasColumnName("added_by_persona_id")
            .HasMaxLength(256);

        builder.Property(m => m.AddedUtc)
            .HasColumnName("added_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(m => m.RevokedUtc)
            .HasColumnName("revoked_utc");

        builder.HasIndex(m => new { m.StallId, m.PersonaId })
            .IsUnique()
            .HasDatabaseName("player_stall_members_unique_idx");
    }
}
