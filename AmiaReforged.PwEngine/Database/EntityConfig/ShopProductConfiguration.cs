using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class ShopProductConfiguration : IEntityTypeConfiguration<ShopProductRecord>
{
    public void Configure(EntityTypeBuilder<ShopProductRecord> builder)
    {
        builder.ToTable("npc_shop_products");

        builder.Property(p => p.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(p => p.ResRef)
            .HasColumnName("resref")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(p => p.CurrentStock)
            .HasColumnName("current_stock")
            .IsRequired();

        builder.Property(p => p.MaxStock)
            .HasColumnName("max_stock")
            .IsRequired();

        builder.Property(p => p.RestockAmount)
            .HasColumnName("restock_amount")
            .IsRequired();

        builder.Property(p => p.IsPlayerManaged)
            .HasColumnName("is_player_managed")
            .IsRequired();

        builder.Property(p => p.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(p => p.LocalVariablesJson)
            .HasColumnName("locals_json")
            .HasColumnType("jsonb");

        builder.Property(p => p.AppearanceJson)
            .HasColumnName("appearance_json")
            .HasColumnType("jsonb");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(p => new { p.ShopId, p.ResRef })
            .IsUnique()
            .HasDatabaseName("npc_shop_products_shop_resref_idx");
    }
}
