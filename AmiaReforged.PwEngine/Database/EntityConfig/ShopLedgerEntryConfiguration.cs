using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class ShopLedgerEntryConfiguration : IEntityTypeConfiguration<ShopLedgerEntry>
{
    public void Configure(EntityTypeBuilder<ShopLedgerEntry> builder)
    {
        builder.ToTable("npc_shop_ledger_entries");

        builder.Property(e => e.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(e => e.BuyerName)
            .HasColumnName("buyer_name")
            .HasMaxLength(255);

        builder.Property(e => e.BuyerPersona)
            .HasColumnName("buyer_persona")
            .HasMaxLength(128);

        builder.Property(e => e.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasColumnName("unit_price")
            .IsRequired();

        builder.Property(e => e.TotalPrice)
            .HasColumnName("total_price")
            .IsRequired();

        builder.Property(e => e.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ResRef)
            .HasColumnName("resref")
            .HasMaxLength(64);

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        builder.HasIndex(e => new { e.ShopId, e.OccurredAtUtc })
            .HasDatabaseName("npc_shop_ledger_entries_shop_timestamp_idx");
    }
}
