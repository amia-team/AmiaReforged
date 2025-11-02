using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class NpcShopLedgerEntryConfiguration : IEntityTypeConfiguration<NpcShopLedgerEntry>
{
    public void Configure(EntityTypeBuilder<NpcShopLedgerEntry> builder)
    {
        builder.ToTable("NpcShopLedgerEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductResRef)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasMaxLength(255);

        builder.Property(x => x.BuyerName)
            .HasMaxLength(255);

        builder.Property(x => x.Notes)
            .HasColumnType("text");

        builder.HasIndex(x => new { x.ShopId, x.SoldAt });
    }
}
