using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class StallProductConfiguration : IEntityTypeConfiguration<StallProduct>
{
    public void Configure(EntityTypeBuilder<StallProduct> builder)
    {
        builder.ToTable("player_stall_products");

        builder.Property(p => p.StallId)
            .HasColumnName("stall_id")
            .IsRequired();

        builder.Property(p => p.ResRef)
            .HasColumnName("resref")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.Property(p => p.Price)
            .HasColumnName("price_per_unit")
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(p => p.BaseItemType)
            .HasColumnName("base_item_type");

        builder.Property(p => p.ItemData)
            .HasColumnName("item_data")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(p => p.ConsignedByPersonaId)
            .HasColumnName("consigned_by_persona_id")
            .HasMaxLength(256);

        builder.Property(p => p.ConsignedByDisplayName)
            .HasColumnName("consigned_by_display_name")
            .HasMaxLength(255);

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasMaxLength(512);

        builder.Property(p => p.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.ListedUtc)
            .HasColumnName("listed_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.SoldOutUtc)
            .HasColumnName("sold_out_utc");

        builder.HasIndex(p => new { p.StallId, p.IsActive })
            .HasDatabaseName("player_stall_products_active_idx");
    }
}
