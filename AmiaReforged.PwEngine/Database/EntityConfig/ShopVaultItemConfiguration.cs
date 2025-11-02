using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class ShopVaultItemConfiguration : IEntityTypeConfiguration<ShopVaultItem>
{
    public void Configure(EntityTypeBuilder<ShopVaultItem> builder)
    {
        builder.ToTable("npc_shop_vault_items");

        builder.Property(v => v.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(v => v.ItemData)
            .HasColumnName("item_data")
            .IsRequired();

        builder.Property(v => v.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255);

        builder.Property(v => v.ResRef)
            .HasColumnName("resref")
            .HasMaxLength(64);

        builder.Property(v => v.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(v => v.StoredAtUtc)
            .HasColumnName("stored_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(v => new { v.ShopId, v.StoredAtUtc })
            .HasDatabaseName("npc_shop_vault_items_shop_timestamp_idx");
    }
}
