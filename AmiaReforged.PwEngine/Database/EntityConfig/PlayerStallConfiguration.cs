using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class PlayerStallConfiguration : IEntityTypeConfiguration<PlayerStall>
{
    public void Configure(EntityTypeBuilder<PlayerStall> builder)
    {
        builder.ToTable("player_stalls");

        builder.Property(s => s.Tag)
            .HasColumnName("stall_tag")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.AreaResRef)
            .HasColumnName("area_resref")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.SettlementTag)
            .HasColumnName("settlement_tag")
            .HasMaxLength(128);

        builder.Property(s => s.OwnerCharacterId)
            .HasColumnName("owner_character_id");

        builder.Property(s => s.OwnerPersonaId)
            .HasColumnName("owner_persona_id")
            .HasMaxLength(256);

        builder.Property(s => s.OwnerPlayerPersonaId)
            .HasColumnName("owner_player_persona_id")
            .HasMaxLength(256);

        builder.Property(s => s.OwnerDisplayName)
            .HasColumnName("owner_display_name")
            .HasMaxLength(255);

        builder.Property(s => s.CustomDisplayName)
            .HasColumnName("custom_display_name")
            .HasMaxLength(255);

        builder.Property(s => s.CoinHouseAccountId)
            .HasColumnName("coinhouse_account_id");

        builder.Property(s => s.HoldEarningsInStall)
            .HasColumnName("hold_earnings_in_stall")
            .HasDefaultValue(false);

        builder.Property(s => s.EscrowBalance)
            .HasColumnName("escrow_balance");

        builder.Property(s => s.LifetimeGrossSales)
            .HasColumnName("lifetime_gross_sales");

        builder.Property(s => s.LifetimeNetEarnings)
            .HasColumnName("lifetime_net_earnings");

        builder.Property(s => s.DailyRent)
            .HasColumnName("daily_rent")
            .HasDefaultValue(10_000);

        builder.Property(s => s.LeaseStartUtc)
            .HasColumnName("lease_start_utc");

        builder.Property(s => s.NextRentDueUtc)
            .HasColumnName("next_rent_due_utc");

        builder.Property(s => s.LastRentPaidUtc)
            .HasColumnName("last_rent_paid_utc");

        builder.Property(s => s.SuspendedUtc)
            .HasColumnName("suspended_utc");

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.DeactivatedUtc)
            .HasColumnName("deactivated_utc");

        builder.HasIndex(s => s.Tag)
            .IsUnique()
            .HasDatabaseName("player_stalls_tag_idx");

        builder.HasIndex(s => new { s.OwnerPersonaId, s.AreaResRef })
            .IsUnique()
            .HasFilter("owner_persona_id IS NOT NULL")
            .HasDatabaseName("player_stalls_owner_area_idx");

        builder.HasIndex(s => new { s.OwnerPlayerPersonaId, s.AreaResRef })
            .IsUnique()
            .HasFilter("owner_player_persona_id IS NOT NULL")
            .HasDatabaseName("player_stalls_owner_player_area_idx");

        builder.HasMany(s => s.Inventory)
            .WithOne(i => i.Stall)
            .HasForeignKey(i => i.StallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Members)
            .WithOne(m => m.Stall)
            .HasForeignKey(m => m.StallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.LedgerEntries)
            .WithOne(l => l.Stall)
            .HasForeignKey(l => l.StallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Transactions)
            .WithOne(t => t.Stall)
            .HasForeignKey(t => t.StallId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
